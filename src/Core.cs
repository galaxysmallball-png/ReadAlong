using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace ReadAlong
{
    public class Word
    {
        public string Text = "", Meaning = "", Sentence = "", Translation = "", Application = "", Added = "";
        public bool Mastered;
    }
    public class Preferences
    {
        public string Provider = "MyMemory", Region = "", Endpoint = "http://localhost:5000", ProtectedKey = "";
        public bool Pinned = true;
        public int Interval = 1, CharactersToday, PollingVersion;
        public string UsageDate = "";
        public double ReadingScale = 1;
    }
    public class AppData
    {
        public int Version = 1;
        public Preferences Settings = new Preferences();
        public List<Word> Words = new List<Word>();
    }
    public class Store
    {
        public readonly string Folder;
        public string Warning = "";
        public string PathName { get { return Path.Combine(Folder, "learning.json"); } }
        public static readonly object Sync = new object();
        public Store(string folder) { Folder = folder; }
        public static string Json(object value) { return new JavaScriptSerializer { MaxJsonLength = 4000000 }.Serialize(value); }
        public static T Parse<T>(string text) { return new JavaScriptSerializer { MaxJsonLength = 4000000 }.Deserialize<T>(text); }
        public static void Validate(AppData data)
        {
            if (data == null || data.Version != 1 || data.Settings == null || data.Words == null || data.Words.Any(w => w == null || String.IsNullOrWhiteSpace(w.Text))) throw new InvalidDataException("学习数据格式不正确。");
            if (data.Words.Select(w => w.Text.ToLowerInvariant()).Distinct().Count() != data.Words.Count) throw new InvalidDataException("生词本包含重复单词。");
        }
        public AppData Load()
        {
            if (!File.Exists(PathName)) return new AppData();
            try { var data = Parse<AppData>(File.ReadAllText(PathName)); Validate(data); return data; }
            catch (Exception ex)
            {
                try
                {
                    var data = Parse<AppData>(File.ReadAllText(PathName + ".bak")); Validate(data);
                    File.Copy(PathName, PathName + ".damaged-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
                    File.Copy(PathName + ".bak", PathName, true); Warning = "已从备份恢复学习数据，损坏的原文件已保留。"; return data;
                }
                catch { throw new IOException("学习数据读取失败，原文件已保留。请检查 data/learning.json 及其备份。", ex); }
            }
        }
        public void Save(AppData data)
        {
            lock (Sync)
            {
                Validate(data); Directory.CreateDirectory(Folder);
                var temp = PathName + ".tmp";
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(Json(data));
                    using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                    if (File.Exists(PathName)) File.Replace(temp, PathName, PathName + ".bak"); else File.Move(temp, PathName);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
        }
        public static string Protect(string key)
        { return String.IsNullOrEmpty(key) ? "" : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser)); }
        public static string Unprotect(string key)
        { return String.IsNullOrEmpty(key) ? "" : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(key), null, DataProtectionScope.CurrentUser)); }
        public static void Export(IEnumerable<Word> words, string path)
        {
            Func<string,string> cell = s => { s = s ?? ""; if (Regex.IsMatch(s, @"^[=+@\t\r\n-]")) s = "'" + s; return "\"" + s.Replace("\"", "\"\"") + "\""; };
            var lines = new List<string> { "Word,Meaning,English sentence,Chinese translation,Application,Added,Mastered" };
            lines.AddRange(words.Select(w => String.Join(",", new[] { w.Text, w.Meaning, w.Sentence, w.Translation, w.Application, w.Added, w.Mastered ? "yes" : "no" }.Select(cell).ToArray())));
            File.WriteAllText(path, String.Join("\r\n", lines.ToArray()), new UTF8Encoding(true));
        }
    }
    public class Segment { public string English = "", Chinese = "", Error = ""; }
    public class ReadResult
    {
        public string Hash = "", Source = "", Error = "";
        public bool Unchanged, Stale;
        public List<string> Lines = new List<string>();
    }
    public class ReadRequest
    {
        public long Handle;
        public uint Pid;
        public string PreviousHash = "";
        public bool Fixture;
    }
    public static class TextRules
    {
        public static bool IsEnglish(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.Length > 1800) return false;
            if (Regex.IsMatch(value, @"(?i)https?://|www\.|[\w.+-]+@[\w.-]+\.[a-z]{2,}|\b(password|passwd|secret|api[ _-]?key|authorization|access[ _-]?token)\b|[A-Z]:\\|\b[a-f0-9]{24,}\b")) return false;
            int latin = Regex.Matches(value, "[A-Za-z]").Count, han = Regex.Matches(value, "[\u4e00-\u9fff]").Count;
            return latin >= 2 && latin > han * 2 && !Regex.IsMatch(value, @"^[A-Za-z0-9+/=_-]{35,}$");
        }
        public static List<string> Sentences(IEnumerable<string> lines)
        {
            var result = new List<string>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                string clean = Regex.Replace(line ?? "", @"\s+", " ").Trim();
                foreach (var sentence in Regex.Split(clean, @"(?<=[.!?])\s+(?=[A-Z])"))
                {
                    if (!IsEnglish(sentence)) continue;
                    string rest = sentence;
                    // MyMemory accepts at most 500 UTF-8 bytes per segment.
                    while (Encoding.UTF8.GetByteCount(rest) > 460)
                    {
                        int n = Math.Min(400, rest.Length); while (Encoding.UTF8.GetByteCount(rest.Substring(0, n)) > 460) n--;
                        int space = rest.LastIndexOf(' ', n - 1, n); if (space > n / 2) n = space;
                        var part = rest.Substring(0, n).Trim(); if (seen.Add(part)) result.Add(part); rest = rest.Substring(n).Trim();
                    }
                    if (rest.Length > 0 && seen.Add(rest)) result.Add(rest);
                }
            }
            return result.Take(70).ToList();
        }
        public static string NormalizeWord(string text) { return Regex.Replace(text ?? "", @"^[^A-Za-z]+|[^A-Za-z]+$", "").ToLowerInvariant(); }
    }
}

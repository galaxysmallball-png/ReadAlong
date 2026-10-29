using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReadAlong
{
    class MockHandler : HttpMessageHandler
    {
        public int Count;
        public string Reply = "{\"responseStatus\":200,\"responseData\":{\"translatedText\":\"选择所需的输出格式。\"},\"quotaFinished\":false}";
        public int Delay;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellation)
        { Count++; if (Delay > 0) await Task.Delay(Delay, cancellation); return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Reply) }; }
    }
    public static partial class Tests
    {
        internal static MockHandler UIHandler;
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        public static int Run(string report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report)));
            string folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)), "store-" + Guid.NewGuid().ToString("N"));
            var passed = new List<string>();
            try
            {
                var store = new Store(folder); var data = store.Load(); Check(data.Words.Count == 0 && !File.Exists(store.PathName), "Fresh state");
                Check(WindowEdges.Code(2,2,560,650,13)==13&&WindowEdges.Code(558,648,560,650,13)==17&&WindowEdges.Code(558,200,560,650,13)==11&&WindowEdges.Code(250,200,560,650,13)==0,"Resize corners and edges must not intercept content");passed.Add("PASS native resize edge and corner hit regions");
                data.Words.Add(new Word { Text = "workspace", Meaning = "工作区，\"空间\"", Sentence = "Save your workspace before closing.", Translation = "关闭前请保存工作区。", Added = DateTimeOffset.Now.ToString("o") }); store.Save(data);
                Check(store.Load().Words[0].Meaning == data.Words[0].Meaning, "Unicode round-trip"); passed.Add("PASS vocabulary Unicode persistence");
                data.Words[0].Mastered = true; store.Save(data); Check(store.Load().Words[0].Mastered, "Mastered survives restart"); passed.Add("PASS mastery state persistence");
                File.WriteAllText(store.PathName, "broken"); var recovered = store.Load(); Check(recovered.Words.Count == 1 && store.Warning.Length > 0 && Directory.GetFiles(folder, "*.damaged-*").Length == 1, "Recovery must preserve damaged bytes"); passed.Add("PASS atomic backup recovery");
                string cipher = Store.Protect("synthetic-test-secret"); Check(cipher != "synthetic-test-secret" && Store.Unprotect(cipher) == "synthetic-test-secret", "DPAPI"); passed.Add("PASS Windows encrypted key round-trip");
                var split = TextRules.Sentences(new[] { "Open settings. Save changes!", "Open settings.", "这是中文", "password: hidden", "https://example.com", "mail@example.com", new string('a', 100) });
                Check(split.Count == 2, "Sentence splitting and sensitive filtering"); passed.Add("PASS deduplication and sensitive-line filtering");
                var longParts = TextRules.Sentences(new[] { String.Join(" ", Enumerable.Range(0, 50).Select(i => "English section number " + i).ToArray()) }); Check(longParts.Count > 1 && longParts.All(x => Encoding.UTF8.GetByteCount(x) <= 460), "Length splitting"); passed.Add("PASS translation segment byte limit");
                Store.Export(data.Words, Path.Combine(folder, "words.csv")); string csv = File.ReadAllText(Path.Combine(folder, "words.csv")); Check(csv.Contains("\"\"空间\"\"") && csv.Contains("工作区"), "CSV quoting"); passed.Add("PASS CSV Unicode and quoting");
                Check(Translator.ParseResponse("Azure", "[{\"translations\":[{\"text\":\"译文\",\"to\":\"zh-Hans\"}]}]") == "译文", "Azure parser");
                Check(Translator.ParseResponse("LibreTranslate", "{\"translatedText\":\"译文\"}") == "译文", "Libre parser"); passed.Add("PASS Azure and LibreTranslate response contracts");
                var handler = new MockHandler();
                using (var service = new Translator(store, data, handler))
                {
                    Check(service.Translate("Settings", CancellationToken.None).GetAwaiter().GetResult() == "设置" && handler.Count == 0, "Built-in glossary bypasses network");
                    Check(service.Translate("Choose the desired output format.", CancellationToken.None).GetAwaiter().GetResult().Contains("输出"), "MyMemory response");
                    service.Translate("Choose the desired output format.", CancellationToken.None).GetAwaiter().GetResult(); Check(handler.Count == 1, "Cache avoids duplicate HTTP");
                    passed.Add("PASS local glossary and network cache");
                    handler.Reply = "{\"responseStatus\":429,\"quotaFinished\":true}"; bool quota = false;
                    try { service.Translate("An entirely different test sentence.", CancellationToken.None).GetAwaiter().GetResult(); } catch (TranslationException ex) { quota = ex.Quota; }
                    Check(quota, "Quota must be actionable"); passed.Add("PASS quota failure handling");
                }
                var slow = new MockHandler { Delay = 3000 }; using (var service = new Translator(store, data, slow)) using (var cancel = new CancellationTokenSource(80))
                { bool canceled = false; try { service.Translate("Cancellation test sentence.", cancel.Token).GetAwaiter().GetResult(); } catch (OperationCanceledException) { canceled = true; } Check(canceled, "In-flight request cancels"); } passed.Add("PASS in-flight request cancellation");
                GlyphTypeface enFace,zhFace;
                Check(new Typeface(ReadingType.English,FontStyles.Italic,FontWeights.Normal,FontStretches.Normal).TryGetGlyphTypeface(out enFace),"English reading face resolves");
                Check(new Typeface(ReadingType.Chinese,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal).TryGetGlyphTypeface(out zhFace),"Chinese reading face resolves");
                Check(enFace.FontUri.LocalPath.EndsWith("SourceSerif4-It.otf",StringComparison.OrdinalIgnoreCase)&&zhFace.FontUri.LocalPath.EndsWith("LXGWWenKaiScreen.ttf",StringComparison.OrdinalIgnoreCase)&&enFace.CharacterToGlyphMap.ContainsKey('a')&&zhFace.CharacterToGlyphMap.ContainsKey('界'),"Bundled fonts must load rather than silently fall back");passed.Add("PASS bundled English and Chinese reading fonts with expected glyph coverage");
                using (var bitmap = new Bitmap(1100, 320, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bitmap)) { g.Clear(System.Drawing.Color.White); using (var f = new Font("Segoe UI", 25)) g.DrawString("Open the settings panel.\nChanges will take effect after restart.\nSave your workspace before closing.", f, System.Drawing.Brushes.Black, 25, 20); }
                    string language; var recognized = Capture.Ocr(bitmap, out language); Check(recognized.Any(s => s.Contains("settings panel")) && recognized.Any(s => s.Contains("workspace")), "Actual local OCR"); passed.Add("PASS actual Windows OCR on authored English fixture (" + language + ")");
                }
                File.WriteAllText(report, String.Join("\r\n", passed.ToArray()), Encoding.UTF8); return 0;
            }
            catch (Exception ex) { File.WriteAllText(report, String.Join("\r\n", passed.ToArray()) + "\r\nFAIL " + ex, Encoding.UTF8); return 1; }
        }
        public static Window FixtureWindow()
        {
            var window = new Window { Title = "ReadAlong authored test fixture", Width = 820, Height = 420, Left = 30, Top = 80, ShowInTaskbar = false, ShowActivated = false, Background = System.Windows.Media.Brushes.White };
            var panel = new StackPanel { Margin = new Thickness(30) };
            panel.Children.Add(new TextBlock { Text = "Open the settings panel.", FontSize = 25, Margin = new Thickness(0, 10, 0, 15) });
            panel.Children.Add(new TextBlock { Text = "Changes will take effect after restart.", FontSize = 25, Margin = new Thickness(0, 10, 0, 15) });
            panel.Children.Add(new TextBlock { Text = "Save your workspace before closing.", FontSize = 25, Margin = new Thickness(0, 10, 0, 15) });
            panel.Children.Add(new TextBox { Text = "Do not capture this editable field", FontSize = 20 });
            window.Content = panel; return window;
        }
        internal static IEnumerable<T> Descendants<T>(DependencyObject node) where T : DependencyObject
        { if (node is T) yield return (T)node; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(node, i))) yield return child; }
        public static int Network(string report)
        {
            try
            {
                var store = new Store(Path.Combine(Path.GetDirectoryName(report), "network-data")); var data = new AppData();
                using (var service = new Translator(store, data, null))
                {
                    string translated = service.Translate("Choose a comfortable reading speed for this application.", CancellationToken.None).GetAwaiter().GetResult();
                    Check(System.Text.RegularExpressions.Regex.IsMatch(translated, "[\u4e00-\u9fff]"), "Real service must return Chinese");
                    File.WriteAllText(report, "PASS actual MyMemory HTTP translation\r\n" + translated, Encoding.UTF8); return 0;
                }
            }
            catch (Exception ex) { File.WriteAllText(report, "FAIL " + ex, Encoding.UTF8); return 1; }
        }
    }
}

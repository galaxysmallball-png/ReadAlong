using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Automation;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace ReadAlong
{
    public static class Native
    {
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out RECT rect);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr window, int id);
        public static uint Pid(IntPtr hwnd) { uint pid; GetWindowThreadProcessId(hwnd, out pid); return pid; }
        public static string Title(IntPtr hwnd) { var text = new StringBuilder(512); GetWindowText(hwnd, text, text.Capacity); return text.ToString(); }
        public static bool Eligible(IntPtr hwnd, int ownPid)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd) || IsIconic(hwnd) || !IsWindowVisible(hwnd) || Pid(hwnd) == ownPid) return false;
            var cls = new StringBuilder(128); GetClassName(hwnd, cls, cls.Capacity);
            return !new[] { "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Windows.UI.Core.CoreWindow" }.Contains(cls.ToString());
        }
    }
    class AccessibleLine { public string Text; public double X, Y; public bool Body; }
    public static class Capture
    {
        static OcrEngine ocrEngine;
        public static List<string> Ocr(Bitmap bitmap, out string language)
        {
            var engine = ocrEngine ?? (ocrEngine = OcrEngine.TryCreateFromLanguage(new Language("en-US")) ?? OcrEngine.TryCreateFromUserProfileLanguages());
            if (engine == null) throw new InvalidOperationException("系统没有可用的 OCR 语言。请在 Windows 设置中安装英语的光学字符识别功能。");
            language = engine.RecognizerLanguage.LanguageTag;
            int max = (int)OcrEngine.MaxImageDimension;
            if (bitmap.Width > max || bitmap.Height > max) throw new InvalidOperationException("窗口分辨率超出本机 OCR 支持范围，请缩小目标窗口。");
            var bits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] bytes = new byte[bitmap.Width * bitmap.Height * 4];
            try { for (int y = 0; y < bitmap.Height; y++) Marshal.Copy(IntPtr.Add(bits.Scan0, y * bits.Stride), bytes, y * bitmap.Width * 4, bitmap.Width * 4); }
            finally { bitmap.UnlockBits(bits); }
            using (var image = SoftwareBitmap.CreateCopyFromBuffer(bytes.AsBuffer(), BitmapPixelFormat.Bgra8, bitmap.Width, bitmap.Height, BitmapAlphaMode.Ignore))
            {
                var result = engine.RecognizeAsync(image).AsTask().GetAwaiter().GetResult();
                var lines = new List<string>();
                foreach (var line in result.Lines)
                {
                    var text = line.Text;
                    // Some non-English packs insert spaces inside common Latin words.
                    foreach (var pair in new[] { new[] { "ta ke", "take" }, new[] { "wor kspace", "workspace" }, new[] { "set tings", "settings" } }) text = text.Replace(pair[0], pair[1]);
                    lines.Add(text);
                }
                return TextRules.Sentences(lines);
            }
        }
        static List<string> Accessible(IntPtr hwnd, List<Rectangle> masks, Native.RECT window, out int bodyCount, out bool complete)
        {
            var lines = new List<AccessibleLine>(); bodyCount = 0;complete=true;
            try
            {
                var request=new CacheRequest{TreeScope=TreeScope.Element};
                request.Add(AutomationElement.NameProperty);request.Add(AutomationElement.ControlTypeProperty);request.Add(AutomationElement.BoundingRectangleProperty);request.Add(AutomationElement.IsOffscreenProperty);request.Add(AutomationElement.IsPasswordProperty);
                var root = AutomationElement.FromHandle(hwnd).GetUpdatedCache(request); var walker = TreeWalker.ControlViewWalker;var clock=Stopwatch.StartNew();
                var queue = new Queue<Tuple<AutomationElement,int>>(); queue.Enqueue(Tuple.Create(root, 0)); int visited = 0;bool truncated=false;
                while (queue.Count > 0 && visited++ < 1400 && clock.ElapsedMilliseconds < 1200)
                {
                    var item = queue.Dequeue();
                    try
                    {
                        var current = item.Item1.Cached; var rect = current.BoundingRectangle;
                        if (current.IsOffscreen || rect.IsEmpty || rect.Width < 1 || rect.Height < 1) continue;
                        if (current.IsPassword || current.ControlType == ControlType.Edit)
                        { masks.Add(new Rectangle((int)rect.X - window.Left, (int)rect.Y - window.Top, (int)Math.Ceiling(rect.Width), (int)Math.Ceiling(rect.Height))); continue; }
                        bool text = current.ControlType == ControlType.Text || current.ControlType == ControlType.Hyperlink;
                        bool label = text || current.ControlType == ControlType.Button || current.ControlType == ControlType.MenuItem || current.ControlType == ControlType.TabItem || current.ControlType == ControlType.CheckBox || current.ControlType == ControlType.RadioButton || current.ControlType == ControlType.ListItem;
                        if (label && TextRules.IsEnglish(current.Name) && rect.X >= window.Left && rect.Y >= window.Top && rect.Right <= window.Right + 2 && rect.Bottom <= window.Bottom + 2)
                            lines.Add(new AccessibleLine { Text = current.Name, X = rect.X, Y = rect.Y, Body = text });
                        if (item.Item2 < 20)
                        {
                            var child = walker.GetFirstChild(item.Item1,request); int siblings = 0;
                            while (child != null && siblings++ < 250 && clock.ElapsedMilliseconds < 1200) { queue.Enqueue(Tuple.Create(child, item.Item2 + 1)); child = walker.GetNextSibling(child,request); }
                            if(child!=null){truncated=true;break;}
                        }
                    }
                    catch (ElementNotAvailableException) { }
                    catch (InvalidOperationException) { }
                }
                complete=!truncated&&queue.Count==0;
                bodyCount = lines.Count(l => l.Body);
            }
            catch { }
            return TextRules.Sentences(lines.OrderBy(l => Math.Round(l.Y / 8)).ThenBy(l => l.X).Select(l => l.Text));
        }
        public static ReadResult Read(IntPtr hwnd, uint expectedPid, string previousHash, bool fixture)
        {
            var result = new ReadResult(); Native.RECT rect;
            if (!Native.IsWindow(hwnd) || Native.Pid(hwnd) != expectedPid || Native.IsIconic(hwnd) || (!fixture && Native.GetForegroundWindow() != hwnd)) { result.Stale = true; return result; }
            if (!Native.GetWindowRect(hwnd, out rect)) { result.Error = "无法读取当前窗口的位置。"; return result; }
            var masks = new List<Rectangle>(); int bodyCount;bool complete;
            var accessible = Accessible(hwnd, masks, rect, out bodyCount,out complete);
            if (!fixture && (Native.GetForegroundWindow() != hwnd || Native.Pid(hwnd) != expectedPid)) { result.Stale = true; return result; }
            if ((bodyCount >= 2 && accessible.Count >= 3)||(!complete&&accessible.Count>0))
            {
                result.Lines = accessible; result.Source = "窗口文本";
                using (var sha = SHA256.Create()) result.Hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(String.Join("\n", accessible.ToArray())))).Replace("-", "");
                result.Unchanged = result.Hash == previousHash; return result;
            }
            if(!complete){result.Error="这个窗口响应较慢，暂未读到可用文本。可重试或使用「粘贴英文」。";return result;}
            int w = rect.Right - rect.Left, h = rect.Bottom - rect.Top;
            if (w < 30 || h < 30 || w > 5000 || h > 5000) { result.Error = "窗口尺寸暂不适合识别，请缩小后重试。"; return result; }
            using (var bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                bool captured;
                using (var graphics = Graphics.FromImage(bitmap))
                { graphics.Clear(Color.White); IntPtr dc = graphics.GetHdc(); try { captured = Native.PrintWindow(hwnd, dc, 2); } finally { graphics.ReleaseHdc(dc); } }
                if (!fixture && (Native.GetForegroundWindow() != hwnd || Native.Pid(hwnd) != expectedPid)) { result.Stale = true; return result; }
                if (captured)
                {
                    using (var graphics = Graphics.FromImage(bitmap)) foreach (var mask in masks) graphics.FillRectangle(Brushes.White, mask);
                    var bits = bitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    var bytes = new byte[w * h * 4];
                    try { for (int y = 0; y < h; y++) Marshal.Copy(IntPtr.Add(bits.Scan0, y * bits.Stride), bytes, y * w * 4, w * 4); } finally { bitmap.UnlockBits(bits); }
                    using (var sha = SHA256.Create()) result.Hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
                    if (result.Hash == previousHash) { result.Unchanged = true; return result; }
                    string language;
                    result.Lines = Ocr(bitmap, out language); result.Source = "本机 OCR / " + language;
                }
                if (result.Lines.Count == 0 && accessible.Count > 0) { result.Lines = accessible; result.Source = "窗口文本"; }
                if (result.Lines.Count == 0) result.Error = "这个窗口暂未读到英文。可尝试滚动、切换页面，或使用「粘贴英文」。部分受保护或显卡绘制的窗口不支持读取。";
                return result;
            }
        }
    }
    public class WorkerProgram
    {
        [STAThread] public static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            if(args.Length==1&&args[0]=="--server")
            {
                string input;
                while((input=Console.ReadLine())!=null)
                {
                    ReadResult result;
                    try
                    {
                        var request=Store.Parse<ReadRequest>(input);
                        result=Capture.Read(new IntPtr(request.Handle),request.Pid,request.PreviousHash,request.Fixture);
                    }
                    catch(Exception ex){result=new ReadResult{Error="识别失败："+ex.Message};}
                    Console.WriteLine(Store.Json(result));Console.Out.Flush();
                }
                return 0;
            }
            try
            {
                var hwnd = new IntPtr(Int64.Parse(args[0])); var pid = UInt32.Parse(args[1]);
                var result = Capture.Read(hwnd, pid, args.Length > 2 ? args[2] : "", args.Length > 3 && args[3] == "fixture");
                Console.Write(Store.Json(result)); return 0;
            }
            catch (Exception ex) { Console.Write(Store.Json(new ReadResult { Error = "识别失败：" + ex.Message })); return 1; }
        }
    }
}

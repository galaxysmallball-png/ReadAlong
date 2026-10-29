using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
namespace ReadAlong
{
    public class Program
    {
        internal static string Option(string[] args, string name) { int at = Array.IndexOf(args, name); return at >= 0 && at + 1 < args.Length ? args[at + 1] : null; }
        [STAThread] public static int Main(string[] args)
        {
            string test = Option(args, "--self-test"); if (test != null) return Tests.Run(test);
            string networkTest = Option(args, "--network-test"); if (networkTest != null) return Tests.Network(networkTest);
            string report = Option(args, "--ui-test"), screenshot = Option(args, "--screenshot");
            bool demo = args.Contains("--demo"); string folder = Option(args, "--data-dir") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, demo ? "demo-data" : "data"); Mutex mutex = null;
            try
            {
                var app = new Application(); app.ShutdownMode = ShutdownMode.OnMainWindowClose; UI.Install(app);
                string miniPreview=Option(args,"--mini-preview");if(miniPreview!=null){MiniPreview.Save(miniPreview);return 0;}
                if (args.Contains("--fixture-window"))
                {
                    var fixture = Tests.FixtureWindow(); string handleFile = Option(args, "--fixture-window");
                    fixture.Loaded += delegate { if (handleFile != null) File.WriteAllText(handleFile, new System.Windows.Interop.WindowInteropHelper(fixture).Handle.ToInt64().ToString()); };
                    return app.Run(fixture);
                }
                bool created; string name;
                using (var sha = System.Security.Cryptography.SHA256.Create()) name = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(folder).ToLowerInvariant()))).Replace("-", "").Substring(0, 24);
                mutex = new Mutex(true, "Local\\ReadAlong-" + name, out created);
                if (!created) { MessageBox.Show("界读已经打开，请在任务栏找到它。", "界读"); return 0; }
                var store = new Store(folder); var data = store.Load();
                if (report != null) Tests.UIHandler = new MockHandler();
                var window = new MainWindow(store, data, report == null ? null : Tests.UIHandler);window.SuppressAutomaticPolling=report!=null||Option(args,"--scan-preview")!=null; app.MainWindow = window;
                app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e) { if (report != null || screenshot != null) { File.WriteAllText(report ?? screenshot + ".error.txt", e.Exception.ToString()); e.Handled = true; app.Shutdown(1); } };
                window.Loaded += delegate
                {
                    if (demo) window.Demo(); if (!String.IsNullOrWhiteSpace(store.Warning)) MessageBox.Show(window, store.Warning, "数据已恢复");
                    if (report != null) Tests.UIRun(window, store, report);
                    if(args.Contains("--start-collapsed"))window.ToggleDock(true);
                    string motionPreview=Option(args,"--motion-preview");
                    if(motionPreview!=null){try{MotionDemo.Save(window,motionPreview);}catch(Exception ex){File.WriteAllText(motionPreview+".error.txt",ex.ToString());}window.Close();}
                    string scanPreview=Option(args,"--scan-preview");
                    if(scanPreview!=null){try{MotionDemo.SaveScan(window,scanPreview);}catch(Exception ex){File.WriteAllText(scanPreview+".error.txt",ex.ToString());}window.Close();}
                    if (screenshot != null) { var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) }; timer.Tick += delegate { timer.Stop(); window.Screenshot(screenshot); window.Close(); }; timer.Start(); }
                };
                return app.Run(window);
            }
            catch (Exception ex)
            {
                if (report != null || screenshot != null) { File.WriteAllText(report ?? screenshot + ".error.txt", "FAIL " + ex); return 1; }
                MessageBox.Show(ex.Message, "界读无法启动", MessageBoxButton.OK, MessageBoxImage.Warning); return 1;
            }
            finally { if (mutex != null) mutex.Dispose(); }
        }
    }
}

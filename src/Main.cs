using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ReadAlong
{
    public partial class MainWindow : Window
    {
        internal AppData Data;
        readonly Store store;
        readonly Translator translator;
        readonly DispatcherTimer timer;
        readonly ReaderClient reader = new ReaderClient();
        readonly StackPanel content = new StackPanel();
        TextBlock targetLabel, detail, footer;
        Button follow, readTab, wordsTab;
        CheckBox learn;
        readonly int ownPid = Process.GetCurrentProcess().Id;
        readonly List<Segment> segments = new List<Segment>();
        readonly Dictionary<Segment,TextBlock> translationBlocks = new Dictionary<Segment,TextBlock>();
        CancellationTokenSource session = new CancellationTokenSource(), translations = new CancellationTokenSource();
        bool following, reading, wordsView, closing;
        IntPtr target, hwnd;
        long windowVersion, contentVersion;
        string hash = "", sourceTitle = "";
        DateTime lastRead = DateTime.MinValue;
        int errors;
        public MainWindow(Store store, AppData data) : this(store, data, null) { }
        internal MainWindow(Store store, AppData data, System.Net.Http.HttpMessageHandler handler)
        {
            this.store = store; Data = data; translator = new Translator(store, data, handler);
            if(data.Settings.OfflineVersion<1){if(data.Settings.Provider=="MyMemory")data.Settings.Provider="Offline";data.Settings.OfflineVersion=1;}
            if(data.Settings.PollingVersion<1){if(data.Settings.Interval==3)data.Settings.Interval=1;data.Settings.PollingVersion=1;}
            Title = "界读 · 轻量伴读窗"; Width = Math.Min(560, SystemParameters.WorkArea.Width - 20); Height = Math.Min(650, SystemParameters.WorkArea.Height); MinWidth = 340; MinHeight = 390;
            Background = UI.B(UI.Back); FontFamily = new FontFamily("Microsoft YaHei UI"); Topmost = data.Settings.Pinned;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = Math.Max(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Right - Width - 18); Top = SystemParameters.WorkArea.Top + Math.Max(0, (SystemParameters.WorkArea.Height - Height) / 2);
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            SetupSizing(); BuildWarmLayout();
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) }; timer.Tick += async delegate { await Poll(); }; timer.Start();
            SourceInitialized += delegate { hwnd = new WindowInteropHelper(this).Handle; HwndSource.FromHwnd(hwnd).AddHook(Hook); if (!Native.RegisterHotKey(hwnd, 1741, 0x4003, 0x54)) footer.Text = "暂停快捷键被占用，请使用伴读按钮"; Native.RegisterHotKey(hwnd,1742,0x4003,0x4D); };
            Closed += delegate { closing = true; timer.Stop(); session.Cancel(); translations.Cancel(); reader.Dispose(); if (hwnd != IntPtr.Zero) Native.UnregisterHotKey(hwnd, 1741); Native.UnregisterHotKey(hwnd,1742); translator.Dispose(); };
            Render();UpdateActivityUI();
        }
        static void At(Grid grid, UIElement element, int row) { Grid.SetRow(element, row); grid.Children.Add(element); }
        IntPtr Hook(IntPtr window, int message, IntPtr wparam, IntPtr lparam, ref bool handled)
        { if (message == 0x312 && wparam.ToInt32() == 1741) { Toggle(); handled = true; } if(message==0x312&&wparam.ToInt32()==1742){ToggleDock(dockMotion==null||!dockMotion.WantsCollapsed);handled=true;} return IntPtr.Zero; }
        void Status(string title, string note) { if (closing) return; targetLabel.Text = title; targetLabel.ToolTip = title; detail.Text = note; detail.ToolTip = note; RefreshMini(); }
        void UpdateSegment(Segment segment)
        { TextBlock block; if (translationBlocks.TryGetValue(segment, out block)) { block.Text = segment.Error.Length > 0 ? segment.Error : segment.Chinese; block.Foreground = UI.B(segment.Error.Length > 0 ? "#9C5235" : UI.Muted); } RefreshMini(); }
        internal void Render()
        {
            content.Children.Clear(); translationBlocks.Clear(); wordsTab.Content = "生词本 " + Data.Words.Count;
            RenderSidebar(); if(readingCount!=null)readingCount.Text=segments.Count>0?segments.Count+" 句":""; RefreshMini();
            if (segments.Count == 0)
            {
                var intro = new StackPanel { Margin = new Thickness(6, 24, 10, 16) }; var heading = UI.Text("让英文，慢慢变熟悉。", 21, UI.Ink); heading.FontFamily = ReadingType.Chinese; intro.Children.Add(heading);
                var sample = UI.Text("Save your workspace\nbefore closing.", 25, UI.Ink); sample.FontFamily = ReadingType.English;sample.FontStyle=FontStyles.Italic; sample.LineHeight = 35; sample.Margin = new Thickness(0, 28, 0, 0); intro.Children.Add(sample);
                var translation = UI.Text("关闭前，请保存你的工作区。", 17, UI.Muted);translation.FontFamily=ReadingType.Chinese; intro.Children.Add(new Border { Child = translation, BorderBrush = UI.B("#C7D1BC"), BorderThickness = new Thickness(2, 0, 0, 0), Padding = new Thickness(11, 3, 0, 3), Margin = new Thickness(0, 12, 0, 24) });
                var explain = UI.Text("点击上方开始伴读 → 切回英文应用\n遇到不认识的词，点一下就能收藏。", 12, UI.Muted); explain.LineHeight = 24; intro.Children.Add(explain);
                var privacy = UI.Text(Data.Settings.Provider=="Offline"?"示例对照。本地离线模式，识别与翻译均在本机完成。\n文字不上传，也没有每日字符额度。":"示例对照。识别在本机完成，截图不上传。\n只有待翻译文字交给你选择的翻译服务。", 10.5, UI.Muted); privacy.LineHeight = 20; privacy.Margin = new Thickness(0, 20, 0, 0); intro.Children.Add(privacy); content.Children.Add(intro); return;
            }
            foreach (var item in segments)
            {
                var segment = item; var panel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
                var english = UI.Text("", 21*readingZoom, UI.Ink); english.FontFamily = ReadingType.English; english.FontStyle=FontStyles.Italic; english.LineHeight = 31*readingZoom;
                AutomationProperties.SetAutomationId(english, "Sentence-" + segments.IndexOf(segment));
                foreach (Match match in Regex.Matches(segment.English, @"[A-Za-z]+(?:['’\-][A-Za-z]+)*|[^A-Za-z]+"))
                {
                    string token = match.Value;
                    if (Regex.IsMatch(token, "^[A-Za-z]"))
                    {
                        bool saved = Data.Words.Any(w => w.Text.Equals(TextRules.NormalizeWord(token), StringComparison.OrdinalIgnoreCase));
                        var link = new Hyperlink(new Run(token)) { Foreground = UI.B(saved ? "#6B531F" : UI.Ink), Background = saved ? UI.B(UI.Highlight) : Brushes.Transparent, TextDecorations = null, ToolTip = saved ? "已收藏，点击编辑释义" : "点击查询并收藏单词", Focusable = true };
                        link.MouseEnter += delegate { link.TextDecorations = TextDecorations.Underline; }; link.MouseLeave += delegate { link.TextDecorations = null; };
                        link.Click += delegate { OpenInspector(token, segment); }; english.Inlines.Add(link);
                    }
                    else english.Inlines.Add(new Run(token));
                }
                var chinese = UI.Text(segment.Chinese.Length > 0 ? segment.Chinese : segment.Error.Length > 0 ? segment.Error : "正在为你翻译…", 17*readingZoom, UI.Muted); chinese.FontFamily=ReadingType.Chinese;chinese.LineHeight = 30*readingZoom; translationBlocks[segment] = chinese;
                var annotation = new Border { Child = chinese, BorderBrush = UI.B("#C7D1BC"), BorderThickness = new Thickness(0), Padding = new Thickness(0), Margin = new Thickness(0, 11, 0, 0) };
                var notes = new StackPanel();
                if (learn.IsChecked == true)
                {
                    annotation.Visibility = Visibility.Collapsed; var reveal = UI.Button("看译文", false, null); reveal.HorizontalAlignment = HorizontalAlignment.Left; reveal.Padding = new Thickness(8, 4, 8, 4); reveal.Margin = new Thickness(0, 8, 0, 0); reveal.Click += delegate { annotation.Visibility = annotation.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; reveal.Content = annotation.Visibility == Visibility.Visible ? "收起译文" : "看译文"; }; notes.Children.Add(reveal);
                }
                notes.Children.Add(annotation);
                var pair = new Grid(); pair.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) }); pair.ColumnDefinitions.Add(new ColumnDefinition());
                var index = UI.Text((segments.IndexOf(segment) + 1).ToString("00"), 10, UI.Muted); index.VerticalAlignment = VerticalAlignment.Top; index.Margin = new Thickness(0, 7, 0, 0); pair.Children.Add(index); Grid.SetColumn(english, 1); pair.Children.Add(english);
                if (studioWide)
                {
                    pair.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); pair.ColumnDefinitions.Add(new ColumnDefinition()); Grid.SetColumn(notes, 3); annotation.Margin = new Thickness(0, 6, 0, 0); annotation.Padding = new Thickness(0); annotation.BorderThickness = new Thickness(0); pair.Children.Add(notes);
                }
                else { pair.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); pair.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); Grid.SetColumn(notes, 1); Grid.SetRow(notes, 1); pair.Children.Add(notes); }
                panel.Children.Add(pair); var card = new Border { Child = panel, Background = UI.B("#F2FFFFFF"), BorderBrush = UI.B("#DEEDE6"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(13, 16, 12, 18), Margin = new Thickness(2, 5, 9, 9) }; var lift=new System.Windows.Media.Effects.DropShadowEffect { BlurRadius=9, ShadowDepth=2, Opacity=.045, Color=Color.FromRgb(42,95,78) };card.Effect=lift; card.MouseEnter += delegate { card.Background = UI.B("#EDFAF5");lift.Opacity=.11; }; card.MouseLeave += delegate { card.Background = UI.B("#F2FFFFFF");lift.Opacity=.045; }; content.Children.Add(card);
            }
        }
        internal void Demo()
        {
            sourceTitle = "示例应用 · Settings"; segments.Clear(); segments.Add(new Segment { English = "Open the settings panel.", Chinese = "打开设置面板。" }); segments.Add(new Segment { English = "Changes will take effect after restart.", Chinese = "更改将在重启后生效。" }); segments.Add(new Segment { English = "Save your workspace before closing.", Chinese = "关闭前请保存工作区。" }); segments.Add(new Segment { English = "Choose the desired output format.", Chinese = "选择所需的输出格式。" });
            Status(sourceTitle, "示例对照，不读取你的屏幕 · 点击单词可收藏"); Render();
        }
        internal void Screenshot(string path)
        {
            UI.SavePreview(this, path);
        }
    }
}

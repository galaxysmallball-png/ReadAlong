using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReadAlong
{
    public partial class MainWindow
    {
        Border inspectorPanel;
        TextBlock inspectorTitle, inspectorSource, inspectorHint;
        TextBox inspectorMeaning;
        Button inspectorSave;
        string inspectedWord = "";
        Segment inspectedSegment;
        CancellationTokenSource wordRequest = new CancellationTokenSource();
        long wordVersion;
        bool inspectorVisible, inspectorLoading, inspectorEdited;
        Border MakeInspector()
        {
            var area = new StackPanel { Margin = new Thickness(2, 10, 2, 14) };
            var label = UI.Text("词句手帐", 16, UI.Ink); label.FontWeight = FontWeights.SemiBold; area.Children.Add(label);
            var help = UI.Text("不打断阅读，留住不熟悉的词。", 11, UI.Muted); help.Margin = new Thickness(0, 8, 0, 30); area.Children.Add(help);
            inspectorTitle = UI.Text("从原句中取词", 27, UI.Ink); inspectorTitle.FontFamily = ReadingType.English;inspectorTitle.FontStyle=FontStyles.Italic; inspectorTitle.LineHeight = 36; area.Children.Add(inspectorTitle);
            inspectorHint = UI.Text("点击英文里的单词，释义会出现在这里。", 11.5, UI.Muted); inspectorHint.LineHeight = 21; inspectorHint.Margin = new Thickness(0, 12, 0, 18); area.Children.Add(inspectorHint);
            var meaningLabel = UI.Text("中文释义 · 可以修改", 11, UI.Muted); meaningLabel.Margin = new Thickness(0, 0, 0, 8); area.Children.Add(meaningLabel);
            inspectorMeaning = new TextBox { Height = 90, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MaxLength = 1000, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, IsEnabled = false };
            AutomationName(inspectorMeaning, "InlineMeaning");
            inspectorMeaning.FontFamily=ReadingType.Chinese;inspectorMeaning.FontSize=17;
            inspectorMeaning.TextChanged += delegate { if (!inspectorLoading) inspectorEdited = true; }; area.Children.Add(inspectorMeaning);
            inspectorSave = UI.Button("收藏到生词本", false, delegate
            {
                if (inspectedWord.Length == 0 || inspectedSegment == null) return;
                if (SaveWord(inspectedWord, inspectorMeaning.Text.Trim(), inspectedSegment, sourceTitle)) { inspectorHint.Text = "已收藏。你可以继续修改释义。"; inspectorSave.Content = "保存释义"; }
            }); inspectorSave.IsEnabled = false; inspectorSave.Margin = new Thickness(0, 12, 0, 25); AutomationName(inspectorSave, "InlineSaveWord"); area.Children.Add(inspectorSave);
            var sourceLabel = UI.Text("遇见它的原句", 11, UI.Muted); sourceLabel.Margin = new Thickness(0, 0, 0, 12); area.Children.Add(sourceLabel);
            inspectorSource = UI.Text("收藏时会一同保存英文语境，方便以后复习。", 15, UI.Muted); inspectorSource.FontFamily = ReadingType.English;inspectorSource.FontStyle=FontStyles.Italic; inspectorSource.LineHeight = 25; area.Children.Add(inspectorSource);
            var note = UI.Text("释义为机器翻译，可按语境修订。", 10.5, UI.Muted); note.Margin = new Thickness(0, 30, 0, 0); area.Children.Add(note);
            inspectorPanel = new Border { Background = Brushes.Transparent, BorderBrush = UI.B(UI.Rule), CornerRadius = new CornerRadius(0, 17, 17, 0), BorderThickness = new Thickness(0), Child = new ScrollViewer { Content = area, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
            Closed += delegate { wordRequest.Cancel(); };
            return inspectorPanel;
        }
        static void AutomationName(DependencyObject element, string id) { System.Windows.Automation.AutomationProperties.SetAutomationId(element, id); }
        internal async void OpenInspector(string word, Segment segment)
        {
            ShowSidebar(2);
            wordRequest.Cancel(); wordRequest = new CancellationTokenSource(); var token = wordRequest.Token; long version = ++wordVersion;
            inspectedWord = TextRules.NormalizeWord(word); inspectedSegment = segment;
            inspectorTitle.Text = inspectedWord; inspectorSource.Text = segment.English; inspectorSave.IsEnabled = inspectorMeaning.IsEnabled = true;
            var saved = Data.Words.FirstOrDefault(w => w.Text.Equals(inspectedWord, StringComparison.OrdinalIgnoreCase));
            inspectorLoading = true; inspectorMeaning.Text = saved == null ? "" : saved.Meaning; inspectorLoading = false; inspectorEdited = false;
            inspectorSave.Content = saved == null ? "收藏到生词本" : "保存释义"; inspectorHint.Text = saved == null ? "正在查找释义…也可以直接填写。" : "已在生词本中，释义可以修改。";
            if (saved != null) return;
            try
            {
                string result = await TranslateWord(inspectedWord, token);
                if (token.IsCancellationRequested || version != wordVersion || closing) return;
                if (!inspectorEdited) { inspectorLoading = true; inspectorMeaning.Text = result; inspectorLoading = false; }
                inspectorHint.Text = "按语境核对一下，再存入生词本。";
            }
            catch (OperationCanceledException) { }
            catch (Exception) { if (!token.IsCancellationRequested && version == wordVersion && !closing) inspectorHint.Text = "暂未查到释义，可自行填写后收藏。"; }
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace ReadAlong
{
    public partial class MainWindow
    {
        void RenderWords()
        {
            var searchLabel = UI.Text("搜索单词、释义或原句", 11.5, UI.Muted); searchLabel.Margin = new Thickness(0, 14, 0, 0); sidebarBook.Children.Add(searchLabel);
            var tools = new Grid { Margin = new Thickness(0, 7, 0, 12) }; tools.ColumnDefinitions.Add(new ColumnDefinition()); tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var search = new TextBox { ToolTip = "搜索单词、释义或原句", Margin = new Thickness(0, 0, 8, 0) }; tools.Children.Add(search);
            var export = UI.Button("导出 CSV", false, delegate
            {
                var dialog = new SaveFileDialog { Title = "导出生词本（可导入 Anki）", Filter = "CSV 文件|*.csv", FileName = "界读-生词本.csv" };
                if (dialog.ShowDialog(this) == true) try { Store.Export(Data.Words, dialog.FileName); MessageBox.Show(this, "生词本已导出。Anki 导入时请跳过首行字段名。", "导出完成"); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "导出失败"); }
            }); Grid.SetColumn(export, 1); tools.Children.Add(export); sidebarBook.Children.Add(tools);
            var list = new StackPanel(); sidebarBook.Children.Add(list);
            Action populate = delegate
            {
                list.Children.Clear(); string query = search.Text.Trim();
                var words = Data.Words.Where(w => query.Length == 0 || (w.Text + " " + w.Meaning + " " + w.Sentence).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(w => w.Mastered).ThenByDescending(w => w.Added).ToList();
                if (words.Count == 0) { var hint = UI.Text(Data.Words.Count == 0 ? "在对照中点击英文单词，就能存下释义和当时的原句。" : "没有找到匹配的单词。", 13, UI.Muted); hint.Margin = new Thickness(0, 24, 0, 0); list.Children.Add(hint); }
                foreach (var item in words)
                {
                    var word = item; var panel = new StackPanel(); var top = new Grid(); top.ColumnDefinitions.Add(new ColumnDefinition()); top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var name = UI.Text(word.Text, 23, UI.Ink); name.FontFamily = ReadingType.English;name.FontStyle=FontStyles.Italic; top.Children.Add(name);
                    var mastered = UI.Button(word.Mastered ? "✓ 已掌握" : "标为掌握", false, delegate { Mutate(delegate { word.Mastered = !word.Mastered; }); }); mastered.FontSize = 11; mastered.Padding = new Thickness(7, 5, 7, 5); Grid.SetColumn(mastered, 1); top.Children.Add(mastered); panel.Children.Add(top);
                    var meaning = UI.Text(String.IsNullOrWhiteSpace(word.Meaning) ? "暂无释义，点击编辑补充" : word.Meaning, 15, UI.Muted); meaning.FontFamily=ReadingType.Chinese;meaning.Margin = new Thickness(0, 8, 0, 7); panel.Children.Add(meaning);
                    var sentence = UI.Text(word.Sentence, 14, UI.Muted); sentence.FontFamily = ReadingType.English;sentence.FontStyle=FontStyles.Italic; sentence.LineHeight = 24; panel.Children.Add(sentence);
                    var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 9, 0, 0) };
                    var edit = UI.Button("编辑", false, delegate { OpenInspector(word.Text, new Segment { English = word.Sentence, Chinese = word.Translation }); }); edit.Padding = new Thickness(8, 4, 8, 4); buttons.Children.Add(edit);
                    var delete = UI.Button("移除", false, delegate { if (MessageBox.Show(this, "从生词本移除 “" + word.Text + "”？", "移除单词", MessageBoxButton.YesNo) == MessageBoxResult.Yes) Mutate(delegate { Data.Words.Remove(word); }); }); delete.Padding = new Thickness(8, 4, 8, 4); delete.Margin = new Thickness(7, 0, 0, 0); buttons.Children.Add(delete); panel.Children.Add(buttons);
                    list.Children.Add(new Border { Child = panel, Padding = new Thickness(0, 14, 3, 16), BorderBrush = UI.B(UI.Rule), BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(0, 0, 8, 0) });
                }
            }; search.TextChanged += delegate { populate(); }; populate();
        }
        bool Mutate(Action change)
        {
            string before = Store.Json(Data);
            try { change(); store.Save(Data); Render(); return true; }
            catch (Exception ex) { var restored = Store.Parse<AppData>(before); Data.Words = restored.Words; Data.Settings = restored.Settings; Render(); MessageBox.Show(this, "更改未保存，已还原。\n" + ex.Message, "保存失败"); return false; }
        }
        internal bool SaveWord(string word, string meaning, Segment context, string application)
        {
            return Mutate(delegate
            {
                string normalized = TextRules.NormalizeWord(word); var saved = Data.Words.FirstOrDefault(w => w.Text.Equals(normalized, StringComparison.OrdinalIgnoreCase));
                if (saved == null) { saved = new Word { Text = normalized, Added = DateTimeOffset.Now.ToString("o") }; Data.Words.Add(saved); }
                saved.Meaning = meaning; saved.Sentence = context.English; saved.Translation = context.Chinese; saved.Application = application;
            });
        }
        internal bool SavePreferences(Preferences prefs)
        { Pause("设置已更新；点击开始后继续跟随。"); bool ok = Mutate(delegate { Data.Settings = prefs; }); Topmost = Data.Settings.Pinned; return ok; }
        internal Task<string> TranslateWord(string word, CancellationToken token) { return translator.Translate(word, token); }
    }
}

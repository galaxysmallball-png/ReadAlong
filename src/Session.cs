using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ReadAlong
{
    public partial class MainWindow
    {
        internal void Start()
        {
            if (following) return; following = true; errors = 0; hash = ""; lastRead = DateTime.MinValue; session.Cancel(); session = new CancellationTokenSource();
            windowVersion++; follow.Content = "暂停伴读"; Status("伴读已开启", "切换到英文应用，遇到生词可以点一下收藏。");UpdateActivityUI();
            try{if(!SuppressAutomaticPolling)reader.WarmUp();}catch(Exception ex){Status("识别组件暂不可用",ex.Message);}
        }
        internal void Pause(string reason)
        { following = false; windowVersion++; contentVersion++; session.Cancel(); translations.Cancel();reader.Stop(); follow.Content = "开始伴读"; Status("已暂停", reason);ResetActivity(); }
        void Toggle() { if (following) Pause("保留当前对照。再次开始后，只读取你当前使用的窗口。"); else Start(); }
        async Task Poll()
        {
            if (!following || closing || SuppressAutomaticPolling) return;
            var active = Native.GetForegroundWindow();
            if (!Native.Eligible(active, ownPid))
            {
                if (Native.Pid(active) != ownPid && target != IntPtr.Zero)
                { session.Cancel(); translations.Cancel(); session = new CancellationTokenSource(); windowVersion++; contentVersion++; target = IntPtr.Zero; hash = "";ResetActivity(); Status("等待英文应用", "当前为桌面或不支持的窗口，没有读取其他窗口。"); }
                return;
            }
            if (active != target)
            {
                session.Cancel(); translations.Cancel();SetTranslationActive(false); session = new CancellationTokenSource(); target = active; windowVersion++; contentVersion++; hash = ""; sourceTitle = Native.Title(target); segments.Clear(); errors = 0; Render();
                Status(sourceTitle, "正在读取此窗口… 截图仅在本机内存中处理。");
            }
            if (reading || (DateTime.UtcNow - lastRead).TotalSeconds < Data.Settings.Interval) return;
            reading = true;SetRecognitionActive(true); lastRead = DateTime.UtcNow; long version = windowVersion; var token = session.Token; var currentTarget = target; uint pid = Native.Pid(target);
            try
            {
                var result = await reader.ReadAsync(currentTarget, pid, hash, token, false);
                if (token.IsCancellationRequested || version != windowVersion || !following) return;
                if (Native.GetForegroundWindow() != currentTarget || Native.Pid(currentTarget) != pid) return;
                if (result.Stale || result.Unchanged) return;
                if (!String.IsNullOrEmpty(result.Error)) { Status(sourceTitle, result.Error); return; }
                if (result.Lines.Count == 0) return;
                hash = result.Hash; sourceTitle = Native.Title(currentTarget);
                Observe(ShowLines(result.Lines, result.Source, token));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (version == windowVersion && !closing) Status(sourceTitle, ex.Message); }
            finally { reading = false;SetRecognitionActive(false); }
        }
        internal static async Task<ReadResult> ReadWindow(IntPtr handle, uint pid, string previousHash, CancellationToken token, bool fixture)
        {
            using(var client=new ReaderClient())return await client.ReadAsync(handle,pid,previousHash,token,fixture);
        }
        internal async Task ShowLines(IEnumerable<string> lines, string source, CancellationToken parent)
        {
            var incoming=TextRules.Sentences(lines);
            parent.ThrowIfCancellationRequested();
            bool hasResults=segments.All(s=>!String.IsNullOrEmpty(s.Chinese));
            if(incoming.Count>0&&incoming.SequenceEqual(segments.Select(s=>s.English))&&segments.All(s=>String.IsNullOrEmpty(s.Error))&&(hasResults||(translating&&!translations.IsCancellationRequested)))return;
            translations.Cancel(); translations = CancellationTokenSource.CreateLinkedTokenSource(parent); var token = translations.Token; long version = ++contentVersion;
            segments.Clear(); segments.AddRange(incoming.Select(s => new Segment { English = s })); Render();
            Status(sourceTitle == "" ? "手动英文" : sourceTitle, source + " · " + segments.Count + " 句 · 正在逐句翻译");
            int completed = 0; var work = segments.ToList();
            SetTranslationActive(true);
            var tasks = work.Select(async segment =>
            {
                try
                {
                    string chinese = await translator.Translate(segment.English, token);
                    if (token.IsCancellationRequested || version != contentVersion || closing) return;
                    segment.Chinese = chinese; errors = 0; completed++; UpdateSegment(segment);
                    Status(sourceTitle == "" ? "手动英文" : sourceTitle, source + " · " + completed + " / " + work.Count + " 句已译 · " + Data.Settings.Provider);
                }
                catch (OperationCanceledException) { }
                catch (TranslationException ex)
                {
                    if (token.IsCancellationRequested || version != contentVersion || closing) return;
                    segment.Error = ex.Message; UpdateSegment(segment); errors++;
                    if (ex.Quota || errors >= 3) Pause(ex.Message); else Status(sourceTitle, ex.Message);
                }
                catch (Exception ex)
                { if (!token.IsCancellationRequested && version == contentVersion && !closing) { segment.Error = "翻译失败：" + ex.Message; UpdateSegment(segment); } }
            }).ToArray();
            try{await Task.WhenAll(tasks);}finally{if(version==contentVersion)SetTranslationActive(false);}
        }
        void Paste()
        {
            var dialog = UI.Dialog(this, "粘贴英文", 490); var panel = new Grid { Margin = new Thickness(24, 8, 24, 24) }; panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); panel.RowDefinitions.Add(new RowDefinition()); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var hint = UI.Text("仅翻译你粘贴到这里的内容，适用于暂时读不到文字的窗口。", 12, UI.Muted); hint.Margin = new Thickness(0, 0, 0, 12); panel.Children.Add(hint);
            var box = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MaxLength = 6000, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; At(panel, box, 1);
            var translate = UI.Button("逐句翻译", true, delegate { dialog.DialogResult = true; }); translate.Margin = new Thickness(0, 12, 0, 0); At(panel, translate, 2); dialog.Content = panel; UI.WrapDialog(dialog);
            if (dialog.ShowDialog() == true)
            {
                Pause("手动文本模式，不读取屏幕。"); sourceTitle = "手动粘贴的英文"; wordsView = false; session = new CancellationTokenSource();
                var lines = TextRules.Sentences(box.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                if (lines.Count == 0) { Status("没有可翻译的英文", "请粘贴英文句子。网址、邮件地址和疑似凭据行会被跳过。"); return; }
                Observe(ShowLines(lines, "手动粘贴", session.Token));
            }
        }
        async void Observe(Task task) { try { await task; } catch (OperationCanceledException) { } catch (Exception ex) { if (!closing) Status("翻译失败", ex.Message); } }
    }
}

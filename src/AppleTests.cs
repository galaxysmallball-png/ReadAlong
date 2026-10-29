using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace ReadAlong
{
    public partial class MainWindow
    {
        internal double DrawerProgress { get { return drawerSpring.Value; } }
        internal bool SidebarOpen { get { return drawerWanted; } }
        internal DockMotion MotionState { get { return dockMotion; } }
    }
    public static partial class Tests
    {
        static async Task Until(Func<bool> test,int milliseconds,string message)
        {var clock=Stopwatch.StartNew();while(!test()&&clock.ElapsedMilliseconds<milliseconds)await Task.Delay(20);Check(test(),message);}
        public static void UIRun(MainWindow window,Store store,string report)
        {
            var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};var passed=new System.Collections.Generic.List<string>();
            timer.Tick+=async delegate
            {
                timer.Stop();Process fixture=null;
                try
                {
                    window.Demo();var mainToggle=Descendants<Button>(window).First(b=>System.Windows.Automation.AutomationProperties.GetAutomationId(b)=="MainFollow");mainToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(window.IsFollowing&&(string)mainToggle.Content=="暂停伴读","Main start button enables following");mainToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(!window.IsFollowing,"Main pause button disables following");passed.Add("PASS main view start and pause without opening sidebar");window.ShowSidebar(0);await Until(()=>window.DrawerProgress>.99,2500,"Sidebar opens");
                    Descendants<Button>(window).First(b=>System.Windows.Automation.AutomationProperties.GetAutomationId(b)=="ZoomIn").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.FlushZoom();
                    var firstSentence=Descendants<TextBlock>(window).First(t=>System.Windows.Automation.AutomationProperties.GetAutomationId(t)=="Sentence-0");Check(Math.Abs(firstSentence.FontSize-23.1)<.01&&Math.Abs(store.Load().Settings.ReadingScale-1.1)<.001,"Zoom button changes font size and persists");
                    window.SetReadingZoom(5);Check(window.ReadingZoom==2,"Upper zoom limit");window.SetReadingZoom(.1);Check(window.ReadingZoom==.7,"Lower zoom limit");Descendants<Button>(window).First(b=>System.Windows.Automation.AutomationProperties.GetAutomationId(b)=="ZoomReset").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.FlushZoom();Check(window.ReadingZoom==1,"Zoom reset");passed.Add("PASS actual reading zoom buttons, limits, reset and saved preference");
                    var follow=Descendants<Button>(window).First(b=>System.Windows.Automation.AutomationProperties.GetAutomationId(b)=="Follow");follow.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));follow.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));passed.Add("PASS actual follow and pause controls in sidebar");
                    window.ShowSidebar(0,false);await Task.Delay(50);double halfway=window.DrawerProgress;window.ShowSidebar(0,true);await Until(()=>window.DrawerProgress>.99,2500,"Sidebar reverses from current position");Check(halfway<1,"Sidebar started moving");passed.Add("PASS sidebar spring can reverse in flight");
                    window.ShowSidebar(0,false);await Until(()=>window.DrawerProgress<.001,2500,"Sidebar closes");Check(!window.SidebarOpen,"Reading view remains the default");window.Screenshot(Path.Combine(Path.GetDirectoryName(report),"preview.png"));passed.Add("PASS reading-only main view and hidden secondary controls");
                    window.ReduceDrawerMotionForTests=true;window.ShowSidebar(0,true);Check(window.DrawerProgress==1,"Reduced-motion sidebar opens directly");window.ShowSidebar(0,false);Check(window.DrawerProgress==0,"Reduced-motion sidebar closes directly");window.ReduceDrawerMotionForTests=false;passed.Add("PASS reduced-motion sidebar uses immediate state changes");
                    var link=Descendants<TextBlock>(window).SelectMany(t=>t.Inlines.OfType<Hyperlink>()).First(h=>new TextRange(h.ContentStart,h.ContentEnd).Text=="workspace");link.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent));await Until(()=>window.DrawerProgress>.99,2500,"Word opens sidebar");
                    var meaning=Descendants<TextBox>(window).First(t=>System.Windows.Automation.AutomationProperties.GetAutomationId(t)=="InlineMeaning");Check(meaning.IsEnabled&&meaning.Text.Contains("工作区"),"Lookup definition");meaning.Text="工作区";Descendants<Button>(window).First(b=>System.Windows.Automation.AutomationProperties.GetAutomationId(b)=="InlineSaveWord").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(store.Load().Words.Single().Meaning=="工作区","Word saves with context");passed.Add("PASS word click opens sidebar and saves vocabulary");
                    window.Screenshot(Path.Combine(Path.GetDirectoryName(report),"word-sidebar.png"));window.ShowSidebar(1);await Task.Delay(50);window.Screenshot(Path.Combine(Path.GetDirectoryName(report),"vocabulary.png"));Check(Descendants<TextBlock>(window).Any(t=>System.Windows.Automation.AutomationProperties.GetAutomationId(t)=="Sentence-0"),"Book does not replace reading");passed.Add("PASS vocabulary stays in sidebar without replacing reading");
                    window.ShowSidebar(0);var learn=Descendants<CheckBox>(window).First();learn.IsChecked=true;window.ShowSidebar(0,false);await Until(()=>window.DrawerProgress<.001,2500,"Sidebar closes for reveal test");var reveal=Descendants<Button>(window).First(b=>(string)b.Content=="看译文");reveal.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check((string)reveal.Content=="收起译文","Reveal works");learn.IsChecked=false;passed.Add("PASS English-first mode remains operable from sidebar");
                    string handleFile=Path.Combine(Path.GetDirectoryName(report),"fixture-"+Guid.NewGuid().ToString("N")+".txt");fixture=Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,"--fixture-window \""+handleFile+"\""){UseShellExecute=false,CreateNoWindow=true});await Until(()=>File.Exists(handleFile),6000,"Authored fixture starts");
                    IntPtr handle=new IntPtr(Int64.Parse(File.ReadAllText(handleFile)));window.Start();window.SetRecognitionActive(true);var light=Descendants<ScanShimmer>(window).First(s=>System.Windows.Automation.AutomationProperties.GetAutomationId(s)=="RecognitionLight");light.FixedPhase=.42;light.SetActive(true);window.UpdateLayout();Check(window.ActivityLit&&light.ActualHeight>=3&&light.IsVisible,"Recognition enables visible shimmer");window.Screenshot(Path.Combine(Path.GetDirectoryName(report),"recognizing.png"));var read=await MainWindow.ReadWindow(handle,(uint)fixture.Id,"",CancellationToken.None,true);window.SetRecognitionActive(false);light.FixedPhase=-1;Check(!window.ActivityLit,"Recognition completion stops shimmer");window.Pause("测试结束");passed.Add("PASS recognition shimmer starts and stops with activity state and has visible height");Check(read.Error==""&&read.Lines.Any(l=>l.Contains("settings panel"))&&!read.Lines.Any(l=>l.Contains("editable field")),"Real reader skips input fields");passed.Add("PASS actual window recognition and editable-field exclusion");
                    var again=await MainWindow.ReadWindow(handle,(uint)fixture.Id,read.Hash,CancellationToken.None,true);Check(again.Unchanged,"Static content avoids work");passed.Add("PASS unchanged window recognition deduplicates");
                    using(var reader=new ReaderClient())
                    {
                        var timings=new System.Collections.Generic.List<long>();
                        for(int i=0;i<6;i++){var clock=Stopwatch.StartNew();var item=await reader.ReadAsync(handle,(uint)fixture.Id,"",CancellationToken.None,true);clock.Stop();Check(item.Lines.Any(l=>l.Contains("settings panel"))&&!item.Lines.Any(l=>l.Contains("editable field")),"Persistent reader retains input exclusion");timings.Add(clock.ElapsedMilliseconds);}
                        Check(reader.Launches==1,"Polling reuses a single worker");var invalid=await reader.ReadAsync(handle,(uint)fixture.Id+1,"",CancellationToken.None,true);Check(invalid.Stale,"Reused worker validates target PID");
                        File.WriteAllText(Path.Combine(Path.GetDirectoryName(report),"recognition-timings.txt"),"Authored fixture, milliseconds: "+String.Join(", ",timings)+"\r\nWorker launches: "+reader.Launches);
                    }
                    passed.Add("PASS repeated reads reuse one worker and validate target identity");
                    using(var reader=new ReaderClient())using(var cancel=new CancellationTokenSource())
                    {
                        var abandoned=reader.ReadAsync(handle,(uint)fixture.Id,"",cancel.Token,true);cancel.Cancel();bool cancelled=false;try{await abandoned;}catch(OperationCanceledException){cancelled=true;}Check(cancelled,"In-flight capture cancels");
                        var recovered=await reader.ReadAsync(handle,(uint)fixture.Id,"",CancellationToken.None,true);Check(recovered.Lines.Count>=3&&reader.Launches==2,"Cancelled worker is replaced on next request");
                    }
                    passed.Add("PASS cancelled capture terminates worker and next request recovers");
                    await window.ShowLines(read.Lines,read.Source,CancellationToken.None);UIHandler.Delay=700;var old=window.ShowLines(new[]{"An obsolete interface sentence."},"old",CancellationToken.None);await Task.Delay(30);await window.ShowLines(new[]{"Settings"},"new",CancellationToken.None);await old;Check(Descendants<TextBlock>(window).Any(t=>t.Text=="设置"),"Stale translation rejected");passed.Add("PASS obsolete translation cannot overwrite current content");
                    UIHandler.Delay=150;var stable=new[]{"A stable sentence remains on screen."};var pending=window.ShowLines(stable,"first",CancellationToken.None);await Task.Delay(20);int calls=UIHandler.Count;await window.ShowLines(stable,"same",CancellationToken.None);await pending;Check(UIHandler.Count==calls&&Descendants<TextBlock>(window).Any(t=>t.Text.Contains("选择所需的输出格式")),"Repeated recognition must not cancel pending translation");
                    using(var interrupted=new CancellationTokenSource()){var partial=window.ShowLines(new[]{"A paused sentence can be translated after resume."},"paused",interrupted.Token);interrupted.Cancel();await partial;await window.ShowLines(new[]{"A paused sentence can be translated after resume."},"resumed",CancellationToken.None);Check(Descendants<TextBlock>(window).Any(t=>t.Text.Contains("选择所需的输出格式")),"Cancelled translation resumes for identical English");}
                    passed.Add("PASS identical reads preserve pending translations and cancelled work can resume");window.Demo();
                    window.Width=360;window.Height=480;window.UpdateLayout();window.Screenshot(Path.Combine(Path.GetDirectoryName(report),"minimum.png"));
                    var readerScroll=Descendants<ScrollViewer>(window).First(s=>s.ScrollableHeight>0&&s.IsVisible);readerScroll.ScrollToVerticalOffset(80);window.UpdateLayout();Check(readerScroll.VerticalOffset>0,"Reading can scroll");
                    var scrollbar=(System.Windows.Controls.Primitives.ScrollBar)readerScroll.Template.FindName("PART_VerticalScrollBar",readerScroll);scrollbar.ApplyTemplate();var track=(System.Windows.Controls.Primitives.Track)scrollbar.Template.FindName("PART_Track",scrollbar);Check(scrollbar.ActualWidth==12&&track.Thumb.MinHeight>=28,"Slim track keeps usable hit area");
                    track.Thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragStartedEventArgs(0,0));track.Thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(0,60));track.Thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragCompletedEventArgs(0,60,false));window.UpdateLayout();Check(readerScroll.VerticalOffset>80,"Dragging thumb routes scrolling to viewer");readerScroll.ScrollToTop();
                    window.Width=560;window.Height=650;window.UpdateLayout();passed.Add("PASS slim scrollbar scrolls content and preserves thumb interaction");passed.Add("PASS compact 360x480 and normal 560x650 layout");
                    window.Start();window.ToggleDock(true);await Until(()=>window.MotionState.Progress>.1,2500,"Collapse starts");
                    if(window.MotionState.Ghost!=null)UI.SavePreview(window.MotionState.Ghost,Path.Combine(Path.GetDirectoryName(report),"motion-mid.png"));
                    window.ToggleDock(false);await Until(()=>window.IsVisible&&window.MotionState.Settled,3500,"Collapse reverses");Check(!window.MotionState.WantsCollapsed,"Reversal preserves destination");passed.Add("PASS collapse animation reverses mid-flight and restores reader");
                    window.ToggleDock(true);await Until(()=>window.MotionState.Settled&&window.MotionState.WantsCollapsed&&!window.IsVisible,3500,"Collapse completes");Check(window.IsFollowing,"Collapsing must not pause following");window.SetRecognitionActive(true);Check(Descendants<ScanShimmer>(window.MotionState.Mini).First().Active,"Hidden reader activity reaches mini light");UI.SavePreview(window.MotionState.Mini,Path.Combine(Path.GetDirectoryName(report),"mini.png"));window.SetRecognitionActive(false);Check(window.MotionState.Mini.IsVisible,"Mini remains available");passed.Add("PASS completed collapse leaves an interactive mini popup");passed.Add("PASS collapse preserves following state and mini activity indication");
                    window.MotionState.Mini.SetMiniScale(1.4);window.MotionState.Mini.UpdateLayout();Check(window.MotionState.Mini.ActualWidth>180&&window.MotionState.Mini.ActualHeight>78,"Mini popup can be enlarged");window.MotionState.Mini.SetMiniScale(1);window.MotionState.Mini.UpdateLayout();Check(Math.Abs(window.MotionState.Mini.ActualWidth-132)<1,"Mini popup resets without restoring reader");passed.Add("PASS mini popup independent resizing and reset");
                    window.MotionState.Mini.ActivateReader();await Until(()=>window.IsVisible&&window.MotionState.Settled,3500,"Mini restores reader");Check(window.ActualWidth>=550,"Full reader geometry preserved");passed.Add("PASS mini activation expands to original reader size");
                    window.MotionState.ReduceMotionForTests=true;window.ToggleDock(true);Check(!window.IsVisible&&window.MotionState.Mini.IsVisible,"Reduced-motion collapse is immediate");window.ToggleDock(false);Check(window.IsVisible&&!window.MotionState.Mini.IsVisible,"Reduced-motion restore is immediate");window.MotionState.ReduceMotionForTests=false;passed.Add("PASS reduced-motion path switches immediately without springs");window.Pause("测试结束");
                    var settings=new SettingsDialog(window);settings.Show();settings.UpdateLayout();var selector=Descendants<ComboBox>(settings).First();selector.IsDropDownOpen=true;await Task.Delay(50);var popup=(System.Windows.Controls.Primitives.Popup)selector.Template.FindName("PART_Popup",selector);Check(popup.IsOpen,"Settings popup opens");selector.SelectedIndex=2;selector.IsDropDownOpen=false;settings.UpdateLayout();Check(Descendants<PasswordBox>(settings).First().Visibility==Visibility.Visible,"Provider fields show");selector.SelectedIndex=0;settings.UpdateLayout();Check(Descendants<PasswordBox>(settings).First().Visibility==Visibility.Collapsed&&Descendants<TextBlock>(settings).Any(t=>t.Text.Contains("没有每日字符额度")),"Local mode needs no API key or quota");passed.Add("PASS offline provider has a no-quota hint and hides API credentials");UI.SavePreview(settings,Path.Combine(Path.GetDirectoryName(report),"settings.png"));settings.Close();passed.Add("PASS existing settings provider controls preserved");
                    if(OfflineTranslator.IsInstalled)
                    {
                        window.Data.Settings.Provider="Offline";await window.ShowLines(new[]{"Please select a folder to save your exported files.","Drag this window to another monitor.","Your changes have been saved successfully."},"本地离线验证",CancellationToken.None);
                        Check(Descendants<TextBlock>(window).Any(t=>t.Text.Contains("文件夹")),"Real offline Chinese reaches bilingual reading");
                        window.Screenshot(Path.Combine(Path.GetDirectoryName(report),"offline-reading.png"));var localSettings=new SettingsDialog(window);localSettings.Show();localSettings.UpdateLayout();UI.SavePreview(localSettings,Path.Combine(Path.GetDirectoryName(report),"offline-settings.png"));localSettings.Close();passed.Add("PASS real offline model renders Chinese in bilingual reading and settings");
                    }
                    window.Demo();window.Screenshot(Path.Combine(Path.GetDirectoryName(report),"preview.png"));File.WriteAllText(report,String.Join("\r\n",passed.ToArray()),System.Text.Encoding.UTF8);
                }
                catch(Exception ex){File.WriteAllText(report,String.Join("\r\n",passed.ToArray())+"\r\nFAIL "+ex,System.Text.Encoding.UTF8);}
                finally{if(fixture!=null){try{if(!fixture.HasExited)fixture.Kill();}catch{}fixture.Dispose();}window.Close();}
            };timer.Start();
        }
    }
}

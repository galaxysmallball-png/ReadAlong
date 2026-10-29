using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ReadAlong
{
    public partial class MainWindow
    {
        TextBlock studioHeading, studioCaption, readingCount;
        Grid studioLegend;
        bool studioWide;
        Border drawer;
        Grid drawerDeck;
        StackPanel sidebarBook;
        UIElement sideOptions, sideInspector;
        TranslateTransform drawerSlide = new TranslateTransform();
        readonly SpringValue drawerSpring = new SpringValue();
        int sideMode;
        bool drawerWanted;
        internal bool ReduceDrawerMotionForTests;
        DateTime drawerTime;
        void BuildWarmLayout()
        {
            WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent; ResizeMode = ResizeMode.CanResizeWithGrip;
            var root = new Grid { ClipToBounds = true };
            Content = new Border { Margin = new Thickness(12), CornerRadius = new CornerRadius(18), Background = UI.B("#FAFFFD"), BorderBrush = UI.B("#C6E0D8"), BorderThickness = new Thickness(1), Child = root, Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 22, ShadowDepth = 5, Opacity = .13 } };
            var page = new Grid { Margin = new Thickness(22, 8, 22, 12) }; page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) }); page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(45) }); page.RowDefinitions.Add(new RowDefinition()); root.Children.Add(page);
            var head = new Grid { Background = Brushes.Transparent }; head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); head.ColumnDefinitions.Add(new ColumnDefinition()); head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var menu = UI.ChromeButton("\uE700", "打开侧栏", delegate { ShowSidebar(sideMode, !drawerWanted); }); AutomationName(menu, "SidebarToggle"); head.Children.Add(menu);
            var title = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(9,0,0,0) }; studioHeading = UI.Text("对照阅读", 14, UI.Ink); studioHeading.FontWeight = FontWeights.SemiBold; title.Children.Add(studioHeading); readingCount = UI.Text("", 10.5, UI.Muted); readingCount.Margin = new Thickness(10, 0, 0, 0); title.Children.Add(readingCount); Grid.SetColumn(title,1); head.Children.Add(title);
            var chrome = new StackPanel { Orientation = Orientation.Horizontal };
            chrome.Children.Add(ZoomButton("−","缩小文字 · Ctrl+-","ZoomOut",delegate{SetReadingZoom(readingZoom-.1);}));
            zoomReset=ZoomButton(Math.Round(readingZoom*100)+"%","恢复 100% · Ctrl+0","ZoomReset",delegate{SetReadingZoom(1);});chrome.Children.Add(zoomReset);
            chrome.Children.Add(ZoomButton("+","放大文字 · Ctrl++","ZoomIn",delegate{SetReadingZoom(readingZoom+.1);}));
            var shrink = UI.ChromeButton("\uE73F", "收成小浮窗 · Ctrl+Alt+M", delegate { ToggleDock(true); }); AutomationName(shrink,"CollapseWindow"); chrome.Children.Add(shrink); chrome.Children.Add(UI.ChromeButton("\uE8BB","退出界读",delegate { Close(); })); Grid.SetColumn(chrome,2); head.Children.Add(chrome);
            head.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { if (!UI.InsideButton(e.OriginalSource as DependencyObject)) DragMove(); }; page.Children.Add(new Border { CornerRadius=new CornerRadius(13), Background=new LinearGradientBrush(Color.FromArgb(225,210,253,240),Color.FromArgb(210,232,228,255),0), BorderBrush=UI.B("#B3FFFFFF"), BorderThickness=new Thickness(1), Child=head });
            var reading = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(3, 6, 0, 0), PanningMode = PanningMode.VerticalOnly }; At(page,reading,2);At(page,BuildMainActivity(),1);
            studioCaption = UI.Text("",11,UI.Muted); studioLegend = new Grid();
            var drawerGrid = new Grid { Margin = new Thickness(18,12,18,16) }; drawerGrid.RowDefinitions.Add(new RowDefinition {Height=new GridLength(38)}); drawerGrid.RowDefinitions.Add(new RowDefinition {Height=new GridLength(42)}); drawerGrid.RowDefinitions.Add(new RowDefinition());
            var top = new Grid(); top.ColumnDefinitions.Add(new ColumnDefinition()); top.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); var label=UI.Text("伴读侧栏",15,UI.Ink); label.FontWeight=FontWeights.SemiBold; top.Children.Add(label); var dismiss=UI.ChromeButton("\uE8BB","收起侧栏",delegate{ShowSidebar(sideMode,false);}); Grid.SetColumn(dismiss,1);top.Children.Add(dismiss);drawerGrid.Children.Add(top);
            var tabs = new Grid(); for(int i=0;i<3;i++) tabs.ColumnDefinitions.Add(new ColumnDefinition());
            readTab=UI.Button("伴读",false,delegate{ShowSidebar(0);}); readTab.Padding=new Thickness(4,7,4,7);tabs.Children.Add(readTab);
            wordsTab=UI.Button("生词本 0",false,delegate{ShowSidebar(1);});wordsTab.Padding=new Thickness(4,7,4,7);Grid.SetColumn(wordsTab,1);tabs.Children.Add(wordsTab);
            var inspect=UI.Button("取词",false,delegate{ShowSidebar(2);}); inspect.Padding=new Thickness(4,7,4,7);Grid.SetColumn(inspect,2);tabs.Children.Add(inspect);At(drawerGrid,tabs,1);
            drawerDeck=new Grid{Margin=new Thickness(0,12,0,0)};At(drawerGrid,drawerDeck,2);
            var options = new StackPanel(); targetLabel=UI.Text("等待开始伴读",13,UI.Ink);targetLabel.FontWeight=FontWeights.SemiBold;options.Children.Add(targetLabel);detail=UI.Text("开始后切换到英文应用。",11,UI.Muted);detail.LineHeight=20;detail.Margin=new Thickness(0,8,0,18);options.Children.Add(detail);
            follow=UI.Button("开始伴读",true,delegate{Toggle();});AutomationName(follow,"Follow");options.Children.Add(follow);
            var repeat=UI.Button("重新读取当前窗口",false,delegate{hash="";lastRead=DateTime.MinValue;if(!following)Start();});repeat.Margin=new Thickness(0,8,0,0);options.Children.Add(repeat);
            var paste=UI.Button("粘贴英文",false,delegate{Paste();});paste.Margin=new Thickness(0,8,0,18);options.Children.Add(paste);
            learn=new CheckBox{Content="先读英文，按句展开中文",Margin=new Thickness(0,2,0,18)};learn.Checked+=delegate{Render();};learn.Unchecked+=delegate{Render();};options.Children.Add(learn);
            var pin=UI.Button("切换窗口置顶",false,delegate{var p=Store.Parse<Preferences>(Store.Json(Data.Settings));p.Pinned=!p.Pinned;SavePreferences(p);});options.Children.Add(pin);
            var settings=UI.Button("翻译与偏好设置",false,delegate{new SettingsDialog(this).ShowDialog();});settings.Margin=new Thickness(0,8,0,18);options.Children.Add(settings);
            footer=UI.Text("Ctrl+Alt+T 暂停 / 继续\nCtrl+Alt+M 收缩 / 展开",10.5,UI.Muted);footer.LineHeight=20;options.Children.Add(footer);
            var privacy=UI.Text("只识别当前窗口，截图不上传。\n翻译文字发送给你选择的服务。",10.5,UI.Muted);privacy.LineHeight=20;privacy.Margin=new Thickness(0,16,0,0);options.Children.Add(privacy);
            sideOptions=new ScrollViewer{Content=options,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};drawerDeck.Children.Add(sideOptions);
            sidebarBook=new StackPanel();var bookScroll=new ScrollViewer{Content=sidebarBook,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};AutomationName(bookScroll,"SidebarBook");drawerDeck.Children.Add(bookScroll);
            sideInspector=MakeInspector();drawerDeck.Children.Add(sideInspector);
            drawer=new Border{Width=300,HorizontalAlignment=HorizontalAlignment.Right,Background=Brushes.Transparent,BorderBrush=UI.B("#BFDCD3"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(16),Margin=new Thickness(0,8,8,8),RenderTransform=drawerSlide,Opacity=0,Visibility=Visibility.Collapsed,Effect=new DropShadowEffect{BlurRadius=18,ShadowDepth=3,Direction=180,Opacity=.09}};var glassLayers = new Grid { ClipToBounds = true }; glassLayers.SizeChanged+=delegate { glassLayers.Clip=new RectangleGeometry(new Rect(0,0,glassLayers.ActualWidth,glassLayers.ActualHeight),15,15); };
            var backdrop = new System.Windows.Shapes.Rectangle { Effect = new BlurEffect { Radius=18, RenderingBias=RenderingBias.Performance } };
            var glassBrush = new VisualBrush(page) { ViewboxUnits=BrushMappingMode.Absolute, Stretch=Stretch.Fill, AlignmentX=AlignmentX.Right };
            backdrop.Fill=glassBrush;glassLayers.Children.Add(backdrop);
            glassLayers.Children.Add(new System.Windows.Shapes.Rectangle { Fill = UI.B("#CBFAFFFD") });
            glassLayers.Children.Add(new System.Windows.Shapes.Rectangle { Fill = new LinearGradientBrush(Color.FromArgb(75,191,248,231),Color.FromArgb(42,202,193,255),45), IsHitTestVisible=false });
            glassLayers.Children.Add(drawerGrid);drawer.Child=glassLayers;root.Children.Add(drawer);
            page.SizeChanged+=delegate{glassBrush.Viewbox=new Rect(Math.Max(0,page.ActualWidth-drawer.Width),0,Math.Max(1,drawer.Width),Math.Max(1,page.ActualHeight));};
            AttachDrawerDrag(top);
            SizeChanged+=delegate{readingCount.Visibility=ActualWidth<430?Visibility.Collapsed:Visibility.Visible;drawer.Width=Math.Min(300,Math.Max(250,ActualWidth-48));bool wide=ActualWidth>=690;if(wide!=studioWide){studioWide=wide;Render();}ApplyDrawer();};
            PreviewKeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape&&drawerWanted){ShowSidebar(sideMode,false);e.Handled=true;}};
            Closed+=delegate{CompositionTarget.Rendering-=DrawerFrame;if(dockMotion!=null)dockMotion.Dispose();};
            RenderSidebar();
        }
        internal void ShowSidebar(int mode, bool open = true)
        {
            sideMode=mode; wordsView=mode==1;drawerWanted=open;RenderSidebar();
            if(ReduceDrawerMotionForTests||!SystemParameters.ClientAreaAnimation){CompositionTarget.Rendering-=DrawerFrame;drawerSpring.Target=open?1:0;drawerSpring.Snap();ApplyDrawer();return;}
            drawerSpring.Target=open?1:0;drawer.Visibility=Visibility.Visible;drawerTime=DateTime.UtcNow;CompositionTarget.Rendering-=DrawerFrame;CompositionTarget.Rendering+=DrawerFrame;
        }
        void RenderSidebar()
        {
            if(drawerDeck==null)return;
            sideOptions.Visibility=sideMode==0?Visibility.Visible:Visibility.Collapsed;
            drawerDeck.Children[1].Visibility=sideMode==1?Visibility.Visible:Visibility.Collapsed;
            sideInspector.Visibility=sideMode==2?Visibility.Visible:Visibility.Collapsed;
            inspectorVisible=sideMode==2;
            readTab.Background=UI.B(sideMode==0?UI.Soft:UI.Paper);wordsTab.Background=UI.B(sideMode==1?UI.Soft:UI.Paper);wordsTab.Content="生词本 "+Data.Words.Count;
            if(sideMode==1){sidebarBook.Children.Clear();RenderWords();}
        }
        void AttachDrawerDrag(FrameworkElement handle)
        {
            bool held=false,moved=false;double startX=0,startP=0,lastP=0;DateTime lastMove=DateTime.UtcNow;
            handle.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(UI.InsideButton(e.OriginalSource as DependencyObject))return;held=true;moved=false;startX=handle.PointToScreen(e.GetPosition(handle)).X;startP=lastP=drawerSpring.Value;lastMove=DateTime.UtcNow;CompositionTarget.Rendering-=DrawerFrame;handle.CaptureMouse();e.Handled=true;};
            handle.MouseMove+=delegate(object sender,MouseEventArgs e){if(!held)return;double delta=handle.PointToScreen(e.GetPosition(handle)).X-startX;if(Math.Abs(delta)>6)moved=true;if(!moved)return;double value=Math.Max(0,Math.Min(1,startP-delta/drawer.Width));var now=DateTime.UtcNow;double dt=(now-lastMove).TotalSeconds;if(dt>.002)drawerSpring.Velocity=(value-lastP)/dt;drawerSpring.Value=value;lastP=value;lastMove=now;ApplyDrawer();};
            handle.MouseLeftButtonUp+=delegate{if(!held)return;held=false;handle.ReleaseMouseCapture();bool open=!moved|| (Math.Abs(drawerSpring.Velocity)>.6?drawerSpring.Velocity>0:drawerSpring.Value>.5);ShowSidebar(sideMode,open);};
            handle.LostMouseCapture+=delegate{if(held){held=false;ShowSidebar(sideMode,drawerSpring.Value>.5);}};
        }
        void DrawerFrame(object sender,EventArgs e)
        {var now=DateTime.UtcNow;drawerSpring.Step(Math.Min(.04,(now-drawerTime).TotalSeconds),.28);drawerTime=now;ApplyDrawer();if(drawerSpring.Settled){drawerSpring.Snap();ApplyDrawer();CompositionTarget.Rendering-=DrawerFrame;}}
        void ApplyDrawer()
        {if(drawer==null)return;double p=Math.Max(0,Math.Min(1,drawerSpring.Value));drawerSlide.X=(1-p)*drawer.Width;drawer.Opacity=p;drawer.IsHitTestVisible=drawerWanted;drawer.Visibility=p<.001&&!drawerWanted?Visibility.Collapsed:Visibility.Visible;}
    }
}

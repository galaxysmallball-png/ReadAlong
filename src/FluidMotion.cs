using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;

namespace ReadAlong
{
    public class SpringValue
    {
        public double Value, Velocity, Target;
        public bool Settled { get { return Math.Abs(Value-Target)<.0008&&Math.Abs(Velocity)<.008; } }
        public void Step(double dt,double response)
        {
            double w=2*Math.PI/response, offset=Value-Target, carry=Velocity+w*offset, decay=Math.Exp(-w*dt);
            Value=Target+(offset+carry*dt)*decay;Velocity=(Velocity-w*carry*dt)*decay;
        }
        public void Snap(){Value=Target;Velocity=0;}
    }
    // Draw only an in-memory snapshot of this application, never desktop pixels.
    public class FunnelVisual : FrameworkElement
    {
        readonly List<BitmapSource> strips=new List<BitmapSource>();
        public Rect SourceRect, DockRect;
        public double Progress;
        public FunnelVisual(BitmapSource source)
        {
            const int count=192;
            for(int i=0;i<count;i++)
            {
                int y=i*source.PixelHeight/count, end=(i+1)*source.PixelHeight/count;
                var crop=new CroppedBitmap(source,new Int32Rect(0,y,source.PixelWidth,Math.Max(1,end-y)));crop.Freeze();strips.Add(crop);
            }
            IsHitTestVisible=false;
        }
        protected override void OnRender(DrawingContext dc)
        {
            double p=Math.Max(0,Math.Min(1,Progress));dc.PushOpacity(1-.98*Math.Pow(p,5));
            for(int i=0;i<strips.Count;i++)
            {
                double t=(i+.5)/strips.Count;
                double local=Math.Max(0,Math.Min(1,p+.36*Math.Sin(Math.PI*p)*(t-.5)));
                double q=1-Math.Pow(1-local,1.35), width=SourceRect.Width*(1-q)+DockRect.Width*q;
                double center=(SourceRect.X+SourceRect.Width/2)*(1-local)+(DockRect.X+DockRect.Width/2)*local;
                double height=SourceRect.Height*(1-p)+DockRect.Height*p;
                double top=SourceRect.Y*(1-p)+DockRect.Y*p+i*height/strips.Count;
                dc.DrawImage(strips[i],new Rect(center-width/2,top,width,height/strips.Count+.7));
            }
            dc.Pop();
        }
    }
    public class MiniReader : Window
    {
        readonly TextBlock label;
        readonly ScanShimmer activityLight = new ScanShimmer();
        bool dragging, pressed;
        Point grab, origin;
        readonly SpringValue snap = new SpringValue();
        DateTime snapTime;
        void SnapFrame(object sender,EventArgs e)
        {var now=DateTime.UtcNow;snap.Step(Math.Min(.04,(now-snapTime).TotalSeconds),.3);snapTime=now;Left=snap.Value;if(snap.Settled){snap.Snap();Left=snap.Value;CompositionTarget.Rendering-=SnapFrame;}}
        public Action ActivateReader;
        public MiniReader(Action toggle,Action pause,Action exit)
        {
            Width=132;Height=58;WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.CanResizeWithGrip;MinWidth=110;MinHeight=52;MaxWidth=240;MaxHeight=110;Topmost=true;ShowInTaskbar=false;ShowActivated=false;WindowEdges.Enable(this);
            var row=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            var dot=new System.Windows.Shapes.Ellipse{Width=7,Height=7,Fill=UI.B(UI.Blue),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,9,0)};row.Children.Add(dot);label=UI.Text("界读",12,UI.Ink);row.Children.Add(label);
            var surface=new Grid();surface.Children.Add(row);activityLight.VerticalAlignment=VerticalAlignment.Bottom;activityLight.Margin=new Thickness(18,0,18,10);surface.Children.Add(activityLight);
            Content=new Border{Margin=new Thickness(8),CornerRadius=new CornerRadius(21),Background=new LinearGradientBrush(Color.FromArgb(238,222,255,244),Color.FromArgb(238,235,232,255),20),BorderBrush=UI.B("#B6DCCF"),BorderThickness=new Thickness(1),Child=surface,Effect=new DropShadowEffect{BlurRadius=13,ShadowDepth=2,Opacity=.16}};
            ToolTip="单击展开 · 拖动移动 · 右键更多";
            var menu=new ContextMenu();Add(menu,"展开对照阅读",toggle);Add(menu,"放大浮窗",delegate{SetMiniScale(Width/132+.15);});Add(menu,"缩小浮窗",delegate{SetMiniScale(Width/132-.15);});Add(menu,"恢复浮窗大小",delegate{SetMiniScale(1);});Add(menu,"暂停 / 继续伴读",pause);Add(menu,"退出界读",exit);ContextMenu=menu;
            ActivateReader=toggle; PreviewMouseWheel+=delegate(object sender,MouseWheelEventArgs e){if((Keyboard.Modifiers&ModifierKeys.Control)!=0){SetMiniScale(Width/132+(e.Delta>0?.1:-.1));e.Handled=true;}}; SizeChanged+=delegate{label.FontSize=Math.Max(11,Math.Min(16,12*ActualWidth/132));};
            MouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){CompositionTarget.Rendering-=SnapFrame;grab=PointToScreen(e.GetPosition(this));origin=new Point(Left,Top);pressed=true;dragging=false;CaptureMouse();e.Handled=true;};
            MouseMove+=delegate(object s,MouseEventArgs e){if(!pressed)return;Point at=PointToScreen(e.GetPosition(this));Vector delta=at-grab;if(delta.Length>6)dragging=true;if(dragging){Left=origin.X+delta.X;Top=origin.Y+delta.Y;}};
            MouseLeftButtonUp+=delegate{if(!pressed)return;pressed=false;ReleaseMouseCapture();if(!dragging){toggle();return;}var area=SystemParameters.WorkArea;double target=Left;if(Left<area.Left+24)target=area.Left+8;else if(Left+Width>area.Right-24)target=area.Right-Width-8;Top=Math.Max(area.Top,Math.Min(area.Bottom-Height,Top));if(SystemParameters.ClientAreaAnimation){snap.Value=Left;snap.Target=target;snap.Velocity=0;snapTime=DateTime.UtcNow;CompositionTarget.Rendering+=SnapFrame;}else Left=target;};
            LostMouseCapture+=delegate{pressed=false;};
            Closed+=delegate{CompositionTarget.Rendering-=SnapFrame;};
        }
        internal void SetMiniScale(double scale){scale=Math.Max(.85,Math.Min(1.8,scale));Width=132*scale;Height=Math.Max(52,58*scale);var area=SystemParameters.WorkArea;Left=Math.Max(area.Left,Math.Min(area.Right-Width-8,Left));Top=Math.Max(area.Top,Math.Min(area.Bottom-Height,Top));}
        static void Add(ContextMenu menu,string text,Action action){var item=new MenuItem{Header=text};item.Click+=delegate{action();};menu.Items.Add(item);}
        public void SetActivity(bool active){activityLight.SetActive(active);}
        public void UpdateCount(int count,bool running){label.Text=(running?"伴读":"界读")+(count>0?" · "+count+"句":"");}
    }
    public class DockMotion : IDisposable
    {
        readonly MainWindow owner;
        internal readonly MiniReader Mini;
        internal Window Ghost;
        FunnelVisual visual;
        BitmapSource snapshot;
        readonly SpringValue progress=new SpringValue();
        Rect source, bounds;
        DateTime last;
        bool disposed;
        internal bool ReduceMotionForTests;
        public bool WantsCollapsed { get; private set; }
        public double Progress { get { return progress.Value; } }
        public bool Settled { get { return progress.Settled; } }
        public DockMotion(MainWindow owner,Action pause)
        {
            this.owner=owner;Mini=new MiniReader(delegate{Set(!WantsCollapsed);},pause,delegate{owner.Close();});var area=SystemParameters.WorkArea;Mini.Left=area.Right-Mini.Width-16;Mini.Top=area.Top+area.Height*.58;
        }
        public void Set(bool collapse)
        {
            if(disposed)return;WantsCollapsed=collapse;
            if(!collapse&&Ghost==null&&owner.IsVisible&&progress.Value<.001)return;
            if(ReduceMotionForTests||!SystemParameters.ClientAreaAnimation){progress.Target=collapse?1:0;progress.Snap();if(collapse){Mini.Show();owner.Hide();}else{owner.Show();Mini.Hide();owner.Activate();}return;}
            if(Ghost==null)
            {
                if(collapse){owner.UpdateLayout();source=new Rect(owner.Left,owner.Top,owner.ActualWidth,owner.ActualHeight);}
                if(collapse||snapshot==null){owner.UpdateLayout();var bitmap=new RenderTargetBitmap((int)Math.Ceiling(owner.ActualWidth),(int)Math.Ceiling(owner.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(owner);bitmap.Freeze();snapshot=bitmap;}
                Rect target=new Rect(Mini.Left,Mini.Top,Mini.Width,Mini.Height);bounds=Rect.Union(source,target);bounds.Inflate(18,18);
                visual=new FunnelVisual(snapshot);Ghost=new Window{WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,Left=bounds.Left,Top=bounds.Top,Width=bounds.Width,Height=bounds.Height,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Topmost=true,ShowActivated=false,IsHitTestVisible=false,Content=visual};
                Ghost.SourceInitialized+=delegate{var h=HwndSource.FromHwnd(new WindowInteropHelper(Ghost).Handle);h.AddHook(delegate(IntPtr w,int m,IntPtr a,IntPtr b,ref bool handled){if(m==0x84){handled=true;return new IntPtr(-1);}return IntPtr.Zero;});};
                Ghost.Show();Mini.Show();owner.Hide();
            }
            progress.Target=collapse?1:0;last=DateTime.UtcNow;
            CompositionTarget.Rendering-=Frame;CompositionTarget.Rendering+=Frame;Draw();
        }
        void Frame(object sender,EventArgs e)
        {
            if(disposed)return;var now=DateTime.UtcNow;progress.Step(Math.Min(.04,(now-last).TotalSeconds),.4);last=now;Draw();
            if(!progress.Settled)return;progress.Snap();Draw();CompositionTarget.Rendering-=Frame;
            if(Ghost!=null){Ghost.Close();Ghost=null;}visual=null;
            if(WantsCollapsed){owner.Hide();Mini.Opacity=1;Mini.Show();}else{owner.Show();owner.Opacity=1;Mini.Hide();owner.Activate();}
        }
        void Draw()
        {
            if(visual==null)return;
            visual.SourceRect=new Rect(source.X-bounds.X,source.Y-bounds.Y,source.Width,source.Height);
            visual.DockRect=new Rect(Mini.Left-bounds.X,Mini.Top-bounds.Y,Mini.Width,Mini.Height);visual.Progress=progress.Value;visual.InvalidateVisual();Mini.Opacity=.35+.65*Math.Max(0,Math.Min(1,progress.Value));
        }
        public void Dispose(){if(disposed)return;disposed=true;CompositionTarget.Rendering-=Frame;if(Ghost!=null){Ghost.Close();Ghost=null;}Mini.Close();}
    }
    public partial class MainWindow
    {
        DockMotion dockMotion;
        internal void ToggleDock(bool collapse)
        {if(dockMotion==null)dockMotion=new DockMotion(this,Toggle);dockMotion.Mini.UpdateCount(segments.Count,following);dockMotion.Mini.SetActivity(recognizing||translating);dockMotion.Set(collapse);}
        void RefreshMini(){if(dockMotion!=null)dockMotion.Mini.UpdateCount(segments.Count,following);}
    }
}

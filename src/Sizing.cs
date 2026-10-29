using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ReadAlong
{
    public static class WindowEdges
    {
        public static int Code(double x,double y,double width,double height,double edge)
        {
            if(x<0||y<0||x>width||y>height)return 0;
            bool left=x<edge,right=x>width-edge,top=y<edge,bottom=y>height-edge;
            if(top&&left)return 13;if(top&&right)return 14;if(bottom&&left)return 16;if(bottom&&right)return 17;
            if(left)return 10;if(right)return 11;if(top)return 12;if(bottom)return 15;return 0;
        }
        public static void Enable(Window window)
        {
            window.SourceInitialized+=delegate
            {
                var source=HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                source.AddHook(delegate(IntPtr h,int message,IntPtr w,IntPtr l,ref bool handled)
                {
                    if(message!=0x84||window.WindowState!=WindowState.Normal||window.ResizeMode==ResizeMode.NoResize)return IntPtr.Zero;
                    Native.RECT rect;if(!Native.GetWindowRect(h,out rect))return IntPtr.Zero;
                    long packed=l.ToInt64();int x=(short)(packed&65535),y=(short)((packed>>16)&65535);
                    double scale=source.CompositionTarget.TransformToDevice.M11;
                    int code=Code(x-rect.Left,y-rect.Top,rect.Right-rect.Left,rect.Bottom-rect.Top,13*scale);
                    if(code==0)return IntPtr.Zero;handled=true;return new IntPtr(code);
                });
            };
        }
    }
    public partial class MainWindow
    {
        double readingZoom=1;
        Button zoomReset;
        DispatcherTimer zoomSaveTimer;
        internal double ReadingZoom { get { return readingZoom; } }
        static double ValidZoom(double value){return Double.IsNaN(value)||Double.IsInfinity(value)?1:Math.Max(.7,Math.Min(2,Math.Round(value,2)));}
        void SetupSizing()
        {
            readingZoom=ValidZoom(Data.Settings.ReadingScale);WindowEdges.Enable(this);
            PreviewMouseWheel+=delegate(object s,MouseWheelEventArgs e){if((Keyboard.Modifiers&ModifierKeys.Control)!=0){SetReadingZoom(readingZoom+(e.Delta>0?.1:-.1));e.Handled=true;}};
            PreviewKeyDown+=delegate(object s,KeyEventArgs e)
            {
                if((Keyboard.Modifiers&ModifierKeys.Control)==0)return;
                if(e.Key==Key.OemPlus||e.Key==Key.Add){SetReadingZoom(readingZoom+.1);e.Handled=true;}
                else if(e.Key==Key.OemMinus||e.Key==Key.Subtract){SetReadingZoom(readingZoom-.1);e.Handled=true;}
                else if(e.Key==Key.D0||e.Key==Key.NumPad0){SetReadingZoom(1);e.Handled=true;}
            };
            Closed+=delegate{FlushZoom();};
        }
        internal void SetReadingZoom(double value)
        {
            readingZoom=ValidZoom(value);Data.Settings.ReadingScale=readingZoom;if(zoomReset!=null)zoomReset.Content=Math.Round(readingZoom*100)+"%";Render();
            if(zoomSaveTimer==null){zoomSaveTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(450)};zoomSaveTimer.Tick+=delegate{FlushZoom();};}
            zoomSaveTimer.Stop();zoomSaveTimer.Start();
        }
        internal void FlushZoom()
        {
            if(zoomSaveTimer==null||!zoomSaveTimer.IsEnabled)return;zoomSaveTimer.Stop();
            try{store.Save(Data);}catch(Exception ex){if(footer!=null){footer.Text="缩放暂未保存，下次打开可能恢复默认。";footer.ToolTip=ex.Message;}}
        }
        Button ZoomButton(string text,string tip,string id,RoutedEventHandler action)
        {
            var button=UI.Button(text,false,action);button.Padding=new Thickness(0);button.Width=id=="ZoomReset"?42:25;button.Height=30;button.FontSize=11;button.BorderThickness=new Thickness(0);button.Background=System.Windows.Media.Brushes.Transparent;button.ToolTip=tip;AutomationName(button,id);return button;
        }
    }
}

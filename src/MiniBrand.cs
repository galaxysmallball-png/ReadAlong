using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReadAlong
{
    // Small, code-drawn reading mark: crisp at every window scale.
    public sealed class ReaderMark : FrameworkElement
    {
        static readonly Geometry pages=Geometry.Parse("M7,10 C10,8.5 12.6,9.5 16,11.2 C19.4,9.5 22,8.5 25,10 L25,23 C21.6,21.7 19.3,22.3 16,24 C12.7,22.3 10.4,21.7 7,23 Z M16,11.2 L16,24");
        static readonly Geometry lines=Geometry.Parse("M10,13.5 L13,14.2 M10,16.8 L13,17.5 M19,14.2 L22,13.5 M19,17.5 L22,16.8");
        readonly LinearGradientBrush fill=new LinearGradientBrush(Color.FromRgb(24,160,145),Color.FromRgb(0,102,102),65);
        bool running;
        public ReaderMark(){Width=30;Height=30;IsHitTestVisible=false;fill.Freeze();}
        public void SetRunning(bool value){if(running==value)return;running=value;InvalidateVisual();}
        protected override void OnRender(DrawingContext dc)
        {
            dc.PushTransform(new ScaleTransform(ActualWidth/32,ActualHeight/32));
            dc.DrawRoundedRectangle(fill,new Pen(UI.B("#BBFFFFFF"),.8),new Rect(1,1,30,30),10,10);
            dc.DrawRoundedRectangle(null,new Pen(UI.B("#30FFFFFF"),.6),new Rect(2.2,2.2,27.6,27.6),8.8,8.8);
            var stroke=new Pen(UI.B("#F4FFFC"),1.15){LineJoin=PenLineJoin.Round,StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            dc.DrawGeometry(UI.B("#0FFFFFFF"),stroke,pages);
            dc.DrawGeometry(null,new Pen(UI.B("#A5F5E4"),.9){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},lines);
            dc.DrawEllipse(UI.B(running?"#C8F3A8":"#A5D2C9"),new Pen(UI.B("#FAFFFD"),1),new Point(27.5,27.5),2.5,2.5);
            dc.Pop();
        }
    }
    public static class MiniPreview
    {
        // Render only authored mini-window states, never the desktop.
        public static void Save(string path)
        {
            var mini=new MiniReader(delegate{},delegate{},delegate{});mini.Show();mini.UpdateLayout();
            try
            {
                var stage=new DrawingVisual();using(var dc=stage.RenderOpen())
                {
                    dc.DrawRectangle(UI.B("#EFF3F0"),null,new Rect(0,0,600,120));
                    for(int i=0;i<3;i++)
                    {
                        mini.UpdateCount(i==0?0:12,i>0);mini.SetActivity(i==2);
                        foreach(var light in Tests.Descendants<ScanShimmer>(mini))light.FixedPhase=.48;
                        mini.UpdateLayout();
                        var shot=new RenderTargetBitmap(264,116,192,192,PixelFormats.Pbgra32);shot.Render(mini);
                        dc.DrawImage(shot,new Rect(24+i*190,22,174,76.5));
                    }
                }
                var image=new RenderTargetBitmap(1200,240,192,192,PixelFormats.Pbgra32);image.Render(stage);
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));using(var stream=File.Create(path))encoder.Save(stream);
            }
            finally{mini.Close();}
        }
    }
}

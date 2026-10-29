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
        static readonly Geometry rear=Geometry.Parse("M11.8,5.1 L25.6,7.5 Q27.3,7.8 26.8,9.8 L23.6,26 Q17.7,22.7 10.2,24.6 Z");
        static readonly Geometry left=Geometry.Parse("M4.8,8 Q10.5,6.8 16.4,11.2 L14.5,27 Q8.9,23.5 3,25 Z");
        static readonly Geometry turning=Geometry.Parse("M16.4,11.2 Q20.7,6.4 26.2,4.5 L23,20.7 Q18.4,22.3 14.5,27 Z");
        static readonly Geometry crease=Geometry.Parse("M16.4,11.4 L14.7,25.3 M19.1,12.5 Q21,10.4 23,9.4");
        readonly LinearGradientBrush ink=new LinearGradientBrush(Color.FromRgb(37,172,153),Color.FromRgb(0,105,103),70);
        readonly LinearGradientBrush paper=new LinearGradientBrush(Color.FromRgb(248,255,252),Color.FromRgb(203,242,226),90);
        bool running;
        public ReaderMark(){Width=30;Height=30;IsHitTestVisible=false;ink.Freeze();paper.Freeze();}
        public void SetRunning(bool value){if(running==value)return;running=value;InvalidateVisual();}
        protected override void OnRender(DrawingContext dc)
        {
            dc.PushTransform(new ScaleTransform(ActualWidth/32,ActualHeight/32));
            dc.PushTransform(new TranslateTransform(0,.8));dc.DrawGeometry(UI.B("#172D6860"),null,rear);dc.Pop();
            dc.DrawGeometry(UI.B("#BFC2EA"),null,rear);
            dc.DrawGeometry(ink,null,left);
            dc.DrawGeometry(paper,new Pen(UI.B("#83B8A9"),.65){LineJoin=PenLineJoin.Round},turning);
            var stroke=new Pen(UI.B(running?"#147C70":"#4A9487"),.85){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            dc.DrawGeometry(null,stroke,crease);
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

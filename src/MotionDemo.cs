using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReadAlong
{
    public static class MotionDemo
    {
        public static void SaveScan(MainWindow window,string path)
        {
            window.Start();window.SetRecognitionActive(true);window.UpdateLayout();
            var light=Tests.Descendants<ScanShimmer>(window).First();light.FixedPhase=0;
            int width=(int)window.ActualWidth,height=(int)window.ActualHeight;var encoder=new GifBitmapEncoder();
            for(int i=0;i<40;i++)
            {
                light.FixedPhase=i/40.0;light.InvalidateVisual();window.UpdateLayout();
                var stage=new DrawingVisual();using(var dc=stage.RenderOpen())dc.DrawRectangle(UI.B("#E6EAE5"),null,new Rect(0,0,width,height));
                var frame=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);frame.Render(stage);frame.Render(window);
                var palette=new BitmapPalette(frame,256);var indexed=new FormatConvertedBitmap(frame,PixelFormats.Indexed8,palette,0);
                var meta=new BitmapMetadata("gif");meta.SetQuery("/grctlext/Delay",(ushort)3);meta.SetQuery("/grctlext/Disposal",(byte)1);
                encoder.Frames.Add(BitmapFrame.Create(indexed,null,meta,null));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));using(var output=File.Create(path))encoder.Save(output);
            window.Pause("预览完成");
        }
        // An authored animation preview made by the same renderer as the application.
        public static void Save(MainWindow window,string path)
        {
            window.UpdateLayout();var shot=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);shot.Render(window);shot.Freeze();
            const int width=800,height=640;
            var funnel=new FunnelVisual(shot){Width=width,Height=height,SourceRect=new Rect(24,24,window.ActualWidth*.85,window.ActualHeight*.85),DockRect=new Rect(642,530,132,58)};
            funnel.Measure(new Size(width,height));funnel.Arrange(new Rect(0,0,width,height));
            var encoder=new GifBitmapEncoder();var spring=new SpringValue();
            for(int i=0;i<108;i++)
            {
                if(i==15)spring.Target=1;if(i==62)spring.Target=0;
                spring.Step(1.0/30,.4);if(spring.Settled)spring.Snap();funnel.Progress=spring.Value;funnel.InvalidateVisual();funnel.UpdateLayout();
                var stage=new DrawingVisual();using(var dc=stage.RenderOpen())
                {
                    dc.DrawRectangle(UI.B("#E6EAE5"),null,new Rect(0,0,width,height));
                    dc.DrawRoundedRectangle(UI.B("#FAFCF9"),new Pen(UI.B("#D3DBCE"),1),new Rect(650,538,116,42),21,21);
                    dc.DrawEllipse(UI.B(UI.Blue),null,new Point(668,559),3,3);
                    var caption=new FormattedText("界读 · 4句",CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),12,UI.B(UI.Ink),1.0);dc.DrawText(caption,new Point(680,550));
                }
                var frame=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);frame.Render(stage);frame.Render(funnel);
                var palette=new BitmapPalette(frame,256);var indexed=new FormatConvertedBitmap(frame,PixelFormats.Indexed8,palette,0);
                var meta=new BitmapMetadata("gif");meta.SetQuery("/grctlext/Delay",(ushort)3);meta.SetQuery("/grctlext/Disposal",(byte)1);
                encoder.Frames.Add(BitmapFrame.Create(indexed,null,meta,null));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));using(var output=File.Create(path))encoder.Save(output);
        }
    }
}

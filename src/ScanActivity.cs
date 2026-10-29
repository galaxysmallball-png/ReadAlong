using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReadAlong
{
    public class ScanShimmer : FrameworkElement
    {
        bool active, subscribed;
        DateTime start;
        internal double FixedPhase = -1;
        public bool Active { get { return active; } }
        public ScanShimmer()
        {
            Height=3;IsHitTestVisible=false;Visibility=Visibility.Hidden;
            IsVisibleChanged+=delegate{Reconcile();};Unloaded+=delegate{Unsubscribe();};
        }
        public void SetActive(bool value)
        {
            if(value&&!active)start=DateTime.UtcNow;active=value;Visibility=value?Visibility.Visible:Visibility.Hidden;Reconcile();InvalidateVisual();
        }
        void Reconcile()
        {
            bool should=active&&IsVisible&&SystemParameters.ClientAreaAnimation&&FixedPhase<0;
            if(should&&!subscribed){CompositionTarget.Rendering+=Frame;subscribed=true;}else if(!should)Unsubscribe();
        }
        void Unsubscribe(){if(subscribed){CompositionTarget.Rendering-=Frame;subscribed=false;}}
        void Frame(object sender,EventArgs e){InvalidateVisual();}
        protected override void OnRender(DrawingContext dc)
        {
            if(!active||ActualWidth<=0)return;
            var bounds=new Rect(0,0,ActualWidth,ActualHeight);dc.DrawRoundedRectangle(UI.B("#DAF2E9"),null,bounds,2,2);
            double p=FixedPhase>=0?FixedPhase:SystemParameters.ClientAreaAnimation?((DateTime.UtcNow-start).TotalSeconds/1.3)%1:.45;
            double x=-.7+2.1*p;
            var band=new LinearGradientBrush{StartPoint=new Point(x,0),EndPoint=new Point(x+.7,0),MappingMode=BrushMappingMode.RelativeToBoundingBox};
            band.GradientStops.Add(new GradientStop(Color.FromArgb(0,0,160,146),0));band.GradientStops.Add(new GradientStop(Color.FromRgb(28,192,164),.36));band.GradientStops.Add(new GradientStop(Color.FromRgb(221,255,244),.5));band.GradientStops.Add(new GradientStop(Color.FromRgb(153,130,236),.65));band.GradientStops.Add(new GradientStop(Color.FromArgb(0,153,130,236),1));
            dc.DrawRoundedRectangle(band,null,bounds,2,2);
        }
    }
    public partial class MainWindow
    {
        Button mainFollow;
        TextBlock activityCaption;
        ScanShimmer recognitionLight;
        bool recognizing, translating;
        internal bool SuppressAutomaticPolling;
        internal bool IsFollowing { get { return following; } }
        internal bool ActivityLit { get { return recognitionLight!=null&&recognitionLight.Active; } }
        internal void SetRecognitionActive(bool active){recognizing=active;UpdateActivityUI();}
        void SetTranslationActive(bool active){translating=active;UpdateActivityUI();}
        void ResetActivity(){recognizing=false;translating=false;UpdateActivityUI();}
        void UpdateActivityUI()
        {
            bool busy=recognizing||translating;
            if(mainFollow!=null){mainFollow.Content=following?"暂停伴读":"开始伴读";mainFollow.Background=UI.B(following?"#DDF6EE":UI.Blue);mainFollow.Foreground=UI.B(following?UI.Blue:"#FFFFFF");}
            if(activityCaption!=null)activityCaption.Text=recognizing?"正在识别当前窗口…":translating?"正在逐句翻译…":following?"伴读中 · 等待界面变化":"已暂停";
            if(recognitionLight!=null)recognitionLight.SetActive(busy);
            if(dockMotion!=null)dockMotion.Mini.SetActivity(busy);
        }
        UIElement BuildMainActivity()
        {
            var section=new Grid{Margin=new Thickness(4,5,3,3)};section.RowDefinitions.Add(new RowDefinition());section.RowDefinitions.Add(new RowDefinition{Height=new GridLength(7)});
            var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});row.ColumnDefinitions.Add(new ColumnDefinition());
            mainFollow=UI.Button("开始伴读",true,delegate{Toggle();});mainFollow.Width=94;mainFollow.Height=29;mainFollow.Padding=new Thickness(9,4,9,4);mainFollow.FontSize=11.5;AutomationName(mainFollow,"MainFollow");row.Children.Add(mainFollow);
            activityCaption=UI.Text("已暂停",11,UI.Muted);AutomationName(activityCaption,"ActivityCaption");activityCaption.Margin=new Thickness(13,0,0,0);activityCaption.TextWrapping=TextWrapping.NoWrap;activityCaption.TextTrimming=TextTrimming.CharacterEllipsis;Grid.SetColumn(activityCaption,1);row.Children.Add(activityCaption);section.Children.Add(row);
            recognitionLight=new ScanShimmer{Margin=new Thickness(0,4,0,0)};AutomationName(recognitionLight,"RecognitionLight");Grid.SetRow(recognitionLight,1);section.Children.Add(recognitionLight);return section;
        }
    }
}

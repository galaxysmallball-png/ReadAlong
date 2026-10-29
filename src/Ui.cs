using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace ReadAlong
{
    public static class UI
    {
        public const string Ink = "#272D27", Blue = "#007C78", Muted = "#566F69", Back = "#F6F7F3", Paper = "#FAFFFD", Rule = "#E6E0D5", Soft = "#E4F5EE", Highlight = "#E0F29B";
        static readonly Dictionary<string,Brush> brushes = new Dictionary<string,Brush>();
        public static Brush B(string hex) { Brush value; if (!brushes.TryGetValue(hex, out value)) { value = (Brush)new BrushConverter().ConvertFromString(hex); value.Freeze(); brushes[hex] = value; } return value; }
        public static TextBlock Text(string text, double size, string color)
        { return new TextBlock { Text = text, FontSize = size, Foreground = B(color), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center }; }
        public static Button Button(string text, bool primary, RoutedEventHandler action)
        {
            var button = new Button { Content = text, FontSize = 12, FontWeight = FontWeights.Medium, Background = B(primary ? Blue : Paper), Foreground = B(primary ? "#FFFFFF" : Ink), Padding = new Thickness(14, 10, 14, 10), BorderBrush = B(Rule), BorderThickness = new Thickness(primary ? 0 : 1), Cursor = System.Windows.Input.Cursors.Hand };
            if (action != null) button.Click += action; return button;
        }
        public static void Install(Application app)
        {
            app.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse(@"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
 <Style TargetType='Button'>
  <Setter Property='FocusVisualStyle'><Setter.Value><Style TargetType='Control'><Setter Property='Template'><Setter.Value><ControlTemplate><Rectangle Margin='-3' Stroke='#007C78' StrokeThickness='1.5' StrokeDashArray='2 2' RadiusX='8' RadiusY='8'/></ControlTemplate></Setter.Value></Setter></Style></Setter.Value></Setter>
  <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border x:Name='Surface' CornerRadius='8' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Surface' Property='Opacity' Value='0.82'/></Trigger><Trigger Property='IsPressed' Value='True'><Setter TargetName='Surface' Property='Opacity' Value='0.66'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter TargetName='Surface' Property='Opacity' Value='0.4'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
 </Style>
 <Style TargetType='TextBox'><Setter Property='Padding' Value='11'/><Setter Property='BorderBrush' Value='#DCD5C7'/><Setter Property='Foreground' Value='#353A31'/><Setter Property='FontSize' Value='13'/><Setter Property='Background' Value='#EEF7F3'/><Setter Property='SelectionBrush' Value='#E8D9A5'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='TextBox'><Border x:Name='Frame' CornerRadius='8' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1'><ScrollViewer x:Name='PART_ContentHost'/></Border><ControlTemplate.Triggers><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Frame' Property='BorderBrush' Value='#007C78'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
 <Style TargetType='PasswordBox'><Setter Property='Background' Value='#EEF7F3'/><Setter Property='Foreground' Value='#353A31'/><Setter Property='BorderBrush' Value='#DCD5C7'/><Setter Property='FontSize' Value='13'/></Style>
 <Style TargetType='ComboBoxItem'><Setter Property='Padding' Value='12,10'/><Setter Property='Foreground' Value='#353A31'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBoxItem'><Border x:Name='Row' Padding='{TemplateBinding Padding}' CornerRadius='5' Background='Transparent'><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Setter TargetName='Row' Property='Background' Value='#EDE8DF'/></Trigger><Trigger Property='IsSelected' Value='True'><Setter TargetName='Row' Property='Background' Value='#D9F2E9'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
 <Style TargetType='ComboBox'>
  <Setter Property='FontSize' Value='13'/><Setter Property='MinHeight' Value='40'/><Setter Property='Background' Value='#FAFFFD'/><Setter Property='Foreground' Value='#353A31'/><Setter Property='BorderBrush' Value='#DCD5C7'/>
  <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBox'><Grid>
   <ToggleButton x:Name='Toggle' Focusable='False' ClickMode='Press' Background='{TemplateBinding Background}' IsChecked='{Binding IsDropDownOpen,Mode=TwoWay,RelativeSource={RelativeSource TemplatedParent}}'>
    <ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Border x:Name='Frame' CornerRadius='8' Background='{TemplateBinding Background}' BorderBrush='#DCD5C7' BorderThickness='1'><Path Data='M 0,0 L 4,4 L 8,0' Stroke='#716B60' StrokeThickness='1.2' Width='8' Height='4' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,14,0'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Frame' Property='BorderBrush' Value='#A59E8C'/></Trigger><Trigger Property='IsChecked' Value='True'><Setter TargetName='Frame' Property='BorderBrush' Value='#007C78'/></Trigger></ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template>
   </ToggleButton>
   <ContentPresenter Margin='12,0,34,0' HorizontalAlignment='Left' VerticalAlignment='Center' IsHitTestVisible='False' Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'/>
   <Popup x:Name='PART_Popup' Placement='Bottom' AllowsTransparency='True' Focusable='False' IsOpen='{TemplateBinding IsDropDownOpen}' PopupAnimation='None'><Border Background='#FFFDFA' BorderBrush='#DCD5C7' BorderThickness='1' CornerRadius='8' Padding='4' Margin='0,4,0,4' MinWidth='{Binding ActualWidth,RelativeSource={RelativeSource TemplatedParent}}'><ScrollViewer MaxHeight='280' CanContentScroll='True'><ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/></ScrollViewer></Border></Popup>
  </Grid><ControlTemplate.Triggers><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
 </Style>
 <Style TargetType='CheckBox'>
  <Setter Property='Foreground' Value='#716B60'/><Setter Property='FontSize' Value='11.5'/><Setter Property='VerticalContentAlignment' Value='Center'/><Setter Property='Padding' Value='1,6'/>
  <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='CheckBox'><Grid Background='Transparent' Margin='{TemplateBinding Padding}'><Grid.ColumnDefinitions><ColumnDefinition Width='14'/><ColumnDefinition Width='6'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions><Border x:Name='Box' Width='13' Height='13' VerticalAlignment='Center' CornerRadius='3' BorderBrush='#A9A18F' BorderThickness='1' Background='#FFFDFA'><Path x:Name='Tick' Data='M 2,5 L 4,7 L 8,2' Stroke='#007C78' StrokeThickness='1.4' Visibility='Collapsed'/></Border><ContentPresenter Grid.Column='2' VerticalAlignment='Center'/></Grid><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Tick' Property='Visibility' Value='Visible'/><Setter TargetName='Box' Property='BorderBrush' Value='#007C78'/><Setter TargetName='Box' Property='Background' Value='#D9F2E9'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Box' Property='BorderThickness' Value='2'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
 </Style>
 <ControlTemplate x:Key='ScrollPage' TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate>
 <Style x:Key='GlassVerticalThumb' TargetType='Thumb'>
  <Setter Property='MinHeight' Value='28'/><Setter Property='Background' Value='#7792AAA2'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Thumb'><Grid Background='Transparent'><Border x:Name='Rail' Width='4' HorizontalAlignment='Center' Margin='0,2' CornerRadius='3' Background='{TemplateBinding Background}'/></Grid><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Rail' Property='Width' Value='6'/><Setter TargetName='Rail' Property='Background' Value='#99007C78'/></Trigger><Trigger Property='IsDragging' Value='True'><Setter TargetName='Rail' Property='Width' Value='6'/><Setter TargetName='Rail' Property='Background' Value='#CC007C78'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
 </Style>
 <Style x:Key='GlassHorizontalThumb' TargetType='Thumb'>
  <Setter Property='MinWidth' Value='28'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Thumb'><Grid Background='Transparent'><Border x:Name='Rail' Height='4' VerticalAlignment='Center' Margin='2,0' CornerRadius='3' Background='#7792AAA2'/></Grid><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Rail' Property='Height' Value='6'/><Setter TargetName='Rail' Property='Background' Value='#99007C78'/></Trigger><Trigger Property='IsDragging' Value='True'><Setter TargetName='Rail' Property='Height' Value='6'/><Setter TargetName='Rail' Property='Background' Value='#CC007C78'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
 </Style>
 <ControlTemplate x:Key='GlassVerticalScroll' TargetType='ScrollBar'>
  <Grid Background='Transparent'><Track x:Name='PART_Track' Orientation='Vertical' IsDirectionReversed='True' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' ViewportSize='{TemplateBinding ViewportSize}'>
   <Track.DecreaseRepeatButton><RepeatButton Command='ScrollBar.PageUpCommand' Focusable='False' Template='{StaticResource ScrollPage}'/></Track.DecreaseRepeatButton><Track.Thumb><Thumb Style='{StaticResource GlassVerticalThumb}'/></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton Command='ScrollBar.PageDownCommand' Focusable='False' Template='{StaticResource ScrollPage}'/></Track.IncreaseRepeatButton>
  </Track></Grid>
 </ControlTemplate>
 <ControlTemplate x:Key='GlassHorizontalScroll' TargetType='ScrollBar'>
  <Grid Background='Transparent'><Track x:Name='PART_Track' Orientation='Horizontal' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' ViewportSize='{TemplateBinding ViewportSize}'>
   <Track.DecreaseRepeatButton><RepeatButton Command='ScrollBar.PageLeftCommand' Focusable='False' Template='{StaticResource ScrollPage}'/></Track.DecreaseRepeatButton><Track.Thumb><Thumb Style='{StaticResource GlassHorizontalThumb}'/></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton Command='ScrollBar.PageRightCommand' Focusable='False' Template='{StaticResource ScrollPage}'/></Track.IncreaseRepeatButton>
  </Track></Grid>
 </ControlTemplate>
 <Style TargetType='ScrollBar'><Setter Property='Width' Value='12'/><Setter Property='MinWidth' Value='12'/><Setter Property='Focusable' Value='False'/><Setter Property='Background' Value='Transparent'/><Setter Property='Template' Value='{StaticResource GlassVerticalScroll}'/><Style.Triggers><Trigger Property='Orientation' Value='Horizontal'><Setter Property='Width' Value='Auto'/><Setter Property='MinWidth' Value='0'/><Setter Property='Height' Value='12'/><Setter Property='Template' Value='{StaticResource GlassHorizontalScroll}'/></Trigger></Style.Triggers></Style>
 <Style TargetType='ScrollViewer'>
  <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollViewer'><Grid Background='{TemplateBinding Background}'>
   <Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions><Grid.RowDefinitions><RowDefinition Height='*'/><RowDefinition Height='Auto'/></Grid.RowDefinitions>
   <ScrollContentPresenter x:Name='PART_ScrollContentPresenter' Content='{TemplateBinding Content}' ContentTemplate='{TemplateBinding ContentTemplate}' CanContentScroll='{TemplateBinding CanContentScroll}' Margin='{TemplateBinding Padding}'/>
   <ScrollBar x:Name='PART_VerticalScrollBar' Grid.Column='1' Width='12' Orientation='Vertical' Visibility='{TemplateBinding ComputedVerticalScrollBarVisibility}' Maximum='{TemplateBinding ScrollableHeight}' ViewportSize='{TemplateBinding ViewportHeight}' Value='{Binding VerticalOffset,RelativeSource={RelativeSource TemplatedParent},Mode=OneWay}'/>
   <ScrollBar x:Name='PART_HorizontalScrollBar' Grid.Row='1' Height='12' Orientation='Horizontal' Visibility='{TemplateBinding ComputedHorizontalScrollBarVisibility}' Maximum='{TemplateBinding ScrollableWidth}' ViewportSize='{TemplateBinding ViewportWidth}' Value='{Binding HorizontalOffset,RelativeSource={RelativeSource TemplatedParent},Mode=OneWay}'/>
  </Grid></ControlTemplate></Setter.Value></Setter>
 </Style>
</ResourceDictionary>"));
        }
        public static Window Dialog(Window owner, string title, double height)
        { return new Window { Owner = owner, Title = title, Width = 450, Height = height, MinWidth = 350, MinHeight = 340, FontFamily = new FontFamily("Microsoft YaHei UI"), Background = B(Back), WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false }; }
        public static Button ChromeButton(string glyph, string name, RoutedEventHandler action)
        {
            var button = Button(glyph, false, action); button.FontFamily = new FontFamily("Segoe MDL2 Assets"); button.FontSize = 13; button.Width = 30; button.Height = 32; button.Padding = new Thickness(0); button.Background = Brushes.Transparent; button.BorderThickness = new Thickness(0); button.ToolTip = name; System.Windows.Automation.AutomationProperties.SetName(button, name); return button;
        }
        public static bool InsideButton(DependencyObject source)
        {
            while (source != null) { if (source is Button) return true; var content = source as FrameworkContentElement; source = content != null ? content.Parent : source is Visual ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source); } return false;
        }
        public static void WrapDialog(Window window)
        {
            var previous = window.Content as UIElement; window.Content = null; window.WindowStyle = WindowStyle.None; window.AllowsTransparency = true; window.Background = Brushes.Transparent; window.ResizeMode = ResizeMode.CanResizeWithGrip;
            var grid = new Grid(); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) }); grid.RowDefinitions.Add(new RowDefinition());
            var header = new Grid { Background = Brushes.Transparent, Margin = new Thickness(22, 8, 12, 0) }; header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); header.Children.Add(Text(window.Title, 12, Muted));
            var close = ChromeButton("\uE8BB", "关闭", delegate { window.Close(); }); Grid.SetColumn(close, 1); header.Children.Add(close); header.MouseLeftButtonDown += delegate(object s, System.Windows.Input.MouseButtonEventArgs e) { if (!InsideButton(e.OriginalSource as DependencyObject)) window.DragMove(); }; grid.Children.Add(header);
            Grid.SetRow(previous, 1); grid.Children.Add(previous);
            window.Content = new Border { Margin = new Thickness(10), CornerRadius = new CornerRadius(18), Background = B(Back), BorderBrush = B(Rule), BorderThickness = new Thickness(1), Child = grid, Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(70, 62, 45), BlurRadius = 18, ShadowDepth = 3, Opacity = .12 } };
        }
        public static void SavePreview(Window window, string path)
        {
            window.UpdateLayout(); var root = (FrameworkElement)window.Content;
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth + root.Margin.Left + root.Margin.Right), (int)Math.Ceiling(root.ActualHeight + root.Margin.Top + root.Margin.Bottom), 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap)); System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))); using (var file = System.IO.File.Create(path)) encoder.Save(file);
        }
    }
    public class WordDialog : Window
    {
        readonly MainWindow main;
        readonly string word;
        readonly Segment context;
        readonly string application;
        readonly TextBox meaning;
        readonly CancellationTokenSource cts = new CancellationTokenSource();
        bool userEdited, loading;
        public WordDialog(MainWindow main, string word, Segment segment, string application)
        {
            this.main = main; this.word = TextRules.NormalizeWord(word); context = segment; this.application = application;
            Title = "收藏单词"; Owner = main; Width = 430; Height = 510; MinWidth = 350; MinHeight = 440; Background = UI.B(UI.Back); FontFamily = main.FontFamily; ShowInTaskbar = false; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var grid = new Grid { Margin = new Thickness(24, 8, 24, 24) };
            foreach (var h in new[] { GridLength.Auto, GridLength.Auto, new GridLength(85), new GridLength(1, GridUnitType.Star), new GridLength(48) }) grid.RowDefinitions.Add(new RowDefinition { Height = h });
            var name = UI.Text(this.word, 31, UI.Ink); name.FontFamily = new FontFamily("Georgia"); grid.Children.Add(name);
            var label = UI.Text("中文释义（可修改）", 12, UI.Muted); label.Margin = new Thickness(0, 18, 0, 8); Grid.SetRow(label, 1); grid.Children.Add(label);
            var saved = main.Data.Words.FirstOrDefault(w => w.Text.Equals(this.word, StringComparison.OrdinalIgnoreCase));
            meaning = new TextBox { Text = saved == null ? "" : saved.Meaning, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxLength = 1000 };
            meaning.TextChanged += delegate { if (!loading) userEdited = true; }; Grid.SetRow(meaning, 2); grid.Children.Add(meaning);
            var source = new StackPanel { Margin = new Thickness(0, 17, 0, 8) }; source.Children.Add(UI.Text("遇到它的地方", 11, UI.Muted));
            var sentence = UI.Text(segment.English, 16, UI.Ink); sentence.FontFamily = new FontFamily("Georgia"); sentence.LineHeight = 25; sentence.Margin = new Thickness(0, 8, 0, 6); source.Children.Add(sentence); source.Children.Add(UI.Text(segment.Chinese, 12, UI.Muted));
            var scroll = new ScrollViewer { Content = source, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; Grid.SetRow(scroll, 3); grid.Children.Add(scroll);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var close = UI.Button("取消", false, delegate { Close(); }); close.IsCancel = true; close.Margin = new Thickness(0, 0, 8, 0); actions.Children.Add(close);
            var save = UI.Button(saved == null ? "收藏单词" : "保存释义", true, delegate
            {
                if (main.SaveWord(this.word, meaning.Text.Trim(), context, this.application)) Close();
            }); actions.Children.Add(save); Grid.SetRow(actions, 4); grid.Children.Add(actions); Content = grid; UI.WrapDialog(this);
            Closed += delegate { cts.Cancel(); };
            Loaded += async delegate
            {
                if (saved != null) return;
                label.Text = "正在查询中文释义…（也可以自行填写）";
                try
                {
                    string translated = await main.TranslateWord(this.word, cts.Token);
                    if (cts.IsCancellationRequested) return;
                    if (!userEdited) { loading = true; meaning.Text = translated; loading = false; }
                    label.Text = "中文释义（可修改）";
                }
                catch (OperationCanceledException) { }
                catch (Exception) { if (!cts.IsCancellationRequested) label.Text = "释义暂未查到，可自行填写后收藏"; }
            };
        }
    }
    public class SettingsDialog : Window
    {
        public SettingsDialog(MainWindow main)
        {
            Owner = main; Title = "翻译设置"; Width = 460; Height = 660; MinWidth = 360; MinHeight = 480; FontFamily = main.FontFamily; Background = UI.B(UI.Back); WindowStartupLocation = WindowStartupLocation.CenterOwner; ShowInTaskbar = false;
            var panel = new StackPanel { Margin = new Thickness(24) }; panel.Children.Add(UI.Text("翻译与识别", 23, UI.Ink));
            var note = UI.Text("识别在本机完成。联网时发送英文文字，不发送截图。只有你收藏的单词和原句会写入生词本。", 12, UI.Muted); note.LineHeight = 21; note.Margin = new Thickness(0, 12, 0, 18); panel.Children.Add(note);
            panel.Children.Add(UI.Text("翻译服务", 12, UI.Muted));
            var provider = new ComboBox { Margin = new Thickness(0, 7, 0, 12), ItemsSource = new[] { "MyMemory（免费试用）", "Azure Translator（自己的密钥）", "LibreTranslate（自己的服务）" }, SelectedIndex = main.Data.Settings.Provider == "Azure" ? 1 : main.Data.Settings.Provider == "LibreTranslate" ? 2 : 0 }; panel.Children.Add(provider);
            var serviceHint = UI.Text("", 12, UI.Muted); serviceHint.LineHeight = 20; serviceHint.Margin = new Thickness(0, 0, 0, 12); panel.Children.Add(serviceHint);
            var keyLabel = UI.Text("API 密钥", 12, UI.Muted); panel.Children.Add(keyLabel);
            var secret = new PasswordBox { Padding = new Thickness(9), Margin = new Thickness(0, 7, 0, 12) }; try { secret.Password = Store.Unprotect(main.Data.Settings.ProtectedKey); } catch { } panel.Children.Add(secret);
            var addressLabel = UI.Text("区域 / 服务地址", 12, UI.Muted); panel.Children.Add(addressLabel);
            var address = new TextBox { Margin = new Thickness(0, 7, 0, 14) }; panel.Children.Add(address);
            Action update = delegate
            {
                string selectedName = provider.SelectedIndex == 1 ? "Azure" : provider.SelectedIndex == 2 ? "LibreTranslate" : "MyMemory";
                try { secret.Password = selectedName == main.Data.Settings.Provider ? Store.Unprotect(main.Data.Settings.ProtectedKey) : ""; } catch { secret.Password = ""; }
                bool my = provider.SelectedIndex == 0; secret.Visibility = keyLabel.Visibility = my ? Visibility.Collapsed : Visibility.Visible; address.Visibility = addressLabel.Visibility = my ? Visibility.Collapsed : Visibility.Visible;
                serviceHint.Text = my ? "MyMemory 匿名免费额度为每天 5,000 字符，适合试用。常用软件词和本次运行中译过的句子优先使用本地结果，不消耗网络额度。达到额度会暂停。" : provider.SelectedIndex == 1 ? "Azure 需要你自己的翻译服务密钥，可能产生费用。密钥用 Windows 当前账户加密保存。区域请填写资源对应值，例如 eastasia。" : "连接你部署的 LibreTranslate。支持 HTTPS；本机服务可使用 http://localhost。目标语言为 zh，需服务端安装中英模型。";
                addressLabel.Text = provider.SelectedIndex == 1 ? "Azure 区域（按资源配置填写）" : "LibreTranslate 服务地址（不带 /translate）";
                address.Text = provider.SelectedIndex == 1 ? main.Data.Settings.Region : main.Data.Settings.Endpoint;
            }; provider.SelectionChanged += delegate { update(); }; update();
            panel.Children.Add(UI.Text("检测间隔", 12, UI.Muted));
            var intervals=new[]{1,2,3,5};
            var interval = new ComboBox { ItemsSource = new[] { "1 秒 · 快速", "2 秒", "3 秒", "5 秒 · 省电" }, SelectedIndex = Math.Max(0,Array.IndexOf(intervals,main.Data.Settings.Interval)), Margin = new Thickness(0, 7, 0, 16) }; panel.Children.Add(interval);
            var ocr = UI.Text("如果 OCR 把英文单词拆开，可在 Windows 语言选项中安装英语的“光学字符识别”。窗口原始文本读取不需要语言包。", 12, UI.Muted); ocr.LineHeight = 20; panel.Children.Add(ocr);
            var language = UI.Button("打开 Windows 语言设置", false, delegate { Process.Start(new ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true }); }); language.HorizontalAlignment = HorizontalAlignment.Left; language.Margin = new Thickness(0, 9, 0, 17); panel.Children.Add(language);
            var save = UI.Button("保存设置", true, delegate
            {
                var prefs = Store.Parse<Preferences>(Store.Json(main.Data.Settings)); prefs.Provider = provider.SelectedIndex == 1 ? "Azure" : provider.SelectedIndex == 2 ? "LibreTranslate" : "MyMemory";
                if (prefs.Provider == "Azure") prefs.Region = address.Text.Trim(); else if (prefs.Provider == "LibreTranslate") prefs.Endpoint = address.Text.Trim();
                prefs.Interval = intervals[Math.Max(0,interval.SelectedIndex)];
                try { prefs.ProtectedKey = Store.Protect(secret.Password.Trim()); if (main.SavePreferences(prefs)) Close(); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "设置未保存"); }
            }); panel.Children.Add(save);
            Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; UI.WrapDialog(this);
        }
    }
}

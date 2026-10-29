using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ReadAlong
{
    public static class ReadingType
    {
        static FontFamily english, chinese, wordmark;
        public static FontFamily English { get { return english ?? (english=Load("SourceSerif4-Regular.otf","Source Serif 4","Georgia")); } }
        public static FontFamily Chinese { get { return chinese ?? (chinese=Load("LXGWWenKaiScreen.ttf","LXGW WenKai Screen","KaiTi")); } }
        public static FontFamily Wordmark
        {
            get
            {
                if(wordmark!=null)return wordmark;
                foreach(var font in Fonts.SystemFontFamilies)
                    foreach(var name in font.FamilyNames.Values)
                        if(name.IndexOf("Xingkai",StringComparison.OrdinalIgnoreCase)>=0||name=="华文行楷")return wordmark=font;
                return wordmark=Chinese;
            }
        }
        static FontFamily Load(string filename,string name,string fallback)
        {
            if(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Fonts",filename)))
                return new FontFamily(new Uri(AppDomain.CurrentDomain.BaseDirectory,UriKind.Absolute),"./Fonts/#"+name);
            return new FontFamily(fallback);
        }
    }
}

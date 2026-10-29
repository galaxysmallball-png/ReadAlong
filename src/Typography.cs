using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ReadAlong
{
    public static class ReadingType
    {
        static FontFamily english, chinese;
        public static FontFamily English { get { return english ?? (english=Load("SourceSerif4-Regular.otf","Source Serif 4","Georgia")); } }
        public static FontFamily Chinese { get { return chinese ?? (chinese=Load("LXGWWenKaiScreen.ttf","LXGW WenKai Screen","KaiTi")); } }
        static FontFamily Load(string filename,string name,string fallback)
        {
            if(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Fonts",filename)))
                return new FontFamily(new Uri(AppDomain.CurrentDomain.BaseDirectory,UriKind.Absolute),"./Fonts/#"+name);
            return new FontFamily(fallback);
        }
    }
}

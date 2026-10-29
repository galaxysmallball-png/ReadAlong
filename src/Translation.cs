using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ReadAlong
{
    public class TranslationException : Exception
    {
        public bool Quota;
        public TranslationException(string message, bool quota) : base(message) { Quota = quota; }
    }
    public class Translator : IDisposable
    {
        readonly HttpClient http;
        readonly SemaphoreSlim gate = new SemaphoreSlim(2);
        readonly Dictionary<string,string> cache = new Dictionary<string,string>();
        readonly Store store;
        readonly AppData data;
        public Translator(Store store, AppData data, HttpMessageHandler handler)
        {
            this.store = store; this.data = data;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            http = handler == null ? new HttpClient() : new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(15); http.DefaultRequestHeaders.UserAgent.ParseAdd("ReadAlong/1.0");
        }
        static readonly Dictionary<string,string> Local = MakeLocal();
        static Dictionary<string,string> MakeLocal()
        {
            var dictionary = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            string pairs = @"settings=设置|preferences=偏好设置|file=文件|edit=编辑|view=视图|help=帮助|save=保存|save as=另存为|open=打开|close=关闭|cancel=取消|apply=应用|confirm=确认|ok=确定|yes=是|no=否|new=新建|delete=删除|remove=移除|add=添加|copy=复制|paste=粘贴|cut=剪切|undo=撤销|redo=重做|search=搜索|find=查找|replace=替换|select all=全选|select=选择|export=导出|import=导入|download=下载|upload=上传|share=分享|print=打印|preview=预览|refresh=刷新|reload=重新载入|restart=重新启动|quit=退出|exit=退出|about=关于|account=账户|profile=个人资料|general=常规|advanced=高级|appearance=外观|language=语言|theme=主题|light=浅色|dark=深色|system=系统|default=默认|custom=自定义|enable=启用|disable=禁用|enabled=已启用|disabled=已禁用|keyboard=键盘|shortcut=快捷键|shortcuts=快捷键|notifications=通知|privacy=隐私|security=安全|updates=更新|update=更新|check for updates=检查更新|install=安装|uninstall=卸载|version=版本|workspace=工作区|project=项目|projects=项目|folder=文件夹|folders=文件夹|document=文档|documents=文档|recent=最近|history=历史记录|favorites=收藏夹|home=主页|back=返回|next=下一步|previous=上一步|continue=继续|finish=完成|done=完成|start=开始|stop=停止|pause=暂停|resume=继续|retry=重试|reset=重置|restore=恢复|restore defaults=恢复默认设置|connect=连接|disconnect=断开连接|connection=连接|network=网络|offline=离线|online=在线|loading=加载中|loading...=正在加载…|ready=就绪|error=错误|warning=警告|success=成功|failed=失败|details=详细信息|information=信息|name=名称|title=标题|description=描述|type=类型|size=大小|date=日期|time=时间|modified=已修改|created=已创建|status=状态|location=位置|path=路径|properties=属性|options=选项|tools=工具|window=窗口|windows=窗口|panel=面板|layout=布局|zoom in=放大|zoom out=缩小|fullscreen=全屏|full screen=全屏|minimize=最小化|maximize=最大化|minimise=最小化|maximise=最大化|pin=置顶|unpin=取消固定|always on top=始终置顶|display=显示|monitor=显示器|resolution=分辨率|scale=缩放|font=字体|font size=字号|bold=加粗|italic=斜体|underline=下划线|alignment=对齐|left=左|right=右|center=居中|top=顶部|bottom=底部|width=宽度|height=高度|color=颜色|colour=颜色|background=背景|foreground=前景|opacity=不透明度|transparency=透明度|border=边框|spacing=间距|margin=外边距|padding=内边距|image=图像|images=图像|text=文本|content=内容|format=格式|encoding=编码|auto save=自动保存|autosave=自动保存|read only=只读|read-only=只读|permissions=权限|permission=权限|access=访问|allow=允许|deny=拒绝|blocked=已阻止|required=必填|optional=选填|recommended=推荐|learn more=了解更多|get started=开始使用|sign in=登录|sign out=退出登录|log in=登录|log out=退出登录|create account=创建账户|remember me=记住我|username=用户名|email=电子邮件|terms=条款|feedback=反馈|report=报告|report a bug=报告问题|support=支持|documentation=使用文档|tutorial=教程|welcome=欢迎|welcome back=欢迎回来|dismiss=关闭提示|skip=跳过|more=更多|less=收起|show=显示|hide=隐藏|show all=显示全部|hide all=隐藏全部|clear=清除|clear all=全部清除|filter=筛选|sort=排序|ascending=升序|descending=降序|duplicate=复制副本|rename=重命名|move=移动|move to=移动到|copy to=复制到|open folder=打开文件夹|open file=打开文件|open recent=打开最近使用|save changes=保存更改|discard changes=放弃更改|unsaved changes=尚未保存的更改|are you sure?=确定要执行吗？|no results found=未找到结果|nothing here yet=这里还没有内容|no items=暂无项目|empty=空|completed=已完成|pending=待处理|running=运行中|stopped=已停止|paused=已暂停|automatic=自动|manual=手动|always=始终|never=从不|none=无|all=全部|selected=已选择|available=可用|unavailable=不可用|on=开|off=关|sync=同步|synchronize=同步|backup=备份|restore backup=恢复备份|storage=存储|cache=缓存|clear cache=清除缓存|memory=内存|performance=性能|hardware=硬件|acceleration=加速|process=进程|thread=线程|terminal=终端|console=控制台|command=命令|run=运行|debug=调试|build=构建|source=源|destination=目标|branch=分支|commit=提交|changes=更改|changed=已更改|merge=合并|conflict=冲突|repository=仓库|extension=扩展|extensions=扩展|plugin=插件|plugins=插件|dashboard=概览|inspector=检查器|properties panel=属性面板|timeline=时间轴|layers=图层|layer=图层|assets=素材|asset=素材|library=资源库|libraries=资源库|component=组件|components=组件|object=对象|objects=对象|scene=场景|scenes=场景|viewport=视口|render=渲染|rendering=渲染中|texture=纹理|material=材质|animation=动画|transition=过渡|transform=变换|position=位置|rotation=旋转|apply changes=应用更改|open the settings panel.=打开设置面板。|changes will take effect after restart.=更改将在重启后生效。|save your workspace before closing.=关闭前请保存工作区。|click any word to save it.=点击任意单词即可收藏。";
            foreach (string pair in pairs.Split('|')) { int at = pair.IndexOf('='); if (at > 0) dictionary[pair.Substring(0, at)] = pair.Substring(at + 1); }
            return dictionary;
        }
        public async Task<string> Translate(string text, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string local; if (Local.TryGetValue(text.Trim(), out local)) return local;
            var prefs = data.Settings;
            string key = prefs.Provider + ":" + prefs.Endpoint + ":" + text;
            lock (cache) if (cache.TryGetValue(key, out local)) return local;
            await gate.WaitAsync(token);
            try
            {
                token.ThrowIfCancellationRequested();
                lock (cache) if (cache.TryGetValue(key, out local)) return local;
                HttpRequestMessage request;
                if (prefs.Provider == "Azure")
                {
                    string secret = Store.Unprotect(prefs.ProtectedKey);
                    if (String.IsNullOrWhiteSpace(secret)) throw new TranslationException("请在设置中填写 Azure 翻译密钥。", false);
                    request = new HttpRequestMessage(HttpMethod.Post, "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from=en&to=zh-Hans");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", secret);
                    if (!String.IsNullOrWhiteSpace(prefs.Region)) request.Headers.Add("Ocp-Apim-Subscription-Region", prefs.Region);
                    request.Content = new StringContent(Store.Json(new[] { new Dictionary<string,string> { { "Text", text } } }), Encoding.UTF8, "application/json");
                }
                else if (prefs.Provider == "LibreTranslate")
                {
                    Uri uri;
                    if (!Uri.TryCreate(prefs.Endpoint, UriKind.Absolute, out uri) || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))) throw new TranslationException("LibreTranslate 地址需为 HTTPS 或本机 localhost 地址。", false);
                    var payload = new Dictionary<string,string> { { "q", text }, { "source", "en" }, { "target", "zh" }, { "format", "text" } };
                    string secret = Store.Unprotect(prefs.ProtectedKey); if (!String.IsNullOrWhiteSpace(secret)) payload["api_key"] = secret;
                    request = new HttpRequestMessage(HttpMethod.Post, prefs.Endpoint.TrimEnd('/') + "/translate") { Content = new StringContent(Store.Json(payload), Encoding.UTF8, "application/json") };
                }
                else
                {
                    if (Encoding.UTF8.GetByteCount(text) > 500) throw new TranslationException("本句超出免费接口长度限制，请拆成更短的句子。", false);
                    lock (Store.Sync)
                    {
                        string today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "China Standard Time").ToString("yyyy-MM-dd");
                        if (prefs.UsageDate != today) { prefs.UsageDate = today; prefs.CharactersToday = 0; }
                        if (prefs.CharactersToday + text.Length > 4900) throw new TranslationException("今日免费试用额度接近上限。请明天继续，或在设置中切换 Azure / 自建 LibreTranslate。", true);
                        prefs.CharactersToday += text.Length; store.Save(data);
                    }
                    request = new HttpRequestMessage(HttpMethod.Get, "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(text) + "&langpair=en%7Czh-CN");
                }
                using (request)
                using (var response = await http.SendAsync(request, token))
                {
                    if ((int)response.StatusCode == 429) throw new TranslationException("翻译服务限流或额度不足，请稍后重试或更换服务。", true);
                    if (!response.IsSuccessStatusCode) throw new TranslationException("翻译服务返回 HTTP " + (int)response.StatusCode + "。请检查网络或服务配置。", false);
                    string body = await response.Content.ReadAsStringAsync(); token.ThrowIfCancellationRequested();
                    string translated = ParseResponse(prefs.Provider, body);
                    if (String.IsNullOrWhiteSpace(translated)) throw new TranslationException("翻译服务没有返回有效译文。", false);
                    translated = WebUtility.HtmlDecode(translated).Trim();
                    lock (cache) { if (cache.Count > 2200) cache.Clear(); cache[key] = translated; }
                    return translated;
                }
            }
            catch (TaskCanceledException)
            { if (token.IsCancellationRequested) throw; throw new TranslationException("翻译请求超时。请检查网络，或在设置中更换服务。", false); }
            catch (HttpRequestException) { throw new TranslationException("无法连接翻译服务。请检查网络，或在设置中更换服务。", false); }
            finally { gate.Release(); }
        }
        public static string ParseResponse(string provider, string body)
        {
            if (provider == "Azure")
            {
                var array = Store.Parse<List<Dictionary<string,object>>>(body);
                var translations = array[0]["translations"] as System.Collections.ArrayList;
                if (translations != null) return (string)((Dictionary<string,object>)translations[0])["text"];
                var rows = array[0]["translations"] as object[];
                return (string)((Dictionary<string,object>)rows[0])["text"];
            }
            var value = Store.Parse<Dictionary<string,object>>(body);
            if (provider == "LibreTranslate")
            { if (value.ContainsKey("error")) throw new TranslationException("LibreTranslate：" + value["error"], false); return (string)value["translatedText"]; }
            if ((value.ContainsKey("quotaFinished") && value["quotaFinished"] is bool && (bool)value["quotaFinished"]) || (value.ContainsKey("responseStatus") && Convert.ToInt32(value["responseStatus"]) == 429)) throw new TranslationException("MyMemory 免费额度已用尽，请明天继续或更换服务。", true);
            if (!value.ContainsKey("responseStatus") || Convert.ToInt32(value["responseStatus"]) != 200) throw new TranslationException("MyMemory 暂未提供有效译文，请稍后重试。", false);
            return (string)((Dictionary<string,object>)value["responseData"])["translatedText"];
        }
        public void Dispose() { http.Dispose(); }
    }
}

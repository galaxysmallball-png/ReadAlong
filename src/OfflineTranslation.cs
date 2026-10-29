using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadAlong
{
    public sealed class OfflineTranslator : IDisposable
    {
        readonly object sync=new object();
        readonly SemaphoreSlim gate=new SemaphoreSlim(1);
        readonly string home;
        Process process;
        StreamWriter input;
        bool disposed;
        internal int Launches {get;private set;}
        public static string DefaultHome {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Offline");}}
        public static bool IsInstalled {get{return Ready(DefaultHome);}}
        public static bool Ready(string folder)
        {return File.Exists(Path.Combine(folder,"ready.json"))&&File.Exists(Path.Combine(folder,"runtime","python.exe"))&&File.Exists(Path.Combine(folder,"translate_worker.py"))&&File.Exists(Path.Combine(folder,"models","translate-en_zh-1_9","model","model.bin"));}
        public OfflineTranslator():this(DefaultHome){}
        internal OfflineTranslator(string folder){home=folder;}
        Process EnsureWorker()
        {
            lock(sync)
            {
                if(disposed)throw new ObjectDisposedException("OfflineTranslator");
                if(!Ready(home))throw new TranslationException("离线翻译尚未安装。请在设置中点击「安装离线翻译」，完成一次下载后即可离线使用。",false);
                if(process!=null&&!process.HasExited)return process;
                Stop(process);
                var info=new ProcessStartInfo(Path.Combine(home,"runtime","python.exe"),"-I \""+Path.Combine(home,"translate_worker.py")+"\""){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=home,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
                info.EnvironmentVariables["PYTHONIOENCODING"]="utf-8";
                process=Process.Start(info);input=new StreamWriter(process.StandardInput.BaseStream,new UTF8Encoding(false)){AutoFlush=true};
                process.ErrorDataReceived+=delegate{};process.BeginErrorReadLine();Launches++;return process;
            }
        }
        public void WarmUp(){EnsureWorker();}
        void Stop(Process expected)
        {
            if(expected==null)return;
            lock(sync)
            {
                if(process==expected){process=null;input=null;}
                try{if(!expected.HasExited)expected.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}
                expected.Dispose();
            }
        }
        public void Stop(){lock(sync){Stop(process);}}
        public async Task<string> Translate(string text,CancellationToken token)
        {
            await gate.WaitAsync(token).ConfigureAwait(false);Process worker=null;
            try
            {
                token.ThrowIfCancellationRequested();worker=EnsureWorker();StreamWriter writer;
                lock(sync){writer=input;}
                using(token.Register(delegate{Stop(worker);}))
                {
                    token.ThrowIfCancellationRequested();if(writer==null)throw new IOException("Offline worker stopped");
                    await writer.WriteLineAsync(Store.Json(new Dictionary<string,string>{{"text",text}})).ConfigureAwait(false);
                    var read=worker.StandardOutput.ReadLineAsync();
                    using(var timeout=new CancellationTokenSource())
                    using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(token,timeout.Token))
                    {
                        if(await Task.WhenAny(read,Task.Delay(45000,deadline.Token)).ConfigureAwait(false)!=read)
                        {Stop(worker);token.ThrowIfCancellationRequested();throw new TranslationException("本机翻译超时，请缩短文本后重试。",false);}
                        timeout.Cancel();string reply=await read.ConfigureAwait(false);token.ThrowIfCancellationRequested();
                        if(String.IsNullOrWhiteSpace(reply))throw new IOException("Offline worker exited");
                        var result=Store.Parse<Dictionary<string,object>>(reply);
                        if(result.ContainsKey("error"))throw new TranslationException(Convert.ToString(result["error"]),false);
                        string translated=result.ContainsKey("translation")?Convert.ToString(result["translation"]):"";
                        if(String.IsNullOrWhiteSpace(translated))throw new TranslationException("本机模型没有返回译文，请重试。",false);
                        return translated;
                    }
                }
            }
            catch(TranslationException){Stop(worker);throw;}
            catch(Exception ex)
            {Stop(worker);token.ThrowIfCancellationRequested();if(ex is OperationCanceledException)throw;throw new TranslationException("离线组件启动或读取失败。请在设置中重新安装离线翻译后重试。",false);}
            finally{gate.Release();}
        }
        public void Dispose(){lock(sync){disposed=true;Stop(process);}}
    }
}

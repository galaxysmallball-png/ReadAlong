using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadAlong
{
    // Only one outstanding request per worker. A cancelled or stalled provider is
    // terminated, so it cannot keep reading an old foreground window.
    public sealed class ReaderClient : IDisposable
    {
        readonly object sync = new object();
        readonly SemaphoreSlim gate = new SemaphoreSlim(1);
        Process process;
        bool disposed;
        internal int Launches { get; private set; }
        Process EnsureWorker()
        {
            lock(sync)
            {
                if(disposed)throw new ObjectDisposedException("ReaderClient");
                if(process!=null&&!process.HasExited)return process;
                if(process!=null){process.Dispose();process=null;}
                string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ReadAlong.Reader.exe");
                if(!File.Exists(path))throw new IOException("缺少 ReadAlong.Reader.exe，请完整解压程序文件夹。");
                var info=new ProcessStartInfo(path,"--server"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
                process=Process.Start(info);process.ErrorDataReceived+=delegate{};process.BeginErrorReadLine();Launches++;return process;
            }
        }
        public void WarmUp(){EnsureWorker();}
        void Stop(Process expected)
        {
            if(expected==null)return;
            lock(sync)
            {
                if(process==expected)process=null;
                try{if(!expected.HasExited)expected.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}
                expected.Dispose();
            }
        }
        public void Stop(){lock(sync){Stop(process);}}
        public async Task<ReadResult> ReadAsync(IntPtr handle,uint pid,string previousHash,CancellationToken token,bool fixture)
        {
            await gate.WaitAsync(token).ConfigureAwait(false);Process worker=null;
            try
            {
                token.ThrowIfCancellationRequested();worker=EnsureWorker();
                using(token.Register(delegate{Stop(worker);}))
                {
                    token.ThrowIfCancellationRequested();
                    var request=new ReadRequest{Handle=handle.ToInt64(),Pid=pid,PreviousHash=previousHash??"",Fixture=fixture};
                    await worker.StandardInput.WriteLineAsync(Store.Json(request)).ConfigureAwait(false);
                    await worker.StandardInput.FlushAsync().ConfigureAwait(false);
                    var read=worker.StandardOutput.ReadLineAsync();
                    using(var timeout=new CancellationTokenSource())
                    using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(token,timeout.Token))
                    {
                        var delay=Task.Delay(9000,deadline.Token);
                        if(await Task.WhenAny(read,delay).ConfigureAwait(false)!=read)
                        {Stop(worker);token.ThrowIfCancellationRequested();throw new TimeoutException("这个窗口读取超时。可重试，或用「粘贴英文」读取所选内容。");}
                        timeout.Cancel();string reply=await read.ConfigureAwait(false);token.ThrowIfCancellationRequested();
                        if(String.IsNullOrWhiteSpace(reply))throw new IOException("识别组件已结束，可重新开始伴读后重试。");
                        return Store.Parse<ReadResult>(reply);
                    }
                }
            }
            catch{Stop(worker);token.ThrowIfCancellationRequested();throw;}
            finally{gate.Release();}
        }
        public void Dispose(){lock(sync){disposed=true;Stop(process);}}
    }
}

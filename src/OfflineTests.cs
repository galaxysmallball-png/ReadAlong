using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReadAlong
{
    public static partial class Tests
    {
        public static int Offline(string report)
        {
            var passed=new System.Collections.Generic.List<string>();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report)));
                Check(new Preferences().Provider=="Offline","Fresh preferences use local translation");
                var data=new AppData();data.Settings.CharactersToday=999999;
                var store=new Store(Path.Combine(Path.GetDirectoryName(report),"offline-test-data"));var handler=new MockHandler();
                using(var service=new Translator(store,data,handler))
                {
                    var clock=Stopwatch.StartNew();string translated=service.Translate("Please select a folder to save your exported files.",CancellationToken.None).GetAwaiter().GetResult();clock.Stop();
                    Check(Regex.IsMatch(translated,"[\u4e00-\u9fff]{2,}")&&!translated.Contains("\u2581")&&handler.Count==0&&data.Settings.CharactersToday==999999,"Unknown English uses model without HTTP or quota");
                    passed.Add("PASS actual local en-zh model, no HTTP and no character quota ("+clock.ElapsedMilliseconds+" ms): "+translated);
                    Check(service.Translate("Please select a folder to save your exported files.",CancellationToken.None).GetAwaiter().GetResult()==translated&&service.OfflineLaunches==1,"Offline cache and worker reuse");passed.Add("PASS offline translation cache and worker reuse");
                    var first=service.Translate("Drag this window to another monitor.",CancellationToken.None);var second=service.Translate("Your changes have been saved successfully.",CancellationToken.None);Task.WhenAll(first,second).GetAwaiter().GetResult();
                    Check(first.Result!=second.Result&&Regex.IsMatch(first.Result,"[\u4e00-\u9fff]")&&Regex.IsMatch(second.Result,"[\u4e00-\u9fff]")&&handler.Count==0,"Queued requests retain their own result");passed.Add("PASS concurrent local requests return the matching translation");
                }
                using(var service=new Translator(store,data,handler))using(var cancel=new CancellationTokenSource())
                {
                    var pending=service.Translate("An interrupted local sentence must never be displayed.",cancel.Token);cancel.Cancel();bool cancelled=false;try{pending.GetAwaiter().GetResult();}catch(OperationCanceledException){cancelled=true;}
                    Check(cancelled,"Local inference cancellation");string recovered=service.Translate("The local translator is ready to read the next sentence.",CancellationToken.None).GetAwaiter().GetResult();
                    Check(Regex.IsMatch(recovered,"[\u4e00-\u9fff]")&&service.OfflineLaunches==2&&handler.Count==0,"Cancelled local worker restarts");passed.Add("PASS local inference cancellation and worker recovery");
                }
                using(var missing=new OfflineTranslator(Path.Combine(store.Folder,"missing-runtime")))
                {bool failed=false;try{missing.Translate("Translate this English sentence.",CancellationToken.None).GetAwaiter().GetResult();}catch(TranslationException ex){failed=ex.Message.Contains("安装");}Check(failed,"Missing model is actionable");}passed.Add("PASS missing local model reports installation requirement");
                data.Settings.Provider="InvalidProvider";using(var service=new Translator(store,data,handler))
                {bool failed=false;try{service.Translate("Never send unknown providers to an online endpoint.",CancellationToken.None).GetAwaiter().GetResult();}catch(TranslationException){failed=true;}Check(failed&&handler.Count==0,"No implicit online fallback");}passed.Add("PASS invalid provider never falls back to an online endpoint");
                var info=new ProcessStartInfo(Path.Combine(OfflineTranslator.DefaultHome,"runtime","python.exe"),"-I \""+Path.Combine(OfflineTranslator.DefaultHome,"translate_worker.py")+"\" --check"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
                using(var process=Process.Start(info))
                {
                    var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();if(!process.WaitForExit(45000)){process.Kill();throw new Exception("Local self-check timeout");}
                    string json=output.GetAwaiter().GetResult();error.GetAwaiter().GetResult();var result=Store.Parse<System.Collections.Generic.Dictionary<string,object>>(json);
                    Check(process.ExitCode==0&&(bool)result["networkBlocked"],"Worker refuses socket networking");
                }
                passed.Add("PASS translation worker explicitly refuses network connections");
                File.WriteAllText(report,String.Join("\r\n",passed),Encoding.UTF8);return 0;
            }
            catch(Exception ex){File.WriteAllText(report,String.Join("\r\n",passed)+"\r\nFAIL "+ex,Encoding.UTF8);return 1;}
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FileSearchApp
{
    public class MHistorySearcherTest : TestBase
    {
        public override List<string> TestAll()
        {
            return base.TestAll<MHistorySearcherTest>();
        }
        public List<string> TestStartSearchAsync()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/08/27 16:30");
                var his = new MHistoryManager();
                his.TryAdd(@"C:\test\7.txt");
                his.TryAdd(@"C:\test\4.txt");
                his.TryAdd(@"C:\test\6.txt");
                his.TryAdd(@"C:\test\3.txt");
                his.TryAdd(@"C:\test\2.txt");
                his.TryAdd(@"C:\test\5.txt");

                var hiss = new MHistorySearcher(his);
                var infos = new List<ExFileInfo>();
                hiss.SearchWords = new List<KeyWord>(){
                    new KeyWord { Word = "5", Fuzzy = true },
                    new KeyWord { Word = "6", Fuzzy = true },
                };
                hiss.RunActionWhenFileFound.Add((info) => infos.Add(info));

                var ct = new CancellationToken();
                var _lock = new object();
                Action errAction = () => { throw new Exception("timeout"); };
                var errThrowTask = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    lock (_lock) { errAction(); }
                });
                var task = hiss.StartSearchAsync(ct);
                lock (_lock) { errAction = () => Task.Delay(1); }
                while (!task.IsCompleted) { Task.Delay(100).Wait(); }

                msg.Add("2024/08/27 16:31");
                if (infos.Count != 2){
                    return msg;
                }
                msg.Add("2024/08/27 16:32");
                var paths = infos.ToList().Select(val => val.FilePath);
                if (!paths.Contains(@"C:\test\5.txt")){
                    return msg;
                }
                msg.Add("2024/08/27 16:33");
                if (!paths.Contains(@"C:\test\6.txt")){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex)
            {
                msg[msg.Count - 1] += ":err " + ex.Message;
                return msg;
            }
            return new List<string>();
        }
    }
}

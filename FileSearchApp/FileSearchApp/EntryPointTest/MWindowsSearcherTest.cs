using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;

namespace FileSearchApp
{
    public class MWindowsSearcherTest : TestBase
    {
        public List<string> Test()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/07/23 10:56");
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var name = "123456789ddddddddddddddddddopsdkpf.txt";
                var path = dir + "\\" + name;
                if (File.Exists(path)){
                    return msg;
                }
                File.Create(path).Dispose();
                Task.Delay(3500).Wait(); //インデックスに追加されるのを待つ
                msg.Add("2024/07/23 10:57");
                var win = new MWindowsSearcher();
                win.GetIncludeDirectorys = () => new List<string>{ dir };;
                win.SearchWords = new List<KeyWord>
                {
                    new KeyWord{Word = "789dddddddddd", Fuzzy = true}
                };
                var infos = new List<ExFileInfo>();
                win.RunActionWhenFileFound.Add(info =>
                {
                    infos.Add(info);
                });
                using (var cts = new CancellationTokenSource())
                {
                    var searchTask = win.StartSearchAsync(cts.Token);
                    Task.Delay(2000).Wait();
                    if (!searchTask.IsCompleted){
                        cts.Cancel();
                        msg.Add("2026/06/18 10:30"); //検索タイムアウトになってる
                        return msg;
                    }
                }
                File.Delete(path);
                msg.Add("2024/07/23 10:58");
                if (infos.Count != 1){
                    return msg;
                }
                msg.Add("2024/07/23 11:00");
                if (infos[0].FileName + infos[0].Extension != name){
                    return msg;
                }
                msg.Add("2024/07/23 12:00");
                infos.Clear();
                win.SearchWords = new List<KeyWord>
                {
                    new KeyWord{ Word = "789dddddddddd", Fuzzy = false }
                };
                using (var cts = new CancellationTokenSource())
                {
                    var task = win.StartSearchAsync(cts.Token);
                    Task.Delay(1000).Wait();
                    if (!task.IsCompleted){
                        cts.Cancel();
                        return msg;
                    }
                }
                if (infos.Count != 0){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestGetNonRepetedDirectorys()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/12/16 10:49"); //チェックポイント
                var win = new MWindowsSearcher();
                var list = new List<string>{
                    @"C:\CurrentUser",
                    @"C:\CurrentUser\admin",
                    @"C:\Winqows",
                };
                msg.Add("2024/12/16 10:52");
                var paths = win.DistinctDirectory(list);
                if (paths.Count != 2){
                    return msg;
                }
                msg.Add("2024/12/16 10:53");
                if (!paths.Contains(@"C:\CurrentUser")){
                    return msg;
                }
                msg.Add("2024/12/16 10:54");
                if (!paths.Contains(@"C:\Winqows")){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public override List<string> TestAll()
        {
            return base.TestAll<MWindowsSearcherTest>();
        }
    }
}

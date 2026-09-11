using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;

namespace FileSearchApp
{
    public class MHistoryManagerTest : TestBase
    {
        public override List<string> TestAll()
        {
            return base.TestAll<MHistoryManagerTest>();
        }
        public List<string> TestRead()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/08/27 16:44");
                var his = new MHistoryManager();
                Func<List<string>> read = () => new List<string>(){
                    @"C:\test\4.txt",
                    @"C:\test\5.txt",
                };
                his.Read(read);
                Task.Delay(100).Wait();
                var result = his.GetExFileInfos("4");
                msg.Add("2024/08/27 16:45");
                if (result.Count() != 1){
                    return msg;
                }
                msg.Add("2024/08/27 16:46");
                if (result.ElementAt(0).FilePath != @"C:\test\4.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("5");
                if (result.ElementAt(0).FilePath != @"C:\test\5.txt"){
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
        public List<string> TestTryAdd()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/08/27 16:47");
                var his = new MHistoryManager();
                his.TryAdd(@"C:\test\5.txt");
                his.TryAdd(@"C:\test\6.txt");
                his.TryAdd(@"C:\test\3.txt");
                his.TryAdd(@"C:\test\4.txt");
                his.TryAdd(@"C:\test\2.txt");
                his.TryAdd(@"C:\test\7.txt");
                his.TryAdd(@"C:\test\3.txt");

                msg.Add("2024/08/27 16:50");
                if (his.Count != 6){
                    return msg;
                }
                var result = his.GetExFileInfos("2");
                msg.Add("2024/08/27 16:51");
                if (result.ElementAt(0).FilePath != @"C:\test\2.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("3");
                if (result.ElementAt(0).FilePath != @"C:\test\3.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("4");
                if (result.ElementAt(0).FilePath != @"C:\test\4.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("5");
                if (result.ElementAt(0).FilePath != @"C:\test\5.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("6");
                if (result.ElementAt(0).FilePath != @"C:\test\6.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("7");
                if (result.ElementAt(0).FilePath != @"C:\test\7.txt"){
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
        public List<string> TestAddBuffer()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/08/27 16:56");
                var his = new MHistoryManager();
                his.AddToBuffer(@"C:\test\5.txt");
                his.AddToBuffer(@"C:\test\6.txt");
                his.AddToBuffer(@"C:\test\3.txt");
                Task.Delay(2050).Wait(); // バッファのフラッシュを待つ

                msg.Add("2024/08/27 16:58");
                if (his.Count != 3){
                    return msg;
                }
                var result = his.GetExFileInfos("3");
                msg.Add("2024/08/27 16:59");
                if (result.ElementAt(0).FilePath != @"C:\test\3.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("5");
                if (result.ElementAt(0).FilePath != @"C:\test\5.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("6");
                if (result.ElementAt(0).FilePath != @"C:\test\6.txt"){
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
        public List<string> TestRemove()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/05/31 16:13");
                var his = new MHistoryManager();
                his.TryAdd(@"C:\test\5.txt");
                his.TryAdd(@"C:\test\6.txt");
                his.TryAdd(@"C:\test\3.txt");
                his.TryAdd(@"C:\test\4.txt");
                his.TryAdd(@"C:\test\2.txt");
                his.TryAdd(@"C:\test\7.txt");
                his.Remove(@"C:\test\4.txt");

                msg.Add("2024/08/27 16:59");
                if (his.Count != 5){
                    return msg;
                }
                var result = his.GetExFileInfos("2");
                msg.Add("2024/08/27 17:00");
                if (result.ElementAt(0).FilePath != @"C:\test\2.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("3");
                if (result.ElementAt(0).FilePath != @"C:\test\3.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("5");
                if (result.ElementAt(0).FilePath != @"C:\test\5.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("6");
                if (result.ElementAt(0).FilePath != @"C:\test\6.txt"){
                    return msg;
                }
                result = his.GetExFileInfos("7");
                if (result.ElementAt(0).FilePath != @"C:\test\7.txt"){
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

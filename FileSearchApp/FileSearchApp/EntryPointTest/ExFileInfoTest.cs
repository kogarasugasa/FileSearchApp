using System;
using System.Collections.Generic;
using System.Reflection;

namespace FileSearchApp
{
    public sealed class ExFileInfoTest : TestBase
    {
        public List<string> TestConstructor()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/09/20 11:18"); //チェックポイント
                var info = ExFileInfo.GetDefault();
                if (info.FileName != string.Empty){
                    return msg;
                }
                msg.Add("2024/09/20 11:19"); //チェックポイント
                if (info.Extension != string.Empty){
                    return msg;
                }
                msg.Add("2024/09/20 11:20"); //チェックポイント
                if (info.DispFileName(true) != string.Empty){
                    return msg;
                }
                msg.Add("2024/09/20 11:21"); //チェックポイント
                if (info.DispFileName(false) != string.Empty){
                    return msg;
                }
                msg.Add("2024/09/20 11:22"); //チェックポイント
                if (info.Invalided != true){
                    return msg;
                }
                msg.Add("2024/09/20 11:23"); //チェックポイント
                if (info.LastWriteTime != DateTime.MinValue){
                    return msg;
                }
                msg.Add("2024/09/20 11:24"); //チェックポイント
                info = new ExFileInfo(@"d:\dir\file.txt");
                msg.Add("2024/09/20 11:25"); //チェックポイント
                if (info.FileName != "file"){
                    return msg;
                }
                msg.Add("2024/09/20 11:25"); //チェックポイント  
                if (info.Extension != ".txt"){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestClone()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                var info = new ExFileInfo(@"c:\temp\text.txt");
                msg.Add("2025/02/04 10:42"); //チェックポイント
                var cloneInfo = info.Clone();
                msg.Add("2025/02/04 10:43"); //チェックポイント
                if (object.ReferenceEquals(info, cloneInfo)){
                    return msg;
                }
                msg.Add("2025/02/04 10:44"); //チェックポイント
                if (cloneInfo.FilePath != info.FilePath){
                    return msg;
                }
                msg.Add("2025/02/04 10:45"); //チェックポイント
                if (cloneInfo.FileName != info.FileName){
                    return msg;
                }
                msg.Add("2025/02/04 10:46"); //チェックポイント
                if (cloneInfo.Alias != info.Alias){
                    return msg;
                }
                msg.Add("2025/02/04 10:47"); //チェックポイント
                if (cloneInfo.Size != info.Size){
                    return msg;
                }
                msg.Add("2025/02/04 10:48"); //チェックポイント
                if (cloneInfo.Invalided != info.Invalided){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public override List<string> TestAll()
        {
            return base.TestAll<ExFileInfoTest>();
        }
    }
}

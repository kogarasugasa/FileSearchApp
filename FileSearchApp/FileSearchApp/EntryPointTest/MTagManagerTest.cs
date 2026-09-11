using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace FileSearchApp
{
    public class MTagManagerTest : TestBase
    {
        public override List<string> TestAll()
        {
            return base.TestAll<MTagManagerTest>();
        }
        public List<string> TestAdd_GetNames_GetTags()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/04 09:50"); //チェックポイント
                var ta = new MTagManager();
                ta.Add("test1", "tag1");
                ta.Add("test2", "tag2");
                ta.Add("test3", "tag3");
                ta.Add("test4", "tag3");
                ta.Add("test3", "tag4");
                msg.Add("2024/06/04 10:21"); //チェックポイント
                if (ta.GetTags("test1").Count != 1){
                    return msg;
                }
                if (ta.GetTags("test1")[0] != "tag1"){
                    return msg;
                }
                msg.Add("2024/06/04 10:22"); //チェックポイント
                if (ta.GetNames("tag2").Count != 1){
                    return msg;
                }
                if (ta.GetNames("tag2")[0] != "test2"){
                    return msg;
                }
                msg.Add("2024/06/04 10:23"); //チェックポイント
                if (ta.GetTags("test3").Count != 2){
                    return msg;
                }
                if (ta.GetTags("test3")[0] != "tag3" &
                    ta.GetTags("test3")[0] != "tag4"){
                    return msg;
                }
                if (ta.GetTags("test3")[1] != "tag3" &
                    ta.GetTags("test3")[1] != "tag4"){
                    return msg;
                }
                msg.Add("2024/06/04 10:24"); //チェックポイント
                if (ta.GetNames("tag3").Count != 2){
                    return msg;
                }
                if (ta.GetNames("tag3")[0] != "test3" &
                    ta.GetNames("tag3")[0] != "test4"){
                    return msg;
                }
                if (ta.GetNames("tag3")[1] != "test3" &
                    ta.GetNames("tag3")[1] != "test4"){
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
        public List<string> TestExists()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/06/04 10:38"); //チェックポイント
                var ta = new MTagManager();
                ta.Add("test1", "tag1");
                ta.Add("test2", "tag2");
                ta.Add("test3", "tag3");
                ta.Add("test4", "tag3");
                ta.Add("test3", "tag4");
                msg.Add("2024/06/04 10:39"); //チェックポイント
                if (!ta.Exists("test1", "tag1")){
                    return msg;
                }
                if (!ta.Exists("test3", "tag3")){
                    return msg;
                }
                if (!ta.Exists("test3", "tag4")){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestRemove()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/06/04 12:47"); //チェックポイント
                var ta = new MTagManager();
                ta.Add("test1", "tag1");
                ta.Add("test2", "tag2");
                ta.Add("test3", "tag3");
                ta.Add("test4", "tag3");
                ta.Add("test3", "tag4");
                msg.Add("2024/06/04 12:48"); //チェックポイント
                ta.Remove("test3", "tag3");
                ta.Remove("test4", "tag3");
                if (ta.Exists("test3", "tag3") || ta.Exists("test4", "tag3")){
                    return msg;
                }
                msg.Add("2025/03/13 16:29"); //チェックポイント
                ta.Add("test9", "tag9");
                ta.Remove("Test9", "tag9");
                if (ta.Exists("test9", "tag9")){
                    return msg;
                }
                msg.Add("2024/06/04 12:49"); //チェックポイント
                if (ta.GetTags("test3").Count != 1){
                    return msg;
                }
                msg.Add("2024/06/04 12:50"); //チェックポイント
                if (ta.GetNames("tag3").Count != 0){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestRead()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/06/04 12:51"); //チェックポイント
                var ta = new MTagManager();
                ta.Read(new List<string>()
                {
                    "test1:tag1",
                    "test2:tag2",
                    "test3:tag3,tag4",
                    "test4:tag3",
                });
                msg.Add("2024/06/04 12:54"); //チェックポイント
                if (ta.GetNames("tag1").Count != 1){
                    return msg;
                }
                if (ta.GetNames("tag1")[0] != "test1"){
                    return msg;
                }
                msg.Add("2024/06/04 12:55"); //チェックポイント
                if (ta.GetNames("tag2").Count != 1){
                    return msg;
                }
                if (ta.GetNames("tag2")[0] != "test2"){
                    return msg;
                }
                msg.Add("2024/06/04 12:56"); //チェックポイント
                if (ta.GetNames("tag3").Count != 2){
                    return msg;
                }
                if (ta.GetNames("tag3")[0] != "test3" & ta.GetNames("tag3")[0] != "test4"){
                    return msg;
                }
                if (ta.GetNames("tag3")[1] != "test3" & ta.GetNames("tag3")[1] != "test4"){
                    return msg;
                }
                msg.Add("2024/06/04 12:57"); //チェックポイント
                if (ta.GetNames("tag4").Count != 1){
                    return msg;
                }
                if (ta.GetNames("tag4")[0] != "test3"){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestSave()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/06/04 13:24"); //チェックポイント
                var ta = new MTagManager();
                ta.Add("test1", "tag1");
                ta.Add("test2", "tag2");
                ta.Add("test3", "tag3");
                ta.Add("test3", "tag4");
                ta.Add("test4", "tag3");
                msg.Add("2024/06/04 13:25"); //チェックポイント
                var path = Path.GetTempFileName();
                Action<List<string>> fn = (list) =>
                {
                    using (var writer = new StreamWriter(path))
                    {
                        foreach (var val in list)
                        {
                            writer.WriteLine(val);
                        }
                    }
                };
                ta.Save(fn);
                msg.Add("2024/06/04 16:46"); //チェックポイント
                using (var reader = new StreamReader(path))
                {
                    var list = new List<string>();
                    while (!reader.EndOfStream)
                    {
                        list.Add(reader.ReadLine());
                    }
                    msg.Add("2024/06/04 16:47"); //チェックポイント
                    if (list.Count != 4){
                        return msg;
                    }
                    msg.Add("2024/06/04 16:48"); //チェックポイント
                    if (list[0] != "test1:tag1"){
                        return msg;
                    }
                    if (list[1] != "test2:tag2"){
                        return msg;
                    }
                    if (list[2] != "test3:tag3,tag4"){
                        return msg;
                    }
                    if (list[3] != "test4:tag3"){
                        return msg;
                    }
                }
                msg.Add("2024/06/04 16:49"); //チェックポイント
                File.Delete(path);
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
    }
}

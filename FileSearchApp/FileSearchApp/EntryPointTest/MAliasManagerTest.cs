using System;
using System.IO;
using System.Collections.Generic;

namespace FileSearchApp
{
    public class MAliasManagerTest : TestBase
    {
        public override List<string> TestAll()
        {
            return base.TestAll<MAliasManagerTest>();
        }
        public List<string> TestAdd()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/03 18:26");
                var al = new MAliasManager();
                al.Add("text1", "alias1", false);
                al.Add("text2", "alias2", false);
                al.Add("text3", "alias3", false);
                msg.Add("2024/06/04 09:01");
                
                var alias = al.GetAlias("text1").Match(none => none, some => some);
                if (alias != "alias1"){
                    return msg;
                }
                alias = al.GetAlias("text2").Match(none => none, some => some);
                if (alias != "alias2"){
                    return msg;
                }
                alias = al.GetAlias("text3").Match(none => none, some => some);
                if (alias != "alias3"){
                    return msg;
                }
                msg.Add("2024/06/04 09:04");
                al.Add("text3", "overWrite", true);
                alias = al.GetAlias("text3").Match(none => none, some => some);
                if (alias != "overWrite"){
                    return msg;
                }
                msg.Add("2024/07/19 10:28");
                al.Add("text3", "alias3", false);
                alias = al.GetAlias("text3").Match(none => none, some => some);
                if (alias != "overWrite"){
                    return msg;
                }
                msg.Add("2024/07/19 10:29");
                var names = al.GetNamesFuzzy("alias3");
                if (names.Count != 0){
                    return msg;
                }
                names = al.GetNamesFuzzy("overWrite");
                if (names.Count != 1){
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
        public List<string> TestRead()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/03 18:26"); // チェックポイント
                var al = new MAliasManager();
                var list = new List<string>()
                {
                    "text1:alias1",
                    "text2:alias2",
                    "text3:alias3"
                };
                Func<List<string>> func = () => list;
                al.Read(func);
                msg.Add("2024/06/04 09:01"); // チェックポイント
                var alias = al.GetAlias("text1").Match(none => none, some => some);
                if (alias != "alias1"){
                    return msg;
                }
                alias = al.GetAlias("text2").Match(none => none, some => some);
                if (alias != "alias2"){
                    return msg;
                }
                alias = al.GetAlias("text3").Match(none => none, some => some);
                if (alias != "alias3"){
                    return msg;
                }
                msg.Add("2024/06/04 09:03"); // チェックポイント
                al.Add("text3", "alias3", true);
                alias = al.GetAlias("text3").Match(none => none, some => some);
                if (alias != "alias3"){
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
        public List<string> TestGetAlias()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/07/26 10:40");
                var al = new MAliasManager();
                al.Add("text1", "alias1", false);
                al.Add("TEXT2", "alias2", false);
                al.Add("tExt3", "alias3", false);
                //
                msg.Add("2024/07/26 10:41");
                var alias = al.GetAlias("text2").Match(none => none, some => some);
                if (alias != "alias2"){
                    return msg;
                }
                msg.Add("2024/07/26 10:42");
                alias = al.GetAlias("text3").Match(none => none, some => some);
                if (alias != "alias3"){
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
                msg.Add("2024/06/04 09:16");
                var al = new MAliasManager();
                al.Add("text1", "alias1", false);
                al.Add("text2", "alias2", false);
                al.Add("text3", "alias3", false);
                msg.Add("2024/06/04 09:16");
                var alias = al.GetAlias("text1").Match(none => none, some => some);
                if (alias != "alias1"){
                    return msg;
                }
                msg.Add("2024/06/04 09:16");
                al.Remove("text1");
                if (al.GetAlias("text1").IsSome()){
                    return msg;
                }
                msg.Add("2024/07/19 10:25");
                alias = al.GetAlias("text2").Match(none => none, some => some);
                if (alias != "alias2"){
                    return msg;
                }
                alias = al.GetAlias("text3").Match(none => none, some => some);
                if (alias != "alias3"){
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
        public List<string> TestGetNamesFuzzy()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/04 09:20"); //チェックポイント
                var al = new MAliasManager();
                al.Add("text1", "alias1", false);
                al.Add("text2", "abcsd", false);
                al.Add("text3", "abbgs", false);
                msg.Add("2024/06/06 22:04");
                var names = al.GetNamesFuzzy("a*");
                if (names.Count != 3){
                    return msg;
                }
                names.Sort();
                msg.Add("2024/06/06 22:05");
                if (names[0] != "text1"){
                    return msg;
                }
                msg.Add("2024/06/06 22:06");
                if (names[1] != "text2"){
                    return msg;
                }
                msg.Add("2024/06/06 22:07");
                if (names[2] != "text3"){
                    return msg;
                }
                names = al.GetNamesFuzzy("ab*");
                names.Sort();
                msg.Add("2024/06/06 22:07-1");
                if (names[0] != "text2"){
                    return msg;
                }
                msg.Add("2024/06/06 22:07-2");
                if (names[1] != "text3"){
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
        public List<string> TestSave()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/04 09:20"); //チェックポイント
                var al = new MAliasManager();
                al.Add("text1", "alias1", false);
                al.Add("text2", "alias2", false);
                al.Add("text3", "alias3", false);
                var path = Path.GetTempFileName();
                Action<IEnumerable<string>> save = list =>
                {
                    using (var writer = new StreamWriter(path))
                    {
                        foreach (var val in list)
                        {
                            writer.WriteLine(val);
                        }
                    }
                };
                msg.Add("2024/06/04 09:23"); //チェックポイント
                al.Save(save);
                msg.Add("2024/06/04 09:24"); //チェックポイント
                al = new MAliasManager();
                Func<List<string>> read = () =>
                {
                    //
                    using (var reader = new StreamReader(path))
                    {
                        var list = new List<string>();
                        while (!reader.EndOfStream)
                        {
                            list.Add(reader.ReadLine());
                        }
                        return list;
                    }
                };
                al.Read(read);
                msg.Add("2024/06/04 09:29"); //チェックポイント
                var alias = al.GetAlias("text1").Match(none => none, some => some);
                if (alias != "alias1"){
                    return msg;
                }
                alias = al.GetAlias("text2").Match(none => none, some => some);
                if (alias != "alias2"){
                    return msg;
                }
                alias = al.GetAlias("text3").Match(none => none, some => some);
                if (alias != "alias3"){
                    return msg;
                }
                File.Delete(path);
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

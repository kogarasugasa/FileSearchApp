using System;
using System.Collections.Generic;

namespace FileSearchApp
{
    public class MExFileInfoCollectionTest : TestBase
    {
        public override List<string> TestAll()
        {
            return base.TestAll<MExFileInfoCollectionTest>();
        }
        public List<string> TestTryAdd()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/03 11:27");
                var ar = new ListViewDataSource();
                ar.Add(new ExFileInfo(@"c:\test\1.c"));
                ar.Add(new ExFileInfo(@"c:\test\2.c"));
                ar.Add(new ExFileInfo(@"c:\test\3.c"));
                ar.Add(new ExFileInfo(@"c:\test\4.c"));
                ar.Add(new ExFileInfo(@"c:\test\5.c"));
                msg.Add("2024/06/03 11:28");
                if (ar.Count != 5){
                    return msg;
                }
                msg.Add("2024/07/22 18:05");
                ar.Add(new ExFileInfo(@"c:\TEST\3.C"));
                if (ar.Count != 5){
                    return msg;
                }
                msg.Add("2024/06/03 11:29");
                var vl1 = ar.Add(new ExFileInfo(@"c:\test\3.c"));
                if (vl1 == true){
                    return msg;
                }
                msg.Add("2024/06/03 11:30");
                var vl2 = ar.Add(new ExFileInfo(@"c:\test\6.c"));
                if (vl2 == false){
                    return msg;
                }
                msg.Add("2024/07/22 17:35");
                var info = new ExFileInfo(@"c:\test\3.c"){
                    Size = 100,
                };
                ar.Add(info);
                if (ar.ElementAt(2).Size != 100){
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
        public List<string> TestElementAt()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/03 11:33");
                var ar = new ListViewDataSource();
                ar.Add(new ExFileInfo(@"c:\test\1.c"));
                ar.Add(new ExFileInfo(@"c:\test\3.c"));
                ar.Add(new ExFileInfo(@"c:\test\2.c"));
                msg.Add("2024/06/03 11:37");
                if (ar.ElementAt(0).FilePath != @"c:\test\1.c"){
                    return msg;
                }
                if (ar.ElementAt(1).FilePath != @"c:\test\3.c"){
                    return msg;
                }
                if (ar.ElementAt(2).FilePath != @"c:\test\2.c"){
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
        public List<string> TestClear()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/03 11:38");
                var ar = new ListViewDataSource();
                ar.Add(new ExFileInfo(@"c:\test\1.c"));
                ar.Add(new ExFileInfo(@"c:\test\3.c"));
                ar.Add(new ExFileInfo(@"c:\test\2.c"));
                ar.Clear();
                msg.Add("2024/06/03 11:39");
                if (ar.Count != 0){
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
        public List<string> TestSort()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/03 11:40");
                var ar = new ListViewDataSource();
                var i1 = new ExFileInfo(@"c:\test\1.c"){Size = 1000};
                var i2 = new ExFileInfo(@"c:\test\3.c"){Size = 1500};
                var i3 = new ExFileInfo(@"c:\test\2.c"){Size = 10};
                var i4 = new ExFileInfo(@"c:\test1\2.c"){Size = 2100000};
                var i5 = new ExFileInfo(@"c:\test2\1.c"){Size = 1450};
                ar.Add(i1);
                ar.Add(i2);
                ar.Add(i3);
                ar.Add(i4);
                ar.Add(i5);
                ar.Sort("Path", System.Windows.Forms.SortOrder.Ascending);
                msg.Add("2024/06/03 11:42");
                if (ar.ElementAt(0).FilePath != @"c:\test\1.c"){
                    return msg;
                }
                if (ar.ElementAt(1).FilePath != @"c:\test\2.c"){
                    return msg;
                }
                if (ar.ElementAt(2).FilePath != @"c:\test\3.c"){
                    return msg;
                }
                if (ar.ElementAt(3).FilePath != @"c:\test1\2.c"){
                    return msg;
                }
                if (ar.ElementAt(4).FilePath != @"c:\test2\1.c"){
                    return msg;
                }
                ar.Sort("Path", System.Windows.Forms.SortOrder.Descending);
                msg.Add("2024/06/03 11:47");
                if (ar.ElementAt(0).FilePath != @"c:\test2\1.c"){
                    return msg;
                }
                if (ar.ElementAt(1).FilePath != @"c:\test1\2.c"){
                    return msg;
                }
                if (ar.ElementAt(2).FilePath != @"c:\test\3.c"){
                    return msg;
                }
                if (ar.ElementAt(3).FilePath != @"c:\test\2.c"){
                    return msg;
                }
                if (ar.ElementAt(4).FilePath != @"c:\test\1.c"){
                    return msg;
                }
                ar.Sort("FileName", System.Windows.Forms.SortOrder.Ascending);
                msg.Add("2024/06/03 11:48");
                if (ar.ElementAt(0).FilePath != @"c:\test2\1.c" &&
                    ar.ElementAt(0).FilePath != @"c:\test\1.c"){
                    return msg;
                }
                if (ar.ElementAt(1).FilePath != @"c:\test2\1.c" &&
                    ar.ElementAt(1).FilePath != @"c:\test\1.c"){
                    return msg;
                }
                if (ar.ElementAt(2).FilePath != @"c:\test1\2.c" &&
                    ar.ElementAt(2).FilePath != @"c:\test\2.c"){
                    return msg;
                }
                if (ar.ElementAt(3).FilePath != @"c:\test1\2.c" &&
                    ar.ElementAt(3).FilePath != @"c:\test\2.c"){
                    return msg;
                }
                if (ar.ElementAt(4).FilePath != @"c:\test\3.c"){
                    return msg;
                }
                ar.Sort("Size", System.Windows.Forms.SortOrder.Ascending);
                msg.Add("2024/06/03 11:50");
                if (ar.ElementAt(0).FilePath != @"c:\test\2.c"){
                    return msg;
                }
                if (ar.ElementAt(1).FilePath != @"c:\test\1.c"){
                    return msg;
                }
                if (ar.ElementAt(2).FilePath != @"c:\test2\1.c"){
                    return msg;
                }
                if (ar.ElementAt(3).FilePath != @"c:\test\3.c"){
                    return msg;
                }
                if (ar.ElementAt(4).FilePath != @"c:\test1\2.c"){
                    return msg;
                }
                ar.Sort("Size", System.Windows.Forms.SortOrder.Descending);
                msg.Add("2024/06/03 11:58");
                if (ar.ElementAt(0).FilePath != @"c:\test1\2.c"){
                    return msg;
                }
                if (ar.ElementAt(1).FilePath != @"c:\test\3.c"){
                    return msg;
                }
                if (ar.ElementAt(2).FilePath != @"c:\test2\1.c"){
                    return msg;
                }
                if (ar.ElementAt(3).FilePath != @"c:\test\1.c"){
                    return msg;
                }
                if (ar.ElementAt(4).FilePath != @"c:\test\2.c"){
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
        public List<string> Test_Filter_TryAdd()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/03 14:32");
                var ar = new ListViewDataSource();
                ar.ApplyFilter(new Filter<ExFileInfo>(info => info.FileName.Contains("1")));
                ar.Add(new ExFileInfo(@"c:\test\1.c"));
                ar.Add(new ExFileInfo(@"c:\test\3.c"));
                ar.Add(new ExFileInfo(@"c:\test\2.c"));
                ar.Add(new ExFileInfo(@"c:\test\4.c"));
                msg.Add("2024/06/03 17:43");
                if (ar.Count != 1){
                    return msg;
                }
                msg.Add("2024/06/03 17:44");
                if (ar.ElementAt(0).FilePath != @"c:\test\1.c"){
                    return msg;
                }
                msg.Add("2024/06/03 17:45");
                ar.ApplyFilter(new Filter<ExFileInfo>());
                if (ar.Count != 4){
                    return msg;
                }
                msg.Add("2024/06/03 17:46");
                if (ar.ElementAt(0).FilePath != @"c:\test\1.c"){
                    return msg;
                }
                if (ar.ElementAt(1).FilePath != @"c:\test\3.c"){
                    return msg;
                }
                if (ar.ElementAt(2).FilePath != @"c:\test\2.c"){
                    return msg;
                }
                if (ar.ElementAt(3).FilePath != @"c:\test\4.c"){
                    return msg;
                }
                msg.Add("2024/06/03 17:53");
                ar.ApplyFilter(new Filter<ExFileInfo>(info => info.FileName.Contains("1")));
                if (ar.Count != 1){
                    return msg;
                }
                if (ar.ElementAt(0).FilePath != @"c:\test\1.c"){
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

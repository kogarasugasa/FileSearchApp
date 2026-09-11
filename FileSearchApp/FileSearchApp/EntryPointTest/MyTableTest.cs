using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FileSearchApp
{
    public class MyTableTest : TestBase
    {
        public override List<string> TestAll()
        {
            return base.TestAll<MyTableTest>();
        }
        public List<string> TestInit()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2025/03/14 16:56"); //チェックポイント
                var table = new TestTable();
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestToPrimaryKeyOnly()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2025/03/14 16:56"); //チェックポイント
                var table = new TestTable();
                var primary = table.ToPrimaryKeyOnly(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName"},
                    { TestEnum.LowerName, "filename" },
                    { TestEnum.Tag, "Tag" },
                });
                if (primary.Keys.Count != 2){
                    return msg;
                }
                msg.Add("2025/03/14 17:32"); //チェックポイント
                if (primary.Keys.ElementAt(0) != TestEnum.LowerName
                && primary.Keys.ElementAt(0) != TestEnum.Tag){
                    return msg;
                }
                msg.Add("2025/03/14 17:33"); //チェックポイント
                if (primary.Keys.ElementAt(1) != TestEnum.LowerName
                && primary.Keys.ElementAt(1) != TestEnum.Tag){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestExists()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                var table = new TestTable();
                msg.Add("2025/03/17 10:16"); //チェックポイント
                table.ZZ_DebugInsert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName1" },
                    { TestEnum.LowerName, "filename1" },
                    { TestEnum.Tag, "Tag1" }
                });
                table.ZZ_DebugInsert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName2" },
                    { TestEnum.LowerName, "filename2" },
                    { TestEnum.Tag, "Tag2" }
                });
                msg.Add("2025/03/14 17:04"); //チェックポイント
                var item = new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName1" },
                    { TestEnum.LowerName, "filename1" },
                    { TestEnum.Tag, "Tag1" }
                };
                if (!table.Exists(item)){
                    return msg;
                }
                msg.Add("2025/03/17 10:57"); //チェックポイント
                item = new Dictionary<TestEnum, string>{
                    { TestEnum.LowerName, "filename1" },
                    { TestEnum.Tag, "Tag1" }
                };
                if (!table.Exists(item)){
                    return msg;
                }
                msg.Add("2025/03/17 10:58"); //チェックポイント
                item = new Dictionary<TestEnum, string>{
                    { TestEnum.LowerName, "filename1" },
                };
                if (!table.Exists(item)){
                    return msg;
                }
                msg.Add("2025/03/14 17:05"); //チェックポイント
                item = new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName1" },
                    { TestEnum.LowerName, "filename1" },
                    { TestEnum.Tag, "Tag999" }
                };
                if (table.Exists(item)){
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
        public List<string> TestInsert()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                var table = new TestTable();
                msg.Add("2025/03/14 16:57"); //チェックポイント
                table.Insert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName" },
                    { TestEnum.LowerName, "filename" },
                    { TestEnum.Tag, "Tag" }
                });
                msg.Add("2025/03/14 16:58"); //チェックポイント
                var items = table.ZZ_DebugGetRecords();
                if (items.Count != 1){
                    return msg;
                }
                msg.Add("2025/03/14 16:59"); //チェックポイント
                if (items[items.Keys.First()][TestEnum.LowerName] != "filename"){
                    return msg;
                }
                msg.Add("2025/03/14 17:00"); //チェックポイント
                var isSuccess = table.Insert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName" },
                    { TestEnum.LowerName, "filename" },
                    { TestEnum.Tag, "Tag" }
                });
                if (isSuccess){
                    return msg;
                }
                msg.Add("2025/03/17 13:32"); //チェックポイント
                var indices = table.ZZ_DebugGetIndices();
                if (indices[TestEnum.LowerName]["filename"].Count != 1){
                    return msg;
                }
                msg.Add("2025/03/14 17:01"); //チェックポイント
                items = table.ZZ_DebugGetRecords();
                if (items.Count != 1){
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
        public List<string> TestDelete()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2025/03/17 10:08"); // チェックポイント
                var table = new TestTable();
                table.Insert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName" },
                    { TestEnum.LowerName, "filename" },
                    { TestEnum.Tag, "Tag" }
                });
                table.Insert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName2" },
                    { TestEnum.LowerName, "filename2" },
                    { TestEnum.Tag, "Tag2" }
                });
                msg.Add("2025/03/14 17:02"); // 消えるかテスト
                var item = new Dictionary<TestEnum, string>{
                    { TestEnum.LowerName, "filename" },
                    { TestEnum.Tag, "Tag" }
                };
                table.Delete(item);
                var items = table.ZZ_DebugGetRecords();
                if (items.Count != 1){
                    return msg;
                }
                msg.Add("2025/03/14 17:03"); // 正しいものが消えたかテスト
                if (items.First().Value[TestEnum.FileName] != "FileName2"){
                    return msg;
                }
                msg.Add("2025/03/14 17:05"); // 存在しない物を消しても大丈夫かテスト
                table.Delete(new Dictionary<TestEnum, string>{
                    { TestEnum.LowerName, "filename" },
                    { TestEnum.Tag, "Tag" }
                });
                items = table.ZZ_DebugGetRecords();
                if (items.Count != 1){
                    return msg;
                }
                msg.Add("2025/03/17 13:36"); // インデックスのテスト
                var indices = table.ZZ_DebugGetIndices();
                if (indices[TestEnum.LowerName].ContainsKey("filename")){
                    return msg;
                }
                msg.Add("2025/03/17 13:37"); // インデックスのテスト
                if (indices[TestEnum.LowerName]["filename2"].Count != 1){
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
        public List<string> TestUpdate()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2025/03/17 10:41"); //チェックポイント
                var table = new TestTable();
                var record = table.GetNewRecord();
                record[TestEnum.FileName] = "FileName1";
                record[TestEnum.LowerName] = "filename1";
                record[TestEnum.Tag] = "tag1";
                table.Insert(record);
                table.Insert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName2"},
                    { TestEnum.LowerName, "filename2"},
                    { TestEnum.Tag, "tag2"},
                });
                msg.Add("2025/03/17 10:42"); //チェックポイント
                if (table.Count != 2){
                    return msg;
                }
                msg.Add("2025/03/17 10:43"); //チェックポイント
                var oldItem = new Dictionary<TestEnum, string>{
                    { TestEnum.LowerName, "filename1"},
                    { TestEnum.Tag, "tag1"},
                };
                var newItem = new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "filename1-1"},
                    { TestEnum.LowerName, "filename1-1"},
                    { TestEnum.Tag, "tag1-1"},
                };
                table.Update(oldItem, newItem);
                msg.Add("2025/03/17 10:44"); //チェックポイント
                if (table.Count != 2){
                    return msg;
                }
                msg.Add("2025/03/17 10:45"); //チェックポイント
                if (table.Exists(oldItem)){
                    return msg;
                }
                msg.Add("2025/03/17 10:46"); //チェックポイント
                if (!table.Exists(newItem)){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestWhere()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                var table = new TestTable();
                msg.Add("2025/03/17 14:04"); //チェックポイント
                table.Insert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName1" },
                    { TestEnum.LowerName, "filename1" },
                    { TestEnum.Tag, "tag1" },
                });
                table.Insert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName2" },
                    { TestEnum.LowerName, "filename2" },
                    { TestEnum.Tag, "tag12" },
                });
                table.Insert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName3" },
                    { TestEnum.LowerName, "filename3" },
                    { TestEnum.Tag, "tag3" },
                });
                table.Insert(new Dictionary<TestEnum, string>{
                    { TestEnum.FileName, "FileName4" },
                    { TestEnum.LowerName, "filename4" },
                    { TestEnum.Tag, "tag4" },
                });
                msg.Add("2025/03/17 14:04"); //チェックポイント
                
                if (DateTime.Now != DateTime.Now){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
    }
    public enum TestEnum { FileName, LowerName, Tag }
    class TestTable : MyTable<TestEnum, string>
    {
        public override List<TestEnum> InitializeIndex()
        {
            return new List<TestEnum>();
        }
        public override List<TestEnum> InitializePrimaryKey()
        {
            return new List<TestEnum>() {
                TestEnum.LowerName,
                TestEnum.Tag
            };
        }
    }
    public abstract partial class MyTable<T ,U>
    {
        public Dictionary<ulong, Dictionary<T, U>> ZZ_DebugGetRecords()
        {
            return _records;
        }
        public void ZZ_DebugInsert(Dictionary<T, U> pRecord)
        {
            _records.Add(_num++, pRecord);
        }
        public Dictionary<T, Dictionary<U, List<ulong>>> ZZ_DebugGetIndices()
        {
            return _indices;
        }
    }
}
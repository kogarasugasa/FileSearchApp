using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FileSearchApp
{
    public static class Test
    {
        public static void Main(string[] args)
        {
            var testMsg = new List<string>();
            Action<string> fn = str => testMsg.Add(str);
            //以下テストインスタンス記述部
            new MHistoryManagerTest().TestAll().ForEach(fn);
            new MHistorySearcherTest().TestAll().ForEach(fn);
            new MExFileInfoCollectionTest().TestAll().ForEach(fn);
            new MAliasManagerTest().TestAll().ForEach(fn);
            new MTagManagerTest().TestAll().ForEach(fn);
            new MyToolTest().TestAll().ForEach(fn);
            new SortedKeyValueTest().TestAll().ForEach(fn);
            new MWindowsSearcherTest().TestAll().ForEach(fn);
            new ExFileInfoTest().TestAll().ForEach(fn);
            new MyTableTest().TestAll().ForEach(fn);
            new KeyWordTest().TestAll().ForEach(fn);
            //以下結果表示部
            Console.Clear();
            testMsg.ForEach(msg => Console.WriteLine(msg));
            Console.WriteLine("-   --   --   --   --   --   --   -");
            if (testMsg.Count == 0){
                Console.WriteLine("Test completed. All Test Case Successfully");
            }else{
                Console.WriteLine("Test completed. Error is Found");
            }
            //Console.ReadLine();
        }
    }
}

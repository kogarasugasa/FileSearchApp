using System;
using System.Collections.Generic;

namespace FileSearchApp
{
    public class MyToolTest : TestBase
    {
        public override List<string> TestAll()
        {
            return base.TestAll<MyToolTest>();
        }
        public List<string> TestFuzzyContains()
        {
            var msg = new List<string>();
            msg.Add(GetType().Name);
            msg.Add(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                //以下テストコード
                msg.Add("2024/06/06 20:20-1");
                if (!CustomFunctions.MyTool.FuzzyContains("*th*", "Method")){
                    return msg;
                }
                msg.Add("2024/06/06 20:20-2");
                if (CustomFunctions.MyTool.FuzzyContains("*tha*", "Method")){
                    return msg;
                }
                msg.Add("2024/06/06 20:20-3");
                if (!CustomFunctions.MyTool.FuzzyContains("*th*d*", "Method")){
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

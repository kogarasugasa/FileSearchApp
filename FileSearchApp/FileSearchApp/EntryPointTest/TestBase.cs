using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace FileSearchApp
{
    public abstract class TestBase
    {
        // ======================================================================================
        // テストメソッドサンプル
        // ======================================================================================
        public List<string> CodeSample()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/05/31 15:52"); //チェックポイント
                if (DateTime.Now != DateTime.Now){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public abstract List<string> TestAll(); //override するとき <T> に入れるのはに継承先のクラス
        protected List<string> TestAll<T>()
        {
            var methods = typeof(T).GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
            );
            var testMsg = new List<string>();
            foreach (var m in methods)
            {
                //このメソッドを除外しないと無限ループする
                if (m.Name == "TestAll"){
                    continue;
                }
                var ins = this;
                var objects = new object[0];
                List<string> methodMsg = (List<string>)m.Invoke(ins,objects);
                for (int i = methodMsg.Count - 4; i >= 0; i--)
                {
                    methodMsg.RemoveAt(2);
                }
                if (methodMsg.Count != 0){
                    testMsg.Add(string.Join(" / ", methodMsg));
                }
            }
            return testMsg;
        }
    }
}

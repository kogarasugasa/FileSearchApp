using System;
using System.Collections.Generic;
using System.Reflection;

namespace FileSearchApp
{
    public class KeyWordTest : TestBase
    {
        // ======================================================================================
        // テストメソッドサンプル
        // ======================================================================================
        public List<string> TestMatchTo()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2025/03/24 09:23"); //チェックポイント
                var kw = new KeyWord { Word = "*", Fuzzy = true };
                var info = new ExFileInfo(@"C:\Directory\Test.txt");
                if (!kw.MatchTo(info, MatchToExtension.Include)){
                    return msg;
                }
                msg.Add("2025/03/24 09:24"); //チェックポイント
                if (!kw.MatchTo(info, MatchToExtension.Exclude)){
                    return msg;
                }
                msg.Add("2025/03/24 09:25"); //チェックポイント
                kw = new KeyWord { Word = ".txt", Fuzzy = true };
                if (!kw.MatchTo(info, MatchToExtension.Include)){
                    return msg;
                }
                msg.Add("2025/03/24 09:26"); //チェックポイント
                kw = new KeyWord { Word = ".txt", Fuzzy = true };
                if (kw.MatchTo(info, MatchToExtension.Exclude)){
                    return msg;
                }
                msg.Add("2025/03/24 09:27"); //チェックポイント
                kw = new KeyWord { Word = "es", Fuzzy = true };
                if (!kw.MatchTo(info, MatchToExtension.Include)){
                    return msg;
                }
                msg.Add("2025/03/24 09:28"); //チェックポイント
                kw = new KeyWord { Word = "es", Fuzzy = true };
                if (!kw.MatchTo(info, MatchToExtension.Exclude)){
                    return msg;
                }
                msg.Add("2025/03/24 09:29"); //チェックポイント
                kw = new KeyWord { Word = "Dir", Fuzzy = true };
                if (kw.MatchTo(info, MatchToExtension.Include)){
                    return msg;
                }
                msg.Add("2025/03/24 09:30"); //チェックポイント
                kw = new KeyWord { Word = "Dir", Fuzzy = true };
                if (kw.MatchTo(info, MatchToExtension.Exclude)){
                    return msg;
                }
                msg.Add("2025/03/24 09:31"); //チェックポイント
                kw = new KeyWord { Word = "test", Fuzzy = false };
                if (kw.MatchTo(info, MatchToExtension.Include)){
                    return msg;
                }
                msg.Add("2025/03/24 09:32"); //チェックポイント
                kw = new KeyWord { Word = "test", Fuzzy = false };
                if (!kw.MatchTo(info, MatchToExtension.Exclude)){
                    return msg;
                }
                msg.Add("2025/03/24 09:33"); //チェックポイント
                kw = new KeyWord { Word = "test.txt", Fuzzy = false };
                if (!kw.MatchTo(info, MatchToExtension.Include)){
                    return msg;
                }
                msg.Add("2025/03/24 09:33"); //チェックポイント
                kw = new KeyWord { Word = "test.txt", Fuzzy = false };
                if (kw.MatchTo(info, MatchToExtension.Exclude)){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public override List<string> TestAll()
        {
            return base.TestAll<KeyWordTest>();
        }
    }
}

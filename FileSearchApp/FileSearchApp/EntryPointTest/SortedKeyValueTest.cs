using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FileSearchApp
{
    public class SortedKeyValueTest : TestBase
    {
        public List<string> TestConstructor()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/07/18 16:03"); //チェックポイント
                var skv = new SortedKeyValue();
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestAdd()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/07/18 16:03"); //チェックポイント
                var skv = new SortedKeyValue();
                skv.Add(new IndexKeyValue("f", "2"));
                skv.Add(new IndexKeyValue("a", "1"));
                skv.Add(new IndexKeyValue("g", "6"));
                skv.Add(new IndexKeyValue("e", "4"));
                skv.Add(new IndexKeyValue("j", "8"));
                skv.Add(new IndexKeyValue("b", "1"));
                msg.Add("2024/07/18 16:32");
                var start = new IndexKeyValue(string.Empty, "1", IndexKeyValue.UnderOrOver.Under);
                var end = new IndexKeyValue(string.Empty, "1", IndexKeyValue.UnderOrOver.Over);
                var infos = skv.GetRange(start, end);
                msg.Add("2024/07/18 16:33");
                if (infos.Count != 2){
                    return msg;
                }
                msg.Add("2024/07/18 16:34");
                if (!(infos[0].Key == "b" || infos[0].Key == "a")){
                    return msg;
                }
                if (!(infos[1].Key == "b" || infos[1].Key == "a")){
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
                msg.Add("2024/09/11 13:00"); //チェックポイント
                var skv = new SortedKeyValue();
                skv.Add(new IndexKeyValue("f", "2"));
                skv.Add(new IndexKeyValue("c", "1"));
                skv.Add(new IndexKeyValue("g", "6"));
                skv.Add(new IndexKeyValue("e", "4"));
                skv.Add(new IndexKeyValue("j", "8"));
                skv.Add(new IndexKeyValue("b", "1"));
                msg.Add("2024/09/11 13:01"); //チェックポイント
                if (skv.Count != 6){
                    return msg;
                }
                msg.Add("2024/09/11 13:02"); //チェックポイント
                skv.Remove(new IndexKeyValue(string.Empty, string.Empty));
                msg.Add("2024/09/11 13:03"); //チェックポイント
                if (skv.Count != 6){
                    return msg;
                }
                msg.Add("2024/09/11 13:04"); //チェックポイント
                skv.Remove(new IndexKeyValue("f", "2"));
                if (skv.Count != 5){
                    return msg;
                }
                msg.Add("2024/09/11 13:05"); //チェックポイント
                skv.Remove(new IndexKeyValue("e", "4"));
                if (skv.Count != 4){
                    return msg;
                }
                msg.Add("2024/09/11 13:06"); //チェックポイント
                var start = new IndexKeyValue(string.Empty, "1", IndexKeyValue.UnderOrOver.Under);
                var end = new IndexKeyValue(string.Empty, "8", IndexKeyValue.UnderOrOver.Over);
                var infos = skv.GetRange(start, end);
                msg.Add("2024/09/11 13:07"); //チェックポイント
                if (infos.Count != 4){
                    return msg;
                }
                msg.Add("2024/09/11 13:08"); //チェックポイント
                if (infos.Where(val => val.Key == "c").Count() != 1){
                    return msg;
                }
                msg.Add("2024/09/11 13:09"); //チェックポイント
                if (infos.Where(val => val.Key == "g").Count() != 1){
                    return msg;
                }
                msg.Add("2024/09/11 13:10"); //チェックポイント
                if (infos.Where(val => val.Key == "j").Count() != 1){
                    return msg;
                }
                msg.Add("2024/09/11 13:11"); //チェックポイント
                if (infos.Where(val => val.Key == "b").Count() != 1){
                    return msg;
                }
                msg.Add("2025/04/02 16:07"); //チェックポイント
                var comp = new IndexKeyValueComparer();
                comp.SetSortPriority(priority => {
                    priority.Key = IndexKeyValueComparer.SortPriority.Hi;
                    priority.Value = IndexKeyValueComparer.SortPriority.None;
                    priority.Stance = IndexKeyValueComparer.SortPriority.Lo;
                });
                skv = new SortedKeyValue(comp);
                skv.Add(new IndexKeyValue("Com.Fujitsu.Fks.Prones.PRONESMenu","PRONES"));
                skv.Add(new IndexKeyValue("EU912（FA0004414）積水窓Ｓ１（摘要表、仕様）","図面_非_仕様"));
                skv.Add(new IndexKeyValue("EU912（FA0004414）積水窓Ｓ２（納まり図）","図面_非_納まり図"));
                skv.Add(new IndexKeyValue("EU912（FA0004414）積水窓Ｓ３（本体・カーテン組立）","図面_非_本体組立_ｼｬｯﾀｰｶｰﾃﾝ"));
                skv.Add(new IndexKeyValue("EU912（FA0004414）積水窓Ｓ４（ケース組立・カバー類）","図面_非_ｹｰｽ組立"));
                skv.Add(new IndexKeyValue("EU912（FA0004414）積水窓Ｓ５（金具・レール・水切板）","図面_非_取付金具_ﾚｰﾙ_水切り"));
                skv.Add(new IndexKeyValue("EU912（FA0004414）積水窓Ｓ６（電装部品・形材図・寸法表・記号説明）","図面_非_電装部品_形材図_寸法表_ｽﾌﾟﾘﾝｸﾞ表"));
                skv.Add(new IndexKeyValue("EW251(FA0003928 )積水ハウス 小型窓シャッター（ｆｇⅡ）","図面_防"));
                skv.Add(new IndexKeyValue("LOG","サーバーバックアップログ"));
                skv.Add(new IndexKeyValue("mirror_01","端末バックアップバッチ"));
                skv.Add(new IndexKeyValue("rBOM V4","rBOM"));
                skv.Add(new IndexKeyValue("showmsg","邪魔なブラウザポップアップを無効化するフォルダ"));
                skv.Add(new IndexKeyValue("SPACE PORTER 入力済","SPACE PORTER"));
                skv.Add(new IndexKeyValue("sqldevelOper","SQLDeveloper"));
                skv.Add(new IndexKeyValue("ST","rBOMテスト環境"));
                skv.Add(new IndexKeyValue("構成情報","大和窓シャッター構成情報"));
                skv.Add(new IndexKeyValue("購買_AmadaVfactory.com","金型購入 アマダ 22e5e9Q9p41,323z64gf"));
                skv.Add(new IndexKeyValue("購買_Conic_金型Express","金型購入 コニック"));
                skv.Add(new IndexKeyValue("窓シャッター_ラベル確認","積水窓シャッター_ラベルチェック表"));
                skv.Add(new IndexKeyValue("中身検索","rBOMストアド内容検索"));
                msg.Add("2025/04/02 16:08"); //チェックポイント
                int cnt = skv.Count;
                skv.Remove(new IndexKeyValue("林工業㈱_SD_ベンダー金型図面", ""));
                msg.Add("2025/04/02 16:09"); //チェックポイント
                if (skv.Count != cnt){
                    return msg;
                }
                // 以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public List<string> TestGetRange()
        {
            var msg = new List<string>(){GetType().Name,MethodBase.GetCurrentMethod().Name};
            try
            {
                //以下テストコード
                msg.Add("2024/07/18 16:03"); //チェックポイント
                var skv = new SortedKeyValue();
                skv.Add(new IndexKeyValue("f", "2"));
                skv.Add(new IndexKeyValue("a", "1"));
                skv.Add(new IndexKeyValue("g", "6"));
                skv.Add(new IndexKeyValue("e", "4"));
                skv.Add(new IndexKeyValue("j", "8"));
                skv.Add(new IndexKeyValue("b", "1"));
                msg.Add("2024/07/18 16:32");
                var start = new IndexKeyValue(string.Empty, "1", IndexKeyValue.UnderOrOver.Under);
                var end = new IndexKeyValue(string. Empty, "1", IndexKeyValue.UnderOrOver.Over);
                var infos = skv.GetRange(start, end);
                msg.Add("2024/07/18 16:33");
                if (infos.Count != 2){
                    return msg;
                }
                bool tes = infos
                    .Select(val => val.Key)
                    .Any(val => new string[]{"a", "b"}.Contains(val));
                msg.Add("2024/07/18 16:34");
                if (!tes){
                    return msg;
                }
                msg.Add("2024/07/18 16:54");
                start = new IndexKeyValue(string.Empty, "2", IndexKeyValue.UnderOrOver.Under);
                end = new IndexKeyValue(string.Empty, "2", IndexKeyValue.UnderOrOver.Over);
                infos = skv.GetRange(start, end);
                if (infos.Count != 1){
                    return msg;
                }
                tes = infos
                    .Select(val => val.Key)
                    .Any(val => new string[]{"f"}.Contains(val));
                if (!tes){
                    return msg;
                }
                msg.Add("2024/07/18 16:55");
                start = new IndexKeyValue(string.Empty, "3", IndexKeyValue.UnderOrOver.Under);
                end = new IndexKeyValue(string.Empty, "8", IndexKeyValue.UnderOrOver.Over);
                infos = skv.GetRange(start, end);
                if (infos.Count != 3){
                    return msg;
                }
                tes = infos
                    .Select(val => val.Key)
                    .Any(val => new string[]{"g", "e", "j"}.Contains(val));
                if (!tes){
                    return msg;
                }
                //以上テストコード
            }
            catch (Exception ex){msg[msg.Count - 1] += ":err " + ex.Message;return msg;}
            return new List<string>();
        }
        public override List<string> TestAll()
        {
            return base.TestAll<SortedKeyValueTest>();
        }
    }
}

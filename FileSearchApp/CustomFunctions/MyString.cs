
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace CustomFunctions
{
    public static class MyString
    {
        static readonly string _harfChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
            + "abcdefghijklmnopqrstuvwxyz"
            + "ｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉ"
            + "ﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜｦﾝｧｨｩｪｫｯ"
            + "0123456789+-*/()<>{}"
            + "!\"#$%&'=^~\\|@`;:,?_"
            + "ｰ "
        ;
        static readonly List<string> _harfChars2 = new List<string>{
            "ｳﾞ",
            "ｶﾞ","ｷﾞ","ｸﾞ","ｹﾞ","ｺﾞ",
            "ｻﾞ","ｼﾞ","ｽﾞ","ｾﾞ","ｿﾞ",
            "ﾀﾞ","ﾁﾞ","ﾂﾞ","ﾃﾞ","ﾄﾞ",
            "ﾊﾞ","ﾋﾞ","ﾌﾞ","ﾍﾞ","ﾎﾞ",
        };
        static readonly string _wideChars = "ＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰＱＲＳＴＵＶＷＸＹＺ"
            + "ａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚ"
            + "アイウエオカキクケコサシスセソタチツテトナニヌネノ"
            + "ハヒフヘホマミムメモヤユヨラリルレロワヲンァィゥェォッ"
            + "０１２３４５６７８９＋－＊／（）＜＞｛｝"
            + "！”＃＄％＆’＝＾～￥｜＠‘；：、？＿"
            + "ー　"
        ;
        static readonly string _wideChars2 = "ヴガギグゲゴザジズゼゾダヂヅデドバビブベボ";
        public static string ToHarf(this string self)
        {
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < self.Length; i++)
            {
                var num = _wideChars.IndexOf(self[i]);
                if (num != -1){
                    builder.Append(_harfChars[num]);
                    continue;
                }
                num = _wideChars2.IndexOf(self[i]);
                if (num != -1){
                    builder.Append(_harfChars2[num]);
                    continue;
                }
                builder.Append(self[i]);
            }
            return builder.ToString();
        }
        public static int HarfLength(this string self)
        {
            int cnt = 0;
            var chars2 = _harfChars2.SelectMany(val => val).Distinct();
            foreach (var cha in self)
            {
                if (_harfChars.Contains(cha) || chars2.Contains(cha)){
                    cnt++;
                }
                else{
                    cnt += 2;
                }
            }
            return cnt;
        }
        public static int GetLevenshteinDistance(this string self, string pString)
        {
            var str1 = self;
            var str2 = pString;
            //コスト計算配列を準備
            int[,] costMatrix = new int[str2.Length + 1, str1.Length + 1];
            for (int i = 1; i <= str1.Length; i++)
            {
                costMatrix[0, i] = i;
            }
            for (int i = 1; i <= str2.Length; i++)
            {
                costMatrix[i, 0] = i;
            }
            //コスト計算
            int Min;
            for (int i = 1; i <= str2.Length; i++)
            {
                for (int j = 1; j <= str1.Length; j++)
                {
                    Min = 0;
                    if (str1.Substring(j - 1, 1) != str2.Substring(i - 1, 1)){
                        //小さい値を求める
                        if (costMatrix[i, j - 1] > costMatrix[i - 1, j]){
                            Min = costMatrix[i - 1, j];
                        }
                        else{
                            Min = costMatrix[i, j - 1];
                        }
                        costMatrix[i, j] = Min + 1;
                    }
                    else{
                        costMatrix[i, j] = costMatrix[i - 1, j - 1];
                    }
                }
            }
            return costMatrix[str2.Length, str1.Length];
        }
        public static bool ContainsFuzzy(this string self, string pSearchWord)
        {
            if (self == ""){
                return false;
            }
            var loWord = self.ToLower();
            var loWordItems = pSearchWord
                .ToLower()
                .Split('*')
                .Where(val => val != "")
            ;
            if (!loWordItems.Any()){
                return false;
            }
            var searchPos = 0;
            foreach (var loWordItem in loWordItems)
            {
                if (searchPos > loWord.Length - 1){
                    return false;
                }
                int findPos = loWord.IndexOf(loWordItem, searchPos);
                if (findPos == -1){
                    return false;
                }
                searchPos = findPos + loWordItem.Length;
            }
            return true;
        }
        public static int CharArrayCompareTo(this string x, char[] y)
        {
            var min = x.Length < y.Length ? x.Length : y.Length;
            for(int i = 0; i < min; i++)
            {
                var r =x[i].CompareTo(y[i]);
                if (r == 0){
                    continue;
                }
                else if (r > 0){
                    return 1;
                }
                else{
                    return -1;
                }
            }
            return x.Length.CompareTo(y.Length);
        }
    }
}
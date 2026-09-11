using System;
using System.Linq;
using System.Collections.Generic;
using CustomFunctions;
using System.Drawing;

namespace FileSearchApp
{
    public static class MsPGothic
    {
        // ======+=========+=========+=========+=========+=========+=========+=========+
        // = フィールド
        // ======+=========+=========+=========+=========+=========+=========+=========+
        static readonly SortedSet<char> Chars_02_Pixels = new SortedSet<char>("'.;:,".ToArray());
        static readonly SortedSet<char> Chars_03_Pixels = new SortedSet<char>("Iil!|ﾞ".ToArray());
        static readonly SortedSet<char> Chars_04_Pixels = new SortedSet<char>(" fjrt[]ﾞ".ToArray());
        static readonly SortedSet<char> Chars_05_Pixels =
            new SortedSet<char>("hkpqxz/*+\"#$%&()=~^\\{}<>?_`ｧｨｩｪｫｯ".ToArray());
        static readonly SortedSet<char> Chars_06_Pixels =
            new SortedSet<char>("abcegdLnosuvyw-0123456789".ToArray());
        static readonly SortedSet<char> Chars_07_Pixels = new SortedSet<char>("ES@".ToArray());
        static readonly SortedSet<char> Chars_08_Pixels =
            new SortedSet<char>("ABCDFGHJKNOPQRTUVXYZｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾉﾔﾕﾖﾗﾘﾙﾚﾛﾜｦﾝｰ".ToArray());
        static readonly SortedSet<char> Chars_09_Pixels = new SortedSet<char>("MmW".ToArray());
        //static readonly SortedSet<char> Chars_11_Pixels = new SortedSet<char>("漢字".ToArray());

        // ======+=========+=========+=========+=========+=========+=========+=========+
        // = メソッド
        // ======+=========+=========+=========+=========+=========+=========+=========+
        /// <summary>
        /// タブの幅に収まるように文字列を削ります
        /// </summary>
        public static string GetAdjustedText(string pSource)
        {
            const int MAX_PIXELS = 34;
            var sourceTrimed = pSource.Trim();
            if (sourceTrimed == string.Empty){
                return sourceTrimed;
            }
            if (Environment.UserName.ToLower() == "wahuku"){
                sourceTrimed = MsPGothic.RemoveInitialParenthesis(sourceTrimed);
            }
            double length = 0;
            for (int i = 0; i < sourceTrimed.Length; i++)
            {
                length += MsPGothic.GetWidthCustomValue(sourceTrimed[i]);
                if (length >= MAX_PIXELS){
                    sourceTrimed = new string(sourceTrimed.Take(i).ToArray());
                    break;
                }
            }
            return sourceTrimed;
        }
        public static string GetAdjustedText(string pSource, int pMaxPixels)
        {
            var sourceTrimed = pSource.Trim().ToHarf();
            if (sourceTrimed == string.Empty){
                return sourceTrimed;
            }
            if (Environment.UserName.ToLower() == "wahuku"){
                sourceTrimed = MsPGothic.RemoveInitialParenthesis(sourceTrimed);
            }
            double length = 0;
            var chars = sourceTrimed.ToCharArray();
            for (int i = 0; i < sourceTrimed.Length; i++)
            {
                length += MsPGothic.GetWidthCustomValue(chars[i]);
                if (length >= pMaxPixels){
                    sourceTrimed = new string(chars.Take(i).ToArray());
                    break;
                }
            }
            return sourceTrimed;
        }
        /// <summary>
        /// 文字列先頭の括弧'('から')'までを削除します
        /// </summary>
        public static string RemoveInitialParenthesis(string pSource)
        {
            if (pSource.StartsWith("(") || pSource.StartsWith("（")){
                int start = -1;
                int end = -1;
                for (int i = 0; i < pSource.Length; i++)
                {
                    if (pSource[i] == '（' || pSource[i] == '('){
                        start = i;
                    }
                    if (start != -1){
                        if (pSource[i] == '）' || pSource[i] == ')'){
                            end = i;
                            break;
                        }
                    }
                }
                if (end < pSource.Length -1){
                    return pSource.Substring(end + 1).Trim();
                }
            }
            return pSource;
        }
        /// <summary>
        /// 文字が占有する幅方向のピクセル数を返します
        /// </summary>
        public static int GetWidthCustomValue(char pCha)
        {
            if (Chars_02_Pixels.Contains(pCha)){
                return 2;
            }
            if (Chars_03_Pixels.Contains(pCha)){
                return 3;
            }
            else if (Chars_04_Pixels.Contains(pCha)){
                return 4;
            }
            else if (Chars_05_Pixels.Contains(pCha)){
                return 5;
            }
            else if (Chars_06_Pixels.Contains(pCha)){
                return 6;
            }
            else if (Chars_07_Pixels.Contains(pCha)){
                return 7;
            }
            else if (Chars_08_Pixels.Contains(pCha)){
                return 8;
            }
            else if (Chars_09_Pixels.Contains(pCha)){
                return 9;
            }
            return 12;
        }
        /// <summary>
        /// ＭＳ Ｐゴシックのフォントを返します
        /// </summary>
        public static Font GetFont(int pSize)
        {
            return new Font("ＭＳ Ｐゴシック", pSize);
        }
    }
}
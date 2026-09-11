using System;
using System.Linq;
using System.Collections.Generic;

namespace CustomFunctions
{
    public static partial class SIUnitBinaryByte
    {
        /// <summary>
        /// 文字列の末尾が「K」キロ「M」メガなどを含む文字列をSI接頭語と解釈し変換します。
        /// 例として、TryParseFromSI("12K", out decimal value); value は 12000 になります
        /// </summary>
        public static bool TryParse(string pNumText, out decimal pValue)
        {
            var numText = pNumText.Trim().ToUpper();
            var matchedItems = _convTableByte.Where(val => numText.EndsWith(val.Key));
            if (!matchedItems.Any()){
                return decimal.TryParse(numText, out pValue);
            }
            var matched = matchedItems.First();
            var iniText = numText.Substring(0, numText.Length - matched.Key.Length);
            if (decimal.TryParse(iniText, out pValue)){
                pValue *= matched.Value;
                return true;
            }
            else{
                pValue = 0;
                return false;
            }
        }
        public static string ToString(decimal pNumber)
        {
            foreach (var item in _convTableByte)
            {
                if (item.Value * 9 < pNumber){
                    return Math.Floor(pNumber / item.Value).ToString() + item.Key;
                }
            }
            return pNumber.ToString() + " B";
        }
        static Dictionary<string, decimal> _convTable = new Dictionary<string, decimal>{
            //{ "qb", 1000000000000000000000000000000 },
            //{ "q",  1000000000000000000000000000000 },
            //{ "rb", 1000000000000000000000000000 },
            //{ "r",  1000000000000000000000000000 },
            //{ "yb", 1000000000000000000000000 },
            //{ "y",  1000000000000000000000000 },
            //{ "zb", 1000000000000000000000 }, //
            //{ "z",  1000000000000000000000 }, // decimalを超える領域
            //{ "eb", 1000000000000000000 },
            //{ "e",  1000000000000000000 },
            //{ "pb", 1000000000000000 },
            //{ "p",  1000000000000000 }, // 実用上必要ない領域

            { "TB", 1000000000000 },
            { "T",  1000000000000 },
            { "GB", 1000000000 },
            { "G",  1000000000 },
            { "MB", 1000000 },
            { "M",  1000000 },
            { "KB", 1000 },
            { "K",  1000 },
        };
        static Dictionary<string, decimal> _convTableByte = new Dictionary<string, decimal>(){
            //{"PB", 1125899906842620}, // 1024^5
            //{"P", 1125899906842620}, // 1024^5
            {"TB", 1099511627776}, // 1024^4
            {"T", 1099511627776}, // 1024^4
            {"BG", 1073741824}, // 1024^3
            {"G", 1073741824}, // 1024^3
            {"MB", 1048576}, // 1024^2
            {"M", 1048576}, // 1024^2
            {"KB", 1024}, // 1024^1
            {"K", 1024}, // 1024^1
        };
    }
}
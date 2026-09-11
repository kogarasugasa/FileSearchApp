using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FileSearchApp
{
    public static class TextMatch
    {
        static object _lock = new object();
        static CancellationTokenSource _cts = new CancellationTokenSource();
        static bool _run = false;
        public static void Cancel()
        {
            _cts.Cancel();
            Task.Delay(1000).Wait();
            lock (_lock) _cts = new CancellationTokenSource();
        }
        public static IEnumerable<string> GetMatchStrings(string str1, string str2)
        {
            lock (_lock){
                if (_run){
                    return new List<string>();
                }
                _run = true;
            }
            string vertical, horizontal;
            if (str1.Length <= str2.Length){
                vertical = str1;
                horizontal = str2;
            }
            else{
                vertical = str2;
                horizontal = str1;
            }
            var records = new bool[vertical.Length][];
            for (int v = 0; v < vertical.Length; v++){
                records[v] = new bool[horizontal.Length];
                for (int h = 0; h < horizontal.Length; h++){
                    records[v][h] = vertical[v] == horizontal[h];
                }
            }
            const int V = 0;
            const int H = 1;
            var pos = new int[2];
            var hLimit = 0;
            var hStrings = new List<string>(){ string.Empty };
            while (true)
            {
                lock (_lock)
                {
                    if (_cts.Token.IsCancellationRequested){
                        break;
                    }
                }
                // 最後まで行ったらループを抜ける
                if (pos[V] >= vertical.Length){
                    break;
                }
                // 次の行
                if (pos[H] >= records[pos[V]].Length){
                    pos[V]++;
                    pos[H] = hLimit;
                    continue;
                }
                if (records[pos[V]][pos[H]]){
                    hStrings[hStrings.Count - 1] += horizontal[pos[H]];
                    pos[V]++;
                    pos[H]++;
                    hLimit = pos[H]; // pos[H]++の後に配置(現在の次の行の値のため)
                }
                else{
                    hStrings.Add(string.Empty);
                    pos[H]++;
                }
            }
            int vLimit = 0;
            pos[V] = 0; pos[H] = 0;
            var vStrings = new List<string>(){ string.Empty };
            while (true)
            {
                lock (_lock)
                {
                    if (_cts.Token.IsCancellationRequested){
                        break;
                    }
                }
                // 最後まで行ったらループを抜ける
                if (pos[H] >= horizontal.Length){
                    break;
                }
                // 次の行
                if (pos[V] >= records.Length){
                    pos[V] = vLimit;
                    pos[H]++;
                    continue;
                }
                if (records[pos[V]][pos[H]]){
                    vStrings[vStrings.Count - 1] += vertical[pos[V]];
                    pos[V]++;
                    pos[H]++;
                    vLimit = pos[V]; // pos[V]++の後に配置(現在の次の行の値のため)
                }
                else{
                    vStrings.Add(string.Empty);
                    pos[V]++;
                }
            }
            if (hStrings.Select(val => val.Length).Sum() > vStrings.Select(val => val.Length).Sum()){
                lock (_lock) _run = false;
                return hStrings.Where(val => val != string.Empty);
            }
            else{
                lock (_lock) _run = false;
                return vStrings.Where(val => val != string.Empty);
            }
        }
    }
}
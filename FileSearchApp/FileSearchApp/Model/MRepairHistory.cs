using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using CustomFunctions;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public class MRepairHistory
    {
        // ============================================================
        // = プロパティ
        // ============================================================
        private MHistoryManager HistoryManager { get; set; }
        private MWindowsSearcher WindowsSearcher { get; set; }
        // ============================================================
        // = コンストラクタ、デストラクタ
        // ============================================================
        public MRepairHistory(MHistoryManager pHistory, MWindowsSearcher pSearcher)
        {
            this.HistoryManager = pHistory;
            this.WindowsSearcher = pSearcher;
        }
        // ============================================================
        // = メソッド
        // ============================================================
        public void StartRepair(
        IEnumerable<KeyValuePair<int, ExFileInfo>> pRepairInfos,
        CancellationToken ct)
        {
            var historyNotFoundItems = new Dictionary<int, ExFileInfo>();
            // 履歴にある物は削除しつつ、履歴に無いものに絞り込む
            foreach (var item in pRepairInfos)
            {
                // 履歴から一致するものを取得
                var hisInfos = this.HistoryManager.GetExFileInfos(item.Value.FileName);
                var validInfos = hisInfos
                    .Where(val => val.FilePath.ToLower() != item.Value.FilePath.ToLower()) // 検索してるパスと違うパス
                    .Where(val => File.Exists(val.FilePath) || Directory.Exists(val.FilePath)) // 存在するパス
                ;
                if (validInfos.Any()){
                    this.HistoryManager.Remove(item.Value.FilePath);
                    this.HistoryManager.AddToBuffer(validInfos.First().FilePath);
                    var args = new HistoryRepairedEventArgs(true, item.Key, item.Value);
                    this.HistoryRepaired.Invoke(this, args);
                    continue;
                }
                else{
                    historyNotFoundItems.Add(item.Key, item.Value);
                }
            }
            // 検索ワードの一致する所を探してあいまい検索出来る文字を作成する
            var repairNames = historyNotFoundItems.Select(val => val.Value.FileName);
            var repairKeyWords = this.GetMixSearchWords(repairNames, 15);
            // ファイルシステムから検索
            var searchDirs = this.WindowsSearcher.GetIncludeDirectorys();
            searchDirs = this.WindowsSearcher.DistinctDirectory(searchDirs);
            int step = 3;
            for (int i = 0; i < repairKeyWords.Count; i+=step)
            {
                if (ct.IsCancellationRequested){
                    return;
                }
                var words = repairKeyWords
                    .Skip(i)
                    .Take(Math.Min(step, repairKeyWords.Count - i))
                    .ToList()
                ;
                foreach (var foundInfo in this.WindowsSearcher.GetExFileInfos(searchDirs, words, ct))
                {
                    var allocFoundInfos = historyNotFoundItems
                        .Where(val => val.Value.FileName.ToLower() == foundInfo.FileName.ToLower())
                        .ToList()
                    ;
                    foreach (var allocFoundInfo in allocFoundInfos)
                    {
                        this.HistoryManager.Remove(allocFoundInfo.Value.FilePath);
                        this.HistoryManager.AddToBuffer(foundInfo.FilePath);
                        var args = new HistoryRepairedEventArgs(true, allocFoundInfo.Key, allocFoundInfo.Value);
                        this.HistoryRepaired.Invoke(this, args);
                        historyNotFoundItems.Remove(allocFoundInfo.Key);
                    }
                    if (!historyNotFoundItems.Any()){
                        break;
                    }
                }
                // 検索終了後今使ったワードに一致する物を見つからなかったのもとして扱う
                foreach (var word in words)
                {
                    var matchItems = historyNotFoundItems
                        .Where(val => MyTool.FuzzyContains(word.Word, val.Value.FileName))
                        .ToList()
                    ;
                    foreach (var matchItem in matchItems)
                    {
                        historyNotFoundItems.Remove(matchItem.Key);
                        var args = new HistoryRepairedEventArgs(false, matchItem.Key, matchItem.Value);
                        this.HistoryRepaired.Invoke(this, args);
                    }
                }
                if (!historyNotFoundItems.Any()){
                    break;
                }
            }
            // foreach (var item in historyNotFoundItems)
            // {
            //     var args = new HistoryRepairedEventArgs(false, item.Key, item.Value);
            //     this.HistoryRepaired.Invoke(this, args);
            // }
        }
        List<KeyWord> GetMixSearchWords(IEnumerable<string> pSearchWords, int pMinMatchSeq)
        {
            var words = pSearchWords.Distinct().ToList();
            var result = new List<KeyWord>();
            int i = 0;
            while (i < words.Count)
            {
                if (i == words.Count - 1){
                    result.Add(new KeyWord(words[i], false));
                    break;
                }
                var main = words[i];
                var sub = words.Skip(i + 1).ToList();
                var mix = this.ConvToMixSearchWords(main, sub, pMinMatchSeq);
                if (mix == main){
                    result.Add(new KeyWord(mix, false));
                }
                else{
                    result.Add(new KeyWord(mix, true));
                    var match = sub.Where(val => MyTool.FuzzyContains(mix, val));
                    foreach (var val in match) words.Remove(val);
                }
                i++;
            }
            return result;
        }
        string ConvToMixSearchWords(string pMainSearchWord, IEnumerable<string> pSearchWords, int pMinMatchSeq)
        {
            //var selectedIndices = new List<int>(this.ViewModelListView.SelectedIndices);
            var names = pSearchWords.Where(val => val != pMainSearchWord);
            var matchResults = names
                // 一致している文字列部分を取得する
                .Select(name => 
                    TextMatch.GetMatchStrings(pMainSearchWord.ToLower(), name.ToLower())
                )
                // 一致している文字列の文字数の合計が pMinMatchSeq 以上
                .Where(matchStrs =>
                    matchStrs.Select(matchStr => matchStr.Length).Sum() >= pMinMatchSeq
                )
            ;
            if (matchResults.Count() == 0){
                return pMainSearchWord;
            }
            // 一致している文字列の文字数の合計が一番小さいものを返す
            var sorted = matchResults.OrderBy(strs => strs.Select(str => str.Length).Sum());
            return string.Join("*", sorted.First());
        }
        // ============================================================
        // = 説明：イベント
        // ============================================================
        public event EventHandler<HistoryRepairedEventArgs> HistoryRepaired = delegate{};
    }
}
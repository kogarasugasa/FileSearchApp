using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using CustomFunctions;

namespace FileSearchApp
{
    public class VMDisabledHistory
    {
        readonly object _lock = new object();
        CancellationTokenSource _cts = new CancellationTokenSource();
        ConcurrentBag<string> _repairedPaths = new ConcurrentBag<string>();
        // ============================================================
        // = プロパティ
        // ============================================================
        bool _isSearching = false;
        public bool IsSearching
        {
            get { lock (_lock) { return _isSearching; } }
            private set { lock (_lock) {_isSearching = value; } }
        }
        private string _runCaption = "選択を検索し修正";
        public string RunCaption
        {
            get { return _runCaption; }
            private set
            {
                _runCaption = value;
                ViewModeRunCaptionChanged.Invoke(this, _runCaption);
            }
        }
        public VMListView ViewModelListView { get; private set; }
        public VMMainWindow MainWindow { get; private set; }
        public Func<ExFileInfo, bool> IsDeleteAlertInfo { get; set; }
        public MWindowsSearcher WindowsSearcher { get; set; }
        public MHistoryManager HistoryManager { get; set; }
        // ============================================================
        // = コンストラクタ、デストラクタ
        // ============================================================
        public VMDisabledHistory(
        VMMainWindow pMainWindow,
        MWindowsSearcher pWindowsSearcher,
        MHistoryManager pHistoryManager,
        VMListView pViewModelListView)
        {
            this.MainWindow = pMainWindow;
            this.WindowsSearcher = pWindowsSearcher;
            this.HistoryManager = pHistoryManager;
            this.ViewModelListView = pViewModelListView;
            this.IsDeleteAlertInfo = info => true;
        }
        ~VMDisabledHistory()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        // ============================================================
        // = メソッド
        // ============================================================
        public async Task StartSearchFolderSelectionAsync()
        {
            Func<List<string>> bak = () => new List<string>();
            bak = this.WindowsSearcher.GetIncludeDirectorys;
            var dirs = this.WindowsSearcher.GetIncludeDirectorys();
            var empty = new List<string>();
            var title = "検索するフォルダを選択を選択してください";
            this.WindowsSearcher.GetIncludeDirectorys = () => {
                return MInputBoxChoose.GetItems(dirs, empty, title, 600).GetOrDefault(empty);
            };
            await this.StartSearch();
            this.WindowsSearcher.GetIncludeDirectorys = bak;
        }
        public async Task StartSearch()
        {
            lock (_lock)
            {
                if (_isSearching){
                    return;
                }
                _isSearching = true;
            }
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            // 検索
            var indices = new List<int>(this.ViewModelListView.SelectedIndices);
            var items = new Dictionary<int, ExFileInfo>();
            foreach (var index in indices)
            {
                items.Add(index, this.ViewModelListView.GetExFileInfo(index));
            }
            var repairer = new MRepairHistory(this.HistoryManager, this.WindowsSearcher);
            repairer.HistoryRepaired += (s ,e) =>
            {
                if (e.ItemFound){
                    _repairedPaths.Add(e.OldInfo.FilePath);
                    this.ViewModelListView.UnSelectItem(e.ItemIndex);
                }
                else{
                    this.ViewModelListView.UnSelectItem(e.ItemIndex);
                }
                this.ViewModelListView.ChangeToEnableRedrawOnce();
            };
            var tk = Task.Run(() => repairer.StartRepair(items, _cts.Token));
            //Runボタンがクルクルし始める
            var anime = this.StartWaitingAnimation(_cts.Token)
                .ContinueWith(comp => this.RunCaption = "Waiting...")
            ;
            await tk;
            _cts.Cancel();
            await anime;
            lock (_lock)
            {
                this.IsSearching = false;
            }
            this.RunCaption = "選択を検索し修正";
            CustomLogger.WriteLine("Search Ended VMDisabledHistory");
        }
        public void CancelSearch()
        {
            _cts.Cancel();
            CustomLogger.WriteLine("Called CancelSearch() from VMDisabledHistory");
        }
        /// <summary>
        /// 検索中であることがわかる検索アニメーションを開始する
        /// </summary>
        private async Task StartWaitingAnimation(CancellationToken ct)
        {
            await Task.Run(async () =>
            {
                var builder = new System.Text.StringBuilder();
                builder.Append("||||");
                for (int i = 0; i < 30; i++) builder.Append(' ');
                while (!ct.IsCancellationRequested)
                {
                    var cha = builder[builder.Length - 1];
                    builder.Insert(0, cha);
                    builder.Remove(builder.Length - 1, 1);
                    this.RunCaption = builder.ToString();
                    await Task.Delay(50);
                }
            });
        }
        public void RemoveHistory(List<int> pIndices)
        {
            lock (_lock)
            {
                if (_isSearching){
                    return;
                }
                var deleteInfos = pIndices
                    .Select(val => this.ViewModelListView.GetExFileInfo(val))
                    .Where(val => val.Invalided)
                    .Where(val => !_repairedPaths.Contains(val.FilePath));
                var okInfos = deleteInfos.Where(val => this.IsDeleteAlertInfo(val) == false);
                if (deleteInfos.Count() != okInfos.Count()){
                    var msg = "タグ、別名が設定されている物も削除する場合は ok と入力してください";
                    MInputBox.GetText(msg, string.Empty).Match(
                        none => deleteInfos = okInfos,
                        some => {
                            if (some.ToLower() != "ok"){
                                deleteInfos = okInfos;
                            }
                        }
                    );
                }
                foreach (var info in deleteInfos)
                {
                    this.HistoryManager.Remove(info.FilePath);
                    _repairedPaths.Add(info.FilePath);
                }
            }
        }
        public bool UpdateHistory(ExFileInfo pOldInfo, ExFileInfo pNewInfo)
        {
            ExFileInfo oldInfo;
            ExFileInfo newInfo;
            lock (_lock)
            {
                oldInfo = pOldInfo.Clone();
                newInfo = pNewInfo.Clone();
            }
            // せっかくの検索結果なので追加処理する
            if (File.Exists(newInfo.FilePath) || Directory.Exists(newInfo.FilePath)){
                this.HistoryManager.AddToBuffer(newInfo.FilePath);
            }
            else{
                return false;
            }
            if (oldInfo.FileName.ToLower() != newInfo.FileName.ToLower()){
                return false;
            }
            if (this.IsRepaired(oldInfo)){
                return false;
            }
            if (!oldInfo.Invalided){
                return false;
            }
            this.HistoryManager.Remove(oldInfo.FilePath);
            lock (_lock)
            {
                _repairedPaths.Add(oldInfo.FilePath);
                //this.ViewModelListView.ChangeToEnableRedrawOnce();
            }
            return true;
        }
        List<string> GetSelectedFileNames()
        {
            var selectedIndices = new List<int>(this.ViewModelListView.SelectedIndices);
            var names = new List<string>();
            foreach (var index in selectedIndices)
            {
                var info = this.ViewModelListView.GetExFileInfo(index);
                if (info.IsDefault()){
                    continue;
                }
                names.Add(info.FileName.ToLower());
            }
            return names;
        }
        List<KeyWord> GetMixSearchWords(IEnumerable<string> pSearchWords, int pMinMatchSeq)
        {
            var words = pSearchWords.Distinct().ToList();
            var result = new List<KeyWord>();
            int i = 0;
            while (i < words.Count)
            {
                if (i == words.Count - 1){
                    result.Add(new KeyWord { Word = words[i], Fuzzy = false });
                    break;
                }
                var main = words[i];
                var sub = words.Skip(i + 1);
                var mix = this.ConvToMixSearchWords(main, sub, pMinMatchSeq);
                if (mix == main){
                    result.Add(new KeyWord { Word = mix, Fuzzy = false });
                }
                else{
                    result.Add(new KeyWord { Word = mix, Fuzzy = true });
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
        public bool IsRepaired(ExFileInfo pInfo)
        {
            return _repairedPaths.Contains(pInfo.FilePath);
        }
        // ============================================================
        // = 説明：イベント
        // ============================================================
        public event EventHandler<string> ViewModeRunCaptionChanged = delegate { };
    }
}
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public class VMFileSearchApp
    {
        protected object _lock = new object();
        IOption<Form> _debugForm = new None<Form>();
        Task _waitAnimeTask = Task.Delay(0);
        protected CancellationTokenSource _cts = new CancellationTokenSource();
        // ============================================================
        // = 説明：プロパティ
        // ============================================================
        public IOption<FilterMenuFactory> FilterMenuFactory { get; set; }
        Func<string, List<string>> _getAliasFuzzyMethod = pFileNameFuzzy => new List<string>();
        public Func<string, List<string>> GetAliasFuzzyMethod
        {
            get { lock (_lock) return _getAliasFuzzyMethod; }
            set { lock (_lock) _getAliasFuzzyMethod = value; }
        }
        Func<string, IOption<string>> _getAliasMethod = pFileName => new None<string>();
        public Func<string, IOption<string>> GetAliasMethod
        {
            get { lock (_lock) return _getAliasMethod; }
            set { lock (_lock) _getAliasMethod = value; }
        }
        public MWindowsSearcher WindowsSearcher { get; private set; }
        public MHistorySearcher HistorySearcher { get; private set; }
        public MTagSearcher TagSearcher { get; private set; }
        public MPowerShellSearcher PowerShellSearcher { get; private set; }
        string _searchWords = string.Empty;
        public string SearchWords
        {
            get { lock (_lock) return _searchWords; }
            set { lock (_lock) _searchWords = value; }
        }
        Task _searchStatusTask = Task.Delay(0);
        public bool IsStandby
        {
            get { lock (_lock) return _searchStatusTask.IsCompleted; }
        }
        public Func<bool> GetIsRealOnlyMethod { get; set; }
        public bool IsReadOnly
        {
            get { return this.GetIsRealOnlyMethod(); }
        }
        string _runCaption = "Run";
        public string RunCaption
        {
            get { lock (_lock) return _runCaption; }
            private set
            {
                ViewModelRunCaptionChangedEventArgs args;
                lock (_lock)
                {
                    _runCaption = value;
                    args = new ViewModelRunCaptionChangedEventArgs(_runCaption);
                }
                ViewModeRunCaptionChanged.Invoke(this, args);
            }
        }
        ToolStripMenuItem[] _filterMenuItems;
        public ToolStripMenuItem[] FilterMenuItems
        {
            get { lock (_lock) return _filterMenuItems; }
            private set { lock (_lock) _filterMenuItems = value; }
        }
        // ============================================================
        // = コンストラクタ、デストラクタ
        // ============================================================
        public VMFileSearchApp(
        MWindowsSearcher pWindowsSearcher,
        MHistorySearcher pHistorySearcher,
        MTagSearcher pTagSearcher,
        MPowerShellSearcher pPowerShellSearcher)
        {
            _searchWords = string.Empty;
            _filterMenuItems = new ToolStripMenuItem[0];
            this.FilterMenuFactory = new None<FilterMenuFactory>();
            this.WindowsSearcher = pWindowsSearcher;
            this.HistorySearcher = pHistorySearcher;
            this.TagSearcher = pTagSearcher;
            this.PowerShellSearcher = pPowerShellSearcher;
            this.GetIsRealOnlyMethod = () => true;
        }
        ~VMFileSearchApp()
        {
            if (!_cts.IsCancellationRequested){
                _cts.Cancel();
            }
            _cts.Dispose();
            foreach (var val in this.FilterMenuItems)
            {
                if (val != null){
                    val.Dispose();
                }
            }
            _debugForm.Match(
                none => {},
                some => some.Dispose()
            );
        }
        // ============================================================
        // = メソッド
        // ============================================================
        public async Task StartFileSearchFolderSelectionAsync()
        {
            Func<List<string>> bak = () => new List<string>();
            bak = this.WindowsSearcher.GetIncludeDirectorys;
            var dirs = this.WindowsSearcher.GetIncludeDirectorys();
            var empty = new List<string>();
            var title = "検索するフォルダを選択を選択してください";
            this.WindowsSearcher.GetIncludeDirectorys = () => {
                return MInputBoxChoose.GetItems(dirs, empty, title, 600)
                    .GetOrDefault(empty)
                ;
            };
            await StartFileSearchAsync();
            this.WindowsSearcher.GetIncludeDirectorys = bak;
        }
        public async Task StartFileSearchAsync()
        {
            if (this.SearchWords.Length == 0){
                return;
            }
            if (!this.ValidatingSearchWords(this.SearchWords)){
                return;
            }
            // 検索開始処理
            this.PreSearchMethod();
            // 検索処理Main
            var tasks = new List<Task>();
            // 入力された文字列を検索用に変換
            var searchWords = this.CreateWordCollection(this.SearchWords);
            if (this.SearchWords.Length == 0){
                return;
            }
            var word = searchWords.Select(val =>{
                return new KeyWord { Word = val.Word, Fuzzy = val.Fuzzy };
            });
            this.WindowsSearcher.SearchWords = word.ToList();
            tasks.Add(this.WindowsSearcher.StartSearchAsync(_cts.Token));
            if (this.SearchWords.Length == 0){
                return;
            }
            this.HistorySearcher.SearchWords = word.ToList();
            tasks.Add(this.HistorySearcher.StartSearchAsync(_cts.Token));
            await Task.WhenAll(tasks);
            // 検索終了処理
            this.PostSearchMethod();
        }
        public async Task StartTagSearchAsync()
        {
            // 検索開始処理
            this.PreSearchMethod();
            // 検索処理Main
            await this.TagSearcher.StartSearchAsync(_cts.Token);
            // 検索終了処理
            this.PostSearchMethod();
        }
        public async Task StartPowerShellSearchAsync()
        {
            // 検索開始処理
            this.PreSearchMethod();
            // 検索処理Main
            await this.PowerShellSearcher.StartSearchAsync(_cts.Token);
            // 検索終了処理
            this.PostSearchMethod();
        }
        void PreSearchMethod()
        {
            lock (_lock)
            {
                if (!_searchStatusTask.IsCompleted){
                    return;
                }
            }
            if (!_cts.IsCancellationRequested){
                _cts.Cancel();
            }
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            lock (_lock)
            {
                var ct = _cts.Token;
                _searchStatusTask = Task.Run(async () =>{
                    while (true)
                    {
                        await Task.Delay(100);
                        if (ct.IsCancellationRequested){
                            break;
                        }
                    }
                });
            }
            // Runボタンがクルクルし始める
            lock (_lock)
            {
                _waitAnimeTask = WaitingAnimationAsync(_cts.Token);
            }
        }
        void PostSearchMethod()
        {
            bool isCancelRequested = _cts.Token.IsCancellationRequested;
            lock (_lock)
            {
                _searchStatusTask = Task.Run(async () => {
                    // 検索のためにボタンを押したタイミングが検索終了直後だった場合の対策
                    // キャンセルされなかった時は即座にボタンが押せないように待機する
                    if (!isCancelRequested){
                        Task anime;
                        lock (_lock) anime = _waitAnimeTask;
                        await anime;
                        this.RunCaption = "Wait";
                        await Task.Delay(600);
                        this.RunCaption = "Run";
                    }
                });
            }
            _cts.Cancel();
            CustomLogger.WriteLine("Search Ended VMFileSearchApp");
        }
        List<KeyWord> CreateWordCollection(string pSearchWords)
        {
            var splitted = pSearchWords.Split(':').Where(val => val != "");
            // 「*」だけの検索条件がある場合他の条件は無視する
            if (splitted.Any(val => val.All(val2 => val2 == '*'))){
                return new List<KeyWord>{ new KeyWord { Word = "*", Fuzzy = true } };
            }
            // 別名が設定されている物は紐づいたファイル名で検索する
            var fromAlias = splitted
                .Select(val => this.GetAliasFuzzyMethod(val))
                .SelectMany(val => val)
            ;
            var searchWords = new List<KeyWord>();
            searchWords.AddRange(splitted.Select(str => {
                return new KeyWord { Word = str, Fuzzy = true };
            }));
            searchWords.AddRange(fromAlias.Select(str =>{
                return new KeyWord { Word = str, Fuzzy = false };
            }));
            return searchWords;
        }
        public void CancelSearch()
        {
            _cts.Cancel();
            CustomLogger.WriteLine("Called CancelSearch() from VMFileSearchApp");
        }
        public void ShowDebug()
        {
            var task = Task.Run(() =>
            {
                var fm = _debugForm.Match(
                    none => CustomLogger.GetMessageForm(),
                    some => {
                        if (some.IsDisposed){
                            return CustomLogger.GetMessageForm();
                        }
                        else{
                            return some;
                        }
                    }
                );
                _debugForm = new Some<Form>(fm);
                if (fm.Visible){
                    fm.Focus();
                }
                else{
                    Application.Run(fm);
                    fm.Dispose();
                }
            });
        }
        protected async Task WaitingAnimationAsync(CancellationToken ct)
        {
            try
            {
                string[] strs = {"／", "─", "＼", "｜"};
                while (true)
                {
                    foreach (var val in strs)
                    {
                        if (ct.IsCancellationRequested){
                            return;
                        }
                        this.RunCaption = val;
                        await Task.Delay(50);
                    }
                }
            }
            finally
            {
                this.RunCaption = "Run";
            }
        }
        public bool ValidatingSearchWords(string pSearchWords)
        {
            var invalidChars = Path.GetInvalidFileNameChars().ToList();
            invalidChars.Remove('*'); // あいまい検索で使う
            invalidChars.Remove(':'); // or条件演算子として使う
            foreach(var cha in pSearchWords)
            {
                if (invalidChars.Contains(cha)){
                    return false;
                }
            }
            return true;
        }
        public string GetClip()
        {
            if (Clipboard.ContainsText()){
                return Clipboard.GetText();
            }else{
                return string.Empty;
            }
        }
        public void ReloadFilterMenu()
        {
            var checkedTags = this.FilterMenuItems
                .Where(val => val.Checked)
                .Select(val => val.Text)
            ;
            var items = this.FilterMenuFactory.Match(
                none => {throw new Exception("FilterMenuFactory が設定されていません");},
                some => some.Create()
            );
            foreach (var val in this.FilterMenuItems)
            {
                if (val != null){
                    val.Dispose();
                }
            }
            //チェックをつける
            foreach (var val in items)
            {
                if (checkedTags.Contains(val.Text)){
                    val.Checked = true;
                }
            }
            this.FilterMenuItems = items.ToArray();
        }
        public void UpdateViewRangeAlias(IEnumerable<int> pUpdateIndices, VMListView pVMListView)
        {
            foreach (int idx in pUpdateIndices)
            {
                // 検索中はキャッシュから読み込む(lock の時間を減らすため)
                var info = pVMListView.DataSource.ElementAt(idx, !this.IsStandby);
                info.Alias = this.GetAliasMethod(info.FileName).GetOrDefault("");
            }
        }
        public void UpdateViewRangeAlias(int pUpdateIndex, VMListView pVMListView)
        {
            // 検索中はキャッシュから読み込む(lock の時間を減らすため)
            var info = pVMListView.DataSource.ElementAt(pUpdateIndex, !this.IsStandby);
            info.Alias = this.GetAliasMethod(info.FileName).GetOrDefault("");
        }
        public void ClearEventAll()
        {
            ViewModeRunCaptionChanged = delegate{};
        }
        public void RiseEventAll()
        {
            var arg1 = new ViewModelRunCaptionChangedEventArgs(this.RunCaption);
            ViewModeRunCaptionChanged.Invoke(this, arg1);
        }
        // ============================================================
        // = 説明：イベント
        // ============================================================
        public event EventHandler<ViewModelRunCaptionChangedEventArgs> ViewModeRunCaptionChanged = delegate { };
        public event EventHandler<string> ErrorNotification = delegate{};
    }
}
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using CustomFunctions;

namespace FileSearchApp
{
    public class VMListView
    {
        bool _disposed = false;
        readonly object _lock = new object();
        SortOrder _previousSortOrder = SortOrder.None;
        int _previousSortColumn = 0;
        int _beforeListSize = 0;
        bool _redrawOneceEnabled = false;
        readonly CancellationTaskManager _cancelables = new CancellationTaskManager();
        readonly System.Timers.Timer _listSizeUpdateTimer = new System.Timers.Timer();
// +=========+=========+=========+=========+=========+=========+=========+=========
// = プロパティ
// +=========+=========+=========+=========+=========+=========+=========+=========
        public bool AliasEnabled { get; set; }
        public int Count
        {
            get { return this.DataSource.Count; }
        }
        Cursor _listViewCursor = Cursors.Default;
        public Cursor ListViewCursor
        {
            get { return _listViewCursor; }
            private set
            {
                _listViewCursor = value;
                var args = new ViewModelListViewCursorChangedEventArgs(_listViewCursor);
                ViewModelCursorChanged.Invoke(this, args);
            }
        }
        private int _selectionIndex = -1;
        public int SelectedIndex
        {
            get { lock (_lock) return _selectionIndex; }
        }
        SortedSet<int> _selectedIndices = new SortedSet<int>();
        public ReadOnlyCollection<int> SelectedIndices
        {
            get
            {
                lock (_lock)
                {
                    _selectedIndices.Remove(-1);
                    return new ReadOnlyCollection<int>(_selectedIndices.ToList());
                }
            }
        }
        ToolStripMenuItem[] _listViewMenuItems;
        public ToolStripMenuItem[] ListViewMenuItems
        {
            get { return _listViewMenuItems; }
            set
            {
                foreach (var val in _listViewMenuItems)
                {
                    if (val != null){
                        val.Dispose();
                    }
                }
                _listViewMenuItems = value;
            }
        }
        public ListViewDataSource DataSource { get; set; }
        public IToolStripMenuItemFactory ListViewMenuFactory { get; set; }
        public MListViewFunction Func { get; set; }
        // ============================================================
        // = 説明：コンストラクタ　デストラクタ
        // ============================================================
        public VMListView(ListViewDataSource pCollection)
        {
            this.AliasEnabled = true;
            this.ListViewMenuFactory = new DefaultMenuFactory();
            this.DataSource = pCollection;
            this.Func = new MListViewFunction();
            _selectedIndices = new SortedSet<int>();
            _selectionIndex = -1;
            this.ListViewCursor = Cursors.Default;
            // this.ListViewColumnHeaders = pColumnHeaders;
            _listSizeUpdateTimer.Elapsed += (s, e) => {
                bool redraw = false;
                lock (_lock)
                {
                    redraw = _redrawOneceEnabled;
                    _redrawOneceEnabled = false;
                }
                RiseDataSourceChange(redraw);
            };
            _listSizeUpdateTimer.Interval = 500;
            _listSizeUpdateTimer.Start();
            _listViewMenuItems = this.ListViewMenuFactory.Create().ToArray();
        }
        ~VMListView()
        {
            _disposed = true;
            foreach (var val in this.ListViewMenuItems)
            {
                if (val != null){
                    val.Dispose();
                }
            }
            // foreach (var val in this.ListViewColumnHeaders)
            // {
            //     if (val != null){
            //         if (val.ImageList != null){
            //             val.ImageList.Dispose();
            //         }
            //     }
            // }
            _listSizeUpdateTimer.Dispose();
            _cancelables.CancelAll();
            _cancelables.WaitAll();
        }
        // ============================================================
        // = 説明：メソッド
        // ============================================================
        public void Clear()
        {
            // 画面表示を先に消す
            lock (_lock)
            {
                RiseDataSourceChange(new ViewModelDataSourceChangedEventArgs(0, false));
                _cancelables.CancelAll();
                //_listSizeUpdateTimer.Stop();
                _selectedIndices.Clear();
                this.DataSource.Clear();
                _selectionIndex = -1;
                _previousSortColumn = 0;
                _previousSortOrder = SortOrder.None;
            }
        }
        public async Task ShowFilterCommandLine(string pColumnText)
        {
            IOption<string> inputResult = new None<string>();
            var msg = "";
            switch (pColumnText)
            {
                case "FileName" :
                case "Extension":
                case "Path"     :
                case "Status"   :
                    msg = "「like」「not like」「remove」を使用できます";
                    break;
                case "Size"     :
                    msg = "「<=」「>=」「remove」を使用できます";
                    break;
                default:
                    return;
            }
            await Task.Run(() => inputResult = MInputBox.GetText(msg));
            if (inputResult.IsNone()){
                return;
            }
            const string NOTHING = "NOTHING", LIKE = "LIKE", NOTLIKE = "NOTLIKE", REMOVE = "REMOVE", GE = ">=", LE = "<=";
            var inputOperator = NOTHING;
            var inputValue = string.Empty;
            IOption<decimal> inputValueInt = new None<decimal>();
            var key = string.Empty;
            
            inputResult.IfSome(some => {
                if (some.ToLower().StartsWith("like")){
                    inputValue = some.ToLower().Substring(4).Trim();
                    inputOperator = LIKE;
                    key = pColumnText + " Like " + inputValue;
                }
                else if (some.ToLower().StartsWith("notlike")){
                    inputValue = some.ToLower().Substring(7).Trim();
                    inputOperator = NOTLIKE;
                    key = pColumnText + " NotLike " + inputValue;
                }
                else if (some.ToLower().StartsWith("not like")){
                    inputValue = some.ToLower().Substring(8).Trim();
                    key = pColumnText + " NotLike " + inputValue;
                    inputOperator = NOTLIKE;
                }
                else if (some.ToLower().StartsWith("not")){
                    inputValue = some.ToLower().Substring(3).Trim();
                    key = pColumnText + " NotLike " + inputValue;
                    inputOperator = NOTLIKE;
                }
                else if (some.ToLower().StartsWith("<=")){
                    decimal inputNum;
                    inputOperator = LE;
                    if (SIUnitBinaryByte.TryParse(some.Substring(2).Trim(), out inputNum)){
                        inputValueInt = Some.New(inputNum);
                        key = pColumnText + " <= " + inputNum.ToString();
                    }
                }
                else if (some.ToLower().StartsWith(">=")){
                    decimal inputNum;
                    inputOperator = GE;
                    if (SIUnitBinaryByte.TryParse(some.Substring(2).Trim(), out inputNum)){
                        inputValueInt = Some.New(inputNum);
                        key = pColumnText + " >= " + inputNum.ToString();
                    }
                }
                else if (some.ToLower().StartsWith("remove")){
                    inputOperator = REMOVE;
                }
                // どれにも判定されなければ Like と解釈する
                if (inputOperator == NOTHING){
                    inputValue = some.ToLower().Trim();
                    inputOperator = LIKE;
                    key = pColumnText + " Like " + inputValue;
                }
            });
            if (inputOperator == REMOVE){
                var tagFilters = this.DataSource.Filters.Where(val => val.Key.StartsWith("Tag Equal "));
                var otherFilters = this.DataSource.Filters.Where(val => !val.Key.StartsWith("Tag Equal "));
                if (otherFilters.Count() == 0){
                    if (tagFilters.Count() == 0){
                        this.ErrorNotification.Invoke(this, "フィルタが適用されていません");
                    }
                    else{
                        this.ErrorNotification.Invoke(this, "タグ以外のフィルタが適用されていません");
                    }
                    return;
                }
                var newFilter = await Task.Run(() => this.Func.ShowFilterRemoveCommandLine(otherFilters));
                foreach (var kv in tagFilters)
                {
                    newFilter.Add(kv.Key, kv.Value);
                }
                ApplyFilter(newFilter);
                return;
            }
            if ((inputOperator == GE || inputOperator == LE) && inputValueInt.IsNone()){
                this.ErrorNotification.Invoke(this, "有効な値が入力されませんでした");
                return;
            }
            if ((inputOperator == LIKE || inputOperator == NOTLIKE) && inputValue == string.Empty){
                this.ErrorNotification.Invoke(this, "有効な値が入力されませんでした");
                return;
            }
            //フィルターの一覧を作成
            var filterTable = new Dictionary<KeyValuePair<string, string>, Func<ExFileInfo, bool>>{
                { new KeyValuePair<string, string>(LIKE, "FileName"), info => MyTool.FuzzyContains(inputValue, info.DispFileName(this.AliasEnabled)) },
                { new KeyValuePair<string, string>(NOTLIKE, "FileName"), info => !MyTool.FuzzyContains(inputValue, info.DispFileName(this.AliasEnabled)) },
                { new KeyValuePair<string, string>(LIKE, "Extension"), info => MyTool.FuzzyContains(inputValue, info.Extension) },
                { new KeyValuePair<string, string>(NOTLIKE, "Extension"), info => !MyTool.FuzzyContains(inputValue, info.Extension) },
                { new KeyValuePair<string, string>(LIKE, "Path"), info => MyTool.FuzzyContains(inputValue, info.FilePath) },
                { new KeyValuePair<string, string>(NOTLIKE, "Path"), info => !MyTool.FuzzyContains(inputValue, info.FilePath) },
                { new KeyValuePair<string, string>(LE, "Size"), info => info.Size <= inputValueInt.Match(none => 0, some => some)},
                { new KeyValuePair<string, string>(GE, "Size"), info => info.Size >= inputValueInt.Match(none => 0, some => some) },
                { new KeyValuePair<string, string>(LIKE, "Status"), info => MyTool.FuzzyContains(inputValue, info.Invalided ? "Invalid" : "Valid") },
                { new KeyValuePair<string, string>(NOTLIKE, "Status"), info => !MyTool.FuzzyContains(inputValue, info.Invalided ? "Invalid" : "Valid") },
            };
            var func = filterTable
                .Where(val => val.Key.Key == inputOperator && val.Key.Value == pColumnText)
                .Select(val => val.Value)
            ;
            if (!func.Any()){
                var errMsg = inputOperator
                    + " + "
                    + pColumnText
                    + " の組み合わせでは検索できません"
                ;
                this.ErrorNotification.Invoke(this, errMsg);
                return;
            }
            if (func.Count() != 1){
                var errMsg = inputOperator
                    + " + "
                    + pColumnText
                    + " 定義している関数テーブルに重複があります"
                ;
                this.ErrorNotification.Invoke(this, errMsg);
                return;
            }
            var filter = new Filter<ExFileInfo>(this.DataSource.Filters);
            filter.Add(key, func.First());
            ApplyFilter(filter);
        }
        public void OpenSelectionItem()
        {
            var info = GetExFileInfo();
            this.ListViewCursor = Cursors.AppStarting;
            _cancelables.Add(async ct => await this.Func.OpenFileAsync(info));
            _cancelables.Add(ct => {
                Task.Delay(1000).Wait();
                this.ListViewCursor = Cursors.Default;
            });
            this.ChangeToEnableRedrawOnce();
            _cancelables.Shrink();
        }
        public bool Add(ExFileInfo pInfo)
        {
            return this.DataSource.Add(pInfo);
        }
        public void ExportCsv()
        {
            var infos = new Dictionary<int, ExFileInfo>();
            foreach (var val in this.DataSource.GetKeyValuePairs())
            {
                infos.Add(val.Key, val.Value);
            }
            //this.DataSource.ForEach(val => infos.Add(val.Key, val.Value));
            this.Func.ExportCsv(infos);
        }
        public ListViewItem GetListViewItem(int pIndex, bool pEnableCache = false)
        {
            var info = this.DataSource.ElementAt(pIndex, pEnableCache);
            var item = this.Func.ConvToListViewItem(info, pIndex, this.AliasEnabled);
            return item;
        }
        public void SortListView(ListView pListView, int pColumnIndex)
        {
            this.UnSelectAll();
            SortOrder order;
            if (_previousSortColumn == pColumnIndex){
                if (_previousSortOrder == SortOrder.Ascending){
                    order = SortOrder.Descending;
                }
                else{
                    order = SortOrder.Ascending;
                }
            }
            else{
                order = SortOrder.Ascending;
            }
            _previousSortOrder = order;
            _previousSortColumn = pColumnIndex;
            IOption<ColumnHeader> column = new None<ColumnHeader>();
            for (int i = 0; i < pListView.Columns.Count; i++)
            {
                var col = pListView.Columns[i];
                if (col.Index == pColumnIndex){
                    column = Some.New(col);
                    break;
                }
            }
            column.IfSome(some =>{
                var textFileName = VMMainWindow.ColumnHeaders.FileName.ToString();
                var textAlias = VMMainWindow.ColumnHeaders.Alias.ToString();
                if (some.Text == textFileName && this.AliasEnabled){
                    this.DataSource.Sort(textAlias, order);
                }
                else{
                    this.DataSource.Sort(some.Text, order);
                }
                this.RiseDataSourceChange(true);
            });
        }
        public ExFileInfo GetExFileInfo()
        {
            return this.DataSource.ElementAt(this.SelectedIndex);
        }
        public ExFileInfo GetExFileInfo(int pIndex)
        {
            return this.DataSource.ElementAt(pIndex);
        }
        public void ApplyFilter(Filter<ExFileInfo> pFilter)
        {
            this.DataSource.ApplyFilter(pFilter);
            this.RiseDataSourceChange(true);
        }
        public bool IsSelected(int pIndex)
        {
            return this.SelectedIndices.Contains(pIndex) || this.SelectedIndex == pIndex;
        }
        public void SelectItem(int pIndex, bool pAddition = false)
        {
            if (pAddition){
                lock (_lock)
                {
                    _selectionIndex = pIndex;
                    _selectedIndices.Add(pIndex);
                }
            }
            else{
                lock (_lock)
                {
                    _selectedIndices.Clear();
                    _selectedIndices.Add(pIndex);
                    _selectionIndex = pIndex;
                }
            }
        }
        public void UnSelectItem(int pIndex)
        {
            lock (_lock)
            {
                _selectedIndices.Remove(pIndex);
                if (_selectedIndices.Count != 0){
                    _selectionIndex = _selectedIndices.Max();
                }
                else{
                    _selectionIndex = -1;
                }
            }
        }
        public void SelectAll()
        {
            lock (_lock)
            {
                _selectionIndex = 0;
                for (int i = 0; i < this.DataSource.Count; i++)
                {
                    _selectedIndices.Add(i);
                }
            }
        }
        public void UnSelectAll()
        {
            lock (_lock)
            {
                _selectedIndices.Clear();
                _selectionIndex = -1;
            }
        }
        public void StartUpdateTimer()
        {
            _listSizeUpdateTimer.Start();
        }
        public void StopUpdateTimer()
        {
            _listSizeUpdateTimer.Stop();
            RiseDataSourceChange(true);
        }
        void RiseDataSourceChange(ViewModelDataSourceChangedEventArgs pArgs)
        {
            this.ViewModelDataSourceChanged.Invoke(this, pArgs);
            _beforeListSize = this.DataSource.Count;
        }
        public void RiseDataSourceChange(bool pRedraw)
        {
            // 前回の更新の時とサイズが同じかつ、表示更新しない場合何もしない
            var cnt = this.DataSource.Count;
            if (_beforeListSize == cnt && pRedraw == false){
                return;
            }
            var args = new ViewModelDataSourceChangedEventArgs(cnt, pRedraw);
            RiseDataSourceChange(args);
        }
        public void RedrawItem(ListView pListView, int pIndex)
        {
            var virtualListSize = pListView.VirtualListSize;
            bool invalid = false;
            invalid |= virtualListSize == 0;
            invalid |= pIndex < 0;
            invalid |= pIndex >= virtualListSize;
            if (invalid){
                return;
            }
            pListView.RedrawItems(pIndex, pIndex, false);
        }
        public void ChangeToEnableRedrawOnce()
        {
            lock (_lock) _redrawOneceEnabled = true;
        }
        public void UpdateFileSize()
        {
            var infos = new List<ExFileInfo>();
            foreach (var val in this.DataSource.GetKeyValuePairs())
            {
                if (val.Value.Size == 0){
                    infos.Add(val.Value);
                }
            }
            _cancelables.Add(ct =>
            {
                using (var progressTimer = new System.Timers.Timer(500))
                {
                    long cur = 0;
                    var cnt = infos.Count;
                    progressTimer.Elapsed += (s, e) => {
                        ViewModelFileSizeUpdatedEventArgs args;
                        args = new ViewModelFileSizeUpdatedEventArgs(Interlocked.Read(ref cur), cnt);
                        this.ViewModelFileSizeUpdated.Invoke(this, args);
                        this.ChangeToEnableRedrawOnce();
                    };
                    progressTimer.Start();
                    foreach (var info in infos)
                    {
                        if (ct.IsCancellationRequested){
                            return;
                        }
                        else if (_disposed){
                            return;
                        }
                        this.Func.UpdateSize(info);
                        Interlocked.Increment(ref cur);
                    }
                    progressTimer.Stop();
                    // 最後に100%で出力する
                    this.ViewModelFileSizeUpdated.Invoke(
                        this,
                        new ViewModelFileSizeUpdatedEventArgs(cnt, cnt)
                    );
                }
                RiseDataSourceChange(true);
            });
            _cancelables.Shrink();
        }
        public void ReloadListViewMenu()
        {
            this.ListViewMenuItems = this.ListViewMenuFactory.Create().ToArray();
        }
        public void ClearEventAll()
        {
            ViewModelDataSourceChanged = delegate{};
            ViewModelCursorChanged = delegate{};
            ViewModelFileSizeUpdated = delegate{};
        }
        /// <summary>
        /// 画面をすべて更新したいときに呼び出す(主にイベントの付け替えをしたときに使う)
        /// </summary>
        public void RiseEventAll()
        {
            var arg1 = new ViewModelDataSourceChangedEventArgs(
                this.Count, // ListSize
                true // Redraw
            );
            ViewModelDataSourceChanged.Invoke(this, arg1);
            var arg2 = new ViewModelListViewCursorChangedEventArgs(this.ListViewCursor);
            ViewModelCursorChanged.Invoke(this, arg2);
            var arg3 = new ViewModelFileSizeUpdatedEventArgs(1, 1);
            ViewModelFileSizeUpdated.Invoke(this, arg3);
        }
        // ============================================================
        // = イベント
        // ============================================================
        public event EventHandler<ViewModelDataSourceChangedEventArgs> ViewModelDataSourceChanged = delegate {};
        public event EventHandler<ViewModelListViewCursorChangedEventArgs> ViewModelCursorChanged = delegate {};
        public event EventHandler<ViewModelFileSizeUpdatedEventArgs> ViewModelFileSizeUpdated = delegate {};
        public event EventHandler<string> ErrorNotification = delegate{};
    }
}
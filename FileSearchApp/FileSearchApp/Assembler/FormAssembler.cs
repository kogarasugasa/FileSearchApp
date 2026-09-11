using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using CustomFunctions;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;

namespace FileSearchApp
{
    public class FormAssembler : IDisposable
    {
        readonly object _lock = new object();
        bool _disposed = false;
        IOption<Icon> _icon = new None<Icon>();
        IOption<MyNativeMethod.Function.ShutdownBlocker> _blocker =
            new None<MyNativeMethod.Function.ShutdownBlocker>();
        readonly MSettings _conf = MSettings.GetInstance();
        readonly MHistoryManager _mHistoryManager = new MHistoryManager();
        readonly MAliasManager _mAliasManager = new MAliasManager();
        readonly MTagManager _mTagManager = new MTagManager();
        readonly MTabRestore _mTabRestore = new MTabRestore();
        MMessageTransceiver _mMessageReceiver = new MMessageTransceiver();
        VMMainWindow _vmMainWindow;
        public FormAssembler()
        {
            _icon = MyIcon.ExtractAssociatedIconFromExecutingAssembly();
            _icon.IfSome(some => MyIcon.SetIcon(some));
            _icon.IfSome(some => MInputBox.SetIcon(some));
            _icon.IfSome(some => MInputBoxTagAlias.SetIcon(some));
            _icon.IfSome(some => MInputBoxChoose.SetIcon(some));
            _icon.IfSome(some => MMessageBox.SetIcon(some));

            _conf.BackupFileFound += ShowBackupFoundView;
            _conf.ErrorNotification += (s, e) => MessageBox.Show(e);

            _vmMainWindow = new VMMainWindow(_conf.GetColumnWidth(), _conf.GetWindowSize());
            _vmMainWindow.MethodSetterGetCandidate = () => _conf.ReadCandidateSettings().ToArray();

            _mTagManager.GetAllTags = _conf.GetTagMS;
            _mTagManager.SaveAction = _conf.WriteFileNameTag;
            _mTagManager.Read(_conf.GetFileNameTags());
            _mTagManager.AutoSaveEnabled = true;

            _mAliasManager.SaveAction = _conf.WriteAlias;
            _mAliasManager.Read(_conf.GetAlias);
            _mAliasManager.AutoSaveEnabled = true;

            _mHistoryManager.InvalidOrderRestoration += ShowReorderIndexView;
            _mHistoryManager.SaveAction = _conf.WriteSearchResultHistory;
            _mHistoryManager.Read(_conf.GetSearchResultHistory);
            _mHistoryManager.AutoSaveEnabled = true;
        }
        ~FormAssembler()
        {
            this.Dispose();
        }
        public void Dispose()
        {
            this.Dispose(true);
        }
        public void Dispose(bool disposing)
        {
            lock (_lock)
            {
                if (_disposed){
                    return;
                }
                if (disposing){
                    // メッセージの受信を停止する
                    _mMessageReceiver.Dispose();
                    // タブを保存する
                    _mTabRestore.SaveVMRead(_conf.WriteContinueTab);
                    // 履歴を保存する 時間がかかるので別スレッドで実行し最後に Wait する
                    var historySaveTask = Task.Run(() => {
                        if (_mHistoryManager.IsLoad || _mHistoryManager.Count > 0){
                            _mHistoryManager.Save(_conf.WriteSearchResultHistory);
                        }
                    });
                    // タグを保存する
                    _mTagManager.Save(_conf.WriteFileNameTag);
                    // 別名を保存する
                    _mAliasManager.Save(_conf.WriteAlias);
                    // 設定ファイルがない場合作成する
                    _conf.TryCreateSearchDirectorys();
                    _conf.TryCreateTagMS();
                    _conf.TryCreateExcludeDirectory();
                    // 見出しの幅を保存する
                    var width = _vmMainWindow.ListViewColumnHeaders.Select(val => val.Width);
                    _conf.WriteColumnWidth(width);
                    // 画面の位置とサイズを保存する
                    _conf.WriteWindowSize(_vmMainWindow.WindowProperty.ToList());
                    // 履歴の保存を待機
                    historySaveTask.ConfigureAwait(false);
                    historySaveTask.Wait();
                    // 設定のロックを解除
                    _conf.Dispose();
                    // シャットダウン時のイベントを解除する
                    _blocker.Match(
                        none => {},
                        some => some.Dispose()
                    );
                }
                _disposed = true;
            }
        }
        public VFileSearchApp CreateForm()
        {
            VMFileSearchTab vm5 = this.GetVMFileSearchTab();
            var fm = new VFileSearchApp(_vmMainWindow, vm5, this.Dispose);
            // GetVMFileSearchTab で設定していないイベントのセット
            this.SetAdditionalEvent(fm);
            _mMessageReceiver.StartReciever();
            // **** セッション終了時に保存する処理 ****
            // 注意！ Formのハンドルを使用する為
            // ウインドウが閉じられるなどして ハンドルが消えているとブロックできない
            if (_blocker.IsNone()){
                _blocker = Some.New(
                    new MyNativeMethod.Function.ShutdownBlocker(fm.Handle)
                );
            }
            _blocker.IfSome(some => some.Block("FileSearchApp 保存中です"));
            return fm;
        }
        void SetAdditionalEvent(VFileSearchApp pForm)
        {
            _conf.LockReleased += (s, e) => pForm.LoadTheme();
            _conf.LockAcquiring += (s, e) => {
                if (e.Force){
                    var msg = "現在以下の端末がロックを取得しています。ロックを奪いますか？"
                        + Environment.NewLine + e.LockInfo
                    ;
                    var msgResult = MessageBox.Show(
                        msg,
                        "ロック取得",
                        MessageBoxButtons.OKCancel
                    );
                    if (msgResult != DialogResult.OK){
                        e.Cancel = true;
                    }
                }
            };
            _conf.LockAcquired += (s, e) => pForm.LoadTheme();
            _mMessageReceiver.ModelMessageReceived += (s, e) => {
                if (e.Message == ""){
                    pForm.InvokeIfRequiredElseNonInvoke(() => pForm.MoveTop());
                }
                else {
                    var name = ExFileInfo.GetFileName(e.Message);
                    var checkeTags = _mTagManager.GetTags(name);
                    var allTags = _mTagManager.GetAllTags();
                    var alias = _mAliasManager.GetAlias(name).GetOrDefault(name);
                    var result = Task.Run(() => {
                        return MInputBoxTagAlias.GetTagAlias(e.Message, alias, allTags, checkeTags);
                    });
                    result.WaitByPollingLoop(100);
                    result.Result.IfSome(some => {
                        foreach (var tag in _mTagManager.GetAllTags())
                        {
                            _mTagManager.Remove(some.FileName, tag);
                        }
                        foreach (var tag in some.CheckedTags)
                        {
                            _mTagManager.Add(some.FileName, tag);
                        }
                        if (some.Alias == ""){
                            _mAliasManager.Remove(some.FileName);
                        }
                        else{
                            _mAliasManager.Add(some.FileName, some.Alias, true);
                        }
                        _mHistoryManager.AddToBuffer(e.Message);
                    });
                }
            };
        }
        public VMFileSearchTab GetVMFileSearchTab()
        {
            var vm = new VMFileSearchTab(new MTabManager(this));
            _mTabRestore.VMReadMethod = () => {
                var records = vm.GetRecords();
                var data = new Dictionary<string, IEnumerable<string>>();
                int num = 1;
                foreach (var record in records)
                {
                    var paths = new List<string>();
                    var source = record.ListView.DataSource;
                    for (int i = 0; i < source.Count; i++)
                    {
                        paths.Add(source.ElementAt(i).FilePath);
                    }
                    data.Add(num.ToString(), paths);
                    num++;
                }
                return data;
            };
            _mTabRestore.Read(_conf.ReadContinueTab);
            var readData = _mTabRestore.GetRecords();
            for(int i = vm.Count; i < readData.Count; i++) vm.OnAddClick();
            var readKeys = readData.Keys.Select(val => val).ToList();
            var vmRecords = vm.GetRecords();
            for (int i = 0; i < readData.Count; i++)
            {
                foreach (var path in readData[readKeys[i]])
                {
                    vmRecords[i].ListView.Add(new ExFileInfo(path));
                }
                if (vmRecords[i].ListView.Count != 0){
                    vmRecords[i].ListView.SelectItem(0);
                    var info = vmRecords[i].ListView.GetExFileInfo();
                    _mAliasManager.GetAlias(info.FileName).IfSome(some => info.Alias = some);
                    vm.OnSelectClick(vmRecords[i].Key);
                }
                //vm.ChangeTagName(vm.Record.Key);
            }
            return vm;
        }
        public void GetViewModel(
            out VMFileSearchApp pVMFileSearchApp,
            out VMListView pVMListView,
            out VMOptionMenu pVMOption
            )
        {
            lock (_lock)
            {
                var tag = _mTagManager;
                var alias = _mAliasManager;
                var his = _mHistoryManager;
                //
                var lvdata = new ListViewDataSource();
                var vml = new VMListView(lvdata);
                vml.ErrorNotification += (s, e) => MMessageBox.Show(e);
                var menuCreater = new MenuRangeTree();
                menuCreater.GetSettingsMethod = _conf.ReadContextMenu;
                var fac = new ListViewMenuFactory(){
                    GetFileInfoMethod = vml.GetExFileInfo,
                    RedrawListViewMethod = () => vml.RiseDataSourceChange(true),
                    TagManager = Some.New(tag),
                    AliasManager = Some.New(alias),
                    UserMenuCreater = Some.New(menuCreater)
                };
                vml.ListViewMenuFactory = fac;
                var pows = new MPowerShellSearcher(_conf.GetScriptDirectoryPath());
                pows.HistoryManager = Some.New(his);
                pows.RunActionWhenFileFound.Add((val) =>
                {
                    val.Alias = alias.GetAlias(val.FileName).GetOrDefault("");
                    vml.Add(val);
                });
                var hiss = new MHistorySearcher(his){
                    GetExcludeDirectory = _conf.GetExcludeDirectory,
                };
                hiss.RunActionWhenFileFound.Add((val) =>
                {
                    val.Alias = alias.GetAlias(val.FileName).GetOrDefault("");
                    vml.Add(val);
                });
                var tags = new MTagSearcher(){
                    GetAllTags = _conf.GetTagMS,
                    GetTags = tag.GetTags,
                    GetNames = tag.GetNames,
                    GetExFileInfos = his.GetExFileInfos,
                };
                tags.RunActionWhenFileFound.Add(val =>
                {
                    val.Alias = alias.GetAlias(val.FileName).GetOrDefault("");
                    vml.Add(val);
                });
                var win = new MWindowsSearcher();
                win.ErrorNotification += (s, e) => CustomLogger.WriteLine(e);
                win.IndexSearchStarted += (s, e) => CustomLogger.WriteLine(e);
                win.WhereSearchStarted += (s, e) => CustomLogger.WriteLine(e);
                win.GetIncludeDirectorys = _conf.GetSearchDirectorys;
                win.GetExcludeDirectorys = _conf.GetExcludeDirectory;
                win.RunActionWhenFileFound.Add((val) =>
                {
                    val.Alias = alias.GetAlias(val.FileName).GetOrDefault(string.Empty);
                    vml.Add(val);
                    his.AddToBuffer(val.FilePath);
                });
                var vmMain = new VMFileSearchApp(win, hiss, tags, pows);
                vmMain.ErrorNotification += (s, e) => MMessageBox.Show(e);
                vmMain.GetAliasFuzzyMethod = alias.GetNamesFuzzy;
                vmMain.GetAliasMethod = alias.GetAlias;
                vmMain.GetIsRealOnlyMethod = _conf.IsReadOnly;
                var vmOption = new VMOptionMenu();
                vmOption.AutoSaveStatusChanging += (s, e) => {
                    if (_conf.IsReadOnly()){
                        e.Cancel = true;
                        MMessageBox.Show("読取専用では有効化できません");
                        return;
                    }
                    if (e.ChangeToEnable){
                        e.ShowProgressView(ct => MMessageBox.ShowAsync("有効化しています...", ct));
                    }
                };
                vmOption.AutoSaveClick += (s, e) =>{
                    if (e.Cancel){
                        return;
                    }
                    tag.AutoSaveEnabled = e.ChangeToEnable;
                    alias.AutoSaveEnabled = e.ChangeToEnable;
                    his.AutoSaveEnabled = e.ChangeToEnable;
                };
                vmOption.AutoSaveStatusChanged += (s, e) => {
                    if (e.Cancel){
                        return;
                    }
                    if (e.ChangeToEnable){
                        MMessageBox.Show("有効化しました");
                    }
                    else{
                        MMessageBox.Show("無効化しました");
                    }
                };
                vmOption.OptionMenuClicked += (s, e) =>{
                    e.AutoSaveEnabled =
                        tag.AutoSaveEnabled &&
                        alias.AutoSaveEnabled &&
                        his.AutoSaveEnabled
                    ;
                };
                vmOption.SwitchLockClick += (s, e) =>{
                    if (vmMain.IsReadOnly){
                        _conf.ChangeToWritable();
                    }
                    else{
                        var msgResult = MessageBox.Show(
                            "読取専用に切り替えます",
                            "読取専用切替",
                            MessageBoxButtons.OKCancel
                        );
                        if (msgResult != DialogResult.OK){
                            return;
                        }
                        _mHistoryManager.AutoSaveEnabled = false;
                        _mTagManager.AutoSaveEnabled = false;
                        _mAliasManager.AutoSaveEnabled = false;
                        _conf.ChangeToReadOnly();
                    }
                };
                vmOption.OpenDirectorySettingsClick += (s, e) => _conf.OpenDirectorySettings();
                vmOption.OpenExcludeDirectorySettingsClick += (s, e) => _conf.OpenExcludeDirectorySettings();
                vmOption.OpenTagSettingsClick += (s, e) => _conf.OpenTagSettings();
                vmOption.OpenCandidateClick += (s, e) => _conf.OpenCandidateSettings();
                vmOption.OpenScriptDirectoryClick += (s, e) => _conf.OpenScriptDirectory();
                vmOption.UpdateListViewInfoClick += (s, e) => vml.UpdateFileSize();
                vmOption.ExportCsvClick += (s, e) => vml.ExportCsv();
                vmOption.ShowInvalidTagAliasViewClick += (s, e) =>{
                    this.ShowInvalidTagAliasForm(e.Token);
                };
                vmOption.ShowHistoryRepairViewClick += (s, e) =>{
                    var func = this.GetHistoryRepairFormShowMethod(
                        _vmMainWindow,
                        alias,
                        tag,
                        his,
                        lvdata
                    );
                    func(e.Token);
                };
                pVMOption = vmOption;
                var filter = new FilterMenuFactory(){
                    ApplyListViewFilterMethod = vml.ApplyFilter,
                    GetAllTagMethod = _conf.GetTagMS,
                    GetAllocatedTagsMethod = tag.GetTags,
                    GetFilterMethod = () => new Filter<ExFileInfo>(vml.DataSource.Filters),
                    GetFilterMenuItems = () => vmMain.FilterMenuItems,
                };
                vmMain.FilterMenuFactory = Some.New(filter);
                pVMFileSearchApp = vmMain;
                pVMListView = vml;
                return;
            }
        }
        Action<CancellationToken> GetHistoryRepairFormShowMethod(
        VMMainWindow window,
        MAliasManager alias,
        MTagManager tag,
        MHistoryManager his,
        ListViewDataSource lvdata)
        {
            Action<CancellationToken> result = ct =>{
                var mExInfos = lvdata
                    .GetKeyValuePairs()
                    .Where(val => val.Value.Invalided)
                    .Where(val => his.Exists(val.Value.FilePath))
                    .Select(val => val.Value)
                ;
                var vdMain = FormAssemblerDisabledHistory.CreateForm(
                    mExInfos,
                    window.ListViewColumnHeaders.Select(val => val.Width),
                    his,
                    _conf,
                    new WindowRectangle(window.WindowProperty.ToRectangle()),
                    alias,
                    tag
                );
                var tk = Task.Run(async () =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        if (vdMain.IsDisposed){
                            break;
                        }
                        await Task.Delay(500);
                    }
                    vdMain.Close();
                });
                var showTask = Task.Run(() => Application.Run(vdMain));
                showTask.ConfigureAwait(false);
                showTask.WaitByPollingLoop(100);
                tk.WaitByPollingLoop(100);
            };
            return result;
        }
        void ShowBackupFoundView(object sender, FoundBackupFileEventArgs e)
        {
            var msg = Path.GetFileName(e.BackupPath)
                + Environment.NewLine + "前回の保存が失敗しています。バックアップから復元しますか？"
                + Environment.NewLine + "「はい」復元します"
                + Environment.NewLine + "「いいえ」バックアップを破棄し新規で作成します"
                + Environment.NewLine + "「キャンセル」読み取り専用で開きます"
            ;
            var msgResult = MessageBox.Show(
                msg,
                "バックアップの復元",
                MessageBoxButtons.YesNoCancel
            );
            switch (msgResult)
            {
                case DialogResult.Yes:
                    e.IsRestoreRequested = true;
                    e.IsDeleteBackupRequested = true;
                    break;
                case DialogResult.No:
                    e.IsRestoreRequested = false;
                    e.IsDeleteBackupRequested = true;
                    break;
                default:
                    e.IsRestoreRequested = false;
                    e.IsDeleteBackupRequested = false;
                    break;
            }
        }
        void ShowReorderIndexView(object sender, ReOrderEventArgs e)
        {
            var msgResult = MessageBox.Show(e.Message, "履歴エラー", MessageBoxButtons.OKCancel);
            e.CancelRequested = msgResult != DialogResult.OK;
        }
        void ShowInvalidTagAliasForm(CancellationToken ct)
        {
            var vm = new VMInvalidTagAlias(_mHistoryManager, _mAliasManager, _mTagManager);
            var v = new VInvalidTagAlias(vm, ct);
            var tk = Task.Run(() => Application.Run(v));
            tk.WaitByPollingLoop(100);
        }
    }
}
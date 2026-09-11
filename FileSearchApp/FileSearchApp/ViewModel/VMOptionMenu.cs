using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public class VMOptionMenu : IDisposable
    {
        readonly object _lock = new object();
        bool _disposed = false;
        List<ToolStripMenuItem> _optionItems = new List<ToolStripMenuItem>();
        List<Task> _cancellableTasks = new List<Task>();
        CancellationTokenSource _cts = new CancellationTokenSource();
        ~VMOptionMenu()
        {
            this.Dispose();
        }
        public void Dispose()
        {
            this.Dispose(true);
        }
        public void Dispose(bool disposing)
        {
            if (_disposed){
                return;
            }
            if (disposing){
                this.ClearEventAll();
                _cts.Cancel();
                foreach (var val in _optionItems) val.Dispose();
            }
            _disposed = true;
        }
        public void ClearEventAll()
        {
            lock (_lock)
            {
                this.OptionMenuClicked = delegate{};
                this.OptionMenuItemsChanged = delegate{};
                this.OpenDirectorySettingsClick = delegate{};
                this.OpenTagSettingsClick = delegate{};
                this.OpenCandidateClick = delegate{};
                this.OpenExcludeDirectorySettingsClick = delegate{};
                this.OpenScriptDirectoryClick = delegate{};
                this.AutoSaveClick = delegate{};
                this.UpdateListViewInfoClick = delegate{};
                this.ExportCsvClick = delegate{};
                this.ShowInvalidTagAliasViewClick = delegate{};
                //this.ShowAliasRepairViewClick = delegate{};
                this.ShowHistoryRepairViewClick = delegate{};
            }
        }
        public void ClearEventOptionMenuItemsChanged()
        {
            lock (_lock) this.OptionMenuItemsChanged = delegate{};
        }
        public void OnOptionMenuClick()
        {
            if (_disposed){
                return;
            }
            var args = new OptionMenuClickEventArgs();
            this.OptionMenuClicked.Invoke(this, args);
            this.OnOptionMenuChanged(args.AutoSaveEnabled);
        }
        void OnOptionMenuChanged(bool pAutoSaveEnabled)
        {
            List<ToolStripMenuItem> old;
            lock (_lock)
            {
                old = _optionItems;
                _optionItems = this.Create(pAutoSaveEnabled);
            }
            var args = new OptionMenuItemsChangedEventArgs(_optionItems.ToArray());
            this.OptionMenuItemsChanged.Invoke(this, args);
            foreach (var val in old)
            {
                val.Dispose();
            }
        }
        List<ToolStripMenuItem> Create(bool pAutoSaveEnabled)
        {
            if (_disposed){
                return new List<ToolStripMenuItem>();
            }
            // 検索フォルダの設定を開く
            var menuItems = new List<ToolStripMenuItem>();
            var item = new ToolStripMenuItem("検索フォルダの設定");
            item.Click += (s, e) => this.OpenDirectorySettingsClick.Invoke(s, e);
            menuItems.Add(item);
            // 除外フォルダの設定を開く
            item = new ToolStripMenuItem("除外フォルダの設定");
            item.Click += (s, e) => this.OpenExcludeDirectorySettingsClick.Invoke(s, e);
            menuItems.Add(item);
            // タグの設定を開く
            item = new ToolStripMenuItem("タグの設定");
            item.Click += (s, e) => this.OpenTagSettingsClick.Invoke(s, e);
            menuItems.Add(item);
            // 検索ワード候補の設定を開く
            item = new ToolStripMenuItem("検索ワード候補設定");
            item.Click += (s, e) => this.OpenCandidateClick.Invoke(s, e);
            menuItems.Add(item);
            // スクリプトフォルダを開く
            item = new ToolStripMenuItem("スクリプトを開く");
            item.Click += (s, e) => this.OpenScriptDirectoryClick.Invoke(s, e);
            menuItems.Add(item);
            // セパレーター
            item = new ToolStripMenuItem("────────────"){ Enabled = false };
            menuItems.Add(item);
            // 自動保存切り替え
            item = new ToolStripMenuItem("自動保存（タグ、別名、履歴）"){
                Checked = pAutoSaveEnabled
            };
            item.Click += (s, e) =>
            {
                var sender = (ToolStripMenuItem)s;
                bool isAutoSaveRequested = !sender.Checked;
                sender.Checked = isAutoSaveRequested;
                AutoSaveStatusChangeEventArgs args;
                if (isAutoSaveRequested){
                    args = AutoSaveStatusChangeEventArgs.GetAutoSaveChangeToRequestedArgs();
                }
                else{
                    args = AutoSaveStatusChangeEventArgs.GetAutoSaveChangeToCancelArgs();
                }
                this.AutoSaveStatusChanging.Invoke(this, args);
                this.AutoSaveClick.Invoke(this, args);
                args.Dispose();
                this.AutoSaveStatusChanged.Invoke(this, args);
            };
            menuItems.Add(item);
            // 読み取り専用に切り替える
            item = new ToolStripMenuItem("読取専用⇔保存可能");
            item.Click += (s, e) => this.SwitchLockClick.Invoke(s, e);
            menuItems.Add(item);
            // ファイルサイズの取得
            item = new ToolStripMenuItem("ファイルサイズを取得");
            item.Click += (s, e) => this.UpdateListViewInfoClick.Invoke(s, e);
            menuItems.Add(item);
            // CSV 出力
            item = new ToolStripMenuItem("CSVで出力");
            item.Click += (s, e) => this.ExportCsvClick.Invoke(s, e);
            menuItems.Add(item);
            // セパレーター
            item = new ToolStripMenuItem("────────────"){ Enabled = false };
            menuItems.Add(item);
            // 無効なタグ、別名を照会する
            item = new ToolStripMenuItem("無効なタグ、別名を参照");
            item.Click += (s, e) => {
                this.AddCancellableTask(() =>{
                    this.ShowInvalidTagAliasViewClick.Invoke(s, new CancelEventArgs(_cts.Token));
                });
            };
            menuItems.Add(item);
            // 無効な履歴を照会
            item = new ToolStripMenuItem("表示中の無効な履歴を参照");
            item.Click += (s, e) => {
                this.AddCancellableTask(() =>{
                    this.ShowHistoryRepairViewClick.Invoke(s, new CancelEventArgs(_cts.Token));
                });
            };
            menuItems.Add(item);
            // MenuItem を戻す
            return menuItems;
        }
        void AddCancellableTask(Action pAction)
        {
            var tk = Task.Run(() => {
                try
                {
                    Task.Run(pAction, _cts.Token);
                }
                finally
                {
                    lock (_lock)
                    {
                        var comp = _cancellableTasks.FindAll(val => val.IsCompleted);
                        foreach (var val in comp)
                        {
                            _cancellableTasks.Remove(val);
                        }
                    }
                }
            });
            lock (_lock) _cancellableTasks.Add(tk);
        }
        public event EventHandler<OptionMenuClickEventArgs> OptionMenuClicked = delegate{};
        public event EventHandler<OptionMenuItemsChangedEventArgs> OptionMenuItemsChanged = delegate{};
        public event EventHandler<EventArgs> OpenDirectorySettingsClick = delegate{};
        public event EventHandler<EventArgs> OpenTagSettingsClick = delegate{};
        public event EventHandler<EventArgs> OpenCandidateClick = delegate{};
        public event EventHandler<EventArgs> OpenExcludeDirectorySettingsClick = delegate{};
        public event EventHandler<EventArgs> OpenScriptDirectoryClick = delegate{};
        public event EventHandler<AutoSaveStatusChangeEventArgs> AutoSaveStatusChanging = delegate{};
        public event EventHandler<AutoSaveStatusChangeReadOnlyEventArgs> AutoSaveClick = delegate{};
        public event EventHandler<AutoSaveStatusChangeReadOnlyEventArgs> AutoSaveStatusChanged = delegate{};
        public event EventHandler<EventArgs> SwitchLockClick = delegate{};
        public event EventHandler<EventArgs> UpdateListViewInfoClick = delegate{};
        public event EventHandler<EventArgs> ExportCsvClick = delegate{};
        public event EventHandler<CancelEventArgs> ShowInvalidTagAliasViewClick = delegate{};
        public event EventHandler<CancelEventArgs> ShowHistoryRepairViewClick = delegate{};
    }
}
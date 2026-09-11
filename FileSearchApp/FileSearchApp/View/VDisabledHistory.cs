using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomFunctions;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public partial class VDisabledHistory : Form
    {
        ListViewDoubleBuff _listView = new ListViewDoubleBuff();
        Button _allSelectBtn = new Button();
        Button _allUnSelectBtn = new Button();
        ButtonMouseDownSave _updBtn = new ButtonMouseDownSave();
        Button _delBtn = new Button();
        VMDisabledHistory _viewModel;
        public VDisabledHistory(VMDisabledHistory pViewModel)
        {
            _viewModel = pViewModel;
            //検索していたら止める
            if (_viewModel.IsSearching){
                _viewModel.CancelSearch();
            }
            InitializeComponent();
            InitializeEventSet();
        }
        protected override void Dispose(bool disposing)
        {
            if (this.IsDisposed){
                return;
            }
            _viewModel.CancelSearch();
            if (disposing){
                ;
            }
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            this.Text = "無効な履歴の管理";
            this.Icon = MyIcon.CloneIcon().GetOrDefault(this.Icon);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = _viewModel.MainWindow.WindowProperty.ToRectangle().Location;
            this.Size = _viewModel.MainWindow.WindowProperty.ToRectangle().Size;
            this.KeyPreview = true;
            //全選択ボタン
            this.Controls.Add(_allSelectBtn);
            _allSelectBtn.Text = "全選択";
            _allSelectBtn.Top = 9;
            _allSelectBtn.Left = 10;
            _allSelectBtn.Height = 21;
            _allSelectBtn.Width = 90;
            //全解除ボタン
            this.Controls.Add(_allUnSelectBtn);
            _allUnSelectBtn.Text = "全解除";
            _allUnSelectBtn.Top = 9;
            _allUnSelectBtn.Left = _allSelectBtn.Left + _allSelectBtn.Width;
            _allUnSelectBtn.Height = 21;
            _allUnSelectBtn.Width = 90;
            //修正ボタン
            this.Controls.Add(_updBtn);
            _updBtn.Text = "選択を検索し修正";
            _updBtn.Top = 9;
            _updBtn.Left = _allUnSelectBtn.Left + _allUnSelectBtn.Width;
            _updBtn.Height = 21;
            _updBtn.Width = 150;
            //削除ボタン
            this.Controls.Add(_delBtn);
            _delBtn.Text = "選択を履歴から削除";
            _delBtn.Top = 9;
            _delBtn.Left = 5 + _updBtn.Left + _updBtn.Width;
            _delBtn.Height = 21;
            _delBtn.Width = 150;
            //リストビュー
            this.Controls.Add(_listView);
            _listView.Top = _allSelectBtn.Top + _allSelectBtn.Height + 5;
            _listView.Left = 10;
            _listView.Height = this.Height - _listView.Top - 50;
            _listView.Width = this.Width - 36;
            _listView.VirtualMode = true;
            _listView.MultiSelect = false;
            _listView.AllowColumnReorder = true;
            _listView.FullRowSelect = true;
            _listView.Scrollable = true;
            _listView.GridLines = true;
            _listView.HideSelection = false;
            _listView.View = View.Details;
            _listView.CheckBoxes = true;
            var cloneHead = new List<ColumnHeader>();
            foreach (var val in _viewModel.MainWindow.ListViewColumnHeaders)
            {
                cloneHead.Add((ColumnHeader)val.Clone());
            }
            _listView.Columns.AddRange(cloneHead.ToArray());
        }
    }
    public partial class VDisabledHistory
    {
        public void InitializeEventSet()
        {
            // ============================================================
            // = 説明：Viewからのイベント
            // ============================================================
            this.SizeChanged += (s, e) =>
            {
                _listView.Height = this.Height - _listView.Top - 50;
                _listView.Width = this.Width - 36;
            };
            this.Shown += (s, e) =>
            {
                this.ActivateForm(1000);
                _listView.VirtualListSize = _viewModel.ViewModelListView.DataSource.Count;
            };
            this.FormClosed += (s, e) =>
            {
                if (_viewModel.IsSearching){
                    _viewModel.CancelSearch();
                }
            };
            this.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Escape){
                    this.Close();
                }
            };
            _allSelectBtn.Click += (s, e) =>
            {
                if (_viewModel.IsSearching){
                    return;
                }
                _viewModel.ViewModelListView.SelectAll();
                int maxCnt = _listView.VirtualListSize;
                if (maxCnt != 0){
                    _listView.RedrawItems(0, maxCnt - 1, false);
                }
            };
            _allUnSelectBtn.Click += (s, e) =>
            {
                if (_viewModel.IsSearching){
                    return;
                }
                _viewModel.ViewModelListView.UnSelectAll();
                var inds = new List<int>(_viewModel.ViewModelListView.SelectedIndices);
                int maxCnt = _listView.VirtualListSize;
                if (maxCnt != 0){
                    _listView.RedrawItems(0, maxCnt - 1, false);
                }
            };
            _updBtn.MouseClick += async (s, e) =>
            //_updBtn.Click += async (s, e) =>
            {
                if (_viewModel.IsSearching){
                    _viewModel.CancelSearch();
                    return;
                }
                if (e.Button == MouseButtons.Right){
                    await _viewModel.StartSearchFolderSelectionAsync();
                }
                else{
                    await _viewModel.StartSearch();
                }
                _viewModel.ViewModelListView.UnSelectAll();
                var filter = new Filter<ExFileInfo>(info => !_viewModel.IsRepaired(info));
                _viewModel.ViewModelListView.ApplyFilter(filter);
                _listView.RedrawItems();
            };
            _delBtn.Click += (s, e) =>
            {
                if (_viewModel.IsSearching){
                    return;
                }
                _viewModel.RemoveHistory(
                    _viewModel.ViewModelListView.SelectedIndices.ToList()
                );
                _viewModel.ViewModelListView.UnSelectAll();
                _viewModel.ViewModelListView.ApplyFilter(
                    new Filter<ExFileInfo>(info => {
                        return !_viewModel.IsRepaired(info);
                    })
                );
                _listView.RedrawItems();
            };
            _listView.ItemSelectionChanged += (s, e) =>
            {
                try
                {
                    bool wantReturn1 = e.ItemIndex < 0;
                    bool wantReturn2 = _viewModel.IsSearching;
                    if (_viewModel.IsSearching){
                        return;
                    }
                    if (wantReturn1 || wantReturn1){
                        return;
                    }
                    const bool ADDITION = true;
                    if ((System.Windows.Forms.Control.ModifierKeys & Keys.Shift) == Keys.Shift){
                        int beforeIndex = _viewModel.ViewModelListView.SelectedIndex;
                        int currentIndex = e.ItemIndex;
                        if (beforeIndex < 0 | currentIndex < 0){
                            return;
                        }
                        int startIndex = Math.Min(beforeIndex, currentIndex);
                        int endIndex = Math.Max(beforeIndex, currentIndex);
                        
                        for (int i = startIndex; i <= endIndex; i++){
                            _viewModel.ViewModelListView.SelectItem(i, ADDITION);
                        }
                        _viewModel.ViewModelListView.SelectItem(currentIndex, ADDITION);
                    }
                    else if (_viewModel.ViewModelListView.IsSelected(e.ItemIndex)){
                        _viewModel.ViewModelListView.UnSelectItem(e.ItemIndex);
                    }
                    else{
                        _viewModel.ViewModelListView.SelectItem(e.ItemIndex, ADDITION);
                    }
                    ChangeItemBackColor(e.Item, e.ItemIndex);
                }
                finally
                {
                    e.Item.Focused = false;
                    _listView.SelectedIndices.Clear();
                }
            };
            _listView.KeyDown += (s, e) =>
            {
                if (_viewModel.IsSearching){
                    return;
                }
                // イベント処理済みなら即リターン。そうでないなら処理済みに設定する
                if (e.Handled){
                    return;
                }
                else{
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.Down){
                    var selectionIndex = _viewModel.ViewModelListView.SelectedIndex;
                    var lastIndex = _viewModel.ViewModelListView.DataSource.Count - 1;
                    if (selectionIndex >= lastIndex){
                        return;
                    }
                    int nextIdx = selectionIndex + 1;
                    if (_viewModel.ViewModelListView.IsSelected(nextIdx)){
                        return;
                    }
                    _listView.SelectedIndices.Add(nextIdx);
                    _listView.EnsureVisible(nextIdx);
                }
                else if (e.KeyCode == Keys.Up){
                    var selectionIndex = _viewModel.ViewModelListView.SelectedIndex;
                    if (selectionIndex <= 0){
                        return;
                    }
                    int nextIdx = selectionIndex - 1;
                    if (_viewModel.ViewModelListView.IsSelected(nextIdx)){
                        return;
                    }
                    _listView.SelectedIndices.Add(nextIdx);
                    _listView.EnsureVisible(nextIdx);
                }
            };
            _listView.RetrieveVirtualItem += (s, e) =>
            {
                var info = _viewModel.ViewModelListView.GetExFileInfo(e.ItemIndex);
                e.Item = _viewModel.ViewModelListView.GetListViewItem(e.ItemIndex);
                if (_viewModel.ViewModelListView.IsSelected(e.ItemIndex)){
                    e.Item.Selected = false;
                    e.Item.Focused = false;
                }
                ChangeItemBackColor(e.Item, e.ItemIndex);
            };
            _listView.ColumnClick += (s, e) =>
            {
                if (_viewModel.IsSearching){
                    return;
                }
                _viewModel.ViewModelListView.SortListView(_listView, e.Column);
                _listView.VirtualListSize = 0;
                _listView.VirtualListSize = _viewModel.ViewModelListView.Count;
            };
            // ============================================================
            // = 説明：ViewModelからのイベント
            // ============================================================
            _viewModel.ViewModelListView.ViewModelCursorChanged += (s, e) =>
            {
                this.InvokeIfRequiredElseNonInvoke(() => 
                {
                    _listView.Cursor = e.Value;
                });
            };
            _viewModel.ViewModelListView.ViewModelDataSourceChanged += (s, e) => 
            {
                this.InvokeIfRequiredElseNonInvoke(() =>
                {
                    if (_listView.VirtualListSize != e.ListSize){
                        _listView.VirtualListSize = e.ListSize;
                    }
                    _listView.RedrawItems();
                });
            };
            _viewModel.ViewModeRunCaptionChanged += (s, e) =>
            {
                this.InvokeIfRequiredElseNonInvoke(() =>
                {
                    _updBtn.Text = e;
                });
            };
        }
        public void ChangeItemBackColor(ListViewItem pItem, int pItemIndex)
        {
            // 有効な行は色付きにして、選択されても色を変えない
            // 削除時に警告が発生する行は色付きにして、選択すれば色が変わる
            // 選択されている行は色を変える
            pItem.BackColor = System.Drawing.Color.White;
            var info = _viewModel.ViewModelListView.GetExFileInfo(pItemIndex);
            // 修復した行
            if (_viewModel.IsRepaired(info)){
                pItem.BackColor = System.Drawing.Color.LimeGreen;
                return;
            }
            // 警告が発生する行
            if (_viewModel.IsDeleteAlertInfo(info)){
                pItem.BackColor = System.Drawing.Color.Yellow;
            }
            // 選択されている行
            if (_viewModel.ViewModelListView.IsSelected(pItemIndex)){
                pItem.BackColor = System.Drawing.Color.SkyBlue;
            }
        }
    }
}

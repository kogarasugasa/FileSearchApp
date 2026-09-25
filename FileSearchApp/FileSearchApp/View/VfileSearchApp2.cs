using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using CustomFunctions;
using System.CodeDom;

namespace FileSearchApp
{
    public partial class VFileSearchApp : Form
    {
        int _tabOffset = 10;
        int _tabMoveIni = 0;
        int _tabBeforeOffset = 0;
        bool _tabMoved = false;
        void SetViewModelTabEvent()
        {
            _vm.TabAdditioned += (s, e) =>
            {
                var label = this.AddTab();
                this.AllocateTab(label, e.Record);
                _vm.OnSelectClick(e.Record.Key);
            };
            _vm.TabRemoving += (s, e) => {
                // すべてのタブ Control を取得する
                var tabControls = new List<Control>();
                for (int i = 0; i < this.Controls.Count; i++)
                {
                    if (object.ReferenceEquals(this.Controls[i].Tag, _controlTag_Tab)){
                        tabControls.Add(this.Controls[i]);
                    }
                }
                // タブが1個以下の場合は削除しない
                if (tabControls.Count <= 1){
                    return;
                }
                // 削除するアイテムを取得する
                var removeItems = tabControls.Where(val => val.Name == e.Record.Key);
                if (removeItems.Count() != 1){
                    throw new Exception(
                        "削除対象のタブが見つからない。または2つ以上検出されました"
                    );
                }
                // アイテムを削除しタブの位置を調整する
                removeItems.Get(0).IfSome(removeItem =>{
                    // 現在の位置を取得する
                    var leftPos = tabControls.Select(val => val.Left).ToList();
                    // 削除する
                    this.Controls.Remove(removeItem);
                    tabControls.Remove(removeItem);
                    removeItem.Dispose();
                    // 位置を調整する
                    tabControls.Sort((x, y) => x.Left.CompareTo(y.Left));
                    leftPos.Sort();
                    for (int i = 0; i < tabControls.Count; i++)
                    {
                        tabControls[i].Left = leftPos[i];
                    }
                });
            };
            _vm.TabChanging += (s, e) => {
                lock (_lock)
                {
                    e.Record.ListView.ClearEventAll();
                    e.Record.FileSearch.ClearEventAll();
                    e.Record.Option.ClearEventOptionMenuItemsChanged();
                }
                for (int i = 0; i < this.Controls.Count; i++)
                {
                    if (object.ReferenceEquals(this.Controls[i], e.Record.UiObjectKey)){
                        var lbl = (Label)this.Controls[i];
                        lbl.Text = this.GetTabText(e.Record.Text, lbl.Width);
                        // lbl.Text = MsPGothic.GetAdjustedText(
                        //     e.Record.Text.ToHarf(), lbl.Width - 6
                        // );
                        lbl.BackColor = _vm.DefaultColor;
                        lbl.BorderStyle = BorderStyle.None;
                        break;
                    }
                }
            };
            _vm.TabChanged += (s, e) => {
                var controls = new List<Control>();
                for (int i = 0; i < this.Controls.Count; i++) controls.Add(this.Controls[i]);
                var tab = controls
                    .Where(val => object.ReferenceEquals(val.Tag, _controlTag_Tab))
                    .Where(val => val.Name == e.Record.Key)
                    .Get(0)
                ;
                tab.IfSome(some => {
                    if (some.GetType() == new Label().GetType()){
                        var lbl = (Label)some;
                        lbl.Text = this.GetTabText(e.Record.Text, lbl.Width);
                        // lbl.Text = MsPGothic.GetAdjustedText(
                        //     e.Record.Text.ToHarf(), lbl.Width - 6
                        // );
                        lbl.BackColor = Color.FromArgb(0, this.BackColor);
                        lbl.BorderStyle = BorderStyle.FixedSingle;
                    }
                    else{
                        throw new Exception("Contros を Label にキャスト出来ません");
                    }
                });
                lock (_lock)
                {
                    _listView.VirtualListSize = 0;
                    e.Record.ListView.Func.ListViewheaderHeight = _listView.ListViewHeaderHeight;
                    e.Record.ListView.Func.ListViewRowHeight = _listView.ListViewRowHeight;
                    this.SetViewModelEvent();
                    _aliasCheckBox.Checked = e.Record.ListView.AliasEnabled;
                    _searchWordsTxtbox.Text = e.Record.FileSearch.SearchWords;
                }
                e.Record.FileSearch.RiseEventAll();
                e.Record.ListView.RiseEventAll();
                var selectionIndex = e.Record.ListView.SelectedIndex;
                if (selectionIndex != -1 && selectionIndex < _listView.VirtualListSize){
                    _listView.EnsureVisible(e.Record.ListView.SelectedIndex);
                }
                _searchWordsTxtbox.Focus();
            };
        }
        /// <summary>
        /// ViewModel の Event に View 側の処理を設定する
        /// </summary>
        void SetViewModelEvent()
        {
            // ============================================================
            // = 説明：ViewModelからのイベント
            // ============================================================
            _vm.Record.Option.OptionMenuItemsChanged += (s, e) =>
            {
                this.InvokeIfRequiredElseNonInvoke(() =>{
                    this.ContextMenuStrip.Items.Clear();
                    this.ContextMenuStrip.Items.AddRange(e.Items);
                });
            };
            _vm.Record.ListView.ViewModelCursorChanged += (s, e) =>
            {
                this.Invoke((MethodInvoker)delegate(){
                    _listView.Cursor = e.Value;
                });
            };
            _vm.Record.ListView.ViewModelDataSourceChanged += (s, e) =>
            {
                this.Invoke((MethodInvoker)delegate(){
                    //リストサイズが違うときだけ更新する
                    if (_listView.VirtualListSize != e.ListSize){
                        _listView.VirtualListSize = e.ListSize;
                        this.Text = _title + " : " + e.ListSize.ToString();
                    }
                    else if (e.Redraw){
                        _listView.RedrawItems();
                    }
                });
            };
            _vm.Record.ListView.ViewModelFileSizeUpdated += (s, e) =>
            {
                this.Invoke((MethodInvoker)delegate(){
                    if (e.Numerator == e.Denominator){
                        this.Text = _title + " : " + _vm.Record.ListView.Count;
                    }
                    else{
                        this.Text = (
                            e.ProgressRatioPercentile +
                            "%" +
                            " : " +
                            _vm.Record.ListView.Count
                        );
                    }
                });
            };
            _vm.Record.FileSearch.ViewModeRunCaptionChanged += (s, e) =>
            {
                this.Invoke((MethodInvoker)delegate(){
                    _runSearchBtn.Text = e.Value;
                });
            };
        }
        Label AddTab()
        {
            var label = new Label();
            this.Controls.Add(label);
            label.Tag = _controlTag_Tab; // タブとしてタグ付する
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.BackColor = _vm.DefaultColor;
            //label.BorderStyle = BorderStyle.FixedSingle;
            label.Height = 21;
            label.Width = 80;
            label.Top = 2;
            // 現在のタブの数を数える
            int cnt = 0;
            for (int i = 0; i < this.Controls.Count; i++)
            {
                if (this.Controls[i].Tag == _controlTag_Tab) cnt++;
            }
            // 新しいタブの表示位置を決定する
            label.Left = _tabOffset + (label.Width + 1) * (cnt - 1);
            return label;
        }
        void AllocateTab(Label pLabel, TabRecord pRecord)
        {
            pRecord.UiObjectKey = pLabel;
            pLabel.Text = this.GetTabText(pRecord.Text, pLabel.Width);
            pLabel.Name = pRecord.Key;
            pLabel.BackColor = _vm.DefaultColor;
            pLabel.MouseDown += this.Tab_MouseDown;
            pLabel.MouseUp += this.Tab_MouseUp;
            pLabel.MouseClick += (sender, eventArgs) =>
            {
                if (_tabMoved){
                    return;
                }
                var lbl = (Label)sender;
                if (eventArgs.Button == MouseButtons.Right){
                    if (_vm.Count <= 1){
                        return;
                    }
                    _vm.OnRemoveClick(lbl.Name);
                }
                else if (eventArgs.Button == MouseButtons.Left){
                    _vm.OnSelectClick(lbl.Name);
                }
            };
        }
        string GetTabText(string pText, int pControlWidth)
        {
            return MsPGothic.GetAdjustedText(pText.ToHarf(), pControlWidth - 6);
        }
        void Tab_MouseDown(object s, MouseEventArgs e)
        {
            if (s.GetType() != new Label().GetType()){
                return;
            }
            var lbl = (Label)s;
            _tabMoved = false;
            _tabMoveIni = Cursor.Position.X;
            _tabBeforeOffset = _tabOffset;
            lbl.MouseMove += Tab_MouseMove;
        }
        void Tab_MouseUp(object s, MouseEventArgs e)
        {
            if (s.GetType() != new Label().GetType()){
                return;
            }
            var lbl = (Label)s;
            lbl.MouseMove -= Tab_MouseMove;
        }
        void Tab_MouseMove(object s, EventArgs e)
        {
            if (s.GetType() != new Label().GetType()){
                return;
            }
            // タブをすべて取得する
            var tabControls = new List<Control>();
            for (int i = 0; i < this.Controls.Count; i++)
            {
                if (object.ReferenceEquals(this.Controls[i].Tag, _controlTag_Tab)){
                    tabControls.Add(this.Controls[i]);
                }
            }
            // 表示範囲内なら移動しない
            var moveValue = Cursor.Position.X - _tabMoveIni;
            if (Math.Abs(moveValue) > 2){
                _tabMoved = true;
            }
            // タブの右端の位置を調べる
            var rightPos = tabControls.Select(val => val.Left + val.Width);
            if (!rightPos.Any()){
                return;
            }
            var tabAllWidth = rightPos.Max() - tabControls[0].Left;
            var tabOffsetNew = _tabBeforeOffset + moveValue;
            if (tabOffsetNew > 10){
                tabOffsetNew = 10;
            }
            else if (tabOffsetNew + tabAllWidth < this.Width - 26){
                //tabOffsetNew = this.Width - 26 - tabAllWidth;
                tabOffsetNew = tabAllWidth - (this.Width - 26);
                if (tabOffsetNew > 10){
                    tabOffsetNew = tabOffsetNew * -1;
                }
                else{
                    tabOffsetNew = 10;
                }
            }
            _tabOffset = tabOffsetNew;
            // 位置を調整する
            tabControls.Sort((x, y) => x.Left.CompareTo(y.Left));
            for (int i = 0; i < tabControls.Count; i++)
            {
                tabControls[i].Left = _tabOffset + (tabControls[i].Width + 1) * i;
            }
        }
    }
}
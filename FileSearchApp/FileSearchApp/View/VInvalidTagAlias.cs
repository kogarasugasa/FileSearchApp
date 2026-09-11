using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using CustomFunctions;

namespace FileSearchApp
{
    public class VInvalidTagAlias : Form
    {
        Button _delBtn = new Button();
        ListBox _tagListBox = new ListBox();
        ListBox _aliasListBox = new ListBox();
        Color _defListBoxBackColor;
        VMInvalidTagAlias _vm;
        CancellationToken _ct;
        System.Timers.Timer _cancelTimer = new System.Timers.Timer();
        public VInvalidTagAlias(VMInvalidTagAlias pVm, CancellationToken ct)
        {
            _vm = pVm;
            _ct = ct;
            using (var obj = new ListBox()) _defListBoxBackColor = obj.BackColor;
            _cancelTimer.Elapsed += (s, e) =>{
                if (_ct.IsCancellationRequested){
                    this.InvokeIfRequiredElseNonInvoke(() =>{
                        this.Close();
                    });
                }
            };
            this.InitializeComponet();
            this.InitializeEventSet();
        }
        ~VInvalidTagAlias()
        {
            _cancelTimer.Stop();
            _cancelTimer.Dispose();
        }
        void InitializeComponet()
        {
            this.Text = "履歴に存在しない割り当てられたタグと別名";
            this.Icon = MyIcon.CloneIcon().GetOrDefault(this.Icon);
            var rect = new Rectangle{
                X = Cursor.Position.X - 200,
                Y = Cursor.Position.Y - 50,
                Width = 500,
                Height = 300,
            };
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = MyTool.CheckWindowRectangle(rect, 50, 50);
            this.KeyPreview = true; // Escape で閉じれるようにする為
            
            this.Controls.Add(_delBtn);
            _delBtn.Text = "削除";
            _delBtn.Left = 10;
            _delBtn.Top = 5;
            _delBtn.Width = 60;
            _delBtn.Height = 21;
            _delBtn.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            this.Controls.Add(_tagListBox);
            _tagListBox.Font = new System.Drawing.Font("ＭＳ ゴシック", _tagListBox.Font.Size);
            _tagListBox.Bounds = GetListBoxPos(_delBtn, 1, 2);
            _tagListBox.SelectionMode = SelectionMode.One;
            _tagListBox.BackColor = Color.Silver;

            this.Controls.Add(_aliasListBox);
            _aliasListBox.Font = new System.Drawing.Font("ＭＳ ゴシック", _aliasListBox.Font.Size);
            _aliasListBox.Bounds = GetListBoxPos(_delBtn, 2, 2);
            _aliasListBox.SelectionMode = SelectionMode.One;
            _aliasListBox.BackColor = Color.Silver;
        }
        Rectangle GetListBoxPos(Control pLastControl, int pLebel, int pSplit)
        {
            const int V_SPAN = 5;
            const int H_SPAN = 10;
            var top = V_SPAN + pLastControl.Top + pLastControl.Height;
            var left = H_SPAN;
            var width = this.Width - left - 26;
            var height = this.Height - top - 42;
            var h = (height - V_SPAN * (pSplit - 1)) / 2;
            var t = top + (V_SPAN + h) * (pLebel - 1);
            return new Rectangle(left, t, width, h);
        }
        void InitializeEventSet()
        {
            this.Load += (s, e) => {
                foreach (var tag in _vm.GetInvalidTags())
                {
                    _tagListBox.Items.Add(tag);
                }
                foreach (var alias in _vm.GetInvalidAliases())
                {
                    _aliasListBox.Items.Add(alias);
                }
            };
            this.Shown += (s, e) =>
            {
                this.ActivateForm(1000);
            };
            this.SizeChanged += (s, e) => {
                _tagListBox.Bounds = this.GetListBoxPos(_delBtn, 1, 2);
                _aliasListBox.Bounds = this.GetListBoxPos(_delBtn, 2, 2);
            };
            this.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Escape){
                    this.Close();
                }
            };
            _delBtn.Click += (s, e) => {
                if (_tagListBox.BackColor == _defListBoxBackColor
                && _aliasListBox.BackColor == _defListBoxBackColor){
                    return;
                }
                else if (_tagListBox.BackColor == _defListBoxBackColor){
                    if (!_vm.IsItemSelected(_tagListBox)){
                        return;
                    }
                    var alloc = (VMInvalidTagAlias.TagAllocate)_tagListBox.SelectedItem;
                    _vm.DeleteTagAllocate(alloc.FileName, alloc.Tag);
                    _tagListBox.Items.RemoveAt(_tagListBox.SelectedIndex);
                    return;
                }
                else if (_aliasListBox.BackColor == _defListBoxBackColor){
                    if (!_vm.IsItemSelected(_aliasListBox)){
                        return;
                    }
                    var alloc = (VMInvalidTagAlias.AliasAllocate)_aliasListBox.SelectedItem;
                    _vm.DeleteAliasAllocate(alloc.FileName);
                    _aliasListBox.Items.RemoveAt(_aliasListBox.SelectedIndex);
                    return;
                }
                else{
                    return;
                }
            };
            _tagListBox.SelectedIndexChanged += (s, e) =>
            {
                if (!_vm.IsItemSelected(_tagListBox)){
                    _tagListBox.SelectedIndex = -1;
                }
            };
            _tagListBox.GotFocus += (s, e) =>
            {
                // 別名側を無効にする
                _vm.AliasSelectedIndex = _aliasListBox.SelectedIndex;
                _aliasListBox.BackColor = Color.Silver;
                _aliasListBox.SelectedIndex = -1;
                // タグ側を有効にする
                _tagListBox.BackColor = _defListBoxBackColor;
                _tagListBox.SelectedIndex = _vm.TagSelectedIndex;
            };
            _aliasListBox.SelectedIndexChanged += (s, e) =>
            {
                if (!_vm.IsItemSelected(_aliasListBox)){
                    _aliasListBox.SelectedIndex = -1;
                }
            };
            _aliasListBox.GotFocus += (s, e) =>
            {
                // タグ側を無効にする
                _vm.TagSelectedIndex = _tagListBox.SelectedIndex;
                _tagListBox.BackColor = Color.Silver;
                _tagListBox.SelectedIndex = -1;
                // 別名側を有効にする
                _aliasListBox.BackColor = _defListBoxBackColor;
                _aliasListBox.SelectedIndex = _vm.AliasSelectedIndex;
            };
        }
    }
}
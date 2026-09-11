using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.CodeDom;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public partial class VFileSearchApp : Form
    {
        readonly object _lock = new object();
        readonly object _controlTag_Tab = new object();
        string _title = "Search";
        Action _parentDispose;
        IOption<ListViewItem> _topItem = new None<ListViewItem>(); // リストビューにタブでフォーカスした場合に先頭行にレティクルが表示される問題の対策用
        ListViewDoubleBuff _listView = new ListViewDoubleBuff();
        Button _optionBtn = new Button();
        Button _filterBtn = new Button();
        Button _getClipBtn = new Button();
        ButtonMouseDownSave _runSearchBtn = new ButtonMouseDownSave();
        CheckBox _aliasCheckBox = new CheckBox();
        Label _clearBtn = new Label();
        TextBoxOptionKey _searchWordsTxtbox = new TextBoxOptionKey();
        VMFileSearchTab _vm;
        VMMainWindow _vmWindow;
        public VFileSearchApp(
        VMMainWindow pVMMainWindow,
        VMFileSearchTab pVMFileSearchTab,
        Action pParentDispose)
        {
            // 初期化
            _vmWindow = pVMMainWindow;
            _vm = pVMFileSearchTab;
            _parentDispose = pParentDispose;
            this.Icon = MyIcon.CloneIcon().GetOrDefault(this.Icon);
            this.InitializeComponent();
            this.InitializeEvent();
            this.SetViewModelTabEvent();
            this.SetViewModelEvent();
            if (_vm.Record.FileSearch.IsReadOnly){
                MMessageBox.Show("読み取り専用で開きます");
            }
        }
        ~VFileSearchApp()
        {
            this.Dispose();
        }
        public new void Dispose()
        {
            _vm.Record.FileSearch.CancelSearch();
            _vm.Record.FileSearch.ClearEventAll();
            _vm.Record.ListView.ClearEventAll();
            _vm.Record.Option.Dispose();
            base.Dispose();
        }
        private void InitializeComponent()
        {
            this.Name = _title;
            this.Text = _title;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = _vmWindow.WindowProperty.ToRectangle().Location;
            this.Size = _vmWindow.WindowProperty.ToRectangle().Size;
            if (_vmWindow.WindowProperty.WindowState == FormWindowState.Maximized){
                this.WindowState = FormWindowState.Maximized;
            }
            //オプションボタンメニュー
            this.ContextMenuStrip = new ContextMenuStrip();
            //オプションボタン
            this.Controls.Add(_optionBtn);
            _optionBtn.Text = "三";
            _optionBtn.Top = 25;
            _optionBtn.Left = 10 - 1;
            _optionBtn.Height = 21;
            _optionBtn.Width = 30;
            _optionBtn.TabStop = false;
            //フィルターボタン
            this.Controls.Add(_filterBtn);
            _filterBtn.Text = "Filter";
            _filterBtn.Top = 25;
            _filterBtn.Left = _optionBtn.Left + _optionBtn.Width;
            _filterBtn.Height = 21;
            _filterBtn.Width = 50;
            _filterBtn.TabStop = false;
            //別名チェックボックス
            this.Controls.Add(_aliasCheckBox);
            _aliasCheckBox.Text = "Alias";
            _aliasCheckBox.Top = 25;
            _aliasCheckBox.Left = 5 + _filterBtn.Left + _filterBtn.Width;
            _aliasCheckBox.Width = 50;
            _aliasCheckBox.Checked = _vm.Record.ListView.AliasEnabled;
            //クリップボード貼り付けボタン
            this.Controls.Add(_getClipBtn);
            _getClipBtn.Text = "Clip";
            _getClipBtn.Top = 25;
            _getClipBtn.Left = _aliasCheckBox.Left + _aliasCheckBox.Width;
            _getClipBtn.Height = 21;
            _getClipBtn.Width = 50;
            //実行ボタン
            this.Controls.Add(_runSearchBtn);
            _runSearchBtn.Text = "Run";
            _runSearchBtn.Top = 25;
            _runSearchBtn.Left = _getClipBtn.Left + _getClipBtn.Width;
            _runSearchBtn.Height = 21;
            _runSearchBtn.Width = 40;
            //クリアボタン
            this.Controls.Add(_clearBtn);
            _clearBtn.Text = "×";
            _clearBtn.Top = 25 + 1; //枠補正 + 1
            _clearBtn.Left = _runSearchBtn.Left + _runSearchBtn.Width;
            _clearBtn.Height = 21 - 2; //枠補正 - 2
            _clearBtn.Width = 21 + 1;
            _clearBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            _clearBtn.BackColor = Color.White;
            _clearBtn.BorderStyle = BorderStyle.FixedSingle;
            _clearBtn.Font = new Font(_clearBtn.Font.FontFamily, 11 ,FontStyle.Bold);
            //入力文字列テキストボックス
            this.Controls.Add(_searchWordsTxtbox);
            _searchWordsTxtbox.Top = 25 + 1; //CrearButton 枠補正
            _searchWordsTxtbox.Left = _clearBtn.Left + _clearBtn.Width - 1;
            _searchWordsTxtbox.Width = this.Width - _searchWordsTxtbox.Left - 26;
            //リストビュー
            this.Controls.Add(_listView);
            _listView.Top = _searchWordsTxtbox.Top + _searchWordsTxtbox.Height + 5;
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
            _listView.Sorting = SortOrder.Ascending;
            _listView.View = View.Details;
            _listView.Columns.AddRange(_vmWindow.ListViewColumnHeaders);
            _listView.ContextMenuStrip = new ContextMenuStrip();
            _listView.AllowDrop = true; //ドラッグドロップでファイルを追加する為
        }
    }
}

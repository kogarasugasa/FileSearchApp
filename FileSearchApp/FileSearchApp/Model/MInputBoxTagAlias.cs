using System;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;

using CustomFunctions;
using System.IO;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public static class MInputBoxTagAlias
    {
        static IOption<Icon> _icon = new None<Icon>();
        static readonly object _lock = new object();
        public static void SetIcon(Icon pIcon)
        {
            lock (_lock)
            {
                _icon = new Some<Icon>(pIcon);
            }
        }
        public static IOption<Icon> CloneIcon()
        {
            lock (_lock)
            {
                return _icon.Match(
                    none => _icon,
                    some => new Some<Icon>(some.CloneToIcon())
                );
            }
        }
        public static IOption<Result> GetTagAlias(
        string pFilePath,
        string pAlias,
        IEnumerable<string> pTags,
        IEnumerable<string> pCheckedTags)
        {
            var vm = new VMInputBoxTagAlias(pFilePath, pTags, pCheckedTags);
            vm.Alias = pAlias;
            var v = new VInputBoxTagAlias(vm);
            var tk = Task.Run(() => Application.Run(v));
            tk.WaitByPollingLoop(100);
            return vm.Result;
        }
        public class Result
        {
            public string FileName { get; set; }
            public string Alias { get; set; }
            public List<string> CheckedTags { get; set; }
            public Result(string pFileName, string pAlias, IEnumerable<string> pCheckedTags)
            {
                this.FileName = pFileName;
                this.Alias = pAlias;
                this.CheckedTags = new List<string>(pCheckedTags);
            }
        }
        class VMInputBoxTagAlias
        {
            // ############################################
            // プロパティ
            // ############################################
            public bool Commited { get; private set; }
            public ColumnHeader[] ListViewColumnHeaders { get; private set; }
            public List<ListViewItem> Items { get; private set; }
            public IOption<Result> Result { get; set; }
            public int Left { get; set; }
            public int Top { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public string Alias { get; set; }
            public string FilePath { get; private set; }
            public string FileName { get; private set; }
            public List<string> CheckedTags { get; set; }
            public List<string> ChoiceTags { get; set; }
            // ############################################
            // コンストラクタ、デストラクタ
            // ############################################
            public VMInputBoxTagAlias(string pFilePath, IEnumerable<string> pChoiceTags, IEnumerable<string> pCheckedTags)
            {
                this.Result = new None<Result>();
                this.ChoiceTags = new List<string>(pChoiceTags);
                this.CheckedTags = new List<string>(pCheckedTags);
                this.FilePath = MyNativeMethod.Function.Drive.GetUniversalName(pFilePath);
                this.FileName = ExFileInfo.GetFileName(this.FilePath);
                this.Alias = "";
                this.Items = this.CreateItems(this.ChoiceTags, this.CheckedTags);
                this.ListViewColumnHeaders = CreateListViewColumnHeader(new List<int>());
                //
                var rect = new Rectangle(Cursor.Position.X, Cursor.Position.Y, 700, 300);
                rect = MyTool.CheckWindowRectangle(rect, 50, 50, true);
                this.Left = rect.Left;
                this.Top = rect.Top;
                this.Width = rect.Width;
                this.Height = rect.Height;
            }
            ~VMInputBoxTagAlias()
            {
                foreach (var val in this.ListViewColumnHeaders)
                {
                    if (val != null){
                        val.Dispose();
                    }
                }
                foreach (var val in this.Items)
                {
                    if (val != null){
                        if (val.ImageList != null){
                            val.ImageList.Dispose();
                        }
                    }
                }
            }
            // ############################################
            // メソッド
            // ############################################
            public void Commit()
            {
                this.Commited = true;
                var result = new Result(
                    this.FileName,
                    this.Alias,
                    new List<string>(this.CheckedTags)
                );
                this.Result = new Some<Result>(result);
            }
            public bool IsExistsPath()
            {
                return File.Exists(this.FilePath) || Directory.Exists(this.FilePath);
            }
            public List<ListViewItem> CreateItems(List<string> pTags, List<string> pCheckedTags)
            {
                var items = new List<ListViewItem>();
                foreach (var val in pTags)
                {
                    var item = new ListViewItem(val.ToString());
                    item.Checked = pCheckedTags.Contains(val);
                    items.Add(item);
                }
                return items;
            }
            public ColumnHeader[] CreateListViewColumnHeader(List<int> pColumnWidths)
            {
                var header = new ColumnHeader[]
                {
                    new ColumnHeader{Text = "TagName", Width = 200}
                };
                if (pColumnWidths.Count == 0)
                {
                    return header;
                }
                int num = pColumnWidths.Count;
                num = Math.Min(header.Count(), num);
                num--;
                for (int i = 0; i < num; i++)
                {
                    header[i].Width = pColumnWidths[i];
                }
                return header;
            }
        }
        class VInputBoxTagAlias : Form
        {
            private ListView _listView = new ListView();
            private Button _commitBtn = new Button();
            private TextBox _aliasTextBox = new TextBox();
            private TextBox _fileNameTextBox = new TextBox();
            private Label _aliasLabel = new Label();
            VMInputBoxTagAlias _vm;
            public VInputBoxTagAlias(VMInputBoxTagAlias pVMAddWindow)
            {
                _vm = pVMAddWindow;
                InitializeComponent();
                InitializeEventSet();
            }
            public void InitializeComponent()
            {
                this.Text = "add Alias and Tag";
                this.Icon = CloneIcon().GetOrDefault(this.Icon);
                this.StartPosition = FormStartPosition.Manual;
                this.Left = _vm.Left;
                this.Top = _vm.Top;
                this.Width = _vm.Width;
                this.Height = _vm.Height;
                //----
                this.Controls.Add(_fileNameTextBox);
                _fileNameTextBox.Text = _vm.FileName;
                _fileNameTextBox.Top = 9;
                _fileNameTextBox.Left = 10;
                _fileNameTextBox.Width = this.Width - 36;
                _fileNameTextBox.Text = _vm.FileName;
                _fileNameTextBox.Enabled = false;

                this.Controls.Add(_commitBtn);
                _commitBtn.Text = "追加";
                _commitBtn.Top = _fileNameTextBox.Top + _fileNameTextBox.Height + 5;
                _commitBtn.Left = 10;
                _commitBtn.Height = 21;
                _commitBtn.Width = 50;

                this.Controls.Add(_aliasLabel);
                _aliasLabel.Top = _fileNameTextBox.Top + _fileNameTextBox.Height + 5;
                _aliasLabel.Left = _commitBtn.Left + _commitBtn.Width + 5;
                _aliasLabel.Width = 35;
                _aliasLabel.Height = _commitBtn.Height;
                _aliasLabel.Text = "Alias";
                _aliasLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

                this.Controls.Add(_aliasTextBox);
                _aliasTextBox.Text = _vm.Alias;
                _aliasTextBox.Top = _fileNameTextBox.Top + _fileNameTextBox.Height + 5;
                _aliasTextBox.Left = _aliasLabel.Left + _aliasLabel.Width + 5;
                _aliasTextBox.Width = this.Width - _aliasTextBox.Left - 26;

                this.Controls.Add(_listView);
                _listView.Top = _commitBtn.Top + _commitBtn.Height + 5;
                _listView.Left = 10;
                _listView.Height = this.Height - _listView.Top - 50;
                _listView.Width = this.Width - 36;
                _listView.MultiSelect = false;
                _listView.AllowColumnReorder = true;
                _listView.FullRowSelect = true;
                _listView.Scrollable = true;
                _listView.GridLines = true;
                _listView.HideSelection = false;
                _listView.Sorting = SortOrder.None;
                _listView.View = View.Details;
                _listView.CheckBoxes = true;
                _listView.Columns.AddRange(_vm.ListViewColumnHeaders);
                _listView.Items.AddRange(_vm.Items.ToArray());
            }
            public void InitializeEventSet()
            {
                this.Shown += (s, e) =>
                {
                    this.ActivateForm(1000);
                    _aliasTextBox.Focus();
                };
                this.FormClosed += (s, e) =>
                {
                    _vm.CheckedTags = _vm.Items.Where(val => val.Checked).Select(val => val.Text).ToList();
                };
                this.SizeChanged += (s, e) =>
                {
                    _listView.Height = this.Height - _listView.Top - 50;
                    _listView.Width = this.Width - 36;
                    _fileNameTextBox.Width = this.Width - 36;
                    _aliasTextBox.Width = this.Width - _aliasTextBox.Left - 26;
                };
                _aliasTextBox.TextChanged += (s, e) =>
                {
                    _vm.Alias = _aliasTextBox.Text;
                };
                _listView.ItemChecked += (s, e) =>
                {
                    if (e.Item.Checked){
                        _vm.CheckedTags.Add(e.Item.Text);
                    }
                    else{
                        _vm.CheckedTags.Remove(e.Item.Text);
                    }
                };
                _commitBtn.Click += (s, e) =>
                {
                    _vm.Commit();
                    this.Close();
                };
            }
        }
    }
}
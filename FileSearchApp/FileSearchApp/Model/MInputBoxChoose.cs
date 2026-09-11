using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using System.Drawing;
using CustomFunctions;

namespace FileSearchApp
{
    public static class MInputBoxChoose
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
        /// <summary>
        /// 表示される配列とその配列でチェック済みで表示する配列を指定して
        /// 選択されたアイテムを取得します
        /// </summary>
        public static IOption<List<string>> GetItems(List<string> pListItems, List<string> pCheckedItems)
        {
            return GetItems(pListItems, pCheckedItems, string.Empty);
        }
        /// <summary>
        /// 表示される配列とその配列でチェック済みで表示する配列、画面のタイトルを指定して
        /// 選択されたアイテムを取得します
        /// </summary>
        public static IOption<List<string>> GetItems(List<string> pListItems, List<string> pCheckedItems, string pTitle)
        {
            return GetItems(pListItems, pCheckedItems, pTitle, int.MinValue);
        }
        /// <summary>
        /// 表示される配列とその配列でチェック済みで表示する配列、画面のタイトル、画面の幅を指定して
        /// 選択されたアイテムを取得します
        /// </summary>
        public static IOption<List<string>> GetItems(List<string> pListItems, List<string> pCheckedItems, string pTitle, int pWidth)
        {
            return GetItemsAsync(pListItems, pCheckedItems, pTitle, pWidth).Result;
        }
        /// <summary>
        /// 表示される配列とその配列でチェック済みで表示する配列、画面のタイトル、画面の幅を指定して
        /// 選択されたアイテムを取得します
        /// </summary>
        public static async Task<IOption<List<string>>> GetItemsAsync(List<string> pListItems, List<string> pCheckedItems, string pTitle, int pWidth)
        {
            var vm = new VMInputBoxChoose(pListItems, pCheckedItems);
            if (pTitle != string.Empty){
                vm.Title = pTitle;
            }
            if (pWidth > vm.MinWidth){
                vm.Width = pWidth;
            }
            VInputBoxChoose view = new VInputBoxChoose(vm);
            var tk = Task.Run(() =>
            {
                //try{view.ShowDialog();}finally{view.Dispose();}
                Application.Run(view);
            });
            await tk.ConfigureAwait(false);
            return vm.Result;
        }
        public class VMInputBoxChoose
        {
            public readonly int MinWidth = 300;
            public readonly int MinHeight = 300;
            List<string> _tags = new List<string>();
            public string Title { get; set; }
            public int Left { get; set; }
            public int Top { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public ColumnHeader[] ListViewColumnHeaders { get; private set; }
            public List<ListViewItem> Items { get; private set; }
            public IOption<List<string>> Result { get; private set; }
            public VMInputBoxChoose(List<string> pTags, List<string> pCheckedTags)
            {
                _tags = pTags;
                this.Title = "選択してください";
                this.Result = new None<List<string>>();
                this.Items = CreateItems();
                foreach (var val in this.Items)
                {
                    val.Checked = pCheckedTags.Contains(val.Text);
                }
                this.ListViewColumnHeaders = CreateListViewColumnHeader(new List<int>());
                SetWindowRectangle();
            }
            ~VMInputBoxChoose()
            {
                foreach (var val in this.Items)
                {
                    if (val != null){
                        if (val.ImageList != null){
                            val.ImageList.Dispose();
                        }
                    }
                }
                foreach (var val in this.ListViewColumnHeaders)
                {
                    if (val != null){
                        if (val.ImageList != null){
                            val.ImageList.Dispose();
                        }
                    }
                }
            }
            public void SetWindowRectangle()
            {
                var rect = MyTool.CheckWindowRectangle(
                    new System.Drawing.Rectangle(
                        Cursor.Position.X - this.MinWidth / 4,
                        Cursor.Position.Y - this.MinHeight / 4,
                        this.MinWidth,
                        this.MinHeight
                    )
                );
                SetWindowRectangle(rect);
            }
            public void SetWindowRectangle(Rectangle pWindowRectangle)
            {
                var rect = MyTool.CheckWindowRectangle(pWindowRectangle);
                this.Left = rect.X;
                this.Top = rect.Y;
                this.Width = rect.Width;
                this.Height = rect.Height;
            }
            List<ListViewItem> CreateItems()
            {
                var items = new List<ListViewItem>();
                foreach (var val in _tags)
                {
                    var item = new ListViewItem(val.ToString());
                    items.Add(item);
                }
                return items;
            }
            ColumnHeader[] CreateListViewColumnHeader(List<int> pColumnWidths)
            {
                var header = new ColumnHeader[]
                {
                    new ColumnHeader{
                        Text = "Item",
                        Width = 50
                    }
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
            public void Commit()
            {
                var result = new Some<List<string>>(this.Items
                    .Where(val => val.Checked)
                    .Select(val => val.Text)
                    .ToList()
                );
                this.Result = result;
            }
        }
        public class VInputBoxChoose : Form
        {
            private ListView _listView = new ListView();
            private Button _runSearchBtn = new Button();
            private VMInputBoxChoose _vm;

            public VInputBoxChoose(VMInputBoxChoose pVM)
            {
                _vm = pVM;
                InitializeComponent();
                InitializeEventSet();
            }
            public void InitializeComponent()
            {
                this.Text = _vm.Title;
                this.Icon = CloneIcon().GetOrDefault(this.Icon);
                this.StartPosition = FormStartPosition.Manual;
                this.Left = _vm.Left;
                this.Top = _vm.Top;
                this.Width = _vm.Width;
                this.Height = _vm.Height;
                
                this.Controls.Add(_runSearchBtn);
                _runSearchBtn.Text = "確定";
                _runSearchBtn.Top = 9;
                _runSearchBtn.Left = 10;
                _runSearchBtn.Height = 21;
                _runSearchBtn.Width = 60;

                this.Controls.Add(_listView);
                _listView.Top = _runSearchBtn.Top + _runSearchBtn.Height + 5;
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
                    _listView.Width = _vm.Width - 36;
                    if (_listView.Columns.Count != 0){
                        _listView.Columns[0].Width = _listView.Width - 22;
                    }
                    this.ActivateForm(1000);
                };
                this.SizeChanged += (s, e) =>
                {
                    _listView.Height = this.Height - _listView.Top - 50;
                    _listView.Width = this.Width - 36;
                };
                _runSearchBtn.KeyPress += (s, e) =>
                {
                    if (e.KeyChar == (char)Keys.Escape){
                        this.Close();
                    }
                };
                _runSearchBtn.Click += (s, e) =>
                {
                    _vm.Commit();
                    this.Close();
                };
                _listView.KeyPress += (s, e) =>
                {
                    if (e.KeyChar == (char) Keys.Escape){
                        this.Close();
                    }
                };
                _listView.ItemChecked += (s, e) =>
                {
                    var vmItems = _vm.Items.Where(val => val.Text == e.Item.Text);
                    if (vmItems.Count() != 1){
                        return;
                    }
                    vmItems.ElementAt(0).Checked = e.Item.Checked;
                };
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using System.IO;

namespace FileSearchApp
{
    public class ListViewMenuFactory : IToolStripMenuItemFactory
    {
        CancellationTaskManager _tasks = new CancellationTaskManager();
        public Action OpenSelectionMethod { get; set; }
        public Func<ExFileInfo> GetFileInfoMethod { get; set; }
        public Action RedrawListViewMethod { get; set; }
        public IOption<MTagManager> TagManager { get; set; }
        public IOption<MAliasManager> AliasManager { get; set; }
        public IOption<MenuRangeTree> UserMenuCreater { get; set; }
        public IOption<VMListView> VMListView { get; set; }
        public IOption<MHistoryManager> HistoryManager { get; set; }
        public ListViewMenuFactory()
        {
            this.OpenSelectionMethod = () => Task.Delay(0);
            this.GetFileInfoMethod = () => ExFileInfo.GetDefault();
            this.RedrawListViewMethod = () => { };
            this.TagManager = new None<MTagManager>();
            this.AliasManager = new None<MAliasManager>();
            this.UserMenuCreater = new None<MenuRangeTree>();
            this.VMListView = new None<VMListView>();
            this.HistoryManager = new None<MHistoryManager>();
        }
        ~ListViewMenuFactory()
        {
            _tasks.CancelAll();
        }
        public List<ToolStripMenuItem> Create()
        {
            var menuItems = new List<ToolStripMenuItem>();
            ToolStripMenuItem item;
            // ユーザー設定のメニュー
            this.UserMenuCreater.Match(
                none => {},
                some => {
                    try
                    {
                        var items = some.CreateMenuItems(this.GetFileInfoMethod());
                        foreach (var val in items) menuItems.Add(val);
                    }
                    catch (Exception ex)
                    {
                        MMessageBox.Show(ex.Message);
                    }
                }
            );
            //別名を追加
            item = new ToolStripMenuItem("別名を追加");
            item.Click += ShowAliasEditer_Click;
            menuItems.Add(item);
            // 名前をコピー
            item = new ToolStripMenuItem("名前をコピー");
            item.Click += CopyName_Click;
            menuItems.Add(item);
            // 履歴から削除
            item = new ToolStripMenuItem("履歴から削除");
            item.Click += RemovePath_Click;
            menuItems.Add(item);
            // フォルダをここで開く
            item = new ToolStripMenuItem("フォルダをここで開く");
            item.Click += ShowDirectory_Click;
            menuItems.Add(item);
            //セパレータ
            menuItems.Add(
                new ToolStripMenuItem("────────────"){
                    Enabled = false
                }
            );
            //タグ
            List<string> allTags = new List<string>();
            List<string> allocTags = new List<string>();
            this.TagManager.IfSome(mn => {
                allTags.AddRange(mn.GetAllTags());
                allocTags.AddRange(mn.GetTags(this.GetFileInfoMethod().FileName));
            });
            foreach (var tag in allTags)
            {
                var menuItem = new ToolStripMenuItem(tag){
                    Name = "Tag",
                    Checked = allocTags.Contains(tag), // 引当あればチェック
                };
                menuItem.Click += EditTagAllocateMenu_Click;
                menuItems.Add(menuItem);
            }
            return menuItems;
        }
        void RemovePath_Click(object s, EventArgs e)
        {
            var info = GetFileInfoMethod();
            this.HistoryManager.IfSome(some =>
            {
                some.Remove(info.FilePath);
            });
        }
        void CopyName_Click(object s, EventArgs e)
        {
            var info = GetFileInfoMethod();
            Clipboard.SetText(info.FileName);
        }
        void ShowDirectory_Click(object s, EventArgs e)
        {
            var info = GetFileInfoMethod();
            var dir = Directory.Exists(info.FilePath) ?
                info.FilePath :
                Path.GetDirectoryName(info.FilePath)
            ;
            var items = new DirectoryInfo(dir);
            var childs = Directory.GetFiles(dir)
                .Where(file =>
                {
                    var fi = new FileInfo(file);
                    var isReparsePoint = fi.Attributes & FileAttributes.ReparsePoint;
                    return isReparsePoint != FileAttributes.ReparsePoint;
                })
                .Union(
                    Directory.GetDirectories(dir)
                        .Where(directory =>
                        {
                            var di = new FileInfo(directory);
                            var isReparsePoint =
                                di.Attributes & FileAttributes.ReparsePoint;
                            return isReparsePoint != FileAttributes.ReparsePoint;
                        })
                )
                .OrderBy(x => x)
            ;
            this.VMListView.IfSome(some =>
            {
                some.Clear();
                foreach (var path in childs)
                {
                    some.Add(new ExFileInfo(path));
                }
                some.RiseDataSourceChange(true);
            });
        }
        void ShowAliasEditer_Click(object s, EventArgs e)
        {
            _tasks.Add(ct =>
            {
                var info = GetFileInfoMethod();
                var input = MInputBox.GetText(
                    "空欄で確定すると別名が削除されます",
                    info.Alias != "" ? info.Alias : info.FileName,
                    false, // 編集不可
                    ct
                );
                input.IfSome(inputStr => {
                    if (inputStr == info.FileName){
                        return;
                    }
                    else if (inputStr == ""){
                        // remove
                        this.AliasManager.IfSome(
                            mn => mn.Remove(info.FileName)
                        );
                    }
                    else{
                        // add
                        this.AliasManager.IfSome(
                            mn => mn.Add(info.FileName, inputStr, true)
                        );
                    }
                    info.Alias = inputStr;
                    this.RedrawListViewMethod();
                });
            });
        }
        void EditTagAllocateMenu_Click(object s, EventArgs e)
        {
            if(s.GetType() != new ToolStripMenuItem().GetType()){
                return;
            }
            var menuItem = (ToolStripMenuItem)s;
            menuItem.Checked = !menuItem.Checked;
            var fileName = GetFileInfoMethod().FileName;
            if (menuItem.Checked){
                // add
                this.TagManager.IfSome(
                    mn => mn.Add(fileName, menuItem.Text)
                );
            }
            else{
                // remove
                this.TagManager.IfSome(
                    mn => mn.Remove(fileName, menuItem.Text)
                );
            }
        }
        public event EventHandler<string> ErrorNotification = delegate{};
    }
}
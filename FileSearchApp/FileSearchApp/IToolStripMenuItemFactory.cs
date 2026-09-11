using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public interface IToolStripMenuItemFactory
    {
        List<ToolStripMenuItem> Create();
    }
    public class DefaultMenuFactory : IToolStripMenuItemFactory
    {
        public List<ToolStripMenuItem> Create()
        {
            return new List<ToolStripMenuItem>();
        }
    }
    public class MultiMenuFactory : IToolStripMenuItemFactory
    {
        public MultiMenuFactory()
        {
            this.MenuCreaters = new List<Func<List<ToolStripMenuItem>>>();
        }
        public List<ToolStripMenuItem> Create()
        {
            var items = new List<ToolStripMenuItem>();
            foreach (var func in MenuCreaters)
            {
                items.AddRange(func());
            }
            return items;
        }
        public List<Func<List<ToolStripMenuItem>>> MenuCreaters { get; private set; }
    }
    public class FilterMenuFactory : IToolStripMenuItemFactory
    {
        public Func<Filter<ExFileInfo>> GetFilterMethod { get; set; }
        public Action<Filter<ExFileInfo>> ApplyListViewFilterMethod { get; set; }
        public Func<List<string>> GetAllTagMethod { get; set; }
        public Func<string, List<string>> GetAllocatedTagsMethod { get; set; }
        public Func<IEnumerable<ToolStripMenuItem>> GetFilterMenuItems { get; set; }
        public FilterMenuFactory()
        {
            this.ApplyListViewFilterMethod = func => Task.Delay(0);
            this.GetAllTagMethod = () => new List<string>();
            this.GetAllocatedTagsMethod = str => new List<string>();
            this.GetFilterMethod = () => new Filter<ExFileInfo>();
            this.GetFilterMenuItems = () => new ToolStripMenuItem[0];
        }
        public List<ToolStripMenuItem> Create()
        {
            var menuItems = new List<ToolStripMenuItem>();
            //すべての選択を解除するメニューを追加
            var unSelectItem = new ToolStripMenuItem("───選択解除───");
            unSelectItem.Click += (s, e) =>
            {
                foreach (var val in menuItems)
                {
                    val.Checked = false;
                }
                this.ApplyListViewFilterMethod(new Filter<ExFileInfo>());
            };
            menuItems.Add(unSelectItem);
            //
            var reapplyItem = new ToolStripMenuItem("───  再適用  ───");
            reapplyItem.Click += (s, e) =>
            {
                var filters = this.GetFilterMethod();
                this.ApplyListViewFilterMethod(filters);
            };
            menuItems.Add(reapplyItem);
            //
            //タグ用のClickEventHandler作成
            var eventHandler = new EventHandler((s, e) => {
                var item = (ToolStripMenuItem)s;
                item.Checked = !item.Checked;
                var filter = this.GetFilterMethod();
                // ここのKeyを変更すると VMListView のタグフィルタを除外するとこも変更する
                var key = "Tag Equal " + item.Text;
                if (item.Checked){
                    filter.Add(key, info => {
                        return GetAllocatedTagsMethod(info.FileName).Contains(item.Text);
                    });
                }
                else{
                    filter.Remove(key);
                }
                this.ApplyListViewFilterMethod(filter);
            });
            //タグ用のメニュー追加
            foreach (var val in GetAllTagMethod())
            {
                var item = new ToolStripMenuItem(){
                    Text = val,
                    Name = "tag",
                    Checked = false,
                };
                item.Click += eventHandler;
                menuItems.Add(item);
            }
            return menuItems;
        }
    }
    public class ListViewMenuFactory : IToolStripMenuItemFactory
    {
        CancellationTaskManager _tasks = new CancellationTaskManager();
        public Action OpenSelectionMethod { get; set; }
        public Func<ExFileInfo> GetFileInfoMethod { get; set; }
        public Action RedrawListViewMethod { get; set; }
        public IOption<MTagManager> TagManager { get; set; }
        public IOption<MAliasManager> AliasManager { get; set; }
        public IOption<MenuRangeTree> UserMenuCreater { get; set; }
        public ListViewMenuFactory()
        {
            this.OpenSelectionMethod = () => Task.Delay(0);
            this.GetFileInfoMethod = () => ExFileInfo.GetDefault();
            this.RedrawListViewMethod = () => {};
            this.TagManager = new None<MTagManager>();
            this.AliasManager = new None<MAliasManager>();
            this.UserMenuCreater = new None<MenuRangeTree>();
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
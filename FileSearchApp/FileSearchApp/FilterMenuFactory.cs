using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FileSearchApp
{
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
}
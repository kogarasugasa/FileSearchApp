using System.Collections.Generic;
using System.Windows.Forms;

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
}
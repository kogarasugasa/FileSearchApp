using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using CustomFunctions;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public class VMMainWindow
    {
        public enum ColumnHeaders { Num, FileName, Extension, Size, Path, Status, Alias }
        public WindowRectangle WindowProperty { get; set; }
        public string[] SearchCandidate
        {
            get
            {
                return _methodSetterGetCandidate.GetOrDefault(() => new string[0])();
            }
        }
        IOption<Func<string[]>> _methodSetterGetCandidate = new None<Func<string[]>>();
        public Func<string[]> MethodSetterGetCandidate { set { _methodSetterGetCandidate = Some.New(value); } }
        public ColumnHeader[] ListViewColumnHeaders { get; private set; }
        public VMMainWindow(
        IEnumerable<int> pColumnWidths,
        IEnumerable<string> pLeft_Top_Width_Height_WindowState)
        {
            var wRect = new WindowRectangle(pLeft_Top_Width_Height_WindowState);
            var rect = MyTool.CheckWindowRectangle(wRect.ToRectangle(), 520, 300);
            this.WindowProperty = new WindowRectangle(rect, wRect.WindowState);
            this.ListViewColumnHeaders = this.CreateListViewColumnHeader(pColumnWidths);
        }
        ColumnHeader[] CreateListViewColumnHeader(IEnumerable<int> pColumnWidths)
        {
            var header = new ColumnHeader[]{
                new ColumnHeader{Text = "", Width = 0},
                new ColumnHeader{Text = "Num", Width = 50, TextAlign = HorizontalAlignment.Right},
                new ColumnHeader{Text = "FileName", Width = 100},
                new ColumnHeader{Text = "Extension", Width = 70},
                new ColumnHeader{Text = "Size", Width = 60, TextAlign = HorizontalAlignment.Right},
                new ColumnHeader{Text = "Path", Width = 100},
                new ColumnHeader{Text = "Status", Width = 50},
            };
            if (pColumnWidths.Count() == 0){
                return header;
            }
            int num = pColumnWidths.Count();
            num = Math.Min(header.Count(), num);
            for (int i = 0; i < num; i++)
            {
                header[i].Width = pColumnWidths.ElementAt(i);
            }
            return header;
        }
    }
}
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public class WindowRectangle
    {
        public FormWindowState WindowState;
        public int Left, Top, Width, Height;
        public WindowRectangle(Form pForm)
        {
            this.WindowState = pForm.WindowState;
            this.Left = pForm.Left;
            this.Top = pForm.Top;
            this.Width = pForm.Width;
            this.Height = pForm.Height;
        }
        public WindowRectangle(Rectangle rectangle)
        {
            WindowState = FormWindowState.Normal;
            Left = rectangle.Left;
            Top = rectangle.Top;
            Width = rectangle.Width;
            Height = rectangle.Height;
        }
        public WindowRectangle(Rectangle pRect, FormWindowState pWindowState)
        {
            WindowState = pWindowState;
            Left = pRect.Left;
            Top = pRect.Top;
            Width = pRect.Width;
            Height = pRect.Height;
        }
        public WindowRectangle(IEnumerable<string> Left_Top_Width_Height_WindowState)
        {
            WindowState = FormWindowState.Normal;
            var list = new List<string>(){"", "", "", "", ""};
            for (int i = 0; i < Left_Top_Width_Height_WindowState.Count(); i++)
            {
                list[i] = Left_Top_Width_Height_WindowState.ElementAt(i);
            }
            int.TryParse(list[0], out this.Left);
            int.TryParse(list[1], out this.Top);
            int.TryParse(list[2], out this.Width);
            int.TryParse(list[3], out this.Height);
            switch (list[4])
            {
                case "Maximized":
                    this.WindowState = FormWindowState.Maximized;
                    break;
                case "Minimized":
                    this.WindowState = FormWindowState.Minimized;
                    break;
                default:
                    this.WindowState = FormWindowState.Normal;
                    break;
            }
        }
        public Rectangle ToRectangle()
        {
            return new Rectangle(Left, Top, Width, Height);
        }
        public List<string> ToList()
        {
            var res = new List<string>{
                this.Left.ToString(),
                this.Top.ToString(),
                this.Width.ToString(),
                this.Height.ToString(),
                this.WindowState.ToString()
            };
            return res;
        }
        public override string ToString()
        {
            return string.Join(", ", this.ToList());
        }
    }
}
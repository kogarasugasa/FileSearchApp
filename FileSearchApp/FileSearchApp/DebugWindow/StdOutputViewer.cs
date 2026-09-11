using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using CustomFunctions;

namespace FileSearchApp
{
    public class StdOutputViewer : Form
    {
        Label _label = new Label();
        System.Timers.Timer _updateTimer = new System.Timers.Timer();
        public StdOutputViewer()
        {
            _updateTimer.Interval = 1000;
            _updateTimer.Elapsed += TextDraw;

            var rect = MyTool.CheckWindowRectangle(
                new Rectangle(Cursor.Position, new Size(1000, 400)),
                0,
                0,
                true
            );
            this.Text = "debug";
            this.Icon = MyIcon.CloneIcon().GetOrDefault(this.Icon);
            this.StartPosition = FormStartPosition.Manual;
            this.Left = rect.Left;
            this.Top = rect.Top;
            this.Width = rect.Width;
            this.Height = rect.Height;
            //
            this.Controls.Add(_label);
            _label.Left = 0;
            _label.Top = 0;
            _label.Width = this.Width - 10;
            _label.Height = this.Height - 10;
            _label.AutoSize = true;

            this.Shown += (s, e) =>
            {
                _updateTimer.Start();
                TextDraw(s, e);
            };
            this.SizeChanged += (s, e) =>
            {
                _label.Width = this.Width - 10;
                _label.Height = this.Height - 10;
                TextDraw(s, e);
            };
            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible){
                    _updateTimer.Start();
                }
                else{
                    _updateTimer.Stop();
                }
            };
            this.DoubleClick += (s, e) =>
            {
                CustomLogger.Clear();
                TextMatch.Cancel();
            };
        }
        ~StdOutputViewer()
        {
            _updateTimer.Dispose();
        }
        public void TextDraw(object s, EventArgs e)
        {
            int fontPixelSize = 12;
            int titlePixelSize = 30;
            int otherPixel = 7;
            int rows = (this.Height - titlePixelSize - otherPixel) / fontPixelSize;
            var msgs = CustomLogger.GetMessages();
            _label.Text = string.Join("\r\n", msgs.Skip(msgs.Count - rows));
        }
    }
}
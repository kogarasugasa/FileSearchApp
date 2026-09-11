using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using CustomFunctions;

namespace FileSearchApp
{
    public static class MMessageBox
    {
        static readonly object _lock = new object();
        static IOption<Icon> _icon = new None<Icon>();
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
        public static void Show(string pMsg)
        {
            ShowAsync(pMsg).Wait();
        }
        public static Task ShowAsync(string pMsg)
        {
            var tk = ShowAsync(pMsg, new CancellationToken());
            return tk;
        }
        public static async Task ShowAsync(string pMsg, CancellationToken ct)
        {
            var task = Task.Run(() => Application.Run(new VMsg(pMsg, ct)));
            await task.ConfigureAwait(false);
        }
        public static async Task ShowAsync(string pMsg, int pLeft, int pTop, CancellationToken ct)
        {
            var fm = new VMsg(pMsg, ct);
            fm.StartPosition = FormStartPosition.Manual;
            fm.Left = pLeft;
            fm.Top = pTop;
            var task = Task.Run(() => Application.Run(fm));
            await task.ConfigureAwait(false);
        }
        class VMsg : Form
        {
            const int MIN_WIDTH = 220;
            const int MIN_HEIGHT = 100;
            PictureBox _pict = new PictureBox();
            Button _okBtn = new Button();
            string _msg = string.Empty;
            Graphics _stringGraphics;
            SizeF _pictSize;
            System.Timers.Timer _timer = new System.Timers.Timer();
            CancellationToken _ct;
            public VMsg(string pMsg, CancellationToken ct)
            {
                _msg = pMsg;
                _ct = ct;
                _stringGraphics = _pict.CreateGraphics();
                _pictSize = PaintGraphics(_msg, _stringGraphics);
                _pictSize.Width += 10;
                _pictSize.Height += 10;

                this.Text = "Message";
                this.FormBorderStyle = FormBorderStyle.FixedSingle;

                this.InitializeComponent();
                this.InitializeEventSet();

                _timer.Elapsed += (s, e) =>
                {
                    if (_ct.IsCancellationRequested){
                        if (this.IsDisposed){
                            return;
                        }
                        this.Close();
                    }
                };
                _timer.Interval = 100;
                _timer.Start();
            }
            ~VMsg()
            {
                this.Dispose(false);
            }
            protected override void Dispose(bool disposing)
            {
                if (this.IsDisposed){
                    return;
                }
                if (disposing){
                    _stringGraphics.Dispose();
                }
                _timer.Stop();
                base.Dispose(disposing);
            }
            void InitializeComponent()
            {
                this.Icon = CloneIcon().GetOrDefault(this.Icon);
                this.Controls.Add(_pict);
                this.Controls.Add(_okBtn);
                _okBtn.Text = "OK";
                _okBtn.Width = 70;
                _okBtn.Height = 25;
            }
            void InitializeEventSet()
            {
                this.Load += (s, e) =>
                {
                    var rect = this.GetWindowRectangle(
                        this.Location,
                        _pictSize.ToSize(),
                        this.StartPosition
                    );
                    this.Location = rect.Location;
                    this.Size = rect.Size;

                    _pict.Width = (int)_pictSize.Width;
                    _pict.Height = (int)_pictSize.Height;
                    _pict.Left = this.Width / 2 - _pict.Width / 2;
                    _pict.Top = this.Height / 2 - _pict.Height / 2 - 30;
                    _okBtn.Top = this.Height / 2 - _okBtn.Height / 2 + 10;
                    _okBtn.Left = this.Width / 2 - _okBtn.Width / 2 - 3;
                };
                this.Shown += (s, e) =>
                {
                    this.ActivateForm(1000);
                    _okBtn.Focus();
                };
                _okBtn.Click += (s, e) =>
                {
                    this.Close();
                };
                _pict.Paint += this.PictureBox_Paint;
            }
            Rectangle GetWindowRectangle(Point pLocation, Size pSize, FormStartPosition pStartPos)
            {
                
                var size = pSize;
                if (size.Width < MIN_WIDTH){
                    size.Width = MIN_WIDTH + 30;
                }
                else{
                    size.Width += 30;
                }
                if (size.Height < MIN_HEIGHT){
                    size.Height = MIN_HEIGHT + 30;
                }
                else{
                    size.Height += 30;
                }
                Rectangle rect;
                if (pStartPos == FormStartPosition.Manual){
                    rect = new Rectangle(
                        pLocation.X,
                        pLocation.Y,
                        size.Width,
                        size.Height
                    );
                }
                else{
                    rect = new Rectangle(
                        Cursor.Position.X - 100,
                        Cursor.Position.Y - 70,
                        size.Width,
                        size.Height
                    );
                }
                return CustomFunctions.MyTool.CheckWindowRectangle(rect);
            }
            Size PaintGraphics(string pMsg, Graphics g)
            {
                var font = new Font("Meiryo UI", 8);
                var brush = new SolidBrush(Color.Black);
                var format = new StringFormat();
                format.FormatFlags = StringFormatFlags.NoWrap;
                g.DrawString(pMsg, font, brush, 0, 0, format);
                return g.MeasureString(_msg, font).ToSize();
            }
            void PictureBox_Paint(object s, PaintEventArgs e)
            {
                this.PaintGraphics(_msg, e.Graphics);
            }
        }
    }
}
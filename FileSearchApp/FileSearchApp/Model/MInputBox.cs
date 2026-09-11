using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using CustomFunctions;
using System.Drawing;

namespace FileSearchApp
{
    public static class MInputBox
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
        /// <summary>
        /// タイトルを設定してテキストの入力を求めます
        /// </summary>
        public static IOption<string> GetText(string pTitle)
        {
            return GetText(pTitle, string.Empty);
        }
        /// <summary>
        /// タイトルと入力欄に初期表示される文字を設定してテキストの入力を求めます
        /// </summary>
        public static IOption<string> GetText(string pTitle, string pInitialText)
        {
            return GetText(pTitle, pInitialText, false, new CancellationToken());
        }
        /// <summary>
        /// タイトルと入力欄に初期表示される文字を設定してテキストの入力を求めます
        /// ReadOnly を指定した場合入力内容は変更出来ません
        /// </summary>
        public static IOption<string> GetText(string pTitle, string pInitialText, bool pReadOnly)
        {
            return GetText(pTitle, pInitialText, pReadOnly, new CancellationToken());
        }
        /// <summary>
        /// タイトルと入力欄に初期表示される文字を設定してテキストの入力を求めます
        /// ReadOnly を指定した場合入力内容は変更出来ません
        /// プログラム側からキャンセル可能です
        /// </summary>
        public static IOption<string> GetText(string pTitle, string pInitialText, bool pReadOnly, CancellationToken ct)
        {
            var vm = new VMInputBox(){
                Title = pTitle,
                Text = pInitialText,
                IsReadOnly = pReadOnly,
            };
            var v = new VInputBox(vm, ct);
            var inputTask = Task.Run(() => Application.Run(v));
            inputTask.WaitByPollingLoop(100);
            return vm.Result;
        }
        class VInputBox : Form
        {
            const int MIN_HEIGHT = 78;
            const int MIN_WIDTH = 350;
            System.Drawing.Color _defaultBackColor;
            IOption<Task> _changeBackColorTask = new None<Task>();
            Button _okBtn = new Button();
            TextBox _textBox = new TextBox();
            VMInputBox _vm;
            CancellationToken _ct;
            System.Timers.Timer _cancelTimer = new System.Timers.Timer(100);
            public VInputBox(VMInputBox pVMInputBox, CancellationToken ct)
            {
                _ct = ct;
                _vm = pVMInputBox;
                //
                _cancelTimer.Elapsed += (s, e) =>{
                    if (_ct.IsCancellationRequested){
                        if (this.IsDisposed){
                            return;
                        }
                        this.Close();
                    }
                };
                _cancelTimer.Start();
                //
                this.InitializeComponent();
                this.InitializeEventSet();
            }
            ~VInputBox()
            {
                _cancelTimer.Stop();
                _cancelTimer.Dispose();
            }
            void InitializeComponent()
            {
                var widthList = new List<int>(){
                    _vm.Text.Length * 10 + 40,
                    _vm.Title.Length * 10 + 200,
                    _vm.Width,
                };
                var rect = new System.Drawing.Rectangle(
                    Cursor.Position.X - 150,
                    Cursor.Position.Y,
                    widthList.Max(),
                    MIN_HEIGHT
                );
                rect = MyTool.CheckWindowRectangle(rect, MIN_WIDTH, MIN_HEIGHT);
                this.Text = _vm.Title;
                this.Icon = CloneIcon().GetOrDefault(this.Icon);
                this.StartPosition = FormStartPosition.Manual;
                this.Left = rect.Left;
                this.Top = rect.Top;
                this.Width = rect.Width;
                this.Height = rect.Height;
                this.MaximizeBox = false;
                //
                this.Controls.Add(_okBtn);
                _okBtn.Text = "OK";
                _okBtn.Left = 10;
                _okBtn.Top = 9;
                _okBtn.Width = 30;
                _okBtn.Height = 21;
                this.Controls.Add(_textBox);
                _textBox.Text = _vm.Text;
                _textBox.Left = 41;
                _textBox.Top = 10;
                _textBox.Width = this.Width - 66;
                _defaultBackColor = _textBox.BackColor;
            }
            void InitializeEventSet()
            {
                _vm.AlreadyCommited += (s, e) => MMessageBox.Show(e);
                this.Shown += (s, e) =>
                {
                    this.ActivateForm(1000);
                    if (_vm.IsReadOnly){
                        return;
                    }
                    this.InvokeIfRequiredElseNonInvoke(() => {
                        _textBox.Focus();
                    });
                };
                this.SizeChanged += (s, e) =>
                {
                    _textBox.Width = this.Width - 66;
                };
                _textBox.KeyDown += (s, e) =>
                {
                    if (_vm.IsReadOnly && e.KeyCode == Keys.Delete){
                        e.Handled = true;
                        if (_changeBackColorTask.IsSome()){
                            return;
                        }
                        _changeBackColorTask = new Some<Task>(this.ChangeBackColorAsync());
                        return;
                    }
                };
                _textBox.KeyPress += (s, e) =>
                {
                    switch (e.KeyChar)
                    {
                        case (char)Keys.Escape:
                            this.Close();
                            break;
                        case (char)Keys.Enter:
                            _vm.Commit();
                            this.Close();
                            break;
                    }
                    if (_vm.IsReadOnly){
                        e.Handled = true;
                        if (_changeBackColorTask.IsSome()){
                            return;
                        }
                        _changeBackColorTask = new Some<Task>(this.ChangeBackColorAsync());
                        return;
                    }
                };
                _okBtn.Click += (s, e) =>
                {
                    _vm.Commit();
                    this.Close();
                };
                _textBox.TextChanged += (s, e) => _vm.Text = _textBox.Text;
            }
            /// <summary>
            /// 入力欄の背景色をピンクにして、一定時間後元に戻す
            /// </summary>
            Task ChangeBackColorAsync()
            {
                _textBox.BackColor = System.Drawing.Color.LightPink;
                var tk = Task.Run(async () =>{
                    await Task.Delay(1000);
                    this.InvokeIfRequiredElseNonInvoke(() =>{
                        _textBox.BackColor = _defaultBackColor;
                        _changeBackColorTask = new None<Task>();
                    });
                });
                return tk;
            }
        }
        class VMInputBox
        {
            public string Text { get; set; }
            public string Title { get; set; }
            public int Width { get; set; }
            public IOption<string> Result { get; set; }
            public bool IsReadOnly { get; set; }
            public VMInputBox()
            {
                this.Title = "Input Text";
                this.Text = string.Empty;
                this.Result = new None<string>();
            }
            public void Commit()
            {
                if (this.Result.IsNone()){
                    this.Result = new Some<string>(this.Text);
                }
            }
            public event EventHandler<string> AlreadyCommited = delegate{};
        }
    }
}
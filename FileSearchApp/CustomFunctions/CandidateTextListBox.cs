using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using CustomFunctions;

namespace CustomFunctions
{
    public class CandidateTextListBox
    {
        static readonly object _tag = new object();
        static readonly ConcurrentDictionary<TextBox, IOption<CandidateTextListBox>> _allocated =
            new ConcurrentDictionary<TextBox, IOption<CandidateTextListBox>>();
        static readonly Type _labelType = new Label().GetType();
        readonly List<object> _items = new List<object>();
        bool _downAfterMoved = false;
        int _selectedIndex;
        bool _rowMove = false;
        Color _selectColor = Color.LightBlue;
        Color _unSelectColor = Color.White;
        readonly List<Label> _labels = new List<Label>();
        readonly Form _form;
        readonly TextBox _textBox;
        int _offset = 0;
        Point _mouseDownPos = new Point();
        Point _beforeMouseMovePos = new Point();
        Label _scrlArea = new Label();
        int _mouseDownOffset = 0;
        bool _disposed = false;
        public bool IsDisposed { get { return _disposed; } }
        public EventArgs EventArgsWhenEnterDowned { get; set; }
        public int RowHeight = 16;
        public int Left;
        public int Top;
        public int Width;
        public int Height;
        CandidateTextListBox(Form form, TextBox textbox, IEnumerable<string> items)
        {
            this.EventArgsWhenEnterDowned = new EventArgs();
            try
            {
                if (items.Any()){
                    _items.AddRange(items);
                }
                else{
                    _items.Add("/* 入力候補が設定されていません。オプションから設定できます */");
                }
                _selectedIndex = -1;
                _form = form;
                _textBox = textbox;
                this.ChangeEvent(true);
                this.Left = _textBox.Left;
                this.Top = _textBox.Bottom;
                this.Width = _textBox.Width;
                this.Height = Math.Min(
                    _form.Height - _textBox.Bottom - this.RowHeight * 5,
                    _items.Count * this.RowHeight
                );
                for (int i = 0; i < _items.Count; i++)
                {
                    var lbl = new Label();
                    lbl.Tag = _tag;
                    lbl.Text = _items[i].ToString();
                    lbl.Left = this.Left;
                    lbl.Top = this.Top + this.RowHeight * i;
                    lbl.Width = lbl.PreferredWidth;
                    lbl.Height = this.RowHeight;
                    lbl.TextAlign = ContentAlignment.MiddleLeft;
                    lbl.BackColor = Color.White;
                    lbl.Paint += (s, e) => {
                        var sender = (Label)s;
                        var rect = sender.Bounds;
                        rect = new Rectangle(0, 0, rect.Width, rect.Height);
                        var bottomLine = new Point[]{
                            new Point(0, rect.Height - 1),
                            new Point(rect.Width - 1, rect.Height - 1)
                        };
                        var leftLine = new Point[]{
                            new Point(0, 0),
                            new Point(0, rect.Height - 1)
                        };
                        var rightLine = new Point[]{
                            new Point(rect.Width - 1, 0),
                            new Point(rect.Width - 1, rect.Height - 1)
                        };
                        e.Graphics.DrawLine(Pens.Black, bottomLine[0], bottomLine[1]); 
                        e.Graphics.DrawLine(Pens.Black, leftLine[0], leftLine[1]); 
                        e.Graphics.DrawLine(Pens.Black, rightLine[0], rightLine[1]); 
                    };
                    lbl.MouseDown += (s, e) => {
                        _downAfterMoved = false;
                        _mouseDownPos = Cursor.Position;
                        _mouseDownOffset = _offset;
                        lbl.MouseMove += Row_Mouse_Move;
                    };
                    lbl.MouseUp += (s, e) => {
                        lbl.MouseMove -= Row_Mouse_Move;
                    };
                    lbl.Click += (s, e) => {
                        if (_downAfterMoved){
                            return;
                        }
                        if (s.GetType() == _labelType){
                            var clickLabel = (Label)s;
                            _textBox.Text = clickLabel.Text;
                            _textBox.Focus();
                            this.Dispose();
                        }
                    };
                    lbl.MouseWheel += this.MouseWheel;
                    lbl.MouseEnter += (s, e) => {
                        if (s.GetType() == _labelType){
                            var leaveLabel = (Label)s;
                            foreach (var val in _labels) val.BackColor = _unSelectColor;
                            leaveLabel.BackColor = _selectColor;
                            _selectedIndex = _labels.FindIndex(
                                val => val.GetHashCode() == s.GetHashCode()
                            );
                        }
                    };
                    _labels.Add(lbl);
                }
                // 幅を揃えて Form に追加する
                int maxWidth = _labels.Max(val => val.Width) + 10;
                foreach (var label in _labels) label.Width = maxWidth;
                _scrlArea.Left = this.Left + maxWidth;
                _scrlArea.Top = this.Top;
                _scrlArea.Width = 35;
                _scrlArea.Height = this.RowHeight * this.GetDisplayLimit();
                _scrlArea.TextAlign = ContentAlignment.BottomCenter;
                _scrlArea.Paint += this.ScrlArea_Paint;
                _scrlArea.MouseDown += (s, e) =>{
                    _mouseDownOffset = _offset;
                    _mouseDownPos = Cursor.Position;
                    _scrlArea.MouseMove += this.ScrlArea_Mouse_Move;
                };
                _scrlArea.MouseUp += (s, e) =>{
                    _scrlArea.MouseMove -= this.ScrlArea_Mouse_Move;
                };
                _scrlArea.MouseWheel += this.MouseWheel;
                this.DisplayUpdate();
            }
            catch (Exception ex)
            {
                if (_form != null){
                    for (int i = 0; i < _form.Controls.Count; i++)
                    {
                        var ctrl = _form.Controls[i];
                        if (ctrl == _tag){
                            _form.Controls.Remove(ctrl);
                            ctrl.Dispose();
                        }
                    }
                }
                throw ex;
            }
        }
        ~CandidateTextListBox()
        {
            this.Dispose();
        }
        public static void Show(Form form, TextBox textbox, IEnumerable<string> items)
        {
            if (_allocated.TryAdd(textbox, new None<CandidateTextListBox>())){
                var obj = new CandidateTextListBox(form, textbox, items);
                _allocated.AddOrUpdate(
                    textbox,
                    Some.New(obj),
                    (key, old) => {
                        old.IfSome(some => some.Dispose());
                        return Some.New(obj);
                    }
                );
            }
        }
        public static void Close(TextBox textbox)
        {
            IOption<CandidateTextListBox> option;
            if(_allocated.TryRemove(textbox, out option)){
                option.IfSome(some => some.Dispose());
            }
        }
        public static bool Contains(TextBox textBox)
        {
            return _allocated.ContainsKey(textBox);
        }
        public void Dispose()
        {
            this.Dispose(true);
        }
        void Dispose(bool disposing)
        {
            if (_disposed){
                return;
            }
            if (disposing) {
                this.ChangeEvent(false);
                foreach (var lbl in _labels)
                {
                    if (_form.Controls.Contains(lbl)){
                        _form.Controls.Remove(lbl);
                    }
                    lbl.Dispose();
                }
                if (_form.Controls.Contains(_scrlArea)){
                    _form.Controls.Remove(_scrlArea);
                }
                _scrlArea.Dispose();
                IOption<CandidateTextListBox> obj;
                _allocated.TryRemove(_textBox, out obj);
            }
            _disposed = true;
        }
        void ChangeEvent(bool pAdd)
        {
            if (pAdd){
                _form.Click += this.Form_Click;
                _textBox.LostFocus += this.TextBox_LostFocus;
                _textBox.KeyDown += this.TextBox_KeyDown;
                _textBox.KeyPress += this.TextBox_KeyPress;
            }
            else{
                _form.Click -= this.Form_Click;
                _textBox.LostFocus -= this.TextBox_LostFocus;
                _textBox.KeyDown -= this.TextBox_KeyDown;
                _textBox.KeyPress -= this.TextBox_KeyPress;
                // KeyDownと分けている理由は KeyPress は Enter と Esc が処理されないから
            }
        }
        void Form_Click(object s, EventArgs e)
        {
            this.Dispose();
        }
        void TextBox_LostFocus(object s, EventArgs e)
        {
            this.Dispose();
        }
        void TextBox_KeyPress(object s, KeyPressEventArgs e)
        {
            this.Dispose();
        }
        void TextBox_KeyDown(object s, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (_selectedIndex >= 0 && _selectedIndex < _items.Count){
                        this.EventArgsWhenEnterDowned = e;
                        _textBox.Text = _items[_selectedIndex].ToString();
                        _textBox.Focus();
                        this.Dispose();
                        e.Handled = true;
                    }
                    break;
                case Keys.Down:
                    if (_selectedIndex < _items.Count - 1){
                        if (_selectedIndex < _offset
                        || _selectedIndex > _offset + this.GetDisplayLimit() - 1){
                            _selectedIndex = _offset;
                        }
                        else{
                            _selectedIndex++;
                        }
                        if (_selectedIndex > _labels.Count - 1){
                            _selectedIndex = _labels.Count -1;
                        }
                        if (_selectedIndex > _offset + this.GetDisplayLimit() - 1){
                            _offset = _selectedIndex - (this.GetDisplayLimit() - 1);
                            _rowMove = true;
                            this.DisplayUpdate();
                        }
                        foreach (var label in _labels) label.BackColor = _unSelectColor;
                        _labels[_selectedIndex].BackColor = _selectColor;
                        e.Handled = true;
                    }
                    break;
                case Keys.Up:
                    if (_selectedIndex > 0){
                        if (_selectedIndex < _offset
                        || _selectedIndex > _offset + this.GetDisplayLimit() - 1){
                            _selectedIndex = _offset + this.GetDisplayLimit() - 1;
                        }
                        else{
                            _selectedIndex--;
                        }
                        if (_selectedIndex < 0){
                            _selectedIndex = 0;
                        }
                        if (_selectedIndex < _offset){
                            _offset = _selectedIndex;
                            this.DisplayUpdate();
                        }
                        foreach (var label in _labels) label.BackColor = _unSelectColor;
                        _labels[_selectedIndex].BackColor = _selectColor;
                        e.Handled = true;
                    }
                    break;
            }
        }
        void This_Dispose(object s, EventArgs e)
        {
            this.Dispose();
        }
        void Row_Mouse_Move(object s, MouseEventArgs e)
        {
            // 候補の表示を変更する
            if (_beforeMouseMovePos == Cursor.Position){
                return;
            }
            var beforeOffset = _offset;
            _beforeMouseMovePos = Cursor.Position;
            _rowMove = true;
            var rowMove = (_mouseDownPos.Y - Cursor.Position.Y) / RowHeight;
            _offset = _mouseDownOffset + rowMove;
            if (beforeOffset != _offset){
                _downAfterMoved = true;
            }
            this.DisplayUpdate();
        }
        void MouseWheel(object s, MouseEventArgs e)
        {
            _offset -= e.Delta / this.RowHeight / 2;
            _rowMove = true;
            this.DisplayUpdate();
        }
        void ScrlArea_Mouse_Move(object s, MouseEventArgs e)
        {
            // 候補の表示を変更する
            if (_beforeMouseMovePos == Cursor.Position){
                return;
            }
            var beforeOffset = _offset;
            _beforeMouseMovePos = Cursor.Position;
            _rowMove = false;
            var rect = _form.RectangleToScreen(_scrlArea.Bounds);
            var range = new Range(rect.Top, rect.Bottom);
            var per = range.GetPercentile(Cursor.Position.Y + this.RowHeight / 2);
            if (per < 0){
                // 上行き過ぎ
                _offset = 0;
            }
            else if (per > 1){
                // 下行き過ぎ
                _offset = _labels.Count - 1;
            }
            else{
                // 範囲内
                _offset = (int)Math.Round((_labels.Count - this.GetDisplayLimit()) * per, 0);
            }
            if (beforeOffset != _offset){
                _downAfterMoved = true;
            }
            this.DisplayUpdate();
        }
        void ScrlArea_Paint(object s, PaintEventArgs e)
        {
            if (s.GetHashCode() != _scrlArea.GetHashCode()){
                throw new Exception(
                    "ScrlArea_Paint　引数「object s」が「Label _scrlArea」ではありません"
                );
            }
            Rectangle cur;
            if (_rowMove){
                var r = new Range(0, _labels.Count - this.GetDisplayLimit());
                var pos = _scrlArea.Height * r.GetPercentile(_offset);
                cur = new Rectangle(0, (int)pos, 1, 1);
                cur.Height += this.RowHeight / 2;
            }
            else{
                cur = _scrlArea.RectangleToClient(
                    new Rectangle(Cursor.Position, new Size(1, 1))
                );
            }
            var range = new Range(2, _scrlArea.Height - 3);
            var per = range.GetPercentile(cur.Y);
            if (per < 0){
                var rect = new Rectangle(0, 0, _scrlArea.Width, 4);
                e.Graphics.FillRectangle(Brushes.Black, rect);
            }
            else if(per > 1){
                var rect = new Rectangle(0, _scrlArea.Height - 5, _scrlArea.Width, 4);
                e.Graphics.FillRectangle(Brushes.Black, rect);
            }
            else {
                var rect = new Rectangle(0, cur.Y - 2, _scrlArea.Width, 4);
                e.Graphics.FillRectangle(Brushes.Black, rect);
            }
            var rightLine = new Point[]{
                new Point(_scrlArea.Width -1, 0),
                new Point(_scrlArea.Width -1, _scrlArea.Height - 1),
            };
            var bottomLine = new Point[]{
                new Point(0, _scrlArea.Height - 1),
                new Point(_scrlArea.Width -1, _scrlArea.Height - 1),
            };
            e.Graphics.DrawLine(Pens.Black, rightLine[0], rightLine[1]);
            e.Graphics.DrawLine(Pens.Black, bottomLine[0], bottomLine[1]);
        }
        void DisplayUpdate()
        {
            this.SuspendLayout();
            var limit = this.GetDisplayLimit();
            if (_offset < 0 || limit >= _labels.Count){
                _offset = 0;
            }
            else if (_offset + limit >= _labels.Count){
                _offset = _labels.Count - limit;
            }
            // リスト描画設定
            for (int i = 0; i < _labels.Count; i++)
            {
                if (i < _offset || i > _offset + limit - 1){
                    if (_form.Controls.Contains(_labels[i])){
                        _form.Controls.Remove(_labels[i]);
                        continue;
                    }
                    continue;
                }
                _labels[i].Top = this.Top + (i - _offset) * this.RowHeight;
                if (!_form.Controls.Contains(_labels[i])){
                    _form.Controls.Add(_labels[i]);
                    _labels[i].BringToFront();
                }
                _labels[i].Invalidate();
            }
            if (limit < _labels.Count){
                if (!_form.Controls.Contains(_scrlArea)){
                    _form.Controls.Add(_scrlArea);
                    _scrlArea.BringToFront();
                }
                _scrlArea.Height = this.RowHeight * limit;
            }
            else{
                if (_form.Controls.Contains(_scrlArea)){
                    _form.Controls.Remove(_scrlArea);
                }
            }
            _scrlArea.Invalidate();
            this.ResumeLayout();
        }
        void SuspendLayout()
        {
            _form.SuspendLayout();
            _scrlArea.SuspendLayout();
            foreach (var item in _labels) item.SuspendLayout();
        }
        void ResumeLayout()
        {
            _form.ResumeLayout();
            _scrlArea.ResumeLayout();
            foreach (var item in _labels) item.ResumeLayout();
        }
        int GetDisplayLimit()
        {
            return (_form.Height - this.Top - this.RowHeight * 4) / this.RowHeight;
        }
    }
}
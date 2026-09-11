using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomFunctions;

namespace FileSearchApp
{
    public class FormAssemblerListBox
    {
        public static ListBoxForm CreateForm(
            IEnumerable<string> pList,
            string pButtonActionName,
            Action<ListBoxForm> pButtonAction)
        {
            return new ListBoxForm(pList, pButtonActionName, pButtonAction);
        }
        public class ListBoxForm : Form
        {
            Button _delBtn = new Button();
            ListBox _listBox = new ListBox();
            public Button Button { get { return _delBtn; } }
            public ListBox ListBox { get { return _listBox; } }
            public ListBoxForm(
                IEnumerable<string> pList,
                string pButtonActionName,
                Action<ListBoxForm> pButtonAction)
            {
                this.Width = 500;
                this.Height = 300;
                this.KeyPreview = true;
                this.Controls.Add(_delBtn);
                _delBtn.Text = pButtonActionName;
                _delBtn.Left = 10;
                _delBtn.Top = 5;
                _delBtn.Width = 60;
                _delBtn.Height = 21;
                _delBtn.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                this.Controls.Add(_listBox);
                _listBox.Left = 10;
                _listBox.Top = _delBtn.Top + _delBtn.Height + 5;
                _listBox.Width = this.Width - _listBox.Left - 26;
                _listBox.Height = this.Height - _listBox.Top - 42;
                _listBox.SelectionMode = SelectionMode.One;
                //
                _delBtn.Click += (s, e) => {
                    pButtonAction(this);
                };
                this.Load += (s, e) => {
                    foreach (var val in pList) _listBox.Items.Add(val);
                };
                this.Shown += (s, e) =>
                {
                    this.ActivateForm(1000);
                };
                this.SizeChanged += (s, e) => {
                    _listBox.Width = this.Width - _listBox.Left - 26;
                    _listBox.Height = this.Height - _listBox.Top - 42;
                };
                this.KeyPress += (s, e) =>
                {
                    if (e.KeyChar == (char)Keys.Escape){
                        this.Close();
                    }
                };
            }
        }
    }
}
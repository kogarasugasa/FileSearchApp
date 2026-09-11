using System;
using CustomFunctions;
using System.Windows.Forms;

namespace FileSearchApp
{
    public class TextBoxCtrlKeySavek : TextBox
    {
        bool _optionKey = false;
        protected override void OnGotFocus(EventArgs e)
        {
            _optionKey = false;
            base.OnGotFocus(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey){
                _optionKey = true;
            }
            base.OnKeyDown(e);
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey){
                _optionKey = false;
            }
        }
        public bool IsOptionKeyDown()
        {
            return _optionKey;
        }
    }
}
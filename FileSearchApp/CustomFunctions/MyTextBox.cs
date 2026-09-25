using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FileSearchApp;

namespace CustomFunctions
{
    public static class MyTextBox
    {
        public static void ShowCandidate(this TextBox textbox, IEnumerable<string> items)
        {
            var fm = textbox.FindForm();
            if (fm == null){
                throw new Exception("ShowCandidate() Control の親フォームが見つかりません");
            }
            if (fm.Visible
            && fm.WindowState != FormWindowState.Minimized
            && fm.CanFocus){
                CandidateTextListBox.Show(fm, textbox, items);
            }
        }
    }
}
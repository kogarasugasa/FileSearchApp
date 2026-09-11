using System.Windows.Forms;

namespace FileSearchApp
{
    public class ButtonMouseDownSave : Button
    {
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right){
                //base.OnClick(e);
                base.OnMouseClick(e);
            }
        }
    }
}
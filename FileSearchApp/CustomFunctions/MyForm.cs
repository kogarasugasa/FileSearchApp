using System;
using System.CodeDom;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpNized.Rust.std;

namespace CustomFunctions
{
    public static class MyForm
    {
        public static void InvokeIfRequiredElseNonInvoke(this Form self, Action pAction)
        {
            if (self.InvokeRequired){
                self.Invoke(pAction);
            }
            else{
                pAction();
            }
        }
        public static void MoveTop(this Form self)
        {
            self.WindowState = FormWindowState.Minimized;
            self.WindowState = FormWindowState.Normal;
        }
        public static void ActivateForm(this Form self, int pTimeOut)
        {
            using (var cts = new CancellationTokenSource())
            {
                var tk = Task.Delay(pTimeOut, cts.Token)
                    .ContinueWith(comp => {
                        if (!comp.IsCanceled) cts.Cancel();
                    })
                ;
                ActivateForm(self, cts.Token);
                cts.Cancel();
                tk.WaitByPollingLoop(100);
            }
        }
        public static void ActivateForm(this Form self, CancellationToken ct)
        {
            while (Form.ActiveForm != self)
            {
                if (ct.IsCancellationRequested){
                    return;
                }
                self.InvokeIfRequiredElseNonInvoke(() =>{
                    self.TopMost = true;
                    self.TopMost = false;
                    self.Activate();
                });
            }
        }
    }
}
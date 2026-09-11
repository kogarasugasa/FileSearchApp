using System;
using System.Runtime.InteropServices;

namespace MyNativeMethod.Function
{
    public class ShutdownBlocker : IDisposable
    {
        bool _disposed = false;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, string pwszReason);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);
        IntPtr _handle = IntPtr.Zero;
        public ShutdownBlocker(IntPtr pHandle)
        {
            _handle = pHandle;
        }
        public ShutdownBlocker(string lpModuleName)
        {
            _handle = GetModuleHandle(lpModuleName);
        }
        ~ShutdownBlocker()
        {
            this.Dispose();
        }
        public void Dispose()
        {
            this.Dispose(true);
        }
        public void Dispose(bool disposing)
        {
            if (_disposed){
                return;
            }
            if (disposing){
                this.Release();
            }
            _disposed = true;
        }
        public bool Block(string pWaitingMsg)
        {
            if (_disposed){
                throw new ObjectDisposedException(this.GetType().ToString());
            }
            if (_handle == IntPtr.Zero){
                return false;
            }
            return ShutdownBlockReasonCreate(_handle, pWaitingMsg);
        }
        public void Release()
        {
            ShutdownBlockReasonDestroy(_handle);
            _handle = IntPtr.Zero;
        }
    }
}

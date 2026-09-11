using System;
using System.Threading;
using System.Windows.Forms;

namespace FileSearchApp
{
    // リストビューにタブでフォーカスした場合に先頭行にレティクルが表示される問題の対策用
    public sealed class ListViewLinkedItem : ListViewItem
    {
        long _disposed = 0;
        public ListViewLinkedItem(string[] strings) : base(strings)
        {
            UnFocusRequested += UnFocus;
        }
        ~ListViewLinkedItem()
        {
            this.Dispose();
        }
        void Dispose()
        {
            if (Interlocked.Read(ref _disposed) == 0){
                return;
            }
            UnFocusRequested -= UnFocus;
            Interlocked.Exchange(ref _disposed, 1);
        }
        void UnFocus(object s, EventArgs e)
        {
            this.Focused = false;
            this.Dispose();
        }
        public static void UnFocusAll()
        {
            UnFocusRequested.Invoke(new object(), new EventArgs());
        }
        public static event EventHandler<EventArgs> UnFocusRequested = delegate{};
    }
}
using System.Threading;
using System.Threading.Tasks;

namespace CustomFunctions
{
    public static class MyTask
    {
        public static void WaitByPollingLoop(this Task pTask, int pPollingSpanMilliseconds)
        {
            while (true)
            {
                if (pTask.IsCompleted){
                    break;
                }
                Task.Delay(pPollingSpanMilliseconds).Wait();
            }
        }
        public static void WaitByPollingLoop(this Task pTask, int pPollingSpanMilliseconds, int pTimeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                Task.Delay(pTimeout).ContinueWith(tk => cts.Cancel());
                WaitByPollingLoop(pTask, pPollingSpanMilliseconds, cts.Token);
            }
        }
        public static void WaitByPollingLoop(this Task pTask, int pPollingSpanMilliseconds, CancellationToken pCancellationToken)
        {
            while (true)
            {
                if (pTask.IsCompleted){
                    break;
                }
                if (pCancellationToken.IsCancellationRequested){
                    break;
                }
                Task.Delay(pPollingSpanMilliseconds).Wait();
            }
        }
    }
}
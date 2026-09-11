using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace FileSearchApp
{
    public class CancellationTaskManager
    {
        object _lock = new object();
        int _nextId = 0;
        List<TaskCancellationTokenSource> _tasks = new List<TaskCancellationTokenSource>();
        public int Add(Action<CancellationToken> pCancelableAction)
        {
            lock (_lock)
            {
                int id = _nextId++;
                var cts = new CancellationTokenSource();
                var task = Task.Run(() => pCancelableAction(cts.Token));
                task.ConfigureAwait(false);
                _tasks.Add(new TaskCancellationTokenSource(id, task, cts));
                return id;
            }
        }
        public void CancelAll()
        {
            lock (_lock)
            {
                while (_tasks.Count != 0)
                {
                    var removeItems = new List<int>();
                    for (int i = 0; i < _tasks.Count; i++)
                    {
                        if (_tasks[i].CancelableTask.IsCompleted){
                            _tasks[i].CTS.Dispose();
                            removeItems.Add(i);
                        }
                        else{
                            _tasks[i].CTS.Cancel();
                            Task.Delay(50).Wait();
                        }
                    }
                    removeItems.Sort();
                    for (int i = removeItems.Count - 1; i >= 0; i--)
                    {
                        _tasks.RemoveAt(removeItems[i]);
                    }
                }
            }
        }
        public void CancelAt(int pId)
        {
            int removeIndex = -1;
            lock (_lock)
            {
                if (_tasks.Count == 0){
                    return;
                }
                for (int i = 0; i < _tasks.Count; i++)
                {
                    if (_tasks[i].Id == pId){
                        removeIndex = i;
                        break;
                    }
                }
                if (removeIndex == -1){
                    return;
                }
                _tasks[removeIndex].CTS.Cancel();
                while (!_tasks[removeIndex].CancelableTask.IsCompleted)
                {
                    Task.Delay(50).Wait();
                }
                _tasks[removeIndex].CTS.Dispose();
                _tasks.RemoveAt(removeIndex);
            }
        }
        public void Shrink()
        {
            lock (_lock)
            {
                while (true)
                {
                    var comps = _tasks.Where(val => val.CancelableTask.IsCompleted);
                    if (comps.Count() == 0){
                        break;
                    }
                    var removeItem = _tasks
                        .Where(val => val.CancelableTask.IsCompleted)
                        .First();
                    _tasks.Remove(removeItem);
                }
            }
        }
        public void Wait(int pId)
        {
            foreach (var val in _tasks)
            {
                if (val.Id == pId){
                    while (!val.CancelableTask.IsCompleted)
                    {
                        Task.Delay(100).Wait();
                    }
                    break;
                }
            }
        }
        public void WaitAll()
        {
            IEnumerable<Task> tasks;
            lock (_lock)
            {
                tasks = _tasks.Select(val => val.CancelableTask);
            }
            var task = Task.WhenAll(tasks);
            task.ConfigureAwait(false);
            task.Wait();
        }
        private class TaskCancellationTokenSource
        {
            public TaskCancellationTokenSource(int pId, Task pCancelableTask, CancellationTokenSource pCancellationTokenSource)
            {
                this.Id = pId;
                this.CancelableTask = pCancelableTask;
                this.CTS = pCancellationTokenSource;
            }
            public int Id { get; private set; }
            public Task CancelableTask { get; private set; }
            public CancellationTokenSource CTS { get; private set; }
        }
    }
}
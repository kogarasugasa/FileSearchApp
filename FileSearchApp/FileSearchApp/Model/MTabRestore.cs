
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FileSearchApp
{
    class MTabRestore
    {
        readonly object _lock = new object();
        Dictionary<int, IEnumerable<string>> _tabs = new Dictionary<int, IEnumerable<string>>();
        public Func<Dictionary<string, IEnumerable<string>>> VMReadMethod { get; set; }
        public int Count { get { lock (_lock) return _tabs.Count; } }
        public List<int> Keys { get { lock (_lock) return _tabs.Keys.ToList(); }}
        public MTabRestore()
        {
            this.VMReadMethod = () => new Dictionary<string, IEnumerable<string>>();
        }
        public IEnumerable<string> Get(int pKey)
        {
            lock (_lock)
            {
                return _tabs[pKey];
            }
        }
        public void Read(Func<Dictionary<string, IEnumerable<string>>> func)
        {
            var data = func();
            lock (_lock)
            {
                _tabs.Clear();
                Queue<string> noNum = new Queue<string>();
                foreach (var key in data.Keys)
                {
                    int parsedNum;
                    if (int.TryParse(key, out parsedNum)){
                        _tabs.Add(parsedNum, data[key]);
                    }
                    else{
                        noNum.Enqueue(key);
                    }
                }
                for (int i = 0; true; i++)
                {
                    if (noNum.Count == 0){
                        break;
                    }
                    if (_tabs.ContainsKey(i)){
                        continue;
                    }
                    if (_tabs.ContainsKey(i)){
                        continue;
                    }
                    var key = noNum.Dequeue();
                    _tabs.Add(i, data[key]);
                }
            }
        }
        public void SaveVMRead(Action<Dictionary<string, IEnumerable<string>>> pSaveMethod)
        {
            var data = this.VMReadMethod();
            pSaveMethod(data);
        }
        public void Save(Action<Dictionary<string, IEnumerable<string>>> pSaveMethod)
        {
            var data = new Dictionary<string, IEnumerable<string>>();
            lock (_lock)
            {
                foreach (var key in _tabs.Keys)
                {
                    data.Add(key.ToString(), _tabs[key]);
                }
            }
            pSaveMethod(data);
        }
        
    }
}
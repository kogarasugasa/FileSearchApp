using System;
using System.Linq;
using System.Collections.Generic;

namespace FileSearchApp
{
    public class SimpleBuffer<T>
    {
        object _lock = new object();
        object _lockFlush = new object();
        List<List<T>> _jag;
        public SimpleBuffer()
        {
            _jag = new List<List<T>>();
            _jag.Add(new List<T>());
        }
        public IEnumerable<T> Flush()
        {
            lock (_lockFlush)
            {
                lock (_lock)
                {
                    _jag.Insert(0, new List<T>());
                }
                foreach (var val in _jag[1])
                {
                    yield return val;
                }
                _jag.RemoveAt(1);
            }
        }
        public void Add(T pValue)
        {
            lock (_lock)
            {
                _jag[0].Add(pValue);
            }
        }
        public int CollectionsCount
        {
            get
            {
                lock (_lock) { return _jag.Count; }
            }
        }
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    int cnt = 0;
                    foreach (var list in _jag)
                    {
                        foreach (var val in list)
                        {
                            cnt++;
                        }
                    }
                    return cnt;
                }
            }
        }
    }
}

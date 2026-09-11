using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FileSearchApp
{
    public sealed class Filter<T>
    {
        object _lock = new object();
        const int MAX_NUM = 100;
        int _autoNum = 0;
        Dictionary<string, Func<T, bool>> _filters = new Dictionary<string, Func<T, bool>>();
        public ReadOnlyDictionary<string, Func<T, bool>> Filters {
            get {
                return new ReadOnlyDictionary<string, Func<T, bool>>(_filters);
            }
        }
        public int Count { get { lock (_lock){ return _filters.Count; } } }
        public Filter(){}
        public Filter(Func<T, bool> pFilter)
        {
            Add(pFilter);
        }
        public Filter(IEnumerable<KeyValuePair<string, Func<T, bool>>> pKeyValuePairs)
        {
            foreach (var kv in pKeyValuePairs)
            {
                Add(kv.Key, kv.Value);
            }
        }
        public bool Add(Func<T, bool> pFilter)
        {
            lock (_lock)
            {
                while (_filters.ContainsKey(_autoNum.ToString()))
                {
                    _autoNum++;
                    if (_autoNum > MAX_NUM){
                        return false;
                    }
                }
                _filters.Add(_autoNum.ToString(), pFilter);
                return true;
            }
        }
        public bool Add(string pKey, Func<T, bool> pFilter)
        {
            lock (_lock)
            {
                if (_filters.ContainsKey(pKey)){
                    return false;
                }
                else{
                    _filters.Add(pKey, pFilter);
                    return true;
                }
            }
        }
        public void Clear()
        {
            lock (_lock)
            {
                _filters.Clear();
                _autoNum = 0;
            }
        }
        public bool Remove(string pKey)
        {
            lock (_lock)
            {
                if (_filters.ContainsKey(pKey)){
                    _filters.Remove(pKey);
                    return true;
                }
                else{
                    return false;
                }
            }
        }
        public bool ContainsKey(string pKey)
        {
            lock (_lock)
            {
                return _filters.ContainsKey(pKey);
            }
        }
        public bool IsThrough(T pInfo)
        {
            lock (_lock)
            {
                bool result = true;
                foreach (var filter in _filters)
                {
                    result &= filter.Value(pInfo);
                }
                return result;
            }
        }
    }
}

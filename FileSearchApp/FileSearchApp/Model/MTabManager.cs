using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using CSharpNized.Rust.std;
using System.Collections.ObjectModel;


namespace FileSearchApp
{
    public class MTabManager
    {
        readonly object _lock = new object();
        readonly FormAssembler _formAssembler;
        readonly List<TabRecord> _vmList = new List<TabRecord>();
        public int Count { get { lock (_lock) return _vmList.Count; } }
        public ReadOnlyCollection<TabRecord> Records { get { lock (_lock) return _vmList.AsReadOnly(); }}
        public MTabManager(FormAssembler pFormAssembler)
        {
            _formAssembler = pFormAssembler;
        }
        public string Add()
        {
            lock (_lock)
            {
                var record = this.AddUnlock();
                return record.Key;
            }
        }
        TabRecord AddUnlock()
        {
            VMFileSearchApp vmMain;
            VMListView vmList;
            VMOptionMenu vmOption;
            _formAssembler.GetViewModel(out vmMain, out vmList, out vmOption);
            var record = new TabRecord(vmMain, vmList, vmOption);
            _vmList.Add(record);
            return record;
        }
        public void Remove(string pKey)
        {
            lock (_lock)
            {
                _vmList.RemoveAll(val => val.Key == pKey);
            }
        }
        public IOption<string> GetNearKey(string pKey)
        {
            IOption<string> result = new None<string>();
            var next = this.GetNextKey(pKey);
            if (next.IsSome()){
                next.IfSome(val => result = new Some<string>(val));
                return result;
            }
            else{
                var prev = this.GetPreviousKey(pKey);
                prev.IfSome(val => result = new Some<string>(val));
                return result;
            }
        }
        public IOption<string> GetNextKey(string pKey)
        {
            return GetSkipKey(pKey, 1);
        }
        public IOption<string> GetPreviousKey(string pKey)
        {
            return GetSkipKey(pKey, -1);
        }
        IOption<string> GetSkipKey(string pKey, int pSkip)
        {
            lock (_lock)
            {
                for (int i = 0; i < _vmList.Count; i++)
                {
                    if (_vmList[i].Key == pKey){
                        int target = i - pSkip;
                        if (target >= 0){
                            return new Some<string>(_vmList[target].Key);
                        }
                    }
                }
                return new None<string>();
            }
        }
        public TabRecord GetFirstOrNew()
        {
            lock (_lock)
            {
                if (_vmList.Count == 0){
                    return this.AddUnlock();
                }
                else{
                    return _vmList[0];
                }
            }
        }
        public IOption<TabRecord> GetViewModel(string pKey)
        {
            if (_vmList.Any(val => val.Key == pKey)){
                return new Some<TabRecord>(_vmList.Find(val => val.Key == pKey));
            }
            else{
                return new None<TabRecord>();
            }
        }
    }
}
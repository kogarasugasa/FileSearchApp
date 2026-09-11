using System.Collections.Generic;
using System.Threading;

namespace FileSearchApp
{
    public sealed class TabRecord
    {
        static readonly object _lockStatic = new object();
        static int _autoNum = 0;
        static readonly SortedSet<int> _managedNums = new SortedSet<int>();
        readonly object _lock = new object();
        readonly int _num;
        public string Key { get; private set; }
        string _text;
        public string Text {
            get { lock (_lock) return _text; }
            set { lock (_lock) _text = value; }
        }
        object _uiObjectKey = new object();
        public object UiObjectKey
        {
            get { lock (_lock) return _uiObjectKey; }
            set { lock (_lock) _uiObjectKey = value; }
        }
        public VMFileSearchApp FileSearch { get; private set; }
        public VMListView ListView { get; private set; }
        public VMOptionMenu Option { get; private set; }
        public TabRecord(VMFileSearchApp pFileSearch, VMListView pListView, VMOptionMenu pOption)
        {
            lock (_lockStatic)
            {
                bool isCounterReset = false;
                while (!_managedNums.Add(_autoNum))
                {
                    if (_autoNum != int.MaxValue){
                        _autoNum++;
                    }
                    else{
                        if (!isCounterReset){
                            _autoNum = 0;
                            isCounterReset = true;
                        }
                        else{
                            throw new System.Exception("TabRecord これ以上採番出来ません");
                        }
                    }
                }
                _num = _autoNum;
            }
            this.Key = _num.ToString();
            this.FileSearch = pFileSearch;
            this.ListView = pListView;
            this.Option = pOption;
            _text = this.Key;
        }
        ~TabRecord()
        {
            lock (_lockStatic) _managedNums.Remove(_num);
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;
using CSharpNized.Rust.std;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FileSearchApp
{
    public partial class MHistoryManager
    {
        readonly object _lock = new object();
        readonly object _propLock = new object();
        BusyChecker _bufChecker = new BusyChecker(25, 1000);
        BusyChecker _mainDataChecker = new BusyChecker(25, 1000);
        System.Timers.Timer _bufferFlushTimer = new System.Timers.Timer();
        System.Timers.Timer _autoSaveTimer = new System.Timers.Timer(10000);
        SimpleBuffer<string> _buf = new SimpleBuffer<string>();
        SortedKeyValue _index = new SortedKeyValue();
        IOption<Task> _readTask = new None<Task>();
        Action<IEnumerable<string>> _defaultSaveAction = list => {return;};
        // ############################################
        // プロパティ
        // ############################################
        bool _isLoad = false;
        public bool IsLoad {
            get { lock (_lock) return _isLoad; }
            private set { lock (_lock) _isLoad = value; }
        }
        public Action<IEnumerable<string>> SaveAction { get; set; }
        public int Count { get { return _index.Count; } }
        bool _isChanged = false;
        public bool IsChanged
        {
            get { lock (_propLock) return _isChanged; }
            private set { lock (_propLock) _isChanged = value; }
        }
        public bool AutoSaveEnabled
        {
            get { lock (_propLock) return _autoSaveTimer.Enabled; }
            set
            {
                lock (_propLock)
                {
                    if (value == true && _autoSaveTimer.Enabled == false){
                        Save();
                        _autoSaveTimer.Start();
                    }
                    else if (value == false && _autoSaveTimer.Enabled == true){
                        _autoSaveTimer.Stop();
                    }
                }
            }
        }
        // ############################################
        // コンストラクタ、デストラクタ
        // ############################################
        public MHistoryManager()
        {
            this.SaveAction = _defaultSaveAction;
            _bufferFlushTimer.Interval = 1000;
            _bufferFlushTimer.Elapsed += (s, e) => {
                if (_bufChecker.IsBusy | _mainDataChecker.IsBusy){
                    return;
                }
                this.FlushBuffer();
            };
            _bufferFlushTimer.Start();
            _autoSaveTimer.Elapsed += (s, e) => Save();
            this.IsChanged = false;
            _index.InvalidOrderRestoration += (s, e) =>
                this.InvalidOrderRestoration.Invoke(this, e)
            ;
        }
        ~MHistoryManager()
        {
            _bufferFlushTimer.Dispose();
        }
        // ############################################
        // メソッド
        // ############################################
        public List<ExFileInfo> GetExFileInfos(string pFileName)
        {
            var start = new IndexKeyValue(string.Empty, pFileName, IndexKeyValue.UnderOrOver.Under);
            var end = new IndexKeyValue(string.Empty, pFileName, IndexKeyValue.UnderOrOver.Over);
            List<IndexKeyValue> range;
            lock (_lock)
            {
                range = _index.GetRange(start, end);
            }
            List<ExFileInfo> infos = new List<ExFileInfo>();
            foreach (var val in range)
            {
                infos.Add(new ExFileInfo(val.Key));
            }
            return infos;
        }
        public void Remove(string pFilePath)
        {
            lock (_lock)
            {
                _mainDataChecker.Notice();
                bool isSuccess = _index.Remove(CreateIndexKeyValue(pFilePath));
                if (isSuccess){
                    this.IsChanged = true;
                }
            }
        }
        public bool TryAdd(string pFilePath)
        {
            lock (_lock)
            {
                _mainDataChecker.Notice();
                bool isSuccess = _index.Add(CreateIndexKeyValue(pFilePath));
                this.IsChanged |= isSuccess;
                return isSuccess;
            }
        }
        public void AddToBuffer(string pFilePath)
        {
            _bufChecker.Notice();
            _buf.Add(pFilePath);
        }
        public void FlushBuffer()
        {
            lock (_lock)
            {
                bool isSuccessExists = false;
                foreach (var val in _buf.Flush())
                {
                    isSuccessExists |= _index.Add(CreateIndexKeyValue(val));
                }
                this.IsChanged |= isSuccessExists;
            }
        }
        public bool Exists(string pFilePath)
        {
            lock (_lock) return _index.Exists(CreateIndexKeyValue(pFilePath));
        }
        public List<IndexKeyValue> GetHistory()
        {
            lock (_lock)
            {
                return _index.GetKeyValues();
            }
        }
        IndexKeyValue CreateIndexKeyValue(string pFilePath)
        {
            return new IndexKeyValue(pFilePath, ExFileInfo.GetFileName(pFilePath));
        }
        public void Save(Action<IEnumerable<string>> pSaveMethod)
        {
            this.FlushBuffer();
            lock (_lock)
            {
                pSaveMethod(_index.GetKeys());
                this.IsChanged = false;
            }
        }
        public void Save(bool pForce = false)
        {
            if (object.ReferenceEquals(this.SaveAction, _defaultSaveAction)){
                throw new Exception("SaveAction が設定されていません");
            }
            if (this.IsChanged || pForce){
                this.Save(this.SaveAction);
            }
        }
        public void Read(Func<IEnumerable<string>> pReadMethod)
        {
            lock (_lock)
            {
                var canRead = _readTask.Match(
                    none => true,
                    some => some.IsCompleted
                );
                if (canRead){
                    var tk = this.ReadInnerAsync(pReadMethod);
                    _readTask = new Some<Task>(tk);
                }
                else{
                    return;
                }
            }
        }
        async Task ReadInnerAsync(Func<IEnumerable<string>> pReadMethod)
        {
            await Task.Run(() => this.ReadInner(pReadMethod));
        }
        void ReadInner(Func<IEnumerable<string>> pReadMethod)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            _mainDataChecker.GetExclusive(() =>
            {
                var paths = pReadMethod();
                var indices = new List<IndexKeyValue>();
                foreach (var path in paths) indices.Add(this.CreateIndexKeyValue(path));
                lock (_lock)
                {
                    _index.Clear();
                    _index.AddRangeSorted(indices);
                    this.IsChanged = false;
                    _isLoad = true;
                }
            });
            sw.Stop();
            this.Load.Invoke(this, "MHistorySearcher.Read() Completed time : " + sw.ElapsedMilliseconds);
        }
        public event EventHandler<string> Load = delegate{};
        public event EventHandler<ReOrderEventArgs> InvalidOrderRestoration = delegate{};
    }
    public partial class MHistoryManager
    {
        private class BusyChecker
        {
            readonly object _lock = new object();
            int _max, _before = 0, _now = 0;
            bool _isExclusive = false;
            System.Timers.Timer _busyCheckTimer = new System.Timers.Timer();
            public bool IsBusy { get; private set; }
            public BusyChecker(int pMax, int pIntervalMilliseconds)
            {
                _max = pMax;
                _busyCheckTimer.Interval = pIntervalMilliseconds;
                _busyCheckTimer.Elapsed += BusyCheck;
                _busyCheckTimer.Start();
            }
            ~BusyChecker()
            {
                _busyCheckTimer.Dispose();
            }
            public void Notice()
            {
                lock (_lock) _now++;
            }
            void BusyCheck(object s, ElapsedEventArgs e)
            {
                bool result;
                lock (_lock)
                {
                    if (_isExclusive){
                        this.IsBusy = true;
                        return;
                    }
                    else{
                        result = _now - _before > _max;
                        _before = 0;
                        _now = 0;
                        this.IsBusy = result;
                        return;
                    }
                }
            }
            public void GetExclusive(Action pAction)
            {
                try
                {
                    lock (_lock) _isExclusive = true;
                    pAction();
                }
                finally
                {
                    lock (_lock) _isExclusive = false;
                }
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CSharpNized.Rust.std;
using CustomFunctions;

namespace FileSearchApp
{
    public class MAliasManager
    {
        readonly object _lock = new object();
        readonly object _propLock = new object();
        SortedKeyValue _index;
        System.Timers.Timer _autoSaveTimer = new System.Timers.Timer(10000);
        readonly Action<IEnumerable<string>> _defaultSaveAction = list => {return;};
        // ############################################
        // プロパティ
        // ############################################
        public Action<IEnumerable<string>> SaveAction { get; set; }
        public bool AutoSaveEnabled
        {
            get { lock (_propLock) return _autoSaveTimer.Enabled; }
            set
            {
                lock (_propLock)
                {
                    if (value == _autoSaveTimer.Enabled){
                        return;
                    }
                    else if (value){
                        Save();
                        _autoSaveTimer.Start();
                    }
                    else if(!value){
                        _autoSaveTimer.Stop();
                    }
                }
            }
        }
        public bool _isChanged = false;
        public bool IsChanged {
            get { lock (_propLock) return _isChanged; }
            private set { lock (_propLock) _isChanged = value; }
        }
        // ############################################
        // コンストラクタ
        // ############################################
        public MAliasManager()
        {
            this.SaveAction = _defaultSaveAction;
            var comp = new IndexKeyValueComparer();
            comp.SetSortPriority(priority => {
                priority.Key = IndexKeyValueComparer.SortPriority.Hi;
                priority.Value = IndexKeyValueComparer.SortPriority.None;
                priority.Stance = IndexKeyValueComparer.SortPriority.Lo;
            });
            _index = new SortedKeyValue(comp);
            _autoSaveTimer.Elapsed += (s, e) => Save();
        }
        // ############################################
        // メソッド
        // ############################################
        public bool Add(string pFileName, string pAlias, bool pOverWrite)
        {
            if (pAlias.Trim() == "" || pFileName == pAlias || pFileName == ""){
                return false;
            }
            lock (_lock)
            {
                var item = new IndexKeyValue(pFileName, pAlias);
                // 追加に失敗して（重複あり）上書き指示があれば上書き
                bool isSuccess = _index.Add(item);
                if (isSuccess){
                    this.IsChanged = true;
                    return true;
                }
                if (pOverWrite){
                    _index.Remove(item);
                    _index.Add(item);
                    this.IsChanged = true;
                    return true;
                }
                else{
                    return false;
                }
            }
        }
        public void Remove(string pFileName)
        {
            lock (_lock)
            {
                bool isSuccess = _index.Remove(new IndexKeyValue(pFileName, string.Empty));
                if (isSuccess){
                    this.IsChanged = true;
                }
            }
        }
        public IOption<string> GetAlias(string pFileName)
        {
            var start = new IndexKeyValue(pFileName, string.Empty, IndexKeyValue.UnderOrOver.Under);
            var end = new IndexKeyValue(pFileName, string.Empty, IndexKeyValue.UnderOrOver.Over);
            List<IndexKeyValue> range;
            lock (_lock)
            {
                range = _index.GetRange(start, end);
            }
            if (range.Count == 1){
                return new Some<string>(range[0].Value);
            }
            else{
                return new None<string>();
            }
        }
        public List<string> GetNames()
        {
            lock (_lock)
            {
                return new List<string>(_index.GetKeys().Distinct());
            }
        }
        public List<string> GetNamesFuzzy(string pFileNameFuzzy)
        {
            lock (_lock)
            {
                var matchNames = new List<string>();
                foreach (var item in _index.GetKeyValues())
                {
                    if (MyTool.FuzzyContains(pFileNameFuzzy.ToLower(), item.Value.ToLower())){
                        matchNames.Add(item.Key);
                    }
                }
                return matchNames;
            }
        }
        public void Save(Action<IEnumerable<string>> pSaveMethod)
        {
            var textLines = new List<string>();
            lock (_lock)
            {
                foreach (var item in _index.GetKeyValues())
                {
                    textLines.Add(item.Key + ":" + item.Value);
                }
                pSaveMethod(textLines);
                this.IsChanged = false;
            }
        }
        public void Save(bool pForce = false)
        {
            if (this.SaveAction == _defaultSaveAction){
                throw new Exception("SaveAction が設定されていません");
            }
            if (this.IsChanged || pForce){
                Save(this.SaveAction);
            }
        }
        public void Read(Func<IEnumerable<string>> pReadMethod)
        {
            lock (_lock)
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                foreach (var line in pReadMethod())
                {
                    var textLine = line;
                    int colonPos = textLine.IndexOf(":");
                    if (colonPos == -1){
                        continue;
                    }
                    if (colonPos == textLine.Length - 1){
                        continue;
                    }
                    var leftText = textLine.Substring(0, textLine.IndexOf(":"));
                    var rightText = textLine.Substring(textLine.IndexOf(":") + 1);
                    this.Add(leftText, rightText, false);
                }
                this.IsChanged = false;
                sw.Stop();
                this.Load.Invoke(this, "MAliasManager.Read() completed time : " + sw.ElapsedMilliseconds);
            }
        }
        public event EventHandler<string> Load = delegate { };
    }
}
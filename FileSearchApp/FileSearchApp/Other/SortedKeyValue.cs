using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpNized.Rust.std;
using CustomFunctions;

namespace FileSearchApp
{
    public sealed class SortedKeyValue
    {
        object _lock = new object();
        List<IndexKeyValue> _index = new List<IndexKeyValue>();
        IndexKeyValueComparer _comp;
        IOption<Task> _sortTask = new None<Task>();
        public SortedKeyValue()
        {
            _comp = new IndexKeyValueComparer();
        }
        public SortedKeyValue(IndexKeyValueComparer pComparer)
        {
            _comp = pComparer;
        }
        public void Clear()
        {
            lock (_lock)
            {
                _index.Clear();
            }
        }
        public bool Add(IndexKeyValue pKeyValue)
        {
            lock (_lock)
            {
                return AddUnLock(pKeyValue);
            }
        }
        public void AddRange(IEnumerable<IndexKeyValue> pList)
        {
            lock (_lock)
            {
                foreach (var val in pList)
                {
                    this.AddUnLock(val);
                }
            }
        }
        public void AddRangeSorted(IEnumerable<IndexKeyValue> pList)
        {
            lock (_lock)
            {
                _index.AddRange(pList);
                _sortTask.Match(
                    none => {
                        _sortTask = new Some<Task>(Task.Run(() => this.CheckOrder()));
                    },
                    some => {
                        if (some.IsCompleted){
                            _sortTask = new Some<Task>(Task.Run(() => this.CheckOrder()));
                        }
                    }
                );
            }
        }
        public void Sort()
        {
            List<IndexKeyValue> copy;
            lock (_lock) copy = new List<IndexKeyValue>(_index);
            this.Clear();
            this.AddRange(copy);
        }
        private bool AddUnLock(IndexKeyValue pKeyValue)
        {
            int insertIdx = InsertIndexOfUnLock(pKeyValue, false);
            if (insertIdx >= _index.Count){
                _index.Add(pKeyValue);
                return true;
            }
            if (_comp.Compare(_index[insertIdx], pKeyValue) == 0){
                return false;
            }
            _index.Insert(insertIdx, pKeyValue);
            return true;
        }
        public bool Exists(IndexKeyValue pKeyValue)
        {
            lock (_lock)
            {
                if (InsertIndexOfUnLock(pKeyValue, true) == -1){
                    return false;
                }else{
                    return true;
                }
            }
        }
        public bool Remove(IndexKeyValue pKeyValue)
        {
            lock (_lock)
            {
                int idx = InsertIndexOfUnLock(pKeyValue, true);
                if (idx != -1){
                    _index.RemoveAt(idx);
                    return true;
                }
                return false;
            }
        }
        public List<IndexKeyValue> GetRange(IndexKeyValue pStart, IndexKeyValue pEnd)
        {
            lock (_lock)
            {
                var infos = new List<IndexKeyValue>();
                int sIdx = InsertIndexOfUnLock(pStart, false);
                int eIdx = InsertIndexOfUnLock(pEnd, false);
                for (int i = sIdx; i < eIdx; i++)
                {
                    infos.Add(_index[i]);
                }
                return infos;
            }
        }
        public List<IndexKeyValue> GetKeyValues()
        {
            lock (_lock)
            {
                return new List<IndexKeyValue>(_index);
            }
        }
        public IEnumerable<string> GetKeys()
        {
            lock (_lock)
            {
                foreach (var val in _index)
                {
                    yield return val.Key;
                }
            }
        }
        public IEnumerable<string> GetValues()
        {
            lock (_lock)
            {
                foreach (var val in _index)
                {
                    yield return val.Value;
                }
            }
        }
        public IndexKeyValue ElementAt(int pIndex)
        {
            lock (_lock)
            {
                return _index[pIndex];
            }
        }
        public void Lock(Action<List<IndexKeyValue>> action)
        {
            lock (_lock)
            {
                action(_index);
            }
        }
        /// <summary>
        /// Insertすべき位置を返す
        /// </summary>
        int InsertIndexOfUnLock(IndexKeyValue pKeyValue, bool pExactMatch)
        {
            int pivot = _index.Count / 2;
            int start = 0;
            int end = _index.Count - 1;
            while (_index.Count > 0)
            {
                int compareTo = _comp.Compare(pKeyValue, _index[pivot]);
                if (compareTo > 0){
                    start = Math.Min(pivot + 1, end);
                    pivot = pivot + (end - pivot) / 2;
                }else{
                    end = Math.Max(pivot - 1, start);
                    pivot = start + (pivot - start) / 2;
                }
                if (start != end){
                    continue;
                }
                var comp = _comp.Compare(pKeyValue, _index[start]);
                if (comp == 0){
                    return start;
                }
                int before = comp > 0 ? 1 : -1;
                int curIdx = start;
                while (true)
                {
                    var comp2 = _comp.Compare(pKeyValue, _index[curIdx]);
                    if (comp2 == 0){
                        comp2 = 0;
                    }else if (comp2 > 0){
                        comp2 = 1;
                    }else{
                        comp2 = -1;
                    }
                    int after = comp2;
                    if (before != after){
                        if (after == 0){
                            return curIdx;
                        }
                        if (pExactMatch){
                            return -1;
                        }
                        if (after > 0){
                            return curIdx + after;
                        }
                        if (after < 0){
                            return curIdx;
                        }
                    }
                    before = after;
                    curIdx += after;
                    if (curIdx >= _index.Count){
                        if (pExactMatch){
                            return -1;
                        }
                        else{
                            return curIdx;
                        }
                    }
                    if (curIdx < 0){
                        if (pExactMatch){
                            return -1;
                        }else{
                            return 0;
                        }
                    }
                }
            }
            return 0;
        }
        public int Count
        {
            get
            {
                lock (_lock) { return _index.Count; }
            }
        }
        void CheckOrder()
        {
            List<IndexKeyValue> clone;
            lock (_lock) clone = new List<IndexKeyValue>(_index);
            bool valided = true;
            for (int i = 0; i < clone.Count - 1; i++)
            {
                if (_comp.Compare(clone[i], clone[i + 1]) > 0){
                    valided = false;
                    break;
                }
            }
            if (valided){
                return;
            }
            var msg = "修復可能なインデックスの破損が検出されました"
                + Environment.NewLine
                + "修復しますか？"
            ;
            var args = new ReOrderEventArgs(msg);
            this.InvalidOrderRestoration.Invoke(this, args);
            if (args.CancelRequested){
                return;
            }
            this.Sort();
        }
        public event EventHandler<ReOrderEventArgs> InvalidOrderRestoration = delegate{};
    }
    public sealed class IndexKeyValue : IEquatable<IndexKeyValue>
    {
        public enum UnderOrOver { Under = -1, Neutral = 0, Over = 1 }
        public string Key { get; private set; }
        public UnderOrOver Stance { get; private set; }
        public string Value { get; private set; }
        public IndexKeyValue(string pKey, string pValue)
        {
            this.Key = pKey;
            this.Value = pValue;
            this.Stance = UnderOrOver.Neutral;
        }
        public IndexKeyValue(string pKey, string pValue, UnderOrOver pStance)
        {
            this.Key = pKey;
            this.Value = pValue;
            this.Stance = pStance;
        }
        public override bool Equals(object obj)
        {
            return obj != null &&
                obj.GetType() == this.GetType() &&
                this.Equals((IndexKeyValue)obj)
            ;
        }
        public bool Equals(IndexKeyValue other)
        {
            return other != null &&
                other.Key == this.Key &&
                other.Value == this.Value &&
                other.Stance == this.Stance
            ;
        }
        public override int GetHashCode()
        {
            var hash = this.Key.GetHashCode() * 3;
            hash = hash ^ this.Value.GetHashCode() * 7;
            hash = hash ^ this.Stance.GetHashCode();
            return hash;
        }
        public override string ToString()
        {
            return this.Value
                + " | " + this.Key
                + " | " + this.Stance.ToString()
            ;
        }
    }
    public sealed class IndexKeyValueComparer : IComparer<IndexKeyValue>
    {
        public enum SortPriority { None, Hi, Mi, Lo, }
        public class PrioritySetter{public SortPriority Key; public SortPriority Value; public SortPriority Stance;}
        int _keyOrder = 1; int _valueOrder = 100; int _stance = 10;
        public IndexKeyValueComparer(){}
        public void SetSortPriority(Action<PrioritySetter> setter)
        {
            var priority = new PrioritySetter();
            setter(priority);
            bool undefined = false;
            switch (priority.Key){
                case SortPriority.Hi: _keyOrder = 100; break;
                case SortPriority.Mi: _keyOrder = 10; break;
                case SortPriority.Lo: _keyOrder = 1; break;
                case SortPriority.None: _keyOrder = 0; break;
                default: undefined = true; break;
            }
            switch (priority.Value){
                case SortPriority.Hi: _valueOrder = 100; break;
                case SortPriority.Mi: _valueOrder = 10; break;
                case SortPriority.Lo: _valueOrder = 1; break;
                case SortPriority.None: _valueOrder = 0; break;
                default: undefined = true; break;
            }
            switch (priority.Stance){
                case SortPriority.Hi: _stance = 100; break;
                case SortPriority.Mi: _stance = 10; break;
                case SortPriority.Lo: _stance = 1; break;
                case SortPriority.None: _stance = 0; break;
                default: undefined = true; break;
            }
            if (undefined){
                throw new Exception("未定義の並び替え優先順位です");
            }
        }
        public int Compare(IndexKeyValue x, IndexKeyValue y)
        {
            int comp = 0;
            //comp += x.Key.ToLower().CompareTo(y.Key.ToLower()) * _keyOrder;
            //comp += x.Value.ToLower().CompareTo(y.Value.ToLower()) * _valueOrder;
            //
            //「サンゲートライト」価格表
            //「ｻﾝｹﾞｰﾄﾗｲﾄ」価格表
            // 上記二つが実行する環境によって重みが変わるため文字ごとに比較することにした
            comp += x.Key
                .ToLower()
                .CharArrayCompareTo(y.Key.ToLower().ToCharArray())
                * _keyOrder
            ;
            comp += x.Value
                .ToLower()
                .CharArrayCompareTo(y.Value.ToLower().ToCharArray())
                * _valueOrder
            ;
            comp += x.Stance.CompareTo(y.Stance) * _stance;
            return comp;
        }
    }
}
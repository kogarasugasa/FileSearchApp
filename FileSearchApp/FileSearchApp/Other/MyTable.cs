using System;
using System.Collections.Generic;
using System.Linq;

namespace FileSearchApp
{
    public abstract partial class MyTable<T, U> where T : IConvertible where U : IComparable<U>, IEquatable<U>
    {
        ulong _num = 0;
        bool _isInitialized = false;
        readonly Dictionary<T, int> _fields = new Dictionary<T, int>();
        readonly Dictionary<ulong, Dictionary<T, U>> _records = new Dictionary<ulong, Dictionary<T, U>>();
        readonly SortedSet<T> _primaryKey = new SortedSet<T>();
        readonly Dictionary<T, Dictionary<U, List<ulong>>> _indices = new Dictionary<T, Dictionary<U, List<ulong>>>();
        public IReadOnlyList<T> PrimaryKey
        {
            get { return _primaryKey.ToList().AsReadOnly(); }
        }
        public List<T> Fields
        {
            get { return _fields.OrderBy(val => val.Value).Select(val => val.Key).ToList() ; }
        }
        public int Count { get { return _records.Count; } }
        public MyTable()
        {
            this.InitializeFields();
        }
        void InitializeFields()
        {
            if (_fields.Count != 0){
                throw new Exception("既にInitializeFieldsは実行されています");
            }
            if (!typeof(T).IsEnum){
                throw new Exception("型引数は列挙体を指定してください");
            }
            int i = 0;
            foreach (var val in Enum.GetValues(typeof(T)))
            {
                _fields.Add((T)val, i++);
            }
        }
        void InitializeIfNotInitialized()
        {
            if (_isInitialized){
                return;
            }
            foreach (var val in this.InitializePrimaryKey())
            {
                _primaryKey.Add(val);
                _indices.Add(val, new Dictionary<U, List<ulong>>());
            }
            foreach (var val in this.InitializeIndex())
            {
                if (_indices.ContainsKey(val)){
                    continue;
                }
                _indices.Add(val, new Dictionary<U, List<ulong>>());
            }
            _isInitialized = true;
        }
        public abstract List<T> InitializePrimaryKey();
        public abstract List<T> InitializeIndex();
        public Dictionary<T, string> GetNewRecord()
        {
            return new Dictionary<T, string>();
        }
        public bool Insert(Dictionary<T, U> pRecord)
        {
            // 初回実行でIndex,PrimaryKeyを設定する
            this.InitializeIfNotInitialized();
            // 主キーは値必須
            if (!_primaryKey.All(val => pRecord.ContainsKey(val))){
                return false;
            }
            // PrimaryKeyの重複を調べる
            if (this.ExistsPrimaryKey(pRecord)){
                return false;
            }
            _records.Add(_num, pRecord);
            this.InsertIndex(_num);
            _num++;
            return true;
        }
        public bool Update(Dictionary<T, U> pOld, Dictionary<T, U> pNew)
        {
            var foundRecords = this.Where(this.ToPrimaryKeyOnly(pOld));
            if (foundRecords.Count == 1){
                // インデックスを削除
                var rowNum = foundRecords.First().Key;
                this.DeleteIndex(rowNum);
                // レコードを差し替え
                _records[foundRecords.First().Key] = pNew;
                this.InsertIndex(rowNum);
                return true;
            }
            return false;
        }
        public void Delete(Dictionary<T, U> pPrimaryKey)
        {
            var filtered = this.Where(this.ToPrimaryKeyOnly(pPrimaryKey));
            if (filtered.Count == 0){
                return;
            }
            var item = filtered.First();
            this.DeleteIndex(item.Key);
            _records.Remove(item.Key);
        }
        public bool ExistsPrimaryKey(Dictionary<T, U> pPrimaryKey)
        {
            var primaryKey = this.ToPrimaryKeyOnly(pPrimaryKey);
            return this.Exists(primaryKey);
        }
        public bool Exists(Dictionary<T, U> pRecord)
        {
            var whereInfo = new Dictionary<T, U>(pRecord);
            var filtered = _records;
            // 効果の高いインデックスで検索する
            T field;
            if (this.TryGetEffectiveIndex(whereInfo, out field)){
                filtered = this.Where(field, whereInfo[field], whereInfo[field], _indices[field]);
                whereInfo.Remove(field);
            }
            // 残りの条件で検索する
            return filtered.Any(val => this.PartialEqualsRecord(val.Key, whereInfo));
        }
        public Dictionary<T, U> ToPrimaryKeyOnly(Dictionary<T, U> pRecord)
        {
            // 初回実行でIndex,PrimaryKeyを設定する
            this.InitializeIfNotInitialized();
            var primaryKey = new Dictionary<T, U>();
            foreach (var key in _primaryKey)
            {
                U value;
                if (pRecord.TryGetValue(key, out value)){
                    primaryKey.Add(key, value);
                }
                else{
                    return new Dictionary<T, U>();
                }
            }
            return primaryKey;
        }
        Dictionary<ulong, Dictionary<T, U>> Where(Dictionary<T, U> pRecord)
        {
            var whereInfo = pRecord;
            Dictionary<ulong, Dictionary<T, U>> filtered = _records;
            // 効果の高いインデックスで検索する
            T field;
            if (this.TryGetEffectiveIndex(whereInfo, out field)){
                filtered = this.Where(field, whereInfo[field], whereInfo[field], _indices[field]);
                whereInfo.Remove(field);
            }
            // 残りの条件で検索する
            foreach (var key in whereInfo)
            {
                filtered = MyTable<T, U>.Where(filtered, key.Key, key.Value);
            }
            return filtered;
        }
        Dictionary<ulong, Dictionary<T, U>> Where(T pField, U pEquals)
        {
            if (this.IsEffectiveIndex(pField, pEquals)){
                return this.Where(pField, pEquals, pEquals, _indices[pField]);
            }
            else{
                return this.Where(pField, pEquals, pEquals, new Dictionary<U, List<ulong>>());
            }
        }
        /// <summary>
        /// インデックスを指定して検索
        /// </summary>
        Dictionary<ulong, Dictionary<T, U>> Where(T pField, U pStart, U pEnd, Dictionary<U, List<ulong>> pIndex)
        {
            var result = new Dictionary<ulong, Dictionary<T, U>>();
            if (pStart.Equals(pEnd)){
                // インデックスに存在する
                if (pIndex.ContainsKey(pStart)){
                    foreach (var rowid in pIndex[pStart])
                    {
                        if (_records[rowid][pField].Equals(pStart)){
                            result.Add(rowid, _records[rowid]);
                        }
                    }
                    return result;
                }
                // インデックスを使えない、使うメリットが無い
                foreach (var val in _records)
                {
                    if (val.Value[pField].Equals(pStart)){
                        result.Add(val.Key, val.Value);
                    }
                }
            }
            else{
                // インデックスに存在する
                if (pIndex.ContainsKey(pStart)){
                    foreach (var rowid in pIndex[pStart])
                    {
                        if (_records[rowid][pField].CompareTo(pStart) >= 0
                        && _records[rowid][pField].CompareTo(pEnd) <= 0){
                            result.Add(rowid, _records[rowid]);
                        }
                    }
                    return result;
                }
                // インデックスを使えない、使うメリットが無い
                foreach (var val in _records)
                {
                    if (val.Value[pField].CompareTo(pStart) >= 0
                    && val.Value[pField].CompareTo(pEnd) <= 0){
                        result.Add(val.Key, val.Value);
                    }
                }
            }
            return result;
        }
        public static Dictionary<ulong, Dictionary<T, U>> Where(Dictionary<ulong, Dictionary<T, U>> pRecords, T pField, U pEquals)
        {
            var result = new Dictionary<ulong, Dictionary<T, U>>();
            foreach (var record in pRecords)
            {
                if (record.Value[pField].Equals(pEquals)){
                    result.Add(record.Key, record.Value);
                }
            }
            return result;
        }
        bool PartialEqualsRecord(ulong pRowId, Dictionary<T, U> pWhereInfo)
        {
            var tableData = _records[pRowId];
            if (pWhereInfo.All(val => tableData[val.Key].Equals(val.Value))){
                return true;
            }
            else{
                return false;
            }
        }
        void DeleteIndex(ulong pRowNum)
        {
            var oldRowNum = pRowNum;
            var oldRecord = _records[pRowNum];
            foreach (var field in _indices.Keys)
            {
                if (oldRecord.ContainsKey(field))
                {
                    if (_indices[field].ContainsKey(oldRecord[field])){
                        _indices[field][oldRecord[field]].Remove(oldRowNum);
                        if (_indices[field][oldRecord[field]].Count == 0){
                            _indices[field].Remove(oldRecord[field]);
                        }
                    }
                }
            }
        }
        void InsertIndex(ulong pRowNum)
        {
            var newRowNum = pRowNum;
            var newRecord = _records[pRowNum];
            foreach (var field in _indices.Keys)
            {
                if (newRecord.ContainsKey(field)){
                    if (_indices[field].ContainsKey(newRecord[field])){
                        _indices[field][newRecord[field]].Add(newRowNum);
                    }
                    else{
                        _indices[field].Add(newRecord[field], new List<ulong>{ newRowNum });
                    }
                }
            }
        }
        bool IsEffectiveIndex(T pField, U pEquals)
        {
            return _records.Count / 2 > _indices[pField][pEquals].Count;
        }
        bool TryGetEffectiveIndex(Dictionary<T, U> pFieldsValues, out T pEffectiveField)
        {
            int min = int.MaxValue;
            bool found = false;
            T field = _fields.First().Key;
            foreach (var val in pFieldsValues)
            {
                if (_indices.ContainsKey(val.Key)){
                    if (_indices[val.Key].ContainsKey(val.Value)){
                        if (_records.Count / 2 < _indices[val.Key][val.Value].Count){
                            continue;
                        }
                        if (min > _indices[val.Key][val.Value].Count){
                            min = _indices[val.Key][val.Value].Count;
                            field = val.Key;
                            found = true;
                        }
                    }
                }
            }
            if (found){
                pEffectiveField = field;
                return true;
            }
            else{
                pEffectiveField = field;
                return false;
            }
        }
    }
}
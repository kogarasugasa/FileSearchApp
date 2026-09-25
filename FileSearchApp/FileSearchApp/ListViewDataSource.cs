using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Runtime.Serialization;

namespace FileSearchApp
{
    public class ListViewDataSource
    {
        object _lock = new object();
        ConcurrentDictionary<int, ExFileInfo> _cache =
            new ConcurrentDictionary<int, ExFileInfo>();
        Dictionary<int, ExFileInfo> _allFileInfos = new Dictionary<int, ExFileInfo>();
        Dictionary<int, ExFileInfo> _publicFileInfos;
        List<ExFileInfo> _bufferFileInfos = new List<ExFileInfo>(); //ソート、フィルタ中に追加出来るようにするため
        SortedSet<ExFileInfo> _primarykey = new SortedSet<ExFileInfo>(new PrimaryKeyComparer());
        CancellationTaskManager _cancelableTask = new CancellationTaskManager();
        bool _isBufferUsed = false;
        // ############################################
        // プロパティ
        // ############################################
        Filter<ExFileInfo> _filter = new Filter<ExFileInfo>();
        public ReadOnlyDictionary<string, Func<ExFileInfo, bool>> Filters
        {
            get { return _filter.Filters; }
        }
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _publicFileInfos.Count;
                }
            }
        }
        // ############################################
        // コンストラクタ、デストラクタ
        // ############################################
        public ListViewDataSource()
        {
            _publicFileInfos = _allFileInfos;
        }
        ~ListViewDataSource()
        {
            _cancelableTask.CancelAll();
        }
        // ############################################
        // メソッド
        // ############################################
        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _cancelableTask.CancelAll();
                _allFileInfos.Clear();
                _publicFileInfos.Clear();
                _bufferFileInfos.Clear();
                _primarykey.Clear();
                //_publicFileInfos = _allFileInfos;
            }
        }
        public bool Add(ExFileInfo pInfo)
        {
            lock (_lock)
            {
                //重複したら追加しない
                var infos = _primarykey.GetViewBetween(pInfo, pInfo);
                if (infos.Count == 0){
                    //追加
                    _primarykey.Add(pInfo);
                    if (_isBufferUsed){
                        _bufferFileInfos.Add(pInfo);
                    }
                    _allFileInfos.Add(_allFileInfos.Count, pInfo);
                    if (_publicFileInfos != _allFileInfos && _filter.IsThrough(pInfo)){
                        _publicFileInfos.Add(_publicFileInfos.Count, pInfo);
                    }
                    return true;
                }
                else{
                    //更新
                    if (pInfo.Size != 0){
                        infos.ElementAt(0).Size = pInfo.Size;
                    }
                    return false;
                }
            }
        }
        public ExFileInfo ElementAt(int pIndex, bool pEnableCache = false)
        {
            // キャッシュを有効にした場合キャッシュが存在すればそちらから読み込む
            if (pEnableCache){
                ExFileInfo info;
                if (_cache.TryGetValue(pIndex, out info)){
                    return info;
                }
                lock (_lock)
                {
                    if (_publicFileInfos.TryGetValue(pIndex, out info)){
                        _cache.TryAdd(pIndex, info);
                        return info;
                    }
                }
            }
            else{
                ExFileInfo info;
                lock (_lock)
                {
                    if (_publicFileInfos.TryGetValue(pIndex, out info)){
                        return info;
                    }
                }
            }
            return ExFileInfo.GetDefault();
        }
        public void Sort(string pColumnText, SortOrder pSortOrder)
        {
            List<KeyValuePair<int ,ExFileInfo>> sorted;
            lock (_lock)
            {
                // ソート中はバッファに追加されるようにする
                _isBufferUsed = true;
                sorted = _publicFileInfos.ToList();
            }
            // ソートを実行
            if (pColumnText == "Size"){
                sorted.Sort((val1, val2) =>
                {
                    if (pSortOrder != SortOrder.Descending){
                        var aa = ((long)val1.Value.Size).CompareTo((long)val2.Value.Size) * 10;
                        var bb = val1.Key.CompareTo(val2.Key);
                        return aa + bb;
                    }
                    else{
                        var aa = ((long)val2.Value.Size).CompareTo((long)val1.Value.Size) * 10;
                        var bb = val2.Key.CompareTo(val1.Key);
                        return aa + bb;
                    }
                });
            }
            else{
                Func<string, ExFileInfo, string> compStringCreater = (colName, info) =>
                {
                    var result = "";
                    switch (colName){
                        case "Path" : result = info.FilePath; break;
                        case "FileName" : result = info.FileName; break;
                        case "Extension" : result = info.Extension; break;
                        case "Alias" : result = info.DispFileName(true); break;
                        case "Status" : result = info.Invalided ? "Invalid" : "Valid"; break;
                    }
                    return result;
                };
                sorted.Sort((val1, val2) =>
                {
                    var str1 = compStringCreater(pColumnText, val1.Value);
                    var str2 = compStringCreater(pColumnText, val2.Value);                    if (pSortOrder != SortOrder.Descending){
                        var aa = str1.CompareTo(str2) * 10;
                        var bb = val1.Key.CompareTo(val2.Key);
                        return aa + bb;
                    }
                    else{
                        var aa = str2.CompareTo(str1) * 10;
                        var bb = val2.Key.CompareTo(val1.Key);
                        return aa + bb;
                    }
                });
            }
            int newKey = 0;
            var sortedDict = sorted.ToDictionary(val => newKey++, val => val.Value);
            lock (_lock)
            {
                // バッファを無効にする
                _isBufferUsed = false;
                // 読み込みキャッシュをクリアする
                _cache.Clear();
                // ソート済みの末尾にバッファを追加する
                foreach (var val in _bufferFileInfos) sortedDict.Add(sortedDict.Count, val);
                // バッファをクリアする
                _bufferFileInfos.Clear();
                // 表示用変数にセット
                _publicFileInfos = sortedDict;
            }
        }
        public List<KeyValuePair<int, ExFileInfo>> GetKeyValuePairs()
        {
            var result = new List<KeyValuePair<int, ExFileInfo>>();
            lock (_lock)
            {
                foreach (var val in _publicFileInfos)
                {
                    result.Add(val);
                }
            }
            return result;
        }
        public void ApplyFilter(Filter<ExFileInfo> pFilter)
        {
            if (pFilter.Count == 0){
                _filter = pFilter;
                _publicFileInfos = _allFileInfos;
                return;
            }
            Dictionary<int, ExFileInfo> clone;
            lock (_lock)
            {
                _filter = pFilter;
                _isBufferUsed = true;
                clone = new Dictionary<int, ExFileInfo>(_allFileInfos);
            }
            var filtered = new Dictionary<int, ExFileInfo>();
            foreach (var info in clone)
            {
                if (_filter.IsThrough(info.Value)){
                    filtered.Add(filtered.Count, info.Value);
                }
            }
            lock (_lock)
            {
                _isBufferUsed = false;
                _cache.Clear();
                foreach (var info in _bufferFileInfos)
                {
                    if (_filter.IsThrough(info)){
                        filtered.Add(filtered.Count, info);
                    }
                }
                _bufferFileInfos.Clear();
                _publicFileInfos = filtered;
            }
        }
        class PrimaryKeyComparer : IComparer<ExFileInfo>
        {
            public int Compare(ExFileInfo x, ExFileInfo y)
            {
                return x.FilePath.ToLower().CompareTo(y.FilePath.ToLower());
            }
        }
    }
}

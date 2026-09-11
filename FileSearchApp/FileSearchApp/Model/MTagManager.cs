using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FileSearchApp
{
    public class MTagManager
    {
        readonly object _lock = new object();
        readonly object _propLock = new object();
        List<string> _names = new List<string>();
        List<string> _namesLo = new List<string>();
        List<string> _tags = new List<string>();
        Action<IEnumerable<string>> _defaultSaveAction = list => {};
        System.Timers.Timer _autoSaveTimer = new System.Timers.Timer(10000);
        // ############################################
        // プロパティ
        // ############################################
        Func<List<string>> _getAllTags = () => new List<string>();
        public Func<List<string>> GetAllTags
        {
            get { lock (_propLock) return _getAllTags; }
            set { lock (_propLock) _getAllTags = value; }
        }
        Action<IEnumerable<string>> _saveAction = arry => {};
        public Action<IEnumerable<string>> SaveAction
        {
            get { lock (_propLock) return _saveAction; }
            set { lock (_propLock) _saveAction = value; }
        }
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
        // コンストラクタ
        // ############################################
        public MTagManager()
        {
            this.SaveAction = _defaultSaveAction;
            this.GetAllTags = () => new List<string>();
            this.IsChanged = false;
            _autoSaveTimer.Elapsed += (s, e) => Save();
        }
        // ############################################
        // メソッド
        // ############################################
        public bool Add(string pFileName, string pTag)
        {
            lock (_lock)
            {
                if (this.Exists(pFileName, pTag)){
                    return false;
                }
                _names.Add(pFileName);
                _namesLo.Add(pFileName.ToLower());
                _tags.Add(pTag);
                this.Sort();
                this.IsChanged = true;
                return true;
            }
        }
        public void Remove(string pFileName, string pTag)
        {
            lock (_lock)
            {
                var lo = pFileName.ToLower();
                for (int i = 0; i < _names.Count; i++)
                {
                    if (_namesLo[i] == lo && _tags[i] == pTag){
                        _names.RemoveAt(i);
                        _namesLo.RemoveAt(i);
                        _tags.RemoveAt(i);
                        this.IsChanged = true;
                        return;
                    }
                }
            }
        }
        public bool Exists(string pFileName, string pTag)
        {
            lock (_lock)
            {
                var lo = pFileName.ToLower();
                for (int i = 0; i < _names.Count; i++)
                {
                    if (_namesLo[i] == lo && _tags[i] == pTag){
                        return true;
                    }
                }
                return false;
            }
        }
        public List<string> GetTags(string pFileName)
        {
            lock (_lock)
            {
                var lo = pFileName.ToLower();
                var founds = new List<string>();
                for (int i = 0; i < _names.Count; i++)
                {
                    if (_namesLo[i] == lo){
                        founds.Add(_tags[i]);
                    }
                }
                return founds.Distinct().ToList();
            }
        }
        public List<string> GetNames()
        {
            lock (_lock)
            {
                return _names.Distinct().ToList();
            }
        }
        public List<string> GetNames(string pTag)
        {
            lock (_lock)
            {
                var founds = new List<string>();
                for (int i = 0; i < _names.Count; i++)
                {
                    if (_tags[i] == pTag){
                        founds.Add(_names[i]);
                    }
                }
                return founds.Distinct().ToList();
            }
        }
        public void Save(Action<List<string>> pSaveMethod)
        {
            lock (_lock)
            {
                pSaveMethod(this.ToContent());
                this.IsChanged = false;
            }
        }
        public void Save(bool pForce = false)
        {
            if (this.SaveAction == _defaultSaveAction){
                throw new Exception("SaveAction が設定されていません");
            }
            if (this.IsChanged || pForce){
                this.Save(this.SaveAction);
            }
        }
        void Sort()
        {
            lock (_lock)
            {
                List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
                for (int i = 0; i < _names.Count; i++)
                {
                    list.Add(new KeyValuePair<int, string>(i, _names[i]));
                }
                list.Sort((x, y) => x.Value.CompareTo(y.Value) * 10 + x.Key.CompareTo(y.Key));
                var names = new List<string>();
                var namesLo = new List<string>();
                var tags = new List<string>();
                foreach (var val in list)
                {
                    names.Add(_names[val.Key]);
                    namesLo.Add(_namesLo[val.Key]);
                    tags.Add(_tags[val.Key]);
                }
                _names = names;
                _namesLo = namesLo;
                _tags = tags;
            }
        }
        List<string> ToContent()
        {
            lock (_lock)
            {
                // 名前でまとめる
                var lines = new Dictionary<string, List<string>>();
                for (int i = 0; i < _names.Count; i++)
                {
                    List<string> tags;
                    if (lines.TryGetValue(_names[i], out tags)){
                        tags.Add(_tags[i]);
                    }
                    else{
                        lines.Add(_names[i], new List<string>() { _tags[i] });
                    }
                }
                // 名前でソート
                var keys = lines.Keys.OrderBy(val => val);
                // 文字列にする
                var strings = new List<string>();
                foreach (var key in keys)
                {
                    //タグには「:」「,」は禁止だが、ファイル名には「,」を含めることができるので注意
                    strings.Add(key + ":" + string.Join(",", lines[key]));
                }
                return strings;
            }
        }
        public void Read(List<string> lines)
        {
            lock (_lock)
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                foreach (var line in lines)
                {
                    var textLine = line;
                    //コメントはスキップ
                    if (textLine.StartsWith("//")){
                        continue;
                    }
                    //コロンが無いのはおかしい
                    int colonPos = textLine.IndexOf(":");
                    if (colonPos == -1){
                        continue;
                    }
                    //コロンが最初の文字なのはおかしい
                    if (colonPos == 0){
                        continue;
                    }
                    //コロンが最後の文字なのはおかしい
                    if (colonPos == textLine.Count() - 1){
                        continue;
                    }
                    //コロンが二つ以上ある場合はおかしい
                    if (textLine.IndexOf(":", colonPos + 1) != -1){
                        continue;
                    }
                    //コロンがカンマの後ろにあるのはおかしい
                    int commaPos = textLine.IndexOf(",");
                    if (commaPos != -1 && colonPos > commaPos){
                        continue;
                    }
                    var leftText = textLine.Substring(0, textLine.IndexOf(":"));
                    var rightText = textLine.Substring(textLine.IndexOf(":") + 1);
                    foreach (var val in rightText.Split(','))
                    {
                        if (val.Trim() == string.Empty){
                            continue;
                        }
                        this.Add(leftText, val.Trim());
                    }
                }
                this.IsChanged = false;
                sw.Stop();
                this.Load.Invoke(this, "MTagManager.Read() completed time : " + sw.ElapsedMilliseconds);
            }
        }
        public event EventHandler<string> Load = delegate{};
    }
}
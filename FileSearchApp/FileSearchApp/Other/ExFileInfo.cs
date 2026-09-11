using System;
using System.ComponentModel;
using System.IO;

namespace FileSearchApp
{
    public sealed class ExFileInfo
    {
        static readonly ExFileInfo _default = new ExFileInfo();
        readonly object _lock = new object();
        public string FilePath { get; private set; }
        public string FileName { get; private set; }
        public string Extension { get; private set; }
        DateTime _lastWriteTime;
        public DateTime LastWriteTime {
            get { lock (_lock) return _lastWriteTime; }
            set { lock (_lock) _lastWriteTime = value; }
        }
        decimal _size;
        public decimal Size {
            get { lock (_lock) return _size; }
            set { lock (_lock) _size = value; }
        }
        string _alias;
        public string Alias {
            get { lock (_lock) return _alias; }
            set { lock (_lock) _alias = value; }
        }
        bool _invalided;
        public bool Invalided {
            get { lock (_lock) return _invalided; }
            set { lock (_lock) _invalided = value; }
        }
        ExFileInfo()
        {
            this.FilePath = "";
            this.FileName = "";
            this.Extension = "";
            _invalided = true;
            _lastWriteTime = DateTime.MinValue;
            _size = 0;
            _alias = "";
        }
        public ExFileInfo(string pFilePath)
        {
            this.FilePath = pFilePath;
            this.FileName = Path.GetFileNameWithoutExtension(pFilePath);
            this.Extension = Path.GetExtension(FilePath);
            _lastWriteTime = DateTime.MinValue;
            _size = 0;
            _alias = "";
        }
        public string DispFileName(bool pViewAlias)
        {
            lock (_lock)
            {
                // ロックされたプロパティを呼ぶためロックしない
                if (_alias != "" && pViewAlias){
                    return _alias;
                }
            }
            return this.FileName;
        }
        public ExFileInfo Clone()
        {
            lock (_lock)
            {
                return new ExFileInfo(this.FilePath){
                    Alias = _alias,
                    Size = _size,
                    LastWriteTime = _lastWriteTime,
                    Invalided = _invalided,
                };
            }
        }
        public bool IsDefault()
        {
            return object.ReferenceEquals(this, _default);
        }
        public static ExFileInfo GetDefault()
        {
            return _default;
        }
        public static string GetFileName(string pFilePath)
        {
            return Path.GetFileNameWithoutExtension(pFilePath);
            // if (pFilePath.Length - pFilePath.LastIndexOf(".") > 5)
            // {
            //     return pFilePath.Substring(pFilePath.LastIndexOf("\\") + 1);
            // }
            // return Path.GetFileNameWithoutExtension(pFilePath);
        }
    }
}
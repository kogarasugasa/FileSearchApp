using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;

namespace FileSearchApp
{
    public partial class MHistorySearcher
    {
        object _lock = new object();
        SortedSet<string> _excludeDir = new SortedSet<string>();
        // ############################################
        // プロパティ
        // ############################################
        public Func<List<string>> GetExcludeDirectory { get; set; }
        public List<KeyWord> SearchWords { get; set; }
        public List<Action<ExFileInfo>> RunActionWhenFileFound { get; set; }
        public MHistoryManager HistoryManager { get; private set; }
        // ############################################
        // コンストラクタ、デストラクタ
        // ############################################
        public MHistorySearcher(MHistoryManager pHistory)
        {
            this.HistoryManager = pHistory;
            this.GetExcludeDirectory = () => new List<string>();
            this.SearchWords = new List<KeyWord>();
            this.RunActionWhenFileFound = new List<Action<ExFileInfo>>();
        }
        // ############################################
        // メソッド
        // ############################################
        public Task StartSearchAsync(CancellationToken ct)
        {
            var tsk = Task.Run(() =>
            {
                foreach (var info in GetResultAndRunSearch(this.SearchWords, ct))
                {
                    foreach (var method in this.RunActionWhenFileFound)
                    {
                        method(info);
                    }
                }
            });
            tsk.ConfigureAwait(false);
            return tsk;
        }
        public IEnumerable<ExFileInfo> GetResultAndRunSearch(List<KeyWord> pSearchWords, CancellationToken cancellationToken)
        {
            if (pSearchWords.Count == 0){
                yield break;
            }
            _excludeDir.Clear();
            foreach (var val in this.GetExcludeDirectory())
            {
                _excludeDir.Add(val);
            }
            foreach (var keyValue in this.HistoryManager.GetHistory())
            {
                if (cancellationToken.IsCancellationRequested){
                    yield break;
                }
                var info = new ExFileInfo(keyValue.Key);
                bool isMatch = pSearchWords.Any(val =>{
                    if (val.Fuzzy){
                        return val.MatchTo(info, MatchToExtension.Include);
                    }
                    else{
                        return val.MatchTo(info, MatchToExtension.Exclude);
                    }
                });
                if (isMatch){
                    info.Invalided = this.IsExcludeDirectory(info.FilePath);
                    yield return info;
                }
            }
        }
        private bool IsExcludeDirectory(string pFilePath)
        {
            bool exists = _excludeDir
                .AsParallel()
                .WithDegreeOfParallelism(MSettings.AvailableCPUs)
                .Any(val =>
                    pFilePath.ToLower() == val.ToLower() |
                    pFilePath.ToLower().StartsWith((val.ToLower() + "\\").TrimEnd('\\'))
                );
            return exists;
        }
    }
}
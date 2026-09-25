using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using CSharpNized.Rust.std;
using CustomFunctions;
using System.Windows.Forms;

namespace FileSearchApp
{
    public partial class MWindowsSearcher
    {
        // ############################################
        // プロパティ
        // ############################################
        static Func<List<string>> _defaultFuncListString = () => new List<string>();
        public Func<List<string>> GetIncludeDirectorys { get; set; }
        public Func<List<string>> GetExcludeDirectorys { get; set; }
        public List<KeyWord> SearchWords { get; set; }
        public List<Action<ExFileInfo>> RunActionWhenFileFound { get; set; }
        // ############################################
        // コンストラクタ
        // ############################################
        public MWindowsSearcher()
        {
            this.GetIncludeDirectorys = _defaultFuncListString;
            this.GetExcludeDirectorys = _defaultFuncListString;
            this.SearchWords = new List<KeyWord>();
            this.RunActionWhenFileFound = new List<Action<ExFileInfo>>();
        }
        // ############################################
        // メソッド
        // ############################################
        public async Task StartSearchAsync(CancellationToken pCt)
        {
            var excludePaths = this.DistinctDirectory(this.GetExcludeDirectorys());
            var includePaths = this.DistinctDirectory(this.GetIncludeDirectorys());
            var results = new ConcurrentQueue<Task<ExFileInfo>>();
            var searchTask = this.LoadBalanceSearchAsync(
                includePaths,
                this.SearchWords,
                results,
                pCt
            );
            var loopTask = Task.Run(() =>
            {
                foreach (var info in MWindowsSearcher.YieldReturnSearchResults(searchTask, results, pCt))
                {
                    if (MWindowsSearcher.IsExcludeDirectory(info.FilePath, excludePaths))
                    {
                        continue;
                    }
                    foreach (var method in this.RunActionWhenFileFound)
                    {
                        method(info);
                    }
                }
            });
            await Task.WhenAll(searchTask, loopTask);
        }
        public IEnumerable<ExFileInfo> GetExFileInfos(
        List<string> pDirPaths,
        List<KeyWord> pSearchWords,
        CancellationToken pCt)
        {
            var excludePaths = this.DistinctDirectory(this.GetExcludeDirectorys());
            var includePaths = this.DistinctDirectory(pDirPaths);
            var results = new ConcurrentQueue<Task<ExFileInfo>>();
            var searchTask = this.LoadBalanceSearchAsync(
                includePaths,
                pSearchWords,
                results,
                pCt
            );
            foreach (var info in MWindowsSearcher.YieldReturnSearchResults(searchTask, results, pCt))
            {
                if (pCt.IsCancellationRequested){
                    break;
                }
                if (MWindowsSearcher.IsExcludeDirectory(info.FilePath, excludePaths)){
                    continue;
                }
                yield return info;
            }
            searchTask.WaitByPollingLoop(200);
        }
        static IEnumerable<ExFileInfo> YieldReturnSearchResults(
        Task pSearchTask,
        ConcurrentQueue<Task<ExFileInfo>> pResult,
        CancellationToken ct)
        {
            while (true)
            {
                // キャンセルされた
                if (ct.IsCancellationRequested)
                {
                    break;
                }
                // 検索完了済かつ値取得中のタスクが無い
                if (pSearchTask.IsCompleted && pResult.Count == 0){
                    break;
                }
                if (pResult.Count == 0){
                    Task.Delay(100).Wait();
                    continue;
                }
                // キューからの取り出しに失敗
                Task<ExFileInfo> tk;
                if (!pResult.TryDequeue(out tk)){
                    Task.Delay(100).Wait();
                    continue;
                }
                // 取得が終わってない
                if (!tk.IsCompleted){
                    pResult.Enqueue(tk);
                    Task.Delay(100).Wait();
                    continue;
                }
                // パスの取得に失敗
                if (tk.Result.IsDefault()){
                    continue;
                }
                yield return tk.Result;
            }
            yield break;
        }
        async Task LoadBalanceSearchAsync(
        List<string> pDirPaths,
        List<KeyWord> pSearchWords,
        ConcurrentQueue<Task<ExFileInfo>> pResults,
        CancellationToken ct)
        {
            var runTasks = new Dictionary<string, Task>();
            var queue = new Queue<string>(pDirPaths);
            while (true)
            {
                if (ct.IsCancellationRequested){
                    queue.Clear();
                }
                // 終了した Task を削除
                if (runTasks.Count != 0){
                    var completedKeys = runTasks
                        .Where(val => val.Value.IsCompleted)
                        .Select(val => val.Key)
                        .ToList()
                    ;
                    foreach (var key in completedKeys) runTasks.Remove(key);
                }
                if (queue.Count == 0 && runTasks.Count == 0){
                    break;
                }
                // queue が無い場合は continue して runTasks が終わるのを待つ
                if (queue.Count == 0){
                    await Task.Delay(100);
                    continue;
                }
                var path = queue.Dequeue();
                var driveGroup = this.GetDriveGroupName(path);
                if (runTasks.ContainsKey(driveGroup)){
                    if (runTasks[driveGroup].IsCompleted){
                        runTasks.Remove(driveGroup);
                    }
                    else{
                        queue.Enqueue(path);
                        await Task.Delay(100);
                        continue;
                    }
                }
                runTasks.Add(driveGroup, this.SwitchSearch(path, pSearchWords, pResults, ct));
            }
        }
        async Task SwitchSearch(
        string pPath,
        List<KeyWord> pKeyWords,
        ConcurrentQueue<Task<ExFileInfo>> pResults,
        CancellationToken ct)
        {
            bool isSuccess = false;
            try
            {
                await MIndexSearcher.SplitSearchAsync(pPath, pKeyWords, pResults, ct);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                this.ErrorNotification.Invoke(this, ex.Message);
                isSuccess = false;
            }
            if (isSuccess){
                return;
            }
            try
            {
                await MWhereSearcher.SplitSearchAsync(pPath, pKeyWords, pResults, ct);
            }
            catch (Exception ex)
            {
                this.ErrorNotification.Invoke(this, ex.Message);
            }
        }
        public List<string> DistinctDirectory(IEnumerable<string> pDirs)
        {
            var dirPaths = pDirs.Select(val => val.Replace("\\", "/"));
            var lowerPaths = new List<string>();
            foreach (var path in dirPaths)
            {
                var lowers = dirPaths.Where(val => val.StartsWith(path + "/"));
                if (lowers.Count() > 0){
                    lowerPaths.AddRange(lowers);
                }
            }
            lowerPaths.Distinct();
            dirPaths.Distinct();
            var nonRepeated = new List<string>();
            foreach (var path in dirPaths)
            {
                if (lowerPaths.Contains(path)){
                    continue;
                }
                nonRepeated.Add(path.Replace("/", "\\"));
            }
            return nonRepeated;
        }
        static public bool IsExcludeDirectory(string pFilePath, IEnumerable<string> pExcludePaths)
        {
            return pExcludePaths
                .AsParallel()
                .WithDegreeOfParallelism(MSettings.AvailableCPUs)
                .Any(val =>
                    pFilePath.ToLower() == val.ToLower() ||
                    pFilePath.ToLower().StartsWith(val.ToLower() + @"\")
                )
            ;
        }
        public string GetDriveGroupName(string pDirectoryPath)
        {
            if (ConvertToNetworkPath(pDirectoryPath).IsNone()){
                return pDirectoryPath.Substring(0, 1).ToUpper();
            }
            else{
                return "Network";
            }
        }
        /// <summary>
        /// パスがネットワーク上を示している場合「\\MyPC」の形式でパスを返す
        /// </summary>
        public IOption<string> ConvertToNetworkPath(string pDirectoryPath)
        {
            //フォルダパス変換
            if (pDirectoryPath.Length == 0){
                return new None<string>();
            }
            var firstChar = pDirectoryPath.Substring(0 ,1);
            if (firstChar == @"\"){
                return new Some<string>(pDirectoryPath);
            }
            else{
                var driveInfo = new DriveInfo(firstChar);
                if (driveInfo.DriveType == DriveType.Network){
                    var driveNetworkPath = MyNativeMethod.Function.Drive.GetUniversalName(driveInfo.Name);
                    // 「Z:\Share」to「\\MyPC\Share」
                    return new Some<string>(driveNetworkPath + pDirectoryPath.Substring(2));
                }
            }
            return new None<string>();
        }
        public event EventHandler<string> IndexSearchStarted = delegate{};
        public event EventHandler<string> WhereSearchStarted = delegate{};
        public event EventHandler<string> ErrorNotification = delegate{};
    }
}
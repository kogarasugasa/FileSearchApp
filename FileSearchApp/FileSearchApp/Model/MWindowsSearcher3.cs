using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FileSearchApp
{
    public partial class MWindowsSearcher
    {
        public class MWhereSearcher
        {
            // ############################################
            // プロパティ
            // ############################################
            // ############################################
            // コンストラクタ
            // ############################################
            // ############################################
            // メソッド
            // ############################################
            public static async Task SplitSearchAsync(
            string pDirPath,
            List<KeyWord> pSearchWords,
            ConcurrentQueue<Task<ExFileInfo>> pResult,
            CancellationToken ct)
            {
                await Task.Run(async () =>
                {
                    var keyWords = pSearchWords.Select(val => {
                        return new KeyWord { Word = val.Word, Fuzzy = val.Fuzzy };
                    });
                    foreach (var psInfo in CreateWhereStartInfo(pDirPath, keyWords))
                    {
                        if (ct.IsCancellationRequested){
                            return;
                        }
                        await MWhereSearcher.WhereSearchAsync(psInfo, pResult, ct);
                    }
                });
            }
            static async Task WhereSearchAsync(
            ProcessStartInfo info,
            ConcurrentQueue<Task<ExFileInfo>> pResult,
            CancellationToken ct)
            {
CustomLogger.WriteLine(info.Arguments);
                if (ct.IsCancellationRequested){
                    return;
                }
                var cts = new CancellationTokenSource();
                var process = Process.Start(info);
                try
                {
                    // where を停止するため Token を監視するタスクを起動しておく
                    var task = WhereKiller(process.Id, cts.Token, ct);
                    // データ読み取り
                    bool isExitRequsted = false;
                    while (true)
                    {
                        if (ct.IsCancellationRequested){
                            break;
                        }
                        var tk = MWhereSearcher.ReadAsyncStandardOutputReader(process);
                        pResult.Enqueue(tk);
                        var fileInfo = await tk;
                        if (fileInfo.IsDefault()){
                            if (isExitRequsted){
                                break;
                            }
                            // ReadLine の瞬間から HasExited までの間に結果が出力されている可能性があるので
                            // フラグだけ立ててループを再開する
                            if (process.HasExited){
                                isExitRequsted = true;
                                continue;
                            }
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    cts.Cancel();
                    cts.Dispose();
                    process.Dispose();
                }
            }
            static async Task<ExFileInfo> ReadAsyncStandardOutputReader(Process pProcess)
            {
                object buf = await pProcess.StandardOutput.ReadLineAsync();
                if (buf == null){
                    return ExFileInfo.GetDefault();
                }
                return WhereResultToExFileInfo((string)buf);
            }
            static List<ProcessStartInfo> CreateWhereStartInfo(string pDirPath, IEnumerable<KeyWord> pSearchWords)
            {
                var searchWords = pSearchWords
                    .Where(val => val.Word.Trim() != string.Empty)
                    .Select(val => new KeyWord { Word = val.Word, Fuzzy = val.Fuzzy })
                ;
                if (searchWords.Count() == 0){
                    throw new Exception("where クエリのコマンドの作成に失敗しました");
                }
                //「*」を先頭と末尾に付加し、「*」または「?」が連続した場合は1つにまとめる
                var fuzzyWords = searchWords
                    .Where(key => key.Fuzzy)
                    .Select(key => key.Word)
                ;
                var checkedSearchWords = new List<string>();
                foreach (var word in fuzzyWords)
                {
                    string word2 = word.Insert(0, "*") + "*";
                    string beforeChar = string.Empty;
                    string chars = string.Empty;
                    for (int i = 0; i <= word2.Length - 1; i++)
                    {
                        string oneChar = word2.Substring(i, 1);
                        if (oneChar == "*" && oneChar == beforeChar) { continue; }
                        chars += oneChar;
                        beforeChar = oneChar;
                    }
                    checkedSearchWords.Add(chars);
                }
                // 完全一致のファイルは末尾に拡張子の始まりである「.」を付加する
                var matchWords = searchWords
                    .Where(key => !key.Fuzzy)
                    .Select(key => key.Word + ".*")
                ;
                checkedSearchWords.AddRange(matchWords);
                //コマンドプロンプトの where コマンドを使用してファイルを検索する
                string searchWordsParams = string.Join("\" \"", checkedSearchWords);
                // / を \ に変えて、末尾に \ があったら消す 
                string argDir = pDirPath.Replace("/","\\").TrimEnd('\\');
                ProcessStartInfo psInfo = new ProcessStartInfo
                {
                    FileName = System.Environment.GetEnvironmentVariable("ComSpec"),
                    Arguments = " /c \"chcp 65001 >nul | where /t /r \"" + argDir + "\" \"" + searchWordsParams + "\"\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                };
                return new List<ProcessStartInfo>{ psInfo };
            }
            static ExFileInfo WhereResultToExFileInfo(string pWhereResult)
            {
                var strAry = pWhereResult.Trim(' ').Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                int parseCount = 0;
                long fileSize = 0;
                if (long.TryParse(strAry[0], out fileSize)){
                    parseCount++;
                    // Where コマンドで出力されたサイズはオーバーフローしている値が存在するため
                    // オーバーフロー分を計算する 計算したとしても4GB以上は判別できない
                    if (fileSize < 0){
                        fileSize += 4294967296; //4GB
                    }
                }
                else if (strAry[0] == "?"){
                    fileSize = 0;
                    parseCount++;
                }
                DateTime lastWriteDate = DateTime.MinValue;
                if (DateTime.TryParse(strAry[1], out lastWriteDate)){
                    parseCount++;
                }
                else if (strAry[1] == "?"){
                    lastWriteDate = DateTime.MinValue;
                    parseCount++;
                }
                TimeSpan lastWriteSpan = TimeSpan.MinValue;
                if (TimeSpan.TryParse(strAry[2], out lastWriteSpan)){
                    parseCount++;
                }
                else if (strAry[2] == "?"){
                    lastWriteSpan = TimeSpan.MinValue;
                    parseCount++;
                }
                if (parseCount != 3){
                    return new ExFileInfo(pWhereResult);
                }
                var net = pWhereResult.IndexOf(@"\\"); // \\MyPC\Share
                var lcl = pWhereResult.IndexOf(@":\"); // C:\Share
                string filePath;
                if (net == -1 && lcl != -1){
                    filePath = pWhereResult.Substring(lcl - 1);
                }
                else if (net != -1 && lcl == -1){
                    filePath = pWhereResult.Substring(net);
                }
                else{
                    return new ExFileInfo(pWhereResult);
                }
                if (filePath.Replace(" ", "").Replace("　", "").Length != 0){
                    filePath = filePath.TrimStart();
                }
                var info = new ExFileInfo(filePath){
                    Size = fileSize,
                };
                if (lastWriteDate == DateTime.MinValue && lastWriteSpan == TimeSpan.MinValue){
                    info.LastWriteTime = lastWriteDate;
                }
                else{
                    info.LastWriteTime = lastWriteDate + lastWriteSpan;
                }
                return info;
            }
            static async Task WhereKiller(int pId, CancellationToken pEndToken, CancellationToken pCancelToken)
            {
                while (true)
                {
                    // Where が終了していたらここを実行
                    if (pEndToken.IsCancellationRequested){
                        return;
                    }
                    // キャンセルされてなければループを続ける
                    if (!pCancelToken.IsCancellationRequested){
                        await Task.Delay(100);
                        continue;
                    }
                    int pid = pId;
                    var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                    var taskkill = System.IO.Path.Combine(systemDir, "taskkill.exe");
                    using (var procKiller = new System.Diagnostics.Process())
                    {
                        procKiller.StartInfo.FileName = taskkill;
                        procKiller.StartInfo.Arguments = string.Format("/PID {0} /T /F", pid);
                        procKiller.StartInfo.CreateNoWindow = true;
                        procKiller.StartInfo.UseShellExecute = false;
                        procKiller.Start();
                        procKiller.WaitForExit();
                    }
                    break;
                }
            }
        }
    }
}
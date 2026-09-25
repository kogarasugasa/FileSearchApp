using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using CSharpNized.Rust.std;
using System.Threading;

namespace FileSearchApp
{
    public class MPowerShellSearcher
    {
        readonly static object _lock = new object();
        static bool _isSearching = false;
        static readonly string _csvPath = Path.GetTempPath() + "FileSearchApp_PowerShellSearch_all.csv";
        static readonly string _csvResultPath = Path.GetTempPath() + "FileSearchApp_PowerShellSearch_result.csv";
        static readonly string _tmpScriptPath = Path.GetTempPath() + "FileSearchApp_PowerShellSearch_script.ps1";
        CancellationTokenSource _waitMsgCts = new CancellationTokenSource();
        // ======+=========+=========+=========+=========+=========+=========+=========+
        // = プロパティ
        // ======+=========+=========+=========+=========+=========+=========+=========+
        public string SettingsDirectoryPath { get; private set; }
        public IOption<MHistoryManager> HistoryManager { get; set; }
        public List<Action<ExFileInfo>> RunActionWhenFileFound { get; set; }
        // ======+=========+=========+=========+=========+=========+=========+=========+
        // = コンストラクタ、デストラクタ
        // ======+=========+=========+=========+=========+=========+=========+=========+
        public MPowerShellSearcher(string pSettingsDirectoryPath)
        {
            this.SettingsDirectoryPath = pSettingsDirectoryPath;
            this.HistoryManager = new None<MHistoryManager>();
            this.RunActionWhenFileFound = new List<Action<ExFileInfo>>();
        }
        ~MPowerShellSearcher()
        {
            _waitMsgCts.Dispose();
            try
            {
                if (File.Exists(_csvPath)){
                    File.Delete(_csvPath);
                }
            }
            finally {}
            try
            {
                if (File.Exists(_csvResultPath)){
                    File.Delete(_csvResultPath);
                }
            }
            finally {}
            try
            {
                if (File.Exists(_tmpScriptPath)){
                    File.Delete(_tmpScriptPath);
                }
            }
            finally {}
        }
        // ======+=========+=========+=========+=========+=========+=========+=========+
        // = メソッド
        // ======+=========+=========+=========+=========+=========+=========+=========+
        /// <summary>
        /// 検索を開始する
        /// </summary>
        public async Task StartSearchAsync(CancellationToken ct)
        {
            lock (_lock)
            {
                if (_isSearching){
                    MMessageBox.Show("別のPowerShell検索が実行中です");
                    return;
                }
                _isSearching = true;
            }
            try
            {
                var exportTask = this.ExportCsvAsync(ct);
                var openPath = this.GetScript();
                if (!File.Exists(openPath)){
                    return;
                }
                // 履歴出力待ちのメッセージを表示
                _waitMsgCts.Dispose();
                _waitMsgCts = new CancellationTokenSource();
                var waitMsgTask = MMessageBox.ShowAsync("履歴を出力中です...", _waitMsgCts.Token);
                // 履歴出力待機
                await exportTask;
                _waitMsgCts.Cancel();
                await this.OpenPowerShellWaitForExitAsync(openPath, ct);
                // 開いているファイルが新規作成の物ならユーザーの同意を得て保存する
                var saveScriptTask = Task.Run(async () =>
                {
                    if (openPath != _tmpScriptPath){
                        return;
                    }
                    var saveFileName = await this.GetSaveFileNameAsync();
                    if (saveFileName != string.Empty){
                        this.SaveScript(openPath, saveFileName);
                    }
                });
                
                // 結果をテキストから取得
                var paths = await this.GetResultAsync();
                // 結果を出力
                var funcs = new List<Action<ExFileInfo>>(this.RunActionWhenFileFound);
                await Task.Run(() =>
                {
                    foreach (var path in paths)
                    {
                        if (ct.IsCancellationRequested){
                            break;
                        }
                        var info = new ExFileInfo(path);
                        foreach (var method in funcs)
                        {
                            method(info);
                        }
                    }
                });
                // スクリプトを保存する
                await saveScriptTask;
                // 終了処理
                try
                {
                    if (File.Exists(_csvResultPath)) File.Delete(_csvResultPath);
                    if (File.Exists(_csvPath)) File.Delete(_csvPath);
                    if (File.Exists(_tmpScriptPath)) File.Delete(_tmpScriptPath);
                }
                catch (Exception ex)
                {
                    MMessageBox.Show(ex.Message);
                    return;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                lock (_lock) _isSearching = false;
            }
        }
        /// <summary>
        /// PowerShellで指定のスクリプトを開きPowerShellを終了するまで待機する
        /// </summary>
        async Task OpenPowerShellWaitForExitAsync(string pScriptPath, CancellationToken ct)
        {
            // "C:\WINDOWS\system32\WindowsPowerShell\v1.0\powershell_ise.exe"
            var ps = new ProcessStartInfo(){
                FileName = "powershell_ise.exe",
                Arguments = pScriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            await Task.Run(() =>
            {
                using (var p = new Process())
                {
                    p.StartInfo = ps;
                    p.Start();
                    var tk = Task.Run(async () => {
                        while (true)
                        {
                            await Task.Delay(100);
                            if (ct.IsCancellationRequested){
                                p.Kill();
                                return;
                            }
                        }
                    });
                    p.WaitForExit();
                }
            });
        }
        /// <summary>
        /// スクリプトのファイル名を入力するプロンプトを表示し、入力内容を取得する
        /// </summary>
        Task<string> GetSaveFileNameAsync()
        {
            return Task.Run(() =>
            {
                var scriptSaveMsg = "スクリプトを保存する場合は名前を入力してください";
                while (true)
                {
                    var inputResult = MInputBox.GetText(scriptSaveMsg);
                    if (inputResult.IsNone()){
                        return string.Empty;
                    }
                    var errMsg = this.GetCehckFileNameErrorMsg(inputResult.GetOrDefault(""));
                    if (errMsg != string.Empty){
                        MMessageBox.Show(errMsg);
                        continue;
                    }
                    return inputResult.GetOrDefault("");
                }
            });
        }
        /// <summary>
        /// 指定したスクリプトのパスを設定されているパスに指定の名前 + .ps1 で保存する
        /// </summary>
        void SaveScript(string pScriptPath, string pScriptName)
        {
            var scriptName = pScriptName;
            if (!scriptName.ToLower().EndsWith(".ps1")){
                scriptName += ".ps1";
            }
            var newScriptPath = this.SettingsDirectoryPath + "\\" + scriptName;
            try
            {
                File.Copy(pScriptPath, newScriptPath);
            }
            catch (Exception ex)
            {
                MMessageBox.Show(ex.Message);
                return;
            }
            return;
        }
        /// <summary>
        /// ファイル名をチェックしてエラーメッセージを取得する
        /// </summary>
        string GetCehckFileNameErrorMsg(string pFileName)
        {
            // スペースしか入力されていない場合再度入力
            if (pFileName.Replace("　", "").Trim(' ').Length == 0){
                return "スペースのみ、または空欄で登録できません";
                
            }
            // 使用できない文字が入力されている場合再度入力
            if (pFileName.Any(val => Path.GetInvalidFileNameChars().Contains(val))){
                return "ファイル名に使用できない文字が使用されています";
            }
            var names = new List<string>();
            foreach (var path in Directory.GetFiles(this.SettingsDirectoryPath)){
                var pos = path.LastIndexOf("\\");
                if (pos < 0){
                    continue;
                }
                if (path.Length <= pos){
                    continue;
                }
                names.Add(path.Substring(pos + 1));
            }
            if (pFileName.ToLower().EndsWith(".ps1")){
                if (names.Select(val => val.ToLower()).Contains(pFileName.ToLower())){
                    return "既に使用されている名前です";
                }
            }
            else {
                if (names.Contains(pFileName.ToLower() + ".ps1")){
                    return "既に使用されている名前です";
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// 履歴をカンマ区切り、文字囲い=""のCSVで保存する
        /// </summary>
        async Task ExportCsvAsync(CancellationToken ct)
        {
            var infos = await this.GetHistoryAsync();
            try
            {
                using (var writer = new StreamWriter(_csvPath, false, System.Text.Encoding.UTF8))
                {
                    await writer.WriteLineAsync("FileName,Extension,FilePath");
                    foreach(var info in infos)
                    {
                        if (ct.IsCancellationRequested){
                            break;
                        }
                        await writer.WriteLineAsync(
                            "\"" + info.FileName + "\""
                            + ",\"" +  info.Extension + "\""
                            + ",\"" + info.FilePath + "\""
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                CustomLogger.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// 結果のテキストファイルの内容を1行1項目として配列で返す
        /// </summary>
        async Task<List<string>> GetResultAsync()
        {
            var paths = new List<string>();
            if (!File.Exists(_csvResultPath)){
                CustomLogger.WriteLine(_csvResultPath + " が見つかりませんでした");
                return new List<string>();
            }
            try
            {
                using (var reader = new StreamReader(_csvResultPath))
                {
                    while (!reader.EndOfStream)
                    {
                        paths.Add(await reader.ReadLineAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                MMessageBox.Show(ex.Message);
            }
            return paths;
        }
        /// <summary>
        /// 履歴の全件を ExFileInfo にして配列で返す
        /// </summary>
        async Task<List<ExFileInfo>> GetHistoryAsync()
        {
            return await Task.Run(() => 
            {
                var infos = this.HistoryManager.Match(
                    none => new List<ExFileInfo>(),
                    some => {
                        var historyInfos = some
                            .GetHistory()
                            .Select(val => new ExFileInfo(val.Key))
                            .ToList()
                        ;
                        return historyInfos;
                    }
                );
                return infos;
            });
        }
        /// <summary>
        /// スクリプトをユーザーに選択させて、パスを取得する
        /// 選択できるスクリプトはプロパティのフォルダの.ps1 のファイルと自動で新規作成されたファイル
        /// </summary>
        string GetScript()
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(this.SettingsDirectoryPath);
            }
            catch (Exception ex)
            {
                MMessageBox.Show(ex.Message);
                return string.Empty;
            }
            var names = files
                .Where(val => val.ToLower().EndsWith(".ps1"))
                .Select(val => Path.GetFileName(val));
            var options = new List<string>(){
                "新規作成"
            };
            options.AddRange(names);
            var selectionName = string.Empty;
            while (true)
            {
                var selection = MInputBoxChoose.GetItems(
                    options,
                    new List<string>(),
                    "スクリプトを選択してください",
                    350
                );
                var cnt = selection.Match(
                    none => -1,
                    some => {
                        if (some.Count != 0){
                            selectionName = some.First();
                        }
                        return some.Count;
                    }
                );
                if (cnt == -1){
                    return string.Empty;
                }
                if (cnt != 1){
                    MMessageBox.Show("1つだけ選択してください");
                    continue;
                }
                break;
            }
            var selectionFile = files.Where(val => val.EndsWith(selectionName));
            if (selectionFile.Count() != 0){
                return selectionFile.First();
            }
            var enc = System.Text.Encoding.GetEncoding("Shift-jis");
            try
            {
                using (var writer = new StreamWriter(_tmpScriptPath, false, enc))
                {
                    writer.Write(this.GetNewScriptSource());
                }
            }
            catch (Exception ex)
            {
                MMessageBox.Show(ex.Message);
                return string.Empty;
            }
            return _tmpScriptPath;
        }
        /// <summary>
        /// スクリプトを新規作成するときの標準の内容を取得する
        /// </summary>
        string GetNewScriptSource()
        {
            var script = new List<string>{
                "# 履歴の全件が $historyに含まれています",
                "$history = Import-Csv -Path " + "'" + _csvPath + "'" + " -Encoding UTF8",
                "# $filterdPathsが検索結果として FileSearchApp に渡されます",
                "$filterdPaths = New-Object 'System.Collections.Generic.List[string]'",
                "#",
                "#",
                "# これより下を編集して履歴を絞り込んでください。CSV項目「FileName Extension FilePath」",
                "# このまま実行するとファイル名が￥の重複しているファイルを出力します",
                "",
                "",
                "$history = $history | Sort-Object FileName",
                "$isFirstAdd = $true",
                "for ($index = 0; $index -lt ($history.Length - 1); $index++){",
                "    $fileName = $history[$index].FileName",
                "    $extension = $history[$index].Extension",
                "    $filePath = $history[$index].FilePath",
                "    if ($fileName.Equals('')){",
                "        continue",
                "    }",
                "    if ($fileName.Equals($fileName)){",
                "        $filterdPaths.Add($filePath)",
                "        if ($isFirstAdd){",
                "            $filterdPaths.Add($filePath)",
                "            $isFirstAdd = $false",
                "        }",
                "    }",
                "    else {",
                "        $isFirstAdd = $true",
                "    }",
                "}",
                "",
                "",
                "# これより上を編集して履歴を絞り込んでください",
                "#",
                "#",
                "echo $filterdPaths > " + "'" + _csvResultPath + "'",
            };
            return string.Join(Environment.NewLine, script);
        }
    }
}

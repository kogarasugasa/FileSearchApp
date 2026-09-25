using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace FileSearchApp
{
    public class MSettings : IDisposable
    {
        static MSettings _this = new MSettings();
        bool _disposed = false;
        string _searchDirFilePath;
        string _columnWidthFilePath;
        string _windowSizeFilePath;
        string _tagMSFilePath;
        string _fileTagsFilePath;
        string _historyFilePath;
        string _aliasFilePath;
        string _excludeDirectoryFilePath;
        string _readmeFilePath;
        string _contextMenuPath;
        string _searchScriptPath;
        string _candidatePath;
        string _continueTabDetailPath;
        string _lockControlPath;
        static string _curDir = string.Empty;
        readonly string _tempFilePath;
        readonly string _settingsDirFilePath = Application.StartupPath + @"\FileSearchApp.ini";
        public static readonly int AvailableCPUs = Environment.ProcessorCount / 4 + 1;
        readonly string _lockInfo = string.Join(",", new string[]
        {
            Environment.UserName,
            DateTime.Now.ToString("yyyyMMddHHmmss"),
            Environment.MachineName,
            Process.GetCurrentProcess().Id.ToString(),
        });
        MSettings()
        {
            //設定から読み込む
            if (TryGetSettingsDir(out _curDir))
            {
                if (!Directory.Exists(_curDir))
                {
                    throw new Exception("設定ファイルが保存されているパスが存在しません : " + _curDir);
                }
            }
            else
            {
                _curDir = Application.StartupPath + @"\settings";
            }
            if (!Directory.Exists(_curDir))
            {
                Directory.CreateDirectory(_curDir);
            }
            _searchDirFilePath = _curDir + @"\SettingsSearchDirectorys.ini";
            _columnWidthFilePath = _curDir + @"\SettingsColumnWidth.ini";
            _windowSizeFilePath = _curDir + @"\SettingsWindowSize.ini";
            _tagMSFilePath = _curDir + @"\SettingsTags.ini";
            _fileTagsFilePath = _curDir + @"\SettingsTagAllocation.txt";
            _historyFilePath = _curDir + @"\SettingsHistory.log.gz";
            _aliasFilePath = _curDir + @"\SettingsAlias.ini";
            _excludeDirectoryFilePath = _curDir + @"\SettingsExcludeDirectorys.ini";
            _readmeFilePath = _curDir + @"\Readme.txt";
            _contextMenuPath = _curDir + @"\SettingsContextMenu.ini";
            _searchScriptPath = _curDir + @"\SearchScript";
            _candidatePath = _curDir + @"\SettingsCandidate.ini";
            _continueTabDetailPath = _curDir + @"\Tab";
            _lockControlPath = _curDir + @"\LockApp.lock"; this.TryGetLock(_lockControlPath, false);
            _tempFilePath = Application.StartupPath + @"\DoDeleteThisFile.txt";
        }
        ~MSettings()
        {
            this.Dispose();
        }
        public void Dispose()
        {
            this.Dispose(true);
        }
        public void Dispose(bool disposing)
        {
            if (_disposed){
                return;
            }
            if (disposing){
                this.ErrorNotification = delegate{};
                this.BackupFileFound = delegate{};
                this.LockReleased = delegate{};
                if (IsReadOnly()){
                    try
                    {
                        File.Delete(_tempFilePath);
                    }
                    finally{}
                }
                else{
                    try
                    {
                        File.Delete(_tempFilePath);
                    }
                    finally{}
                    try
                    {
                        File.Delete(_lockControlPath);
                    }
                    finally{}
                }
            }
            _disposed = true;
        }
        public static MSettings GetInstance()
        {
            return _this;
        }
        /// <summary>
        /// ロックの取得を試みます true の場合強制的にロックを取得しようとします
        /// </summary>
        /// <returns></returns>
        void TryGetLock(string pLockControlPathbool, bool pForce)
        {
            if (pLockControlPathbool == ""){
                throw new Exception("_lockControlPath は string.Empty です");
            }
            if (_lockInfo == ""){
                throw new Exception("_lockInfo は string.Empty です");
            }
            try
            {
                var force = pForce;
                var mode = FileMode.OpenOrCreate;
                var access = FileAccess.ReadWrite;
                using (var stream = File.Open(pLockControlPathbool, mode, access))
                {
                    // 既存のロック情報を取得する
                    string streamLockInfo = "";
                    var reader = new StreamReader(stream);
                    {
                        if (!reader.EndOfStream){
                            streamLockInfo = reader.ReadLine();
                        }
                    }
                    // 取得結果が空の場合強制モードを解除する
                    if (streamLockInfo == ""){
                        force = false;
                    }
                    var args = new LockGettingEventArgs(streamLockInfo, force);
                    this.LockAcquiring.Invoke(this, args);
                    if (args.Cancel){
                        return;
                    }
                    // 取得が強制の場合内容をクリアする
                    if (force){
                        stream.SetLength(0);
                    }
                    // ロック情報を書き込む
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine(_lockInfo);
                    }
                }
            }
            catch
            {
                this.ErrorNotification.Invoke(this, "ロックの取得に失敗しました");
            }
        }
        public void ChangeToReadOnly()
        {
            if (this.IsReadOnly()){
                this.LockReleased.Invoke(this, new EventArgs());
                return;
            }
            try
            {
                File.Delete(_lockControlPath);
                this.LockReleased.Invoke(this, new EventArgs());
            }
            catch
            {
                this.ErrorNotification.Invoke(this, "読取専用に変更出来ませんでした");
            }
        }
        public void ChangeToWritable()
        {
            if (!this.IsReadOnly()){
                this.LockAcquired.Invoke(this, new EventArgs());
                return;
            }
            this.TryGetLock(_lockControlPath, true);
            if (!this.IsReadOnly()){
                this.LockAcquired.Invoke(this, new EventArgs());
            }
        }
        public bool IsReadOnly()
        {
            try
            {
                if (_disposed){
                    return true;
                }
                if (File.Exists(_lockControlPath)){
                    using (var reader = new StreamReader(_lockControlPath))
                    {
                        return reader.ReadLine() != _lockInfo;
                    }
                }
                else{
                    return true;
                }
            }
            catch
            {
                return true;
            }
        }
        public Dictionary<string, IEnumerable<string>> ReadContinueTabDetail()
        {
            //var _continueTabDetailPath = @"c:\users\kobayashiha\desktop\test";
            var data = new Dictionary<string, IEnumerable<string>>();
            try
            {
                if (!Directory.Exists(_continueTabDetailPath)){
                    return data;
                }
                var tabs = Directory.GetFiles(_continueTabDetailPath);
                // 数値に変換出来るタブを追加する
                foreach (var tab in tabs)
                {
                    var name = Path.GetFileNameWithoutExtension(tab);
                    var paths = this.ReadSettings(tab);
                    data.Add(name, paths);
                }
                // 数値に変換出来なかったファイル名を改めて採番して追加する
            }
            catch (Exception ex)
            {
                this.ErrorNotification.Invoke(this, ex.Message);
            }
            return data;
        }
        public void WriteContinueTabDetail(Dictionary<string, IEnumerable<string>> pData)
        {
            //var _continueTabDetailPath = @"c:\users\kobayashiha\desktop\test";
            if (this.IsReadOnly())
            {
                return;
            }
            try
            {
                if (!Directory.Exists(_continueTabDetailPath))
                {
                    Directory.CreateDirectory(_continueTabDetailPath);
                }
                // 既存を削除する
                var paths = Directory.GetFiles(_continueTabDetailPath);
                foreach (var path in paths) File.Delete(path);
                // Dictonary.Key をファイル名にして保存する
                foreach (var key in pData.Keys)
                {
                    var path = _continueTabDetailPath + "\\" + key.ToString() + ".ini";
                    this.WriteSettings(path, pData[key]);
                }
            }
            catch (Exception ex)
            {
                this.ErrorNotification.Invoke(this, ex.Message);
            }
        }
        public List<string> ReadCandidateSettings()
        {
            return this.ReadSettings(_candidatePath);
        }
        public void OpenCandidateSettings()
        {
            if (!File.Exists(_candidatePath)){
                var content = new List<string>{
                    "//検索候補の設定",
                };
                this.WriteSettings(_candidatePath, content);
            }
            if (!File.Exists(_candidatePath)){
                this.ErrorNotification.Invoke(this, "ファイルが存在しないかアクセス出来ません");
                return;
            }
            string path;
            if (this.IsReadOnly()){
                this.CreateTempSettingsFile(_candidatePath, _tempFilePath);
                path = _tempFilePath;
            }
            else{
                path = _candidatePath;
            }
            using (var pro = new Process())
            {
                var psInfo = new ProcessStartInfo{
                    FileName = "notepad.exe",
                    Arguments = path,
                    UseShellExecute = true
                };
                pro.StartInfo = psInfo;
                pro.Start();
            }
        }
        public void WriteAlias(IEnumerable<string> pAliases)
        {
            WriteSettings(_aliasFilePath, pAliases);
        }
        public List<string> GetAlias()
        {
            return ReadSettings(_aliasFilePath);
        }
        public void WriteContextMenu(IEnumerable<string> pContextMenus)
        {
            WriteSettings(_contextMenuPath, pContextMenus);
        }
        public List<string> ReadContextMenu()
        {
            if (!File.Exists(_contextMenuPath)){
                var setting = new List<string>(){
                    "// 範囲開始番号 | 範囲終了番号 | 表示 | 表示優先順 | 拡張子 | プログラム | 引数先頭 | 引数末尾",
                    "0|1|フォルダを開く|0|*|explorer.exe| /select,\"|\"",
                    "2|9|Excel|1|*.xls/*.xlsx/*.xlsm|||",
                    "3|4|Excel(読取専用)|1|*.xls/*.xlsx/*.xlsm|\"C:\\Program Files\\Microsoft Office\\root\\Office16\\EXCEL.EXE\"| /r \"|\"",
                    "5|6|Excel(別プロセス)|2|*.xls/*.xlsx/*.xlsm|\"C:\\Program Files\\Microsoft Office\\root\\Office16\\EXCEL.EXE\"| /x \"|\"",
                    "7|8|Excel(読取専用別プロセス)|3|*.xls/*.xlsx/*.xlsm|\"C:\\Program Files\\Microsoft Office\\root\\Office16\\EXCEL.EXE\"| /x /r \"|\"",
                    "10|11|削除|0|*|DeleteApp.exe|\"|\"",
                    "",
                    "// このデータは入れ子集合モデルによって親子関係を表現しています",
                    "// また、表示優先順は使用されていません",
                };
                WriteSettings(_contextMenuPath, setting);
            }
            return ReadSettings(_contextMenuPath);
        }
        public void WriteSearchResultHistory(IEnumerable<string> pFilePaths)
        {
            WriteSettingsCompress(_historyFilePath, pFilePaths);
        }
        public List<string> GetSearchResultHistory()
        {
            return ReadSettingsDeCompress(_historyFilePath, 100000);
        }
        public void WriteFileNameTag(IEnumerable<string> list)
        {
            WriteSettings(_fileTagsFilePath, list);
        }
        public List<string> GetFileNameTags()
        {
            return this.ReadSettings(_fileTagsFilePath);
        }
        public void TryCreateTagMS()
        {
            if (File.Exists(_tagMSFilePath)){
                return;
            }
            var line = new List<string>(){
                "// 「,:」の2文字は使用できません",
                "// 「end:」を入力するとそれ以降は読み込まれません",
                "",
                "",
                "end:",
            };
            WriteSettings(_tagMSFilePath, line);
        }
        public List<string> GetExcludeDirectory()
        {
            return this.ReadSettings(_excludeDirectoryFilePath);
        }
        public List<string> GetTagMS()
        {
            var tags = ReadSettings(_tagMSFilePath);
            var filtered = tags
                .Where(val => !val.Contains(":"))
                .Where(val => !val.Contains(","));
            if (tags.Count != filtered.Count()){
                var msg = "タグに「:」「,」が含まれている行は除外して開始します";
                this.ErrorNotification.Invoke(this, msg);
            }
            return tags.ToList();
        }
        public void OpenTagSettings()
        {
            if (!File.Exists(_tagMSFilePath)){
                TryCreateTagMS();
            }
            if (!File.Exists(_tagMSFilePath)){
                this.ErrorNotification.Invoke(this, "ファイルが存在しないかアクセスできません");
                return;
            }
            string path;
            if (IsReadOnly()){
                this.CreateTempSettingsFile(_tagMSFilePath, _tempFilePath);
                path = _tempFilePath;
            }
            else{
                path = _tagMSFilePath;
            }
            using (var pro = new Process())
            {
                var psInfo = new ProcessStartInfo{
                    FileName = "notepad.exe",
                    Arguments = path,
                    UseShellExecute = true
                };
                pro.StartInfo = psInfo;
                pro.Start();
            }
        }
        public void OpenDirectorySettings()
        {
            if(!File.Exists(_searchDirFilePath)){
                TryCreateSearchDirectorys();
            }
            if (!File.Exists(_tagMSFilePath)){
                this.ErrorNotification.Invoke(this, "ファイルが存在しないかアクセスできません");
                return;
            }
            string path;
            if (IsReadOnly()){
                this.CreateTempSettingsFile(_searchDirFilePath, _tempFilePath);
                path = _tempFilePath;
            }
            else{
                path = _searchDirFilePath;
            }
            using (var pro = new Process())
            {
                var psInfo = new ProcessStartInfo{
                    FileName = "notepad.exe",
                    Arguments = path,
                    UseShellExecute = true
                };
                pro.StartInfo = psInfo;
                pro.Start();
            }
        }
        public void OpenExcludeDirectorySettings()
        {
            if(!File.Exists(_excludeDirectoryFilePath)){
                TryCreateExcludeDirectory();
            }
            if (!File.Exists(_tagMSFilePath)){
                this.ErrorNotification.Invoke(this, "ファイルが存在しないかアクセスできません");
                return;
            }
            string path;
            if (IsReadOnly()){
                this.CreateTempSettingsFile(_excludeDirectoryFilePath, _tempFilePath);
                path = _tempFilePath;
            }
            else{
                path = _excludeDirectoryFilePath;
            }
            using (var pro = new Process())
            {
                var psInfo = new ProcessStartInfo{
                    FileName = "notepad.exe",
                    Arguments = path,
                    UseShellExecute = true
                };
                pro.StartInfo = psInfo;
                pro.Start();
            }
        }
        public void OpenReadme()
        {
            if(!File.Exists(_readmeFilePath)){
                return;
            }
            using (var pro = new Process())
            {
                var psInfo = new ProcessStartInfo{
                    FileName = _readmeFilePath,
                    UseShellExecute = true
                };
                pro.StartInfo = psInfo;
                pro.Start();
            }
        }
        public string GetScriptDirectoryPath()
        {
            if (Directory.Exists(_searchScriptPath)){
                return _searchScriptPath;
            }
            else if (File.Exists(_searchScriptPath)){
                var msg = "ファイル名 : " + _searchScriptPath + " が存在するため設定を作成できませんでした"
                    + Environment.NewLine + "スクリプトの設定を読み込まずに続行します";
                this.ErrorNotification.Invoke(this, msg);
                return string.Empty;
            }
            else {
                Directory.CreateDirectory(_searchScriptPath);
                return _searchScriptPath;
            }
        }
        public void OpenScriptDirectory()
        {
            var path = GetScriptDirectoryPath();
            if(!Directory.Exists(path)){
                return;
            }
            using (var pro = new Process())
            {
                var psInfo = new ProcessStartInfo{
                    FileName = path,
                    UseShellExecute = true
                };
                pro.StartInfo = psInfo;
                pro.Start();
            }
        }
        public void WriteWindowSize(IEnumerable<string> size)
        {
            var lines = size.Select(val => val.ToString()).ToList();
            lines.Insert(0, "//1行目がLeft、2行目がTop、3行目Width、4行目がHeight、5行目がWindowState");
            WriteSettings(_windowSizeFilePath, lines);
        }
        public void WriteColumnWidth(IEnumerable<int> width)
        {
            var lines = width.Select(val => val.ToString()).ToList();
            lines.Insert(0, "//先頭の列から順番に1行ずつ書き込む");
            WriteSettings(_columnWidthFilePath, width.Select(val => val.ToString()));
        }
        public List<string> GetWindowSize()
        {
            var result = new List<string>();
            foreach (var str in ReadSettings(_windowSizeFilePath))
            {
                result.Add(str);
            }
            return result;
        }
        public List<int> GetColumnWidth()
        {
            int width;
            List<int> result = new List<int>();
            foreach (var str in ReadSettings(_columnWidthFilePath))
            {
                if (!int.TryParse(str, out width)){
                    return result;
                }
                result.Add(width);
            }
            return result;
        }
        public void TryCreateExcludeDirectory()
        {
            if (File.Exists(_excludeDirectoryFilePath)){
                return;
            }
            var line = new List<string>(){
                "//除外したいフォルダのパスを入力してください",
                "//ここで設定したフォルダ以下の階層は検索結果から除外されます",
                "// $RECYCLE.BIN はゴミ箱のフォルダです",
                "// 「end:」を入力するとそれ以降は読み込まれません",
                @"C:\$RECYCLE.BIN",
                @"D:\$RECYCLE.BIN",
                @"E:\$RECYCLE.BIN",
                "",
                "",
                "end:"
            };
            WriteSettings(_excludeDirectoryFilePath, line);
        }
        public void TryCreateSearchDirectorys()
        {
            if (File.Exists(_searchDirFilePath)){
                return;
            }
            var line = new List<string>(){
                "//フォルダパスを1行ずつ入力してください",
                "//「end:」を入力するとそれ以降は読み込まれません",
                @"C:\Users\UserName",
                @"D:",
                "",
                "",
                "end:",
            };
            WriteSettings(_searchDirFilePath, line);
        }
        public bool TryGetSettingsDir(out string pSettingsDir)
        {
            if (!File.Exists(_settingsDirFilePath)){
                var line = new List<string>(){
                    "//一番上のフォルダパスだけが有効です",
                    "//一つもない場合は実行ファイルと同じフォルダが",
                    "//設定ファイルの保存場所として使用されます",
                };
                using (var writer = new StreamWriter(_settingsDirFilePath))
                {
                    foreach (var val in line)
                    {
                        writer.WriteLine(val);
                    }
                }
                pSettingsDir = string.Empty;
                return false;
            }
            var dirs = ReadSettings(_settingsDirFilePath);
            if (dirs.Count == 0){
                pSettingsDir = string.Empty;
                return false;
            }else{
                pSettingsDir = dirs.First();
                return true;
            }
        }
        public List<string> GetSearchDirectorys()
        {
            return ReadSettings(_searchDirFilePath);
        }
        private List<string> ReadSettings(string settingFilePath)
        {
            if (!File.Exists(settingFilePath)){
                return new List<string>();
            }
            List<string> settingList = new List<string>();
            try
            {
                using (StreamReader stream = new StreamReader(settingFilePath, System.Text.Encoding.UTF8))
                {
                    while (stream.EndOfStream == false)
                    {
                        settingList.Add(stream.ReadLine());
                    }
                }
            }
            catch (Exception ex)
            {
                this.ErrorNotification.Invoke(this, ex.Message);
                throw ex;
            }
            var result = new List<string>();
            foreach ( string str in settingList)
            {
                if (str.Length == 0){
                    continue;
                }
                if (str.StartsWith("//")){
                    continue;
                }
                if (str.Trim().ToLower() == "end:"){
                    break;
                }
                result.Add(str);
            }
            return result;
        }
        private void WriteSettings(string pFilePath, IEnumerable<string> pLines)
        {
            if (this.IsReadOnly() || _disposed){
                return;
            }
            try
            {
                using (StreamWriter stream = new StreamWriter(pFilePath))
                {
                    foreach(var val in pLines)
                    {
                        stream.WriteLine(val);
                    }
                }
            }
            catch (Exception ex)
            {
                this.ErrorNotification.Invoke(this, ex.Message);
                throw ex;
            }
        }
        public void CreateTempSettingsFile(string pSettingPath, string pTempPath)
        {
            using (var writer = new StreamWriter(pTempPath))
            using (var reader = new StreamReader(pSettingPath))
            {
                writer.WriteLine("###########################################################");
                writer.WriteLine("====                                                   ====");
                writer.WriteLine("====  アプリケーションが読み取り専用で開かれています   ====");
                writer.WriteLine("====                                                   ====");
                writer.WriteLine("###########################################################");
                writer.WriteLine();
                while (!reader.EndOfStream)
                {
                    writer.WriteLine(reader.ReadLine());
                }
            }
        }
        private List<string> ReadSettingsDeCompress(string pFilePath , int pCapacity)
        {
            if (!File.Exists(pFilePath)){
                return new List<string>();
            }
            List<string> settingList = new List<string>(pCapacity);
            using (var fileStream = File.Open(pFilePath, FileMode.Open))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream, System.Text.Encoding.UTF8))
            {
                var backPath = pFilePath + ".bak";
                if (File.Exists(backPath)){
                    var msg = ".bak が存在する為、前回の保存が失敗しています"
                        + Environment.NewLine + pFilePath
                        + ".bakファイルを復元してください"
                    ;
                    var args = new FoundBackupFileEventArgs(backPath);
                    this.BackupFileFound.Invoke(this, args);
                    // リストアしてバックアップ削除
                    if (args.IsRestoreRequested && args.IsDeleteBackupRequested){
                        var restorePath = backPath + ".clone";
                        File.Move(backPath, restorePath);
                        var restoreContent = this.ReadSettingsDeCompress(restorePath, pCapacity);
                        File.Delete(restorePath);
                        return restoreContent;
                    }
                    // リストアせずバックアップ削除
                    else if (!args.IsRestoreRequested && args.IsDeleteBackupRequested){
                        File.Delete(backPath);
                        return new List<string>();
                    }
                    // リストアせずバックアップ消さない
                    else if (!args.IsRestoreRequested && !args.IsDeleteBackupRequested){
                        this.ChangeToReadOnly();
                    }
                    // リストアしてバックアップ消さない
                    else{
                        return this.ReadSettingsDeCompress(backPath, pCapacity); 
                    }
                }
                while (reader.EndOfStream == false)
                {
                    settingList.Add(reader.ReadLine());
                }
            }
            var result = new List<string>(settingList.Count);
            foreach (string str in settingList)
            {
                if (str.Length == 0 || str.StartsWith("//")){
                    continue;
                }
                result.Add(str);
            }
            return result;
        }
        private void WriteSettingsCompress(string pFilePath, IEnumerable<string> pLines)
        {
            if (this.IsReadOnly() || _disposed){
                return;
            }
            var bakPath = pFilePath + ".bak";
            if (File.Exists(bakPath)){
                var errMsg = ".bak が存在する為、保存を中止します"
                    + Environment.NewLine + pFilePath
                ;
                this.ErrorNotification.Invoke(this, errMsg);
                return;
            }
            if (File.Exists(pFilePath)){
                File.Copy(pFilePath, bakPath);
            }
            using (var fileStream = File.Create(pFilePath))
            using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzipStream))
            {
                foreach(var val in pLines)
                {
                    writer.WriteLine(val);
                }
            }
            File.Delete(bakPath);
        }
        public event EventHandler<string> ErrorNotification = delegate{};
        public event EventHandler<FoundBackupFileEventArgs> BackupFileFound = delegate{};
        public event EventHandler<EventArgs> LockReleased = delegate{};
        public event EventHandler<LockGettingEventArgs> LockAcquiring = delegate{};
        public event EventHandler<EventArgs> LockAcquired = delegate{};
    }
}
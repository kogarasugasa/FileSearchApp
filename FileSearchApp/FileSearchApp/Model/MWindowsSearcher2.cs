using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data.OleDb;
using CSharpNized.Rust.std;
using System.Runtime.InteropServices;

namespace FileSearchApp
{
    public partial class MWindowsSearcher
    {
        public class MIndexSearcher
        {
            static readonly string _conStr =
                "Provider=Search.CollatorDSO;Extended Properties='Application=Windows';";
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
                    var keyWords = pSearchWords.Select(val => val.Clone());
                    if (!IsIndexEnabled(pDirPath, ct)){
                        throw new Exception("インデックス検索の結果が 0件です");
                    }
                    var querys = CreateIndexSearchQuerys(pDirPath, keyWords, false, 2);
                    foreach (var query in querys)
                    {
                        if (ct.IsCancellationRequested){
                            return;
                        }
                        await IndexSearchAsync(query, pResult, ct);
                    }
                });
            }
            static bool IsIndexEnabled(
            string pDirPath,
            CancellationToken ct)
            {
                var dummy = new List<KeyWord>{ new KeyWord("*", true) };
                var checkQuery = CreateIndexSearchQuerys(pDirPath, dummy, true, 0)
                    .First()
                ;
                var checkResult = new ConcurrentQueue<Task<ExFileInfo>>();
                var checkTask = IndexSearchAsync(checkQuery, checkResult, ct);
                var checkItems = YieldReturnSearchResults(checkTask, checkResult, ct);
                return checkItems.Any();
            }
            static async Task IndexSearchAsync(
            string query,
            ConcurrentQueue<Task<ExFileInfo>> pResult,
            CancellationToken pCt)
            {
                if (pCt.IsCancellationRequested){
                    return;
                }
CustomLogger.WriteLine("start " + query);
                using (var conn = new OleDbConnection(_conStr))
                using (var cmd = new OleDbCommand(query, conn))
                using (var endCts = new CancellationTokenSource())
                {
                    await conn.OpenAsync(pCt);
                    var results = cmd.ExecuteReader();
                    while (true)
                    {
                        if (pCt.IsCancellationRequested){
                            break;
                        }
                        if (endCts.IsCancellationRequested){
                            break;
                        }
                        var tk = ReadAsyncOleDbDataReader(results, endCts, pCt);
                        pResult.Enqueue(tk);
                        ExFileInfo info;
                        info = await tk;
                        if (info.IsDefault()){
                            break;
                        }
                    }
                    //await Task.WhenAll(pResult);
                    results.Close();
                    conn.Close();
                }
            }
            static async Task<ExFileInfo> ReadAsyncOleDbDataReader(
            OleDbDataReader pReader,
            CancellationTokenSource pEndCts,
            CancellationToken ct)
            {
                if (await pReader.ReadAsync(ct)){
                    decimal fileSize = 0;
                    if (!pReader.IsDBNull(1)){
                        fileSize = pReader.GetDecimal(1);
                    }
                    var filePath = pReader.GetString(0);
                    // users Documents などが日本語名で返ってくるので変換する
                    filePath = ConvFilePath(filePath);
                    var fileInfo = new ExFileInfo(filePath){
                        Size = fileSize
                    };
                    return fileInfo;
                }
                else{
                    pEndCts.Cancel();
                }
                return ExFileInfo.GetDefault();
            }
            static string ConvFilePath(string from)
            {
                string filePath;
                filePath = from.Replace(@"C:\ユーザー", @"C:\users");
                if (filePath.Length >= @"C:\users".Length + 1){
                    var userName = filePath.Substring(@"C:\users".Length + 1);
                    if (userName.Contains('\\')){
                        userName = userName.Substring(0, userName.IndexOf('\\'));
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\デスクトップ", @"C:\users\" + userName + @"\Desktop");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\ダウンロード", @"C:\users\" + userName + @"\Downloads");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\ドキュメント", @"C:\users\" + userName + @"\Documents");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\ピクチャ", @"C:\users\" + userName + @"\Pictures");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\ミュージック", @"C:\users\" + userName + @"\Music");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\ビデオ", @"C:\users\" + userName + @"\Videos");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\アドレス帳", @"C:\users\" + userName + @"\Contacts");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\お気に入り", @"C:\users\" + userName + @"\Favorites");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\リンク", @"C:\users\" + userName + @"\Links");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\検索", @"C:\users\" + userName + @"\Searches");
                        filePath = filePath.Replace(@"C:\users\" + userName + @"\保存したゲーム", @"C:\users\" + userName + @"\Saved Games");
                    }
                }
                return filePath;
            }
            /// <summary>
            /// Index で検索するためのクエリを作成する
            /// </summary>
            /// <param name="pDirPath">検索するフォルダ名</param>
            /// <param name="pSearchWords">検索するファイル名</param>
            /// <param name="pEnableIndexCheck">インデックスが有効かチェックするためのクエリを作成するフラグ</param>
            /// <param name="pOneQueryInKeyWords">1クエリに含める where 条件の数</param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            static List<string> CreateIndexSearchQuerys(
            string pDirPath,
            IEnumerable<KeyWord> pSearchWords,
            bool pEnableIndexCheck,
            int pOneQueryInKeyWords)
            {
                // 特殊文字を変換し ワイルドカード*が連続していたら消す
                var searchWords = new List<KeyWord>();
                foreach (var key in pSearchWords)
                {
                    if (key.Word.Trim() == ""){
                        continue;
                    }
                    var searchWord = string.Empty;
                    var word = key.Fuzzy ? "*" + key.Word + "*" : key.Word;
                    var before = 'a'; // 連続で「*」があったら消すための変数「*」以外の文字なら何でもOK
                    foreach(var cha in word)
                    {
                        if (cha == '*' && cha == before){
                            continue;
                        }
                        switch (cha)
                        {
                            case '[': searchWord += "[[]"; break;
                            case ']': searchWord += "[]]"; break;
                            case '*': searchWord += "%"; break;
                            case '?': searchWord += "_"; break;
                            default : searchWord += cha; break;
                        }
                        before = cha;
                    }
                    searchWords.Add(new KeyWord(searchWord, key.Fuzzy));
                }
                if (searchWords.Count == 0){
                    throw new Exception("検索ワードに有効な文字がありません");
                }
                string computerName = string.Empty;
                string dirPath = string.Empty;
                bool isNetwork = ConvertToNetworkPath(pDirPath).Match(
                    none => {
                        dirPath = pDirPath.Replace(@"\", "/").TrimEnd('/');
                        return false;
                    },
                    some => {
                        dirPath = some.Replace(@"\", "/").TrimEnd('/');
                        computerName = dirPath.Split('/')[2] + ".";
                        return true;
                    }
                );

                // インデックスが有効か確認するクエリ
                if (pEnableIndexCheck){
                    var queryCheck = "SELECT Top 1 System.ItemPathDisplay, System.Size ";
                    queryCheck += "FROM " + (isNetwork ? computerName : "") + "SystemIndex ";
                    queryCheck += "WHERE Scope = 'file:" + dirPath + "' ";
                    queryCheck += "AND System.ItemName LIKE '%' ";
                    return new List<string>{ queryCheck };
                }
                // 拡張子なしで検索するので完全一致のファイルの場合は「.%」を末尾に付ける必要がある
                var fileWords = searchWords.Where(key => !key.Fuzzy).Select(key => key.Word + ".%");
                var joinWords = searchWords.Select(val => val.Word).Union(fileWords).ToList();
                // 
                var whereItems = new List<string>();
                var querys = new List<string>();
                for (int i = 0; i < joinWords.Count; i++)
                {
                    if (whereItems.Count >= (pOneQueryInKeyWords - 1) || i == (joinWords.Count - 1)){
                        whereItems.Add(joinWords[i]);
                        string whereSection = "System.ItemName LIKE '"
                            + string.Join("' or System.ItemName LIKE '", whereItems)
                            + "'"
                        ;
                        //クエリ作成
                        var query = "SELECT System.ItemPathDisplay, System.Size ";
                        query += "FROM " + (isNetwork ? computerName : "") + "SystemIndex ";
                        query += "WHERE Scope = 'file:" + dirPath + "' ";
                        query += "AND (" + whereSection + ")";
                        querys.Add(query);
                        whereItems.Clear();
                        continue;
                    }
                    whereItems.Add(joinWords[i]);
                }
                return querys;
            }
            /// <summary>
            /// パスがネットワーク上を示している場合「\\MyPC」の形式でパスを返す
            /// </summary>
            static IOption<string> ConvertToNetworkPath(string pDirectoryPath)
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
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;

namespace FileSearchApp
{
    public class MListViewFunction
    {
        public int ListViewRowHeight { get; set; }
        public int ListViewheaderHeight { get; set; }
        public MListViewFunction()
        {
            ListViewRowHeight = 16;
            ListViewheaderHeight = 24;
        }
        public MListViewFunction(int pListViewRowHeight, int pListViewheaderHeight)
        {
            ListViewRowHeight = pListViewRowHeight;
            ListViewheaderHeight = pListViewheaderHeight;
        }
        public ListViewItem ConvToListViewItem(ExFileInfo pInfo, int pSeq, bool pAliasEnabled)
        {
            // 順番大事！
            var item = new ListViewItem(new string[]{
            // リストビューにタブでフォーカスした場合に先頭行にレティクルが表示される問題の対策用
            //var item = new ListViewLinkedItem(new string[]{ 
                "",
                pSeq.ToString(),
                pInfo.DispFileName(pAliasEnabled),
                pInfo.Extension,
                CustomFunctions.SIUnitBinaryByte.ToString(pInfo.Size),
                pInfo.FilePath,
                pInfo.Invalided ? "Invalid" : "Valid",
            });
            return item;
        }
        public ColumnHeader[] CreateListViewColumnHeader(IEnumerable<int> pColumnWidths)
        {
            var header = new ColumnHeader[]{
                new ColumnHeader{Text = "", Width = 0},
                new ColumnHeader{Text = "Num", Width = 50, TextAlign = HorizontalAlignment.Right},
                new ColumnHeader{Text = "FileName", Width = 100},
                new ColumnHeader{Text = "Extension", Width = 70},
                new ColumnHeader{Text = "Size", Width = 60, TextAlign = HorizontalAlignment.Right},
                new ColumnHeader{Text = "Path", Width = 100},
                new ColumnHeader{Text = "Status", Width = 50},
            };
            if (pColumnWidths.Count() == 0){
                return header;
            }
            int num = pColumnWidths.Count();
            num = Math.Min(header.Count(), num);
            for (int i = 0; i < num; i++)
            {
                header[i].Width = pColumnWidths.ElementAt(i);
            }
            return header;
        }
        public Filter<ExFileInfo> ShowFilterRemoveCommandLine(IEnumerable<KeyValuePair<string, Func<ExFileInfo, bool>>> keyValuePairs)
        {
            var filter = new Filter<ExFileInfo>();
            var isExit = false;
            foreach (var keyValue in keyValuePairs)
            {
                MInputBox.GetText("空欄にしてから確定でFilterを削除します", keyValue.Key).Match(
                    none => isExit = true,
                    some => {
                        if (some.ToLower() == "exit"){
                            isExit = true;
                            return;
                        }
                        if (some == string.Empty){
                            return;
                        }
                        filter.Add(keyValue.Key, keyValue.Value);
                    }
                );
                if (isExit){
                    filter.Add(keyValue.Key, keyValue.Value);
                    continue;
                }
            }
            return filter;
        }
        public async Task OpenFileAsync(ExFileInfo pInfo)
        {
            var filePath = pInfo.FilePath;
            if (await Task.Run(() => File.Exists(filePath))){
                pInfo.Size = new FileInfo(filePath).Length;
            }
            else if (await Task.Run(() => Directory.Exists(filePath))){
                // スルーする
            }
            else {
                pInfo.Invalided = true;
                MMessageBox.Show("ファイルまたはフォルダが存在しません");
                return;
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo{
                    FileName = filePath,
                    UseShellExecute = true
                };
                using (var pro = new Process())
                {
                    pro.StartInfo = psi;
                    await Task.Run(() => pro.Start());
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MMessageBox.Show("ファイルが開けませんでした");
            }
            catch (Exception ex)
            {
                MMessageBox.Show(ex.Message);
            }
        }
        public void ExportCsv(Dictionary<int, ExFileInfo> pInfos)
        {
            var csvText = new System.Text.StringBuilder("Num,FileName,Extension,Alias,Size,FilePath,Status\r\n");
            string filePath = string.Empty;
            var saveTask = Task.Run(async () =>
            {
                foreach (var info in pInfos)
                {
                    csvText.Append(info.Key.ToString());
                    csvText.Append(",\"" + info.Value.FileName + "\"");
                    csvText.Append(",\"" + info.Value.Extension + "\"");
                    csvText.Append(",\"" + info.Value.Alias + "\"");
                    csvText.Append("," + info.Value.Size.ToString());
                    csvText.Append(",\"" + info.Value.FilePath + "\"");
                    csvText.Append(",\"" + (info.Value.Invalided ? "Invalid" : "Valid") + "\"");
                    csvText.Append("\r\n");
                }
                //一時ファイルに書き出す
                filePath = Path.GetTempFileName();
                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    await writer.WriteAsync(csvText.ToString());
                }
            });
            //別名で保存する
            var dialog = new SaveFileDialog{
                FileName = "FileSearchApp_SearchResult.csv",
                InitialDirectory = @"%UserProfile%\Documents",
                Filter = "CSVファイル(*.csv)|*.csv|すべてのファイル(*.*)|*.*",
                FilterIndex = 1,
                Title = "保存先のファイルを選択してください",
                OverwritePrompt = true,
            };
            string newFilePath;
            if (dialog.ShowDialog() == DialogResult.OK){
                newFilePath = dialog.FileName;
                if (File.Exists(newFilePath)){
                    File.Delete(newFilePath);
                }
                saveTask.ConfigureAwait(false);
                saveTask.Wait();
                File.Move(filePath, newFilePath);
            }
            else{
                saveTask.ConfigureAwait(false);
                saveTask.Wait();
                File.Delete(filePath);
                return;
            }
            //エクセルで開く
            var type = Type.GetTypeFromProgID("Excel.Application");
            if (type != null){
                ProcessStartInfo psInfo = new ProcessStartInfo
                {
                    FileName = "excel.exe",
                    Arguments = newFilePath,
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true
                };
                Process.Start(psInfo);
            }
        }
        public void UpdateSize(ExFileInfo pInfo)
        {
            if (pInfo.Size != 0){
                return;
            }
            //ディレクトリはサイズの更新はしない
            if (System.IO.Directory.Exists(pInfo.FilePath)){
                return;
            }
            if (System.IO.File.Exists(pInfo.FilePath)){
                var info = new System.IO.FileInfo(pInfo.FilePath);
                pInfo.Size = info.Length;
                return;
            }
            //存在しなければ無効にする
            pInfo.Invalided = true;
        }
    }
}
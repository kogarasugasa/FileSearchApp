using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using CustomFunctions;

namespace FileSearchApp
{
    public class MenuRangeTree
    {
        readonly object _lock = new object();
        readonly List<SettingsRow> _menuSettings = new List<SettingsRow>();
        Func<IEnumerable<string>> _getSettingsMethod = () => new List<string>();
        public Func<IEnumerable<string>> GetSettingsMethod
        {
            get { lock (_lock) return _getSettingsMethod; } 
            set { lock (_lock) _getSettingsMethod = value; }
        }
        public List<ToolStripMenuItem> CreateMenuItems(ExFileInfo pExFileInfo)
        {
            ImportContextMenu();
            lock (_lock)
            {
                var items = new Dictionary<int, ToolStripMenuItem>();
                for (int i = 0; i < _menuSettings.Count; i++)
                {
                    var row = _menuSettings[i];
                    items.Add(row.RangeStartNum, new ToolStripMenuItem(row.Text));
                    // 下位が存在しないものはイベントを設定する
                    bool isLowerExists = _menuSettings
                        .Any(val => {
                            return
                                val.RangeStartNum > row.RangeStartNum &&
                                val.RangeEndNum < row.RangeEndNum;
                        });
                    if (!isLowerExists){
                        row.StartInfo = new System.Diagnostics.ProcessStartInfo(){
                            FileName = row.Program,
                            Arguments = row.ArgumentsPre + pExFileInfo.FilePath + row.ArgumentsPost,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        items[row.RangeStartNum].Click += (s, e) => {
                            Task.Run(() => {
                                if (File.Exists(pExFileInfo.FilePath) || Directory.Exists(pExFileInfo.FilePath)){
                                    using (var ps = new System.Diagnostics.Process())
                                    {
                                        ps.StartInfo = row.StartInfo;
                                        ps.Start();
                                    }
                                }
                                else{
                                    pExFileInfo.Invalided = true;
                                    MMessageBox.Show("パスが存在しません");
                                }
                            });
                        };
                    }
                    // 上位が存在するものは上位にぶら下げる
                    var upperSettings = _menuSettings
                        .Where(val => {
                            return 
                                val.RangeStartNum < row.RangeStartNum &&
                                val.RangeEndNum > row.RangeEndNum;
                        });
                    if (upperSettings.Count() != 0){
                        if (IsExtensionsEnabled(row.Extensions, pExFileInfo.FilePath))
                        {
                            var upperSetting = upperSettings.Max();
                            var upperItem = items[upperSetting.RangeStartNum];
                            var curItem = items[row.RangeStartNum];
                            upperItem.DropDown.Items.Add(curItem);
                        }
                    }
                }
                // 上位が存在しないものだけ結果として返す
                var topItems = new List<ToolStripMenuItem>();
                foreach (var setting in _menuSettings)
                {
                    bool isUpperExists = _menuSettings.Any(val =>
                    {
                        var result = val.RangeStartNum < setting.RangeStartNum;
                        result &= val.RangeEndNum > setting.RangeEndNum;
                        return result;
                    });
                    if (isUpperExists)
                    {
                        continue;
                    }
                    // if (IsExtensionsEnabled(setting.Extensions, pExFileInfo.FilePath))
                    // {
                    //     var item = items[setting.RangeStartNum];

                    //     topItems.Add(items[setting.RangeStartNum]);
                    // }
                    if (!IsExtensionsEnabled(setting.Extensions, pExFileInfo.FilePath))
                    {
                        continue;
                    }
                    topItems.Add(items[setting.RangeStartNum]);
                }
                return topItems;
            }
        }
        public void ImportContextMenu()
        {
            var settings = this.GetSettingsMethod();
            ImportContextMenu(settings);
        }
        public void ImportContextMenu(IEnumerable<string> pContextMenuSettings)
        {
            lock (_lock)
            {
                _menuSettings.Clear();
                foreach (var line in pContextMenuSettings)
                {
                    var arry = line.Split('|');
                    if (arry.Count() != 8)
                    {
                        return;
                    }
                    try
                    {
                        int start;
                        if (!int.TryParse(arry[0], out start))
                        {
                            throw new Exception(arry[0]);
                        }
                        int end;
                        if (!int.TryParse(arry[1], out end))
                        {
                            throw new Exception(arry[1]);
                        }
                        int seq;
                        if (!int.TryParse(arry[3], out seq))
                        {
                            throw new Exception(arry[3]);
                        }
                        var row = new SettingsRow
                        {
                            RangeStartNum = start,
                            RangeEndNum = end,
                            Text = arry[2],
                            Extensions = arry[4],
                            Program = arry[5],
                            ArgumentsPre = arry[6],
                            ArgumentsPost = arry[7]
                        };
                        if (row.RangeStartNum == row.RangeEndNum)
                        {
                            throw new Exception("開始と終了が同じです");
                        }
                        var startNums = _menuSettings.Select(val => val.RangeStartNum);
                        var endNums = _menuSettings.Select(val => val.RangeEndNum);
                        var isNonUnique = startNums.Union(endNums)
                            .Any(val => val == row.RangeStartNum || val == row.RangeEndNum);
                        if (isNonUnique)
                        {
                            throw new Exception(row.RangeStartNum.ToString());
                        }
                        _menuSettings.Add(row);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("リストビューコンテキストメニューの読み込みに失敗しました。" + ex.Message);
                    }
                }
            }
        }
        static bool IsExtensionsEnabled(
        string pExtensionsListString,
        string pFilePath)
        {
            return pExtensionsListString
                .Split('/')
                .Any(val => MyTool.FuzzyContains(val, pFilePath))
            ;
        }
        class SettingsRow : IComparer<SettingsRow>, IComparable<SettingsRow>
        {
            public int RangeStartNum = 0;
            public int RangeEndNum = 0;
            public string Text = string.Empty;
            public int Seq = 0;
            public string Extensions = string.Empty;
            public string Program = string.Empty;
            public string ArgumentsPre = string.Empty;
            public string ArgumentsPost = string.Empty;
            public System.Diagnostics.ProcessStartInfo StartInfo = new System.Diagnostics.ProcessStartInfo();
            public int Compare(SettingsRow x, SettingsRow y)
            {
                return x.RangeStartNum.CompareTo(y.RangeStartNum);
            }
            public int CompareTo(SettingsRow other)
            {
                return this.RangeStartNum.CompareTo(other.RangeStartNum);
            }
        }
    }
}

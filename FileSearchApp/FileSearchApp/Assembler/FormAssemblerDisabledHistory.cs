using System;
using System.Collections.Generic;

namespace FileSearchApp
{
    public class FormAssemblerDisabledHistory
    {
        public static VDisabledHistory CreateForm(
            IEnumerable<ExFileInfo> pInfos,
            IEnumerable<int> pHeaderWidth,
            MHistoryManager pHistory,
            MSettings pSettings,
            WindowRectangle pWindowRectangle,
            MAliasManager pAliasManager,
            MTagManager pTagManager)
        {
            {
                var mExInfos = new ListViewDataSource();
                foreach (var val in pInfos)
                {
                    mExInfos.Add(val);
                }
                var vmdListView = new VMListView(
                    mExInfos
                );
                var vmMainWindow = new VMMainWindow(pHeaderWidth, pWindowRectangle.ToList());
                var vmdMain = new VMDisabledHistory(
                    vmMainWindow,
                    new MWindowsSearcher(),
                    pHistory,
                    vmdListView
                );
                vmdMain.WindowsSearcher.GetIncludeDirectorys = pSettings.GetSearchDirectorys;
                vmdMain.WindowsSearcher.GetExcludeDirectorys = pSettings.GetExcludeDirectory;
                // 削除警告を出す条件
                vmdMain.IsDeleteAlertInfo = delInfo =>
                {
                    var check1 = pAliasManager.GetAlias(delInfo.FileName).IsSome();
                    var check2 = pTagManager.GetTags(delInfo.FileName).Count != 0;
                    return check1 || check2;
                };
                return new VDisabledHistory(vmdMain);
            }
        }
    }
}
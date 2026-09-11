using System;
using System.CodeDom;
using System.Threading.Tasks;
using CSharpNized.Rust.std;
using CustomFunctions;

namespace FileSearchApp
{
    public static class FormAssemblerAliasAndTagInput
    {
        public static void ShowForm(string pFilePath)
        {
            // 設定を読み込み
            var conf = MSettings.GetInstance();
            //タグを読み込み
            var tags = new MTagManager{
                GetAllTags = conf.GetTagMS
            };
            tags.Read(conf.GetFileNameTags());
            //別名を読み込み
            var alias = new MAliasManager();
            alias.Read(conf.GetAlias);
            //履歴を読み込み
            var his = new MHistoryManager();
            his.Read(conf.GetSearchResultHistory);
            //表示
            var icon = MyIcon.ExtractAssociatedIconFromExecutingAssembly();
            var name = ExFileInfo.GetFileName(pFilePath);
            icon.IfSome(some => MInputBoxTagAlias.SetIcon(some));
            var result = MInputBoxTagAlias.GetTagAlias(
                pFilePath,
                alias.GetAlias(ExFileInfo.GetFileName(pFilePath)).GetOrDefault(name),
                tags.GetAllTags(),
                tags.GetTags(ExFileInfo.GetFileName(pFilePath))
            );
            result.Match(
                none => {},
                some => {
                    foreach (var tag in tags.GetAllTags())
                    {
                        tags.Remove(some.FileName, tag);
                    }
                    foreach (var tag in some.CheckedTags)
                    {
                        tags.Add(some.FileName, tag);
                    }
                    if (some.Alias == ""){
                        alias.Remove(some.FileName);
                    }
                    else{
                        alias.Add(some.FileName, some.Alias, true);
                    }
                    his.AddToBuffer(pFilePath);
                }
            );
            alias.Save(conf.WriteAlias);
            tags.Save(conf.WriteFileNameTag);
            his.Save(conf.WriteSearchResultHistory);
        }
    }
}
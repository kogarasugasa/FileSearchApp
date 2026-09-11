using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms.VisualStyles;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public class MTagSearcher
    {
        public Func<string, List<string>> GetTags { private get; set; }
        public Func<string, List<string>> GetNames { private get; set; }
        public Func<List<string>> GetAllTags { private get; set; }
        public Func<string, List<ExFileInfo>> GetExFileInfos { get; set; }
        public List<string> SearchTags { get; set; }
        public List<Action<ExFileInfo>> RunActionWhenFileFound { get; set; }
        public MTagSearcher()
        {
            this.GetTags = (a) => new List<string>();
            this.GetNames = (a) => new List<string>();
            this.GetAllTags = () => new List<string>();
            this.GetExFileInfos = (a) => new List<ExFileInfo>();
            this.SearchTags = new List<string>();
            this.RunActionWhenFileFound = new List<Action<ExFileInfo>>();
        }
        public async Task StartSearchAsync(CancellationToken ct)
        {
            var searchTags = new List<string>();
            await Task.Run(() => {
                searchTags = MInputBoxChoose.GetItems(this.GetAllTags(), new List<string>()).Match(
                    none => new List<string>(),
                    some => some
                );
            });
            this.SearchTags = searchTags;
            await Task.Run(() =>
            {
                foreach (var info in GetResultAndRunSearch(ct))
                {
                    foreach (var method in this.RunActionWhenFileFound)
                    {
                        method(info);
                    }
                }
            });
        }
        public IEnumerable<ExFileInfo> GetResultAndRunSearch(CancellationToken ct)
        {
            if (this.SearchTags.Count() == 0){
                yield break;
            }
            var fileNames = new List<string>();
            foreach (var tag in this.SearchTags)
            {
                fileNames.AddRange(this.GetNames(tag));
            }
            fileNames = fileNames.Distinct().ToList();
            foreach (var name in fileNames)
            {
                foreach (var info in this.GetExFileInfos(name))
                {
                    if (ct.IsCancellationRequested){
                        yield break;
                    }
                    yield return info;
                }
            }
        }
    }
}
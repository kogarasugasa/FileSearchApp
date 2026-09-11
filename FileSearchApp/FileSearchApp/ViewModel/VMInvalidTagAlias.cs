using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using CustomFunctions;

namespace FileSearchApp
{
    public class VMInvalidTagAlias
    {
        MHistoryManager _his;
        MAliasManager _alias;
        MTagManager _tag;
        public int TagSelectedIndex { get; set; }
        public int AliasSelectedIndex { get; set; }
        public SortedSet<int> InvalidIndices { get; set; }
        public VMInvalidTagAlias(MHistoryManager pHis, MAliasManager pAlias, MTagManager pTag)
        {
            _his = pHis;
            _alias = pAlias;
            _tag = pTag;
            AliasAllocate.AliasDigit = 0;
            TagAllocate.TagDigit = 0;
            this.TagSelectedIndex = -1;
            this.AliasSelectedIndex = -1;
            this.InvalidIndices = new SortedSet<int>{ 0 }; // 1行目は項目名を表示する為
        }
        public bool IsItemSelected(ListBox pListBox)
        {
            if (this.InvalidIndices.Contains(pListBox.SelectedIndex)){
                return false;
            }
            return pListBox.SelectedIndex >= 0;
        }
        public List<TagAllocate> GetInvalidTags()
        {
            var names = _tag.GetNames();
            var invalidNames = names.Where(name => !_his.GetExFileInfos(name).Any());
            var initialAlloc = new TagAllocate{ FileName = "ファイル名", Tag = "タグ" };
            var invalidAllocate = new List<TagAllocate>{ initialAlloc };
            foreach (var name in invalidNames)
            {
                var tags = _tag.GetTags(name);
                foreach (var tag in tags)
                {
                    var alloc = new TagAllocate{
                        FileName = name,
                        Tag = tag
                    };
                    invalidAllocate.Add(alloc);
                }
            }
            return invalidAllocate;
        }
        public List<AliasAllocate> GetInvalidAliases()
        {
            var names = _alias.GetNames();
            var invalidNames = names.Where(name => !_his.GetExFileInfos(name).Any());
            var initialAlloc = new AliasAllocate{ FileName = "ファイル名", Alias = "別名" };
            var invalidAllocate = new List<AliasAllocate>{ initialAlloc };
            foreach (var name in invalidNames)
            {
                _alias.GetAlias(name).IfSome(some =>{
                    var alloc = new AliasAllocate{
                        FileName = name,
                        Alias = some,
                    };
                    invalidAllocate.Add(alloc);
                });
            }
            return invalidAllocate;
        }
        public void DeleteAliasAllocate(string pFileName)
        {
            _alias.Remove(pFileName);
        }
        public void DeleteTagAllocate(string pFileName, string pTag)
        {
            _tag.Remove(pFileName, pTag);
        }
        public struct AliasAllocate
        {
            public static int AliasDigit;
            string _alias;
            public string Alias {
                get { return _alias; }
                set
                {
                    if (AliasDigit < value.HarfLength()){
                        AliasDigit = value.HarfLength();
                    }
                    _alias = value;
                }
            }
            public string FileName { get; set; }
            public override string ToString()
            {
                var builder = new System.Text.StringBuilder();
                builder.Append(Alias);
                for (int i = 0; i < AliasDigit - Alias.HarfLength(); i++) builder.Append(' ');
                builder.Append(" | ");
                builder.Append(FileName);
                return builder.ToString();
            }
        }
        public struct TagAllocate
        {
            public static int TagDigit;
            string _tag;
            public string Tag
            {
                get { return _tag; }
                set
                {
                    if (TagDigit < value.HarfLength()){
                        TagDigit = value.HarfLength();
                    }
                    _tag = value;
                }
            }
            public string FileName { get; set; }
            public override string ToString()
            {
                var builder = new System.Text.StringBuilder();
                builder.Append(Tag);
                for (int i = 0; i < TagDigit - Tag.HarfLength(); i++) builder.Append(' ');
                builder.Append(" | ");
                builder.Append(FileName);
                return builder.ToString();
            }
        }
    }
}
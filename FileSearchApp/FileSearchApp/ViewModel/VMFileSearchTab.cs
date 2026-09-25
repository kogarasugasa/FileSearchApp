using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CustomFunctions;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public class VMFileSearchTab
    {
        readonly object _lock = new object();
        readonly MTabManager _tabManager;
        public int Count
        {
            get { return _tabManager.Count; }
        }
        TabRecord _record;
        public TabRecord Record
        {
            get { lock (_lock) return _record; }
            set { lock (_lock) _record = value; }
        }
        Color _defaultColor = Color.FromArgb(100, SystemColors.ControlDark);
        public Color DefaultColor
        {
            get { lock (_lock) return _defaultColor; }
            //set { lock (_lock) _defaultColor = value; }
        }
        public VMFileSearchTab(MTabManager pMTabManager)
        {
            _tabManager = pMTabManager;
            _record = _tabManager.GetFirstOrNew();
            _tabManager.Remove(_record.Key);
        }
        public ReadOnlyCollection<TabRecord> GetTabRecords()
        {
            return _tabManager.Records;
        }
        public void OnAddClick()
        {
            var key = _tabManager.Add();
            _tabManager.GetViewModel(key).Match(
                none => {},
                some => {
                    //lock (_lock) this.Record = some;
                    var args = new TabAdditionedEventArgs(some);
                    this.TabAdditioned.Invoke(this, args);
                }
            );
        }
        public void OnRemoveClick(string pKey)
        {
            // 現在のタブを消す場合は別のタブに切り替える
            if (this.Record.Key == pKey){
                _tabManager.GetNearKey(pKey).IfSome(otherKey => {
                    this.OnSelectClick(otherKey);
                });
            }
            // タブを検索して消す
            _tabManager.GetViewModel(pKey).IfSome(removeVm => {
                this.TabRemoving.Invoke(this, new TabRemovingEventArgs(removeVm));
                _tabManager.Remove(removeVm.Key);
            });
        }
        public void OnSelectClick(string pKey)
        {
            this.ChangeTagName();
            var vm = _tabManager.GetViewModel(pKey);
            vm.IfSome(newVm =>{
                this.TabChanging.Invoke(this, new TabChangingEventArgs(this.Record));
                lock (_lock) this.Record = newVm;
                this.TabChanged.Invoke(this, new TabChangedEventArgs(newVm));
            });
        }
        public void ChangeTagName(string pKey = "")
        {
            TabRecord textSource = this.Record;
            _tabManager.GetViewModel(pKey).IfSome(some => textSource = some);
            {
                var texts = new List<string>();
                if (textSource.FileSearch.SearchWords != ""){
                    texts.Add(textSource.FileSearch.SearchWords);
                }
                if (textSource.ListView.SelectedIndex != -1){
                    var info = textSource.ListView.GetExFileInfo();
                    var name = info.DispFileName(textSource.ListView.AliasEnabled);
                    if (name != ""){
                        texts.Add(name);
                    }
                }
                if (texts.Count == 0){
                    texts.Add(textSource.Key);
                }
                var text = texts.Get(0).Match(
                    none => "err",
                    some => some
                );
                if (text != string.Empty){
                    textSource.Text = text;
                }
            }
        }
        public event EventHandler<TabAdditionedEventArgs> TabAdditioned = delegate{};
        public event EventHandler<TabRemovingEventArgs> TabRemoving = delegate{};
        public event EventHandler<TabChangingEventArgs> TabChanging = delegate{};
        public event EventHandler<TabChangedEventArgs> TabChanged = delegate{};
        public class TabAdditionedEventArgs
        {
            public TabRecord Record { get; private set; }
            public TabAdditionedEventArgs(TabRecord pRecord)
            {
                this.Record = pRecord;
            }
        }
        public class TabRemovingEventArgs
        {
            public TabRecord Record { get; private set; }
            public TabRemovingEventArgs(TabRecord pRecord)
            {
                this.Record = pRecord;
            }
        }
        public class TabChangingEventArgs
        {
            public TabRecord Record { get; private set; }
            public TabChangingEventArgs(TabRecord pRecord)
            {
                this.Record = pRecord;
            }
        }
        public class TabChangedEventArgs
        {
            public TabRecord Record { get; private set; }
            public TabChangedEventArgs(TabRecord pRecord)
            {
                this.Record = pRecord;
            }
        }
    }
}
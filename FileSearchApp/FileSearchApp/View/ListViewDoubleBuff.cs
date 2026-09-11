using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using System.Runtime.Serialization;

namespace FileSearchApp
{
    public partial class ListViewDoubleBuff : ListView
    {
        readonly object _lock = new object();
        RetrieveVirtualItemLogCollection _dispIndices = new RetrieveVirtualItemLogCollection();
        uint _timeStamp = uint.MinValue;
        public int ListViewRowHeight { get; private set; }
        public int ListViewHeaderHeight { get; private set; }
        public ListViewDoubleBuff()
        {
            this.DoubleBuffered = true;
            this.ListViewRowHeight = 16;
            this.ListViewHeaderHeight = 24;
        }
        public ListViewDoubleBuff(int pListViewRowHeight, int pListViewHeaderHeight)
        {
            this.DoubleBuffered = true;
            this.ListViewRowHeight = pListViewRowHeight;
            this.ListViewHeaderHeight = pListViewHeaderHeight;
        }
        protected override void OnRetrieveVirtualItem(RetrieveVirtualItemEventArgs e)
        {
            this.AddRetrieveVirtualItemLog(e.ItemIndex);
            base.OnRetrieveVirtualItem(e);
        }
        public bool IsMouseOverColumnHeader()
        {
            var listViewRect = this.RectangleToScreen(this.ClientRectangle);
            var headRect = new Rectangle(
                listViewRect.Left,
                listViewRect.Top,
                listViewRect.Width,
                this.ListViewHeaderHeight
            );
            var cursorRect = new Rectangle(Cursor.Position.X, Cursor.Position.Y, 1, 1);
            return headRect.IntersectsWith(cursorRect);
        }
        public IOption<string> GetPointingHeaderText()
        {
            var listViewRect = this.RectangleToScreen(this.ClientRectangle);
            var headRect = new Rectangle(
                listViewRect.Left,
                listViewRect.Top,
                listViewRect.Width,
                this.ListViewHeaderHeight
            );
            var columnRect = new List<Rectangle>();
            var cursorRect = new Rectangle(Cursor.Position.X, Cursor.Position.Y, 1, 1);
            var headers = new List<ColumnHeader>();
            foreach (var val in this.Columns) headers.Add((ColumnHeader)val);
            var columns = headers.ToList();
            columns.Sort((x,y) => x.DisplayIndex.CompareTo(y.DisplayIndex));
            int leftPos = headRect.Left;
            foreach (var column in columns)
            {
                var rect = new Rectangle(leftPos, headRect.Top, column.Width, headRect.Height);
                if (rect.IntersectsWith(cursorRect)){
                    return Some.New(column.Text);
                }
                leftPos += column.Width;
            }
            return new None<string>();
        }
        public int GetDispRowsCount()
        {
            decimal headHeight = this.ListViewHeaderHeight;
            decimal rowHeight = this.ListViewRowHeight;
            int rowCount = (int)Math.Ceiling((this.Height - headHeight) / rowHeight);
            if (rowCount < 0){
                var msg = "GetDispRowsCount() 結果が負の値です rowCount : " + rowCount + " headHeight : " + headHeight + " rowHeight : " + rowHeight;
                CustomLogger.WriteLine(msg);
                rowCount = 0;
            }
            return rowCount;
        }
        public void RedrawItems()
        {
            var virtualListSize = this.VirtualListSize;
            if (virtualListSize == 0){
                return;
            }
            var updateIndices = GetDisplayIndices().Where(val => val < virtualListSize);
            if (!updateIndices.Any()){
                return;
            }
            //if (updateIndices.Count() == 0) return;
            this.RedrawItems(updateIndices.Min(), updateIndices.Max(), false);
        }
        public void AddRetrieveVirtualItemLog(int pIndex)
        {
            lock (_lock)
            {
                _timeStamp++;
                // タイムスタンプが最大になったらすべてのタイムスタンプをなるべく小さくする
                if (_timeStamp == uint.MaxValue){
                    if (_dispIndices.Count == 0){
                        _timeStamp = 0;
                    }
                    else{
                        uint minTimeStamp = uint.MaxValue;
                        for (int i = 0; i < _dispIndices.Count; i++)
                        {
                            uint timeStamp;
                            _dispIndices.GetTimeStamp(i, out timeStamp);
                            if (minTimeStamp > timeStamp){
                                minTimeStamp = timeStamp;
                            }
                        }
                        for (int i = 0; i < _dispIndices.Count; i++)
                        {
                            uint timeStamp;
                            _dispIndices.GetTimeStamp(i, out timeStamp);
                            _dispIndices.SetTimeStamp(i, timeStamp - minTimeStamp);
                        }
                        _timeStamp = minTimeStamp;
                    }
                }
                // 追加したいインデックスが存在する場合、日付を更新して終了
                if (_dispIndices.ContainsRowId(pIndex)){
                    _dispIndices.RemoveRowId(pIndex);
                    _dispIndices.Add(pIndex, _timeStamp);
                    return;
                }
                // 追加したいインデックスが 0 かつ
                // 配列内で最も大きいインデックスが 0 + RowCount * 2 - 1 を超える場合何もせずに終了
                if (pIndex == 0 && _dispIndices.Count != 0){
                    int rowCnt = GetDispRowsCount();
                    int pMaxindex = _dispIndices.GetRowIds().Max();
                    if (pMaxindex > (0 + rowCnt * 2 - 1)){
                        return;
                    }
                }
                // 追加したいインデックスが 最後の配列 かつ
                // 別の要素が存在する状態で 要素に 最後の配列-1 がない場合何もせずに終了
                var lastIndex = this.VirtualListSize - 1;
                if (pIndex == lastIndex && _dispIndices.Count != 0 & !_dispIndices.ContainsRowId(lastIndex - 1)){
                    return;
                }
                // ログを追加して表示範囲を超えた分を消す
                _dispIndices.Add(pIndex, _timeStamp);
                while (true)
                {
                    var removeCnt = _dispIndices.Count - Math.Min(GetDispRowsCount(), this.VirtualListSize);
                    if (removeCnt <= 0){
                        break;
                    }
                    _dispIndices.RemoveFirst();
                }
            }
        }
        public List<int> GetDisplayIndices()
        {
            lock (_lock)
            {
                return _dispIndices.GetRowIds()
                    .FindAll(val => val >= 0 && val < this.VirtualListSize)
                ;
            }
        }
        public void SelectItem(int pIndex)
        {
            for (int i = 0; i < this.Items.Count; i++)
            {
                if (this.Items[i].Index == pIndex){
                    this.Items[i].Selected = true;
                    this.Items[i].Focused = true;
                    return;
                }
            }
        }
    }
}
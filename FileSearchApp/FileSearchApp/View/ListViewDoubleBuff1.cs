using System;
using System.Collections.Generic;

namespace FileSearchApp
{
    public partial class ListViewDoubleBuff
    {
        sealed class RetrieveVirtualItemLogCollection
        {
            readonly List<int> _rowIds = new List<int>();
            readonly List<uint> _timeStamps = new List<uint>();
            public int Count { get { return _rowIds.Count; } }
            public void RemoveFirst()
            {
                if (_rowIds.Count != 0){
                    this.RemoveAt(0);
                }
            }
            public void RemoveRowId(int pRowId)
            {
                int index = -1;
                for (int i = 0; i < _rowIds.Count; i++)
                {
                    if (_rowIds[i] == pRowId){
                        index = i;
                        break;
                    }
                }
                if (index != -1){
                    _rowIds.RemoveAt(index);
                    _timeStamps.RemoveAt(index);
                }
            }
            public void RemoveAt(int pIndex)
            {
                _rowIds.RemoveAt(pIndex);
                _timeStamps.RemoveAt(pIndex);
            }
            public void Add(int pRowId, uint pTimeStamp)
            {
                if (_rowIds.Contains(pRowId)){
                    return;
                }
                _rowIds.Add(pRowId);
                _timeStamps.Add(pTimeStamp);
            }
            public bool ContainsRowId(int pRowId)
            {
                return _rowIds.Contains(pRowId);
            }
            public int FindIndex(Func<int, uint, bool> func)
            {
                for (int i = 0; i < _rowIds.Count; i++){
                    if (func(_rowIds[i], _timeStamps[i])){
                        return i;
                    }
                }
                return -1;
            }
            public void Get(int pIndex, out int pRowId, out uint pTimeStamp)
            {
                pRowId = _rowIds[pIndex];
                pTimeStamp = _timeStamps[pIndex];
            }
            public List<int> GetRowIds()
            {
                return new List<int>(_rowIds);
            }
            public void GetTimeStamp(int pIndex, out uint pTimeStamp)
            {
                pTimeStamp = _timeStamps[pIndex];
            }
            public void Set(int pIndex, int pRowId, uint pTimeStamp)
            {
                _rowIds[pIndex] = pRowId;
                _timeStamps[pIndex] = pTimeStamp;
            }
            public void SetTimeStamp(int pIndex, uint pTimeStamp)
            {
                _timeStamps[pIndex] = pTimeStamp;
            }
        }
    }
}
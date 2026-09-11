using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using System.Dynamic;

namespace FileSearchApp
{
    public sealed class LockGettingEventArgs
    {
        public string LockInfo;
        public bool Force;
        public bool Cancel { get; set; }
        public LockGettingEventArgs(string pLockInfo, bool pForce)
        {
            this.LockInfo = pLockInfo;
            this.Force = pForce;
            this.Cancel = false;
        }
    }
    public sealed class HistoryRepairedEventArgs
    {
        public bool ItemFound { get; private set; }
        public int ItemIndex { get; private set; }
        public ExFileInfo OldInfo { get; private set; }
        public HistoryRepairedEventArgs(bool pItemFound, int pIndex, ExFileInfo pOldInfo)
        {
            this.ItemFound = pItemFound;
            this.ItemIndex = pIndex;
            this.OldInfo = pOldInfo;
        }
    }
    public class OptionMenuClickEventArgs
    {
        public bool AutoSaveEnabled { get; set; }
    }
    public class OptionMenuItemsChangedEventArgs
    {
        public ToolStripMenuItem[] Items { get; private set; }
        public OptionMenuItemsChangedEventArgs(ToolStripMenuItem[] pArgs)
        {
            this.Items = pArgs;
        }
    }
    public class AutoSaveStatusChangeReadOnlyEventArgs : IDisposable
    {
        bool _disposed = false;
        protected CancellationTokenSource _cts = new CancellationTokenSource();
        public bool ChangeToEnable { get; private set; }
        public virtual bool Cancel { get; protected set; }
        public IOption<Task> ProgressView { get; protected set; }
        public AutoSaveStatusChangeReadOnlyEventArgs(bool pChangeToEnable)
        {
            this.ChangeToEnable = pChangeToEnable;
            this.ProgressView = new None<Task>();
        }
        ~AutoSaveStatusChangeReadOnlyEventArgs()
        {
            this.Dispose();
        }
        public void Dispose()
        {
            this.Dispose(true);
        }
        public void Dispose(bool disposing)
        {
            if (_disposed){
                return;
            }
            if (disposing)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _disposed = true;
        }
    }
    public class AutoSaveStatusChangeEventArgs : AutoSaveStatusChangeReadOnlyEventArgs
    {
        public new bool Cancel
        {
            get { return base.Cancel; }
            set { base.Cancel = value; }
        }
        public AutoSaveStatusChangeEventArgs(bool pChangeToEnable) : base(pChangeToEnable){}
        public void ShowProgressView(Func<CancellationToken, Task> action)
        {
            this.ProgressView = new Some<Task>(action(_cts.Token));
        }
        public static AutoSaveStatusChangeEventArgs GetAutoSaveChangeToRequestedArgs()
        {
            return new AutoSaveStatusChangeEventArgs(true);
        }
        public static AutoSaveStatusChangeEventArgs GetAutoSaveChangeToCancelArgs()
        {
            return new AutoSaveStatusChangeEventArgs(false);
        }
    }
    public class CancelEventArgs
    {
        public CancellationToken Token { get; private set; }
        public CancelEventArgs(CancellationToken pToken)
        {
            this.Token = pToken;
        }
    }
    public class ReOrderEventArgs
    {
        public bool CancelRequested { get; set; }
        public string Message { get; private set; }
        public ReOrderEventArgs(string pMsg)
        {
            this.CancelRequested = false;
            this.Message = pMsg;
        }
    }
    public sealed class ViewModelFileSizeUpdatedEventArgs
    {
        public ViewModelFileSizeUpdatedEventArgs(long pNumerator, long pDenominator)
        {
            this.Numerator = pNumerator;
            this.Denominator = pDenominator;
            this.ProgressRatio = pNumerator / pDenominator;
            this.ProgressRatioPercentile = (int)((float)pNumerator / (float)pDenominator * (float)100);
            this.ProgressString = pNumerator + " / " + pDenominator;
        }
        public long Denominator { get; private set; }
        public long Numerator { get; private set; }
        public float ProgressRatio { get; private set; }
        public int ProgressRatioPercentile { get; private set; }
        public string ProgressString { get; private set; }
    }
    public class ModelMessageReceivedEventArgs
    {
        public ModelMessageReceivedEventArgs(string pMsg)
        {
            this.Message = pMsg;
        }
        public string Message { get; private set; }
    }
    public class ViewModelListViewCursorChangedEventArgs
    {
        private Cursor _val;
        public ViewModelListViewCursorChangedEventArgs(Cursor args) { _val = args; }
        public Cursor Value { get { return _val; } }
    }
    public sealed class ViewModelDataSourceChangedEventArgs
    {
        public int ListSize { get; private set; }
        public bool Redraw { get; private set; }
        public ViewModelDataSourceChangedEventArgs(int listSize, bool redraw)
        {
            this.ListSize = listSize;
            this.Redraw = redraw;
        }
    }
    public sealed class ViewModelRunCaptionChangedEventArgs
    {
        public string Value { get; private set; }
        public ViewModelRunCaptionChangedEventArgs(string args)
        {
            this.Value = args;
        }
    }
    public sealed class FoundBackupFileEventArgs
    {
        public string BackupPath { get; private set; }
        public bool IsRestoreRequested { get; set; }
        public bool IsDeleteBackupRequested { get; set; }
        public FoundBackupFileEventArgs(string pBackupPath)
        {
            this.BackupPath = pBackupPath;
            this.IsRestoreRequested = false;
            this.IsDeleteBackupRequested = false;
        }
    }
    public sealed class IsReadOnlyChangedEventArgs
    {
        public bool IsReadOnly { get; private set; }
        public IsReadOnlyChangedEventArgs(bool pIsReadOnly)
        {
            this.IsReadOnly = pIsReadOnly;
        }
    }
}
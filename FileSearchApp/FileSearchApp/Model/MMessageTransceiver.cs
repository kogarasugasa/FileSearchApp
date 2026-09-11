using System;
using System.Linq;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Principal;
using CustomFunctions;

namespace FileSearchApp
{
    public class MMessageTransceiver : IDisposable
    {
        bool _disposed = false;
        CancellationTokenSource _cts = new CancellationTokenSource();
        static string _pipeName = string.Empty;
        Task _recieveTask = Task.Delay(0);
        // ======+=========+=========+=========+=========+=========+=========+=========+
        // = コンストラクタ、デストラクタ
        // ======+=========+=========+=========+=========+=========+=========+=========+
        ~MMessageTransceiver()
        {
            this.Dispose();
        }
        // ======+=========+=========+=========+=========+=========+=========+=========+
        // = メソッド
        // ======+=========+=========+=========+=========+=========+=========+=========+
        public void Dispose()
        {
            this.Dispose(true);
        }
        public void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cts.Cancel();
                    _recieveTask.WaitByPollingLoop(50);
                    _cts.Dispose();
                }
                _disposed = true;
            }
        }
        public static void SetPipeName(string pPipeName)
        {
            _pipeName = pPipeName;
        }
        public void StartReciever()
        {
            if (!_recieveTask.IsCompleted){
                throw new Exception("StartReciever はすでに開始されています");
            }
            if (_pipeName == ""){
                throw new Exception("PipeName が設定されていません");
            }
            _recieveTask = this.StartRecieverAsync();
        }
        async Task StartRecieverAsync()
        {
            string pipeName = _pipeName;
            while (true)
            {
                if (_cts.Token.IsCancellationRequested){
                    break;
                }
                try
                {
                    using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        // クライアントの接続待ち
                        await pipeServer.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);
                        StreamString ss = new StreamString(pipeServer);

                        // 受信待ち
                        var read = ss.ReadString();

                        // 受信したら応答を送信
                        ss.WriteString("OK");
                        ModelMessageReceived.Invoke(this, new ModelMessageReceivedEventArgs(read));
                        pipeServer.Close();
                    }
                }
                catch (OverflowException ex)
                {
                    // クライアントが切断
                    CustomLogger.WriteLine(ex.Message);
                }
            }
        }
        public static void SendMessage(string pMessage)
        {
            if (_pipeName == string.Empty){
                throw new Exception("PipeName が設定されていません");
            }
            using (var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
            {
                pipeClient.Connect(500);
                var ss = new StreamString(pipeClient);
                // 入力された文字列を送信する
                var write = ss.WriteString(pMessage);
                // 応答待ち
                var read = ss.ReadString();
            }
        }
        public event EventHandler<ModelMessageReceivedEventArgs> ModelMessageReceived = delegate { };
    }
}
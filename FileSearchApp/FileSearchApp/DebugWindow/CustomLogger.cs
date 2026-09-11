using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using CSharpNized.Rust.std;
using CustomFunctions;
using System.Collections.ObjectModel;

namespace FileSearchApp
{
    public static class CustomLogger
    {
        static readonly object _lock = new object();
        static Task _outputTask = Task.Delay(0);
        static string _outputTextFilePath = string.Empty;
        static readonly ConcurrentQueue<string> _outputTextFileMsgs = new ConcurrentQueue<string>();
        static string _lastMessage = string.Empty;
        static List<string> _messages = new List<string>();
        static int _messagesMax = 60;
        public static string LastMessage
        {
            get { lock (_lock) { return _lastMessage; } }
        }
        static bool _outputTextFile = false;
        public static bool OutputTextFile
        {
            get { lock (_lock) { return _outputTextFile; } }
        }
        public static void StartOutputTextFile()
        {
            lock (_lock)
            {
                _outputTextFile = true;
                var timeStr = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                _outputTextFilePath = Application.StartupPath + @"\" + "FileSearchApp_" + timeStr + ".log";
            }
        }
        public static void StopOutputTextFile()
        {
            lock (_lock)
            {
                _outputTextFile = false;
            }
        }
        static void WriteLineInner(string pMsg)
        {
            lock (_lock)
            {
                if (_outputTextFile){
                    OutPutTextFileUnLock();
                }
                _outputTextFileMsgs.Enqueue(pMsg);
                if (_messages.Count >= _messagesMax){
                    _messages.RemoveAt(0);
                }
                _messages.Add(pMsg);
                _lastMessage = pMsg;
            }
            Console.WriteLine(pMsg);
        }
        public static void WriteLine<T>(T pMsg)
        {
            if (pMsg != null){
                WriteLineInner(pMsg.ToString());
            }
        }
        static void OutPutTextFileUnLock()
        {
            if (!_outputTask.IsCompleted){
                return;
            }
            _outputTask = Task.Run(() =>
            {
                using (var writer = new StreamWriter(_outputTextFilePath, true))
                {
                    while (_outputTextFileMsgs.Count != 0)
                    {
                        var msg = string.Empty;
                        if (_outputTextFileMsgs.TryDequeue(out msg)){
                            writer.WriteLine(msg);
                        }
                        else{
                            break;
                        }
                    }
                }
            });
        }
        public static void Clear()
        {
            lock (_lock) _messages.Clear();
        }
        public static List<string> GetMessages()
        {
            lock (_lock)
            {
                return new List<string>(_messages);
            }
        }
        public static Form GetMessageForm()
        {
            return new StdOutputViewer();
        }
        public static ReadOnlyCollection<string> Messages { get { lock (_lock) return new ReadOnlyCollection<string>(_messages); }}
    }
}
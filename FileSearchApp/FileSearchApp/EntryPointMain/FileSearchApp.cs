// FileSearchApp is fast file search and categorize
// Copyright (C) 2025  kobahashi hayato of author
// mail 

// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

[assembly: System.Reflection.AssemblyTitle("FileSearchApp")]
[assembly: System.Reflection.AssemblyDescription("Windowsのファイルを検索します")]
[assembly: System.Reflection.AssemblyProduct("FileSearchApp")]
[assembly: System.Reflection.AssemblyCopyright("著作権")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0.0")]
[assembly: System.Resources.NeutralResourcesLanguage("ja-JP")]

namespace FileSearchApp
{
    public class FileSearchApp
    {
        static string _pipeName = "FileSearchAppPipeName_1157954612354987";
        static string _mutexName = "PreventMultipleLaunchFileSearchApp";
        [STAThread]
        public static void Main(string[] args)
        {
            // 拡縮が設定されている場合座標が思った通りに設定できないためこれを入れる
            MyNativeMethod.Function.Screen.SetProcessDPIAware();
            // OS標準の視覚スタイルが適用される
            Application.EnableVisualStyles();
            // GDIを使った描画になります(なぜ入れてるかは忘れた)
            Application.SetCompatibleTextRenderingDefault(false);
            // パイプ名をセットする
            MMessageTransceiver.SetPipeName(_pipeName);
            bool createdNew;
            using (var mutex = new Mutex(true, _mutexName, out createdNew))
            {
                // 起動引数に -force が含まれていたら認証をスキップ
                if (args.Any(val => val.ToLower() == "-force"))
                {
                    // -force の引数を消す
                    args = args.Where(val => val.ToLower() != "-force").ToArray();
                }
                else
                {
                    // パスワードの入力を求める
                    if (!LicenseConfirmation())
                    {
                        return;
                    }
                }

                // 既に起動されているか
                bool otherProcessExists = !createdNew;
                // 引数ありで起動されているか
                bool argExists = args.Length != 0;
                try
                {
                    // 通常起動
                    if (otherProcessExists == false && argExists == false)
                    {
                        using (var fa = new FormAssembler())
                        {
                            Application.Run(fa.CreateForm());
                        }
                    }
                    // 追加だけするFormを表示する
                    else if (otherProcessExists == false && argExists == true)
                    {
                        FormAssemblerAliasAndTagInput.ShowForm(args[0]);
                    }
                    // すで起動されている画面を前面に表示
                    else if (otherProcessExists == true && argExists == false)
                    {
                        MMessageTransceiver.SendMessage(string.Empty);
                    }
                    // 既に起動しているインスタンスにファイルパスだけ送信する
                    else if (otherProcessExists == true && argExists == true)
                    {
                        MMessageTransceiver.SendMessage(args[0]);
                    }
                }
                catch (Exception ex)
                {
                    var msg = "Main(string[] args) " + ex.Message + Environment.NewLine +
                        ex.StackTrace;
                    MessageBox.Show(msg);
                }
            }
        }
        public static bool LicenseConfirmation()
        {
            // ライセンスを表示したことがあるかチェック
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dirPath = roaming + @"\FileSearchApp";
            var checkPath = dirPath + @"\AppropriateUserOfLicense.txt";
            if (File.Exists(checkPath))
            {
                return true;
            }
            // ライセンスを表示
            var message = CustomFunctions.GPLv2.Notice()
                + Environment.NewLine + "|"
                + Environment.NewLine + "|"
                + Environment.NewLine + CustomFunctions.GPLv2.NoticeJp()
            ;
            Show(message);
            // ライセンスを書き出し
            var path = AppDomain.CurrentDomain.BaseDirectory;
            path = path.EndsWith("\\") ? path + "License" : path + "\\License";
            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.Write(CustomFunctions.GPLv2.License());
                }
                Directory.CreateDirectory(dirPath);
                File.CreateText(checkPath);
            }
            catch (Exception ex)
            {
                MMessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }
        public static void Show(String pMessage)
        {
            var form = new Form() {
                Width = 800,
                Height = 500,
            };
            
            var label = new Label()
            {
                Left = 0,
                Top = 0,
                Width = form.Width - 10,
                Height = form.Height - 10,
                AutoSize = true,
                Text = pMessage,
            };
            form.Controls.Add(label);
            Application.Run(form);
        }
    }
}
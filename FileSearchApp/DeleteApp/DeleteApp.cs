using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FileSearchApp;

namespace DeleteApp
{
    public static class DeleteApp
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // 拡縮が設定されている場合座標が思った通りに設定できないためこれを入れる
            MyNativeMethod.Function.Screen.SetProcessDPIAware();
            // OS標準の視覚スタイルが適用される
            Application.EnableVisualStyles();
            // GDIを使った描画になります(なぜ入れてるかは忘れた)
            Application.SetCompatibleTextRenderingDefault(false);
            // アイコン取得
            MyIcon.ExtractAssociatedIconFromExecutingAssembly().IfSome(some => {
                MMessageBox.SetIcon(some);
                MInputBox.SetIcon(some);
            });
            // 引数無しは即終了
            if (args.Count() == 0){
                return;
            }
            var result = FileSearchApp.MInputBox.GetText(
                "下記のファイルまたはフォルダを削除します",
                args[0],
                true
            );
            result.Match(
                none => {},
                some => {
                    if (File.Exists(some)){
                        try
                        {
                            File.Delete(some);
                        }
                        catch (Exception ex)
                        {
                            FileSearchApp.MMessageBox.Show(ex.Message);
                        }
                    }
                    else if (Directory.Exists(some)){
                        try
                        {
                            Directory.Delete(some, true);
                        }
                        catch (Exception ex)
                        {
                            FileSearchApp.MMessageBox.Show(ex.Message);
                        }
                    }
                    else {
                        FileSearchApp.MMessageBox.Show("パスが存在しません");
                    }
                }
            );
        }
    }
}
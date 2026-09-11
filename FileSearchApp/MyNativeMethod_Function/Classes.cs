using System;
using System.CodeDom;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;//Marshalのために追加

namespace MyNativeMethod.Function
{
    public static class Screen
    {
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();
    }
    public static class Drive
    {
        [STAThread]
        /* 
         * WNetGetUniversalNameをインポートする
         */
        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U4)] static extern int
        WNetGetUniversalName(
            string lpLocalPath,                                 // ネットワーク資源のパス 
            [MarshalAs(UnmanagedType.U4)] int dwInfoLevel,      // 情報のレベル
            IntPtr lpBuffer,                                    // 名前バッファ
            [MarshalAs(UnmanagedType.U4)] ref int lpBufferSize  // バッファのサイズ
        );


        /*
         * dwInfoLevelに指定するパラメータ
         *  lpBuffer パラメータが指すバッファで受け取る構造体の種類を次のいずれかで指定
         */
        const int UNIVERSAL_NAME_INFO_LEVEL = 0x00000001;
        const int REMOTE_NAME_INFO_LEVEL    = 0x00000002; //こちらは、テストしていない


        /*
         * lpBufferで受け取る構造体
         */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct UNIVERSAL_NAME_INFO
        {
            public string lpUniversalName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct _REMOTE_NAME_INFO  //こちらは、テストしていない
        {
            string lpUniversalName;
            string lpConnectionName;
            string lpRemainingPath;
        }

        /* エラーコード一覧
         * WNetGetUniversalName固有のエラーコード
         *   http://msdn.microsoft.com/ja-jp/library/cc447067.aspx
         * System Error Codes (0-499)
         *   http://msdn.microsoft.com/en-us/library/windows/desktop/ms681382(v=vs.85).aspx
         */
        const int NO_ERROR                 = 0;
        const int ERROR_NOT_SUPPORTED      = 50;
        const int ERROR_MORE_DATA          = 234;
        const int ERROR_BAD_DEVICE         = 1200;
        const int ERROR_CONNECTION_UNAVAIL = 1201;
        const int ERROR_NO_NET_OR_BAD_PATH = 1203;
        const int ERROR_EXTENDED_ERROR     = 1208;
        const int ERROR_NO_NETWORK         = 1222;
        const int ERROR_NOT_CONNECTED      = 2250;


        /*
         * UNC変換ロジック本体
         */
        public static string GetUniversalName(string path_src)
        {
            string unc_path_dest = path_src; //解決できないエラーが発生した場合は、入力されたパスをそのまま戻す
            int size = 1;

            /*
             * 前処理
             *   意図的に、ERROR_MORE_DATAを発生させて、必要なバッファ・サイズ(size)を取得する。
             */
            //1バイトならば、確実にERROR_MORE_DATAが発生するだろうという期待。
            IntPtr lp_dummy = Marshal.AllocCoTaskMem(size);

            //サイズ取得をトライ
            int apiRetVal = WNetGetUniversalName(path_src, UNIVERSAL_NAME_INFO_LEVEL, lp_dummy, ref size);

            //ダミーを解放
            Marshal.FreeCoTaskMem(lp_dummy);


            /*
             * UNC変換処理
             */ 
            switch(apiRetVal)
            {
                case ERROR_MORE_DATA :
                    //受け取ったバッファ・サイズ(size)で再度メモリ確保
                    IntPtr lpBufUniversalNameInfo = Marshal.AllocCoTaskMem(size);
 
                    //UNCパスへの変換を実施する。
                    apiRetVal = WNetGetUniversalName(path_src, UNIVERSAL_NAME_INFO_LEVEL, lpBufUniversalNameInfo, ref size);
 
                    //UNIVERSAL_NAME_INFOを取り出す。
                    UNIVERSAL_NAME_INFO a = (UNIVERSAL_NAME_INFO)Marshal.PtrToStructure(lpBufUniversalNameInfo, typeof(UNIVERSAL_NAME_INFO));
 
                    //バッファを解放する
                    Marshal.FreeCoTaskMem(lpBufUniversalNameInfo);
 
                    if (apiRetVal == NO_ERROR)
                    {
                        //UNCに変換したパスを返す
                        unc_path_dest = a.lpUniversalName;
                    }
                    else
                    {
                        ErrorNotification.Invoke(new object(), path_src + "\nErrorCode:" + apiRetVal.ToString());
                    }
                    break;

                case ERROR_BAD_DEVICE   : //すでにUNC名(\\servername\test)
                case ERROR_NOT_CONNECTED: //ローカル・ドライブのパス(C:\test)
                    ErrorNotification.Invoke(new object(), path_src + "\nErrorCode:" + apiRetVal.ToString());
                    break;
                default:
                    ErrorNotification.Invoke(new object(), path_src + "\nErrorCode:" + apiRetVal.ToString());
                    break;
            }
            return unc_path_dest;
        }
        public static event EventHandler<string> ErrorNotification = delegate{};
    }
}
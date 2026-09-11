using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace CustomFunctions
{
    public static partial class MyTool
    {
        /// <summary>
        /// 「*」を使ってあいまい検索ができる「*」は検索できない
        /// </summary>
        public static bool FuzzyContains(string pKeyWord, string pSourceWord)
        {
            if (pKeyWord.Length == 0){
                return false;
            }
            var loKeyWord = pKeyWord.ToLower();
            // アスタリスクだけで検索していた場合 true
            if (loKeyWord.All(val => val == '*')){
                return true;
            }
            var loWord = pSourceWord.ToLower();
            var keyWordSplit = loKeyWord.Split('*');
            var notEmptyWords = keyWordSplit.Where(val => val != "");
            int searchPos = 0;
            foreach (var val in notEmptyWords)
            {
                if (val == string.Empty){
                    continue;
                }
                int findPos = loWord.IndexOf(val, searchPos);
                if (findPos == -1){
                    return false;
                }
                searchPos = findPos + val.Length;
            }
            return true;
        }
        public static Rectangle CheckWindowRectangle(
        Rectangle pWindowRectangle,
        int pMinWidth = 0,
        int pMinHeight = 0,
        bool pPrioriryOneToCursor = false)
        {
            const int HIDDEN_AREA = 7; //下、左、右の非表示領域の幅
            // 一番表示される面積が大きいモニタに表示する
            var screenRectangle = Screen.PrimaryScreen.Bounds;
            long crossSize = 0;
            foreach (var scrn in Screen.AllScreens)
            {
                if (pPrioriryOneToCursor){
                    var rect = new Rectangle{
                        Location = Cursor.Position,
                        Size = new Size(1, 1)
                    };
                    if (Rectangle.Intersect(scrn.Bounds, rect).Width == 1){
                        screenRectangle = scrn.Bounds;
                        break;
                    }
                }
                var overRapRect = Rectangle.Intersect(scrn.Bounds, pWindowRectangle);
                long curSize = overRapRect.Width * overRapRect.Height;
                if (crossSize < curSize){
                    crossSize = curSize;
                    screenRectangle = scrn.Bounds;
                }
            }
            // 画面からはみ出さないように調整
            int top;
            if (pWindowRectangle.Bottom > screenRectangle.Bottom){
                top = screenRectangle.Bottom - pWindowRectangle.Height + HIDDEN_AREA;
            }
            else if (pWindowRectangle.Top < screenRectangle.Top){
                top = screenRectangle.Top;
            }
            else{
                top = pWindowRectangle.Top;
            }
            int left;
            if (pWindowRectangle.Right > screenRectangle.Right){
                left = screenRectangle.Right - pWindowRectangle.Width + HIDDEN_AREA;
            }
            else if (pWindowRectangle.Left < screenRectangle.Left){
                left = screenRectangle.Left - HIDDEN_AREA;
            }
            else{
                left = pWindowRectangle.Left - HIDDEN_AREA;
            }
            int width = Math.Max(pWindowRectangle.Width, pMinWidth);
            int height = Math.Max(pWindowRectangle.Height, pMinHeight);
            return new Rectangle(left, top, width, height);
        }
        public static bool IsDarkTheme()
        {
            var key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            var name1 = "AppsUseLightTheme";
            //var name2 = "SystemUsesLightTheme";
            const int LIGHT_THEME = 1;
            const int DARK_THEME = 0;
            using (var subKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(key, false))
            {
                if (subKey == null){
                    return false;
                }
                var regValue = subKey.GetValue(name1);
                if (regValue == null){
                    return false;
                }
                switch ((int)regValue){
                    case LIGHT_THEME: return false;
                    case DARK_THEME: return true;
                    default: return false;
                }
            }
        }
    }
}
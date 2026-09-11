using System;
using System.IO;
using System.Drawing;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public static class MyIcon
    {
        static readonly object _iconLock = new object();
        static IOption<Icon> _icon = new None<Icon>();
        public static void SetIcon(Icon pIcon)
        {
            lock (_iconLock) _icon = new Some<Icon>(pIcon);
        }
        public static IOption<Icon> CloneIcon()
        {
            lock (_iconLock)
            {
                return _icon.Match(
                    none => _icon,
                    some => new Some<Icon>(some.CloneToIcon())
                );
            }
        }
        public static Icon CloneToIcon(this Icon self)
        {
            return (Icon)self.Clone();
        }
        public static IOption<Icon> ExtractAssociatedIconFromExecutingAssembly()
        {
            var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var tmpPath = Path.GetTempFileName();
            File.Copy(appPath, tmpPath, true);
            Icon clone;
            try
            {
                var icon = Icon.ExtractAssociatedIcon(tmpPath);
                clone = icon.CloneToIcon();
                icon.Dispose();
                return Some.New(clone);
            }
            catch
            {
                return new None<Icon>();
            }
            finally
            {
                if (File.Exists(tmpPath)){
                    File.Delete(tmpPath);
                }
            }
        }
    }
}
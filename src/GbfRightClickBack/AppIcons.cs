using System;
using System.IO;
using System.Windows.Forms;

namespace GbfRightClickBack;

/// <summary>Loader icon aplikasi (dipakai tray + jendela utama).</summary>
internal static class AppIcons
{
    public static Icon Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.ico");
        if (File.Exists(path))
        {
            try { return new Icon(path); } catch { /* fallback */ }
        }
        return SystemIcons.Application;
    }
}

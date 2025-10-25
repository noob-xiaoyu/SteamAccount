using Microsoft.Win32;
using System.IO;

namespace SteamAccountManager.Utils
{
    public static class RegistryHelper
    {
        public static string GetSteamPath()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                if (key != null)
                {
                    var exePath = key.GetValue("SteamExe") as string;
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                        return exePath;

                    var steamPath = key.GetValue("SteamPath") as string;
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        var fullPath = Path.Combine(steamPath.Replace("/", "\\"), "Steam.exe");
                        if (File.Exists(fullPath))
                            return fullPath;
                    }
                }
            }
            return null;
        }
    }
}
using Microsoft.Win32;

namespace PetDesk.App.Services;

internal static class SystemAppearance
{
    public static bool IsDark(string mode)
    {
        if (mode != "System")
        {
            return mode == "Dark";
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }
}

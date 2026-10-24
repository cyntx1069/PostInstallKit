using System.Collections.Generic;
using Microsoft.Win32;

namespace PostInstallKit;

public static class InstalledPrograms
{
    // Reads the names shown in Windows "Apps & features" (machine-wide and current user)
    public static List<string> GetDisplayNames()
    {
        var names = new List<string>();
        ReadUninstallKey(RegistryHive.LocalMachine, RegistryView.Registry64, names);
        ReadUninstallKey(RegistryHive.LocalMachine, RegistryView.Registry32, names);
        ReadUninstallKey(RegistryHive.CurrentUser, RegistryView.Registry64, names);
        return names;
    }

    private static void ReadUninstallKey(RegistryHive hive, RegistryView view, List<string> names)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstall = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall == null) return;

            foreach (var subName in uninstall.GetSubKeyNames())
            {
                using var sub = uninstall.OpenSubKey(subName);
                if (sub?.GetValue("DisplayName") is string name && name.Length > 0)
                    names.Add(name);
            }
        }
        catch
        {
            // Ignore keys we can't read
        }
    }
}
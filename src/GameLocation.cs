using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class GameLocation
{
    internal static string Find(string launcherFolder, IEnumerable<string> installedFolders)
    {
        string found = Near(launcherFolder);
        if (found != null) return found;
        foreach (string folder in installedFolders) {
            found = Near(folder);
            if (found != null) return found;
        }
        return null;
    }

    static string Near(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return null;
        try {
            string full = Path.GetFullPath(folder);
            if (File.Exists(Path.Combine(full, "FarCry6.exe"))) return full.TrimEnd(Path.DirectorySeparatorChar);
            string bin = Path.Combine(full, "bin");
            if (File.Exists(Path.Combine(bin, "FarCry6.exe"))) return bin;
            DirectoryInfo parent = Directory.GetParent(full.TrimEnd(Path.DirectorySeparatorChar));
            if (parent != null) {
                bin = Path.Combine(parent.FullName, "bin");
                if (File.Exists(Path.Combine(bin, "FarCry6.exe"))) return bin;
            }
        }
        catch (ArgumentException) { }
        catch (NotSupportedException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (SecurityException) { }
        return null;
    }

    internal static IEnumerable<string> InstalledFolders()
    {
        var folders = new List<string>();
        foreach (RegistryHive hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 }) {
            try {
                using (var root = RegistryKey.OpenBaseKey(hive, view))
                using (var key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Far Cry 6_is1")) {
                    string folder = key == null ? null : key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrWhiteSpace(folder)) folders.Add(folder);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (SecurityException) { }
        }
        return folders;
    }

    internal static string Choose()
    {
        using (var dialog = new FolderBrowserDialog()) {
            dialog.Description = "Select your Far Cry 6 installation folder.";
            dialog.ShowNewFolderButton = false;
            while (true) {
                if (dialog.ShowDialog() != DialogResult.OK) throw new OperationCanceledException();
                string found = Find(dialog.SelectedPath, new string[0]);
                if (found != null) return found;
                MessageBox.Show("Far Cry 6 wasn't found in that folder. Select the main game folder or its bin folder.",
                    "Select Far Cry 6", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}

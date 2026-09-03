using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class TyporaLocator
{
    internal const string ConfigFileName = "Typora.path.txt";

    internal static string ReadConfiguredPath(string directory)
    {
        try
        {
            string configPath = Path.Combine(directory, ConfigFileName);
            if (!File.Exists(configPath))
                return null;

            string path = File.ReadAllText(configPath, Encoding.UTF8).Trim();
            return IsTypora(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    internal static void WriteConfiguredPath(string directory, string typoraPath)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, ConfigFileName),
            Path.GetFullPath(typoraPath),
            new UTF8Encoding(false));
    }

    internal static List<string> FindCandidates()
    {
        var candidates = new List<string>();

        foreach (Process process in Process.GetProcessesByName("Typora"))
        {
            using (process)
            {
                try
                {
                    AddCandidate(candidates, process.MainModule.FileName);
                }
                catch
                {
                }
            }
        }

        AddCandidate(candidates, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Typora",
            "Typora.exe"));
        AddCandidate(candidates, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Typora",
            "Typora.exe"));
        AddCandidate(candidates, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Typora",
            "Typora.exe"));

        AddAppPath(candidates, Registry.CurrentUser);
        AddAppPath(candidates, Registry.LocalMachine);
        AddUninstallPaths(candidates, Registry.CurrentUser);
        AddUninstallPaths(candidates, Registry.LocalMachine);

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(GetFileVersion)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static string ChooseTypora(IEnumerable<string> candidates)
    {
        string first = candidates.FirstOrDefault();
        using (var dialog = new OpenFileDialog())
        {
            dialog.Title = "请选择 Typora.exe / Select Typora.exe";
            dialog.Filter = "Typora.exe|Typora.exe";
            dialog.Multiselect = false;
            if (first != null)
            {
                dialog.InitialDirectory = Path.GetDirectoryName(first);
                dialog.FileName = "Typora.exe";
            }

            if (dialog.ShowDialog() != DialogResult.OK)
                return null;
            if (!IsTypora(dialog.FileName))
            {
                MessageBox.Show(
                    "所选文件不是有效的 Typora.exe。\r\nThe selected file is not a valid Typora.exe.",
                    "Typora Multi-Window Launcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }
            return Path.GetFullPath(dialog.FileName);
        }
    }

    internal static bool IsTypora(string path)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(path)
                && File.Exists(path)
                && string.Equals(Path.GetFileName(path), "Typora.exe", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    internal static string GetDisplayVersion(string path)
    {
        try
        {
            string version = FileVersionInfo.GetVersionInfo(path).FileVersion;
            return string.IsNullOrWhiteSpace(version) ? "unknown" : version;
        }
        catch
        {
            return "unknown";
        }
    }

    private static void AddCandidate(ICollection<string> candidates, string path)
    {
        path = CleanRegistryPath(path);
        if (IsTypora(path))
            candidates.Add(Path.GetFullPath(path));
    }

    private static void AddAppPath(ICollection<string> candidates, RegistryKey root)
    {
        try
        {
            using (RegistryKey key = root.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\App Paths\Typora.exe"))
            {
                if (key != null)
                    AddCandidate(candidates, Convert.ToString(key.GetValue(null)));
            }
        }
        catch
        {
        }
    }

    private static void AddUninstallPaths(ICollection<string> candidates, RegistryKey root)
    {
        string[] uninstallRoots =
        {
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
            @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (string uninstallRoot in uninstallRoots)
        {
            try
            {
                using (RegistryKey rootKey = root.OpenSubKey(uninstallRoot))
                {
                    if (rootKey == null)
                        continue;

                    foreach (string subKeyName in rootKey.GetSubKeyNames())
                    {
                        using (RegistryKey appKey = rootKey.OpenSubKey(subKeyName))
                        {
                            if (appKey == null)
                                continue;

                            string displayName = Convert.ToString(appKey.GetValue("DisplayName"));
                            if (displayName.IndexOf("Typora", StringComparison.OrdinalIgnoreCase) < 0)
                                continue;

                            string installLocation = Convert.ToString(appKey.GetValue("InstallLocation"));
                            if (!string.IsNullOrWhiteSpace(installLocation))
                                AddCandidate(candidates, Path.Combine(installLocation.Trim('"'), "Typora.exe"));
                            AddCandidate(candidates, Convert.ToString(appKey.GetValue("DisplayIcon")));
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }

    private static string CleanRegistryPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string path = value.Trim();
        if (path.StartsWith("\"", StringComparison.Ordinal))
        {
            int closingQuote = path.IndexOf('"', 1);
            if (closingQuote > 1)
                return path.Substring(1, closingQuote - 1);
        }

        int comma = path.LastIndexOf(',');
        int iconIndex;
        if (comma > 0 && int.TryParse(path.Substring(comma + 1), out iconIndex))
            path = path.Substring(0, comma);
        return path.Trim().Trim('"');
    }

    private static Version GetFileVersion(string path)
    {
        Version version;
        return Version.TryParse(GetDisplayVersion(path), out version) ? version : new Version(0, 0);
    }
}

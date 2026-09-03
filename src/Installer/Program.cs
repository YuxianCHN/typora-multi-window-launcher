using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
    private const string RegisteredName = "TyporaMultiWindowLauncher";
    private const string DisplayName = "Typora Multi-Window Launcher";
    private const string ProgId = "TyporaMultiWindowLauncher.md";
    private const string LauncherFileName = "TyporaMultiWindowLauncher.exe";
    private const uint ShcneAssocChanged = 0x08000000;
    private const uint ShcnfIdList = 0x0000;

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();

        try
        {
            string packageDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string sourceLauncher = Path.Combine(packageDirectory, LauncherFileName);
            if (!File.Exists(sourceLauncher))
                throw new FileNotFoundException("安装包中缺少 " + LauncherFileName, sourceLauncher);

            List<string> candidates = TyporaLocator.FindCandidates();
            string typoraPath = candidates.Count == 1 ? candidates[0] : TyporaLocator.ChooseTypora(candidates);
            if (typoraPath == null)
                return;

            DialogResult confirmation = MessageBox.Show(
                "将使用以下 Typora：\r\n" + typoraPath + "\r\n\r\n" +
                "版本 / Version: " + TyporaLocator.GetDisplayVersion(typoraPath) + "\r\n\r\n继续安装？ / Continue?",
                DisplayName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirmation != DialogResult.Yes)
                return;

            string installDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                RegisteredName);
            string installedLauncher = Path.Combine(installDirectory, LauncherFileName);

            Directory.CreateDirectory(installDirectory);
            File.Copy(sourceLauncher, installedLauncher, true);
            TyporaLocator.WriteConfiguredPath(installDirectory, typoraPath);

            RegisterApplication(installedLauncher);
            SHChangeNotify(ShcneAssocChanged, ShcnfIdList, IntPtr.Zero, IntPtr.Zero);

            MessageBox.Show(
                "安装完成。\r\n\r\n" +
                "Windows 将打开“默认应用”页面。搜索“" + DisplayName + "”，并把 .md 设置为它。\r\n" +
                "Windows 11 requires you to select the .md default app manually.",
                DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Process.Start(new ProcessStartInfo(
                "ms-settings:defaultapps?registeredAppUser=" + Uri.EscapeDataString(RegisteredName))
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "安装失败 / Installation failed:\r\n\r\n" + ex.Message,
                DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void RegisterApplication(string launcherPath)
    {
        string command = "\"" + launcherPath + "\" \"%1\"";

        SetDefaultValue(@"Software\Classes\" + ProgId, "Markdown document (Typora multi-window)");
        SetDefaultValue(@"Software\Classes\" + ProgId + @"\DefaultIcon", launcherPath + ",0");
        SetDefaultValue(@"Software\Classes\" + ProgId + @"\shell\open\command", command);

        using (RegistryKey openWith = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.md\OpenWithProgids"))
            openWith.SetValue(ProgId, new byte[0], RegistryValueKind.None);

        string applicationKey = @"Software\Classes\Applications\" + LauncherFileName;
        SetDefaultValue(applicationKey + @"\shell\open\command", command);
        using (RegistryKey supportedTypes = Registry.CurrentUser.CreateSubKey(applicationKey + @"\SupportedTypes"))
            supportedTypes.SetValue(".md", "", RegistryValueKind.String);

        string capabilities = @"Software\" + RegisteredName + @"\Capabilities";
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(capabilities))
        {
            key.SetValue("ApplicationName", DisplayName, RegistryValueKind.String);
            key.SetValue(
                "ApplicationDescription",
                "Open each Markdown file in a separate Typora window",
                RegistryValueKind.String);
        }
        using (RegistryKey associations = Registry.CurrentUser.CreateSubKey(capabilities + @"\FileAssociations"))
            associations.SetValue(".md", ProgId, RegistryValueKind.String);

        using (RegistryKey registered = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
            registered.SetValue(RegisteredName, capabilities, RegistryValueKind.String);
    }

    private static void SetDefaultValue(string keyPath, string value)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
            key.SetValue(null, value, RegistryValueKind.String);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
}

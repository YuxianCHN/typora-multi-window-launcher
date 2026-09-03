using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
    private const string RegisteredName = "TyporaMultiWindowLauncher";
    private const string ProgId = "TyporaMultiWindowLauncher.md";
    private const string LauncherFileName = "TyporaMultiWindowLauncher.exe";
    private const uint ShcneAssocChanged = 0x08000000;
    private const uint ShcnfIdList = 0x0000;

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();

        if (MessageBox.Show(
            "确定卸载 Typora Multi-Window Launcher？\r\n\r\n" +
            "注册信息和程序文件会被删除，独立配置数据会保留。\r\n" +
            "Remove registration and program files? Isolated profile data will be kept.",
            "Uninstall Typora Multi-Window Launcher",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            RemoveRegistration();
            RemoveInstalledFiles();
            SHChangeNotify(ShcneAssocChanged, ShcnfIdList, IntPtr.Zero, IntPtr.Zero);

            MessageBox.Show(
                "卸载完成。请在接下来打开的 Windows 设置中重新选择 .md 的默认应用。\r\n" +
                "Uninstalled. Select a new default app for .md in Windows Settings.",
                "Uninstall Typora Multi-Window Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "卸载未完成 / Uninstall failed:\r\n\r\n" + ex.Message,
                "Uninstall Typora Multi-Window Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void RemoveRegistration()
    {
        using (RegistryKey registered = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", true))
        {
            if (registered != null)
                registered.DeleteValue(RegisteredName, false);
        }

        using (RegistryKey openWith = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.md\OpenWithProgids", true))
        {
            if (openWith != null)
                openWith.DeleteValue(ProgId, false);
        }

        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + ProgId, false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Applications\" + LauncherFileName, false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\" + RegisteredName, false);
    }

    private static void RemoveInstalledFiles()
    {
        string installDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            RegisteredName);
        string launcher = Path.Combine(installDirectory, LauncherFileName);
        string config = Path.Combine(installDirectory, TyporaLocator.ConfigFileName);

        if (File.Exists(launcher))
            File.Delete(launcher);
        if (File.Exists(config))
            File.Delete(config);
        if (Directory.Exists(installDirectory) && Directory.GetFileSystemEntries(installDirectory).Length == 0)
            Directory.Delete(installDirectory, false);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
}

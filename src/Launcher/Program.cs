using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class Program
{
    private const string ProductName = "Typora Multi-Window Launcher";

    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string[] files = GetFiles(args);
        if (files.Length == 0)
            return;

        string typoraPath = ResolveTyporaPath();
        if (typoraPath == null)
            return;

        string profilePath = GetProfilePath(typoraPath);
        try
        {
            Directory.CreateDirectory(profilePath);
        }
        catch (Exception ex)
        {
            ShowError("无法创建专用配置目录。\r\nCannot create the isolated profile.\r\n\r\n" + ex.Message);
            return;
        }

        var failures = new List<string>();
        for (int index = 0; index < files.Length; index++)
        {
            try
            {
                OpenFile(typoraPath, profilePath, files[index]);
                Thread.Sleep(index == 0 ? 1200 : 500);
            }
            catch (Exception ex)
            {
                failures.Add(Path.GetFileName(files[index]) + "：" + ex.Message);
            }
        }

        if (failures.Count > 0)
        {
            MessageBox.Show(
                "以下文件未能打开 / Failed to open:\r\n\r\n" + string.Join("\r\n", failures.ToArray()),
                ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static string[] GetFiles(string[] args)
    {
        string[] files = args
            .Select(RemoveOuterQuotes)
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length > 0)
            return files;

        using (var dialog = new OpenFileDialog())
        {
            dialog.Title = "选择 Markdown 文件 / Select Markdown files";
            dialog.Filter = "Markdown|*.md;*.markdown;*.mdown;*.mkd|Text|*.txt|All files|*.*";
            dialog.Multiselect = true;
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileNames : new string[0];
        }
    }

    private static string ResolveTyporaPath()
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string configured = TyporaLocator.ReadConfiguredPath(baseDirectory);
        if (configured != null)
            return configured;

        List<string> candidates = TyporaLocator.FindCandidates();
        string selected = candidates.Count == 1 ? candidates[0] : TyporaLocator.ChooseTypora(candidates);
        if (selected == null)
            return null;

        try
        {
            TyporaLocator.WriteConfiguredPath(baseDirectory, selected);
        }
        catch (Exception ex)
        {
            ShowError("无法保存 Typora 路径。\r\nCannot save the Typora path.\r\n\r\n" + ex.Message);
            return null;
        }
        return selected;
    }

    private static string GetProfilePath(string typoraPath)
    {
        string version = MakeFileNameSafe(TyporaLocator.GetDisplayVersion(typoraPath));
        string identity;
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(Path.GetFullPath(typoraPath).ToLowerInvariant());
            identity = BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", "").Substring(0, 12);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TyporaMultiWindowLauncher",
            "Profiles",
            version + "-" + identity);
    }

    private static string MakeFileNameSafe(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (char character in value)
            result.Append(char.IsLetterOrDigit(character) || character == '.' || character == '-' ? character : '_');
        return result.Length == 0 ? "unknown" : result.ToString();
    }

    private static void OpenFile(string typoraPath, string profilePath, string filePath)
    {
        string arguments = "--user-data-dir=" + Quote(profilePath) + " " + Quote(filePath);
        Process process = Process.Start(new ProcessStartInfo(typoraPath, arguments)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(typoraPath)
        });

        if (process == null)
            throw new InvalidOperationException("无法启动 Typora / Cannot start Typora");
        process.Dispose();
    }

    private static string Quote(string value)
    {
        return "\"" + value + "\"";
    }

    private static string RemoveOuterQuotes(string value)
    {
        if (value != null && value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            return value.Substring(1, value.Length - 2);
        return value;
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

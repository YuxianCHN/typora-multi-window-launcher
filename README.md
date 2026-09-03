# Typora Multi-Window Launcher

在 Windows 10/11 中双击 Markdown 文件时，让每个文件分别打开为独立的 Typora 顶层窗口。

> 本项目不包含 Typora，也不修改 Typora 安装文件。使用者需要自行安装并合法使用 Typora。

## 工作原理

安装器将本程序注册为 `.md` 的可选打开方式。Windows 把双击的文件路径传给启动器后，启动器使用一个按“Typora 版本 + 安装路径”隔离的用户数据目录启动 Typora：

```text
双击 .md
  → TyporaMultiWindowLauncher.exe
  → Typora.exe --user-data-dir=<独立配置目录> <文件路径>
  → 独立 Windows 顶层窗口
```

同一个 Typora 版本和安装位置会复用同一套隔离配置；不同版本或不同安装位置不会混用配置。

## 支持范围

- Windows 10/11。
- 官方安装版或便携版 `Typora.exe`。
- 不限制 Typora 版本号，但要求该版本接受 Chromium/Electron 的 `--user-data-dir` 参数。
- 已在 Typora 1.12.1（Windows x64）上完成端到端验证。

无法对尚未发布的 Typora 版本作绝对兼容保证。如果 Typora 将来停止接受该参数，启动器会失效，但不会修改或损坏 Markdown 文件。

## 安装

1. 从 Releases 下载并解压发布包。
2. 运行 `Install.exe`。
3. 安装器会自动查找 Typora；发现多个版本或无法找到时，选择你希望使用的 `Typora.exe`。
4. 在打开的 Windows“默认应用”页面中搜索 `Typora Multi-Window Launcher`。
5. 将 `.md` 设置为该程序。

Windows 11 不允许桌面程序静默替用户更改受保护的默认应用选择，因此第 5 步必须由用户本人完成。

安装仅针对当前 Windows 用户，不需要管理员权限。

## 使用

设置完成后，直接双击任意 `.md` 文件。即使已经存在 Typora 窗口，新文件也会出现在另一个独立窗口中。

也可以直接运行 `TyporaMultiWindowLauncher.exe`，一次选择多个文件，或把多个文件拖到程序图标上。

## 配置与隐私

- 程序不联网、不上传文档，也不收集遥测。
- Typora 路径保存在安装目录旁的纯文本文件 `Typora.path.txt` 中。
- 隔离配置位于 `%LOCALAPPDATA%\TyporaMultiWindowLauncher\Profiles`。
- 独立窗口不会自动继承普通 Typora 配置中的主题和偏好设置。
- 首次创建某个版本的隔离配置时，Typora 可能显示一次欢迎页或更新提示。

## 卸载

运行发布包中的 `Uninstall.exe`，然后在 Windows“默认应用”中重新选择 `.md` 的打开程序。

卸载器只删除注册信息、已安装的启动器和路径配置。为避免误删草稿或恢复数据，`%LOCALAPPDATA%\TyporaMultiWindowLauncher\Profiles` 会保留，需要时由用户自行处理。

## 从源码构建

要求：Windows 10/11、Windows PowerShell 5.1，以及系统自带的 .NET Framework C# 编译器。

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

构建结果位于 `dist`。程序只依赖 Windows 自带的 .NET Framework 和 Win32 API，不需要安装第三方依赖。

## 安全说明

发布包未进行商业代码签名，Windows SmartScreen 可能提示“未知发布者”。建议从本仓库 Releases 下载，并对照发布页的 SHA-256。

本项目与 Typora 及其开发者无隶属或授权关系。Typora 是其相应权利人的商标和商业软件。

## English

This Windows utility registers itself as an optional `.md` handler and launches each Markdown file in a separate Typora window by using an isolated `--user-data-dir`. Typora itself is not included or modified. Windows 10/11 is supported; Typora 1.12.1 has been tested. Other versions are supported on a best-effort basis as long as they accept the Chromium/Electron launch argument.

## License

MIT. See [LICENSE](LICENSE).

# 文件占用释放 · File Occupied Release

适用于 Windows 10/11 x64 的独立文件占用查看与释放工具。界面随 Windows 显示语言启动，可通过右上角按钮切换简体中文和英文。

A standalone Windows 10/11 x64 tool for finding and releasing file locks. The app follows the Windows display language at launch and provides a Simplified Chinese / English toggle.

## 使用方法

1. 输入路径、通过「浏览」选择，或把文件和文件夹拖入窗口。可以一次选择多个文件，手动输入多个路径时用 `|` 分隔。
2. 点击「检测占用」。对文件夹，工具检查其中的文件。勾选「递归查询」时也检查子文件夹中的文件。
3. 从列表选择一个进程，点击「正常关闭」请求应用退出。如果需要强制结束，使用「强制结束」并确认。强制结束可能丢失未保存的数据。

## Usage

1. Type a path, use **Browse**, or drop files and folders into the window. For multiple typed paths, separate them with `|`.
2. Click **Scan for locks**. For folders, the app checks the files they contain. **Recursive** also includes files in subfolders.
3. Select a process and click **Close normally** to request that its application exit. **Force end** requires confirmation and may discard unsaved work.

## 实现 / Implementation

- C# WinForms 直接调用 Windows Restart Manager 的 `RmStartSession`、`RmRegisterResources` 和 `RmGetList`。按批注册文件路径，并用 PID 和进程启动时间去重。操作前重新检查占用，退出后自动重新扫描。
- The WinForms app calls Windows Restart Manager directly, registers paths in batches, and identifies each process by PID and creation time. It rechecks ownership before closing a process and refreshes the results after it exits. See [Windows Restart Manager](https://learn.microsoft.com/en-us/windows/win32/rstmgr/restart-manager-portal).
- 文件夹扫描跳过内部重解析点，且结果受 Restart Manager 的检测范围限制。仅持有目录句柄、仅把目录用作工作目录的进程可能无法检测。系统服务、Windows 资源管理器和受保护进程不提供释放操作。
- Folder traversal skips reparse points. Restart Manager detection may miss processes that hold only a directory handle or use a folder only as their working directory. Service, Explorer, and protected processes cannot be closed through the app.

## 编译和测试 / Build and test

在 Windows 上使用 .NET 8 SDK。下列发布命令生成自包含的单文件 EXE，可直接运行。

Use the .NET 8 SDK on Windows. The publish command creates a self-contained single-file executable.

```powershell
dotnet run --project tests/SmokeTests.csproj -c Release
dotnet publish src/FileOccupiedRelease.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish
```

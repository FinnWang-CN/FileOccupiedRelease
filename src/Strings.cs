namespace FileOccupiedRelease;

internal sealed record Strings(
    string Title, string Path, string File, string Folder, string Scan, string Cancel,
    string Results, string Process, string Pid, string Close, string Force, string Ready, string Scanning,
    string Cancelling, string Cancelled, string Empty, string Count, string Confirm, string Releasing,
    string Closed, string Gone, string Protected, string NoWindow, string StillRunning,
    string MissingPath, string AccessDenied, string WindowsError, string Error,
    string Browse, string Recursive, string Drop, string DropHint, string Kind, string State,
    string App, string Console, string Service, string System, string Unknown, string InUse,
    string Clear, string Selection, string NoLongerUsingPath,
    string SupportAuthor, string SupportThanks, string SupportQr, string SupportInstructions, string Dismiss)
{
    public static Strings Chinese { get; } = new(
        "文件占用释放", "路径", "选择文件…", "选择文件夹…", "检测占用", "取消扫描",
        "占用进程", "程序", "PID", "正常关闭", "强制结束",
        "输入路径，或拖入文件和文件夹。", "正在扫描占用…", "正在取消扫描…", "扫描已取消。",
        "未检测到占用进程。", "检测到 {0} 个进程。", "强制结束「{0}」（PID {1}）可能丢失未保存的数据。是否继续？",
        "正在关闭选中的进程…", "选中的进程已关闭。", "选中的进程已退出或已更换。",
        "此进程受保护，无法关闭。", "无法请求正常关闭。请在程序内退出，或使用「强制结束」。",
        "进程仍在运行。请检查程序中是否有保存提示。",
        "请选择存在的文件或文件夹。", "访问被拒绝。", "Windows 操作失败（错误 {0}）。",
        "操作失败。请检查目标路径或进程状态。",
        "浏览…", "递归查询", "拖放文件或文件夹", "可一次拖入多个项目。递归查询会检查子文件夹中的文件。",
        "类型", "状态", "应用", "控制台", "服务", "系统", "未知", "占用中",
        "清空", "已选 {0} 个进程", "选中的进程已不再占用目标。",
        "赞赏作者", "感谢你支持「文件占用释放」的持续开发。", "Finn Wang 的微信赞赏码",
        "可用微信扫一扫，或截图后在「扫一扫」中从相册识别。赞赏完全自愿。", "关闭");

    public static Strings English { get; } = new(
        "File Occupied Release", "Path", "Choose file…", "Choose folder…", "Scan for locks", "Cancel scan",
        "Processes using this path", "Process name", "PID", "Close normally", "Force end",
        "Enter a path, or drop files and folders here.", "Scanning for locks…", "Cancelling scan…", "Scan cancelled.",
        "No lock owners detected.", "Processes found: {0}.", "Force-ending {0} (PID {1}) may discard unsaved work. Continue?",
        "Closing the selected process…", "The selected process closed.", "The selected process exited or changed.",
        "This process is protected and cannot be closed.", "A normal close could not be requested. Exit the application or use Force end.",
        "The process is still running. Check for a save prompt in the application.",
        "Choose an existing file or folder.", "Access denied.", "The Windows operation failed (error {0}).",
        "The operation failed. Check the path or process state.",
        "Browse…", "Recursive", "Drop files or folders", "Drop multiple items. Recursive scanning checks files in subfolders.",
        "Type", "Status", "App", "Console", "Service", "System", "Unknown", "In use",
        "Clear", "{0} selected", "The selected process is no longer using the target.",
        "Support the author", "Thank you for supporting File Occupied Release.", "Finn Wang’s WeChat appreciation code",
        "Scan with WeChat or open a screenshot in WeChat’s scanner. Support is optional.", "Close");
}

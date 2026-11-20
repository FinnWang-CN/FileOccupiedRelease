using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FileOccupiedRelease;

public enum ReleaseResult { Closed, Gone, Protected, NoWindow, StillRunning }

public static class ProcessRelease
{
    private const uint QueryLimitedInformation = 0x1000;
    private const uint Synchronize = 0x100000;
    private const uint Terminate = 0x0001;
    private const uint WaitTimeout = 258;
    private const int InvalidParameter = 87;

    public static ReleaseResult Release(LockOwner owner, bool force)
    {
        if (!owner.CanClose)
            return ReleaseResult.Protected;

        // Pin the verified process object through shutdown, even if it exits during this call.
        using var handle = OpenProcess(QueryLimitedInformation | Synchronize | (force ? Terminate : 0), false, owner.Pid);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            return error == InvalidParameter ? ReleaseResult.Gone : throw new Win32Exception(error);
        }

        if (HasExited(handle, 0))
            return ReleaseResult.Gone;
        if (!GetProcessTimes(handle, out long started, out _, out _, out _))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        if (started != owner.StartTime)
            return ReleaseResult.Gone;
        if (!IsProcessCritical(handle, out bool critical))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        if (critical)
            return ReleaseResult.Protected;

        try
        {
            using var process = Process.GetProcessById(owner.Pid);
            if (process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                return ReleaseResult.Protected;

            if (force)
            {
                if (!TerminateProcess(handle, 1))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            else if (!process.CloseMainWindow())
                return HasExited(handle, 0) ? ReleaseResult.Gone : ReleaseResult.NoWindow;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or Win32Exception
            && HasExited(handle, 0))
        {
            return ReleaseResult.Gone;
        }

        return HasExited(handle, 3000) ? ReleaseResult.Closed : ReleaseResult.StillRunning;
    }

    private static bool HasExited(SafeProcessHandle process, uint milliseconds) =>
        WaitForSingleObject(process, milliseconds) switch
        {
            0 => true,
            WaitTimeout => false,
            _ => throw new Win32Exception(Marshal.GetLastWin32Error())
        };

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(SafeProcessHandle process, uint exitCode);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessTimes(SafeProcessHandle process, out long creation, out long exit,
        out long kernel, out long user);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsProcessCritical(SafeProcessHandle process, [MarshalAs(UnmanagedType.Bool)] out bool critical);
}

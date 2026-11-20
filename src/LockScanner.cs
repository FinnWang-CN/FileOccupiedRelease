using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace FileOccupiedRelease;

public sealed record LockOwner(int Pid, long StartTime, string Name, int ApplicationType)
{
    public bool CanClose => Pid > 4 && Pid != Environment.ProcessId && ApplicationType is 0 or 1 or 2 or 5;
}

public static class LockScanner
{
    private const int MoreData = 234;
    private const int BatchSize = 128;

    public static LockOwner[] Scan(string path, CancellationToken cancellation) =>
        Scan([path], recursive: true, cancellation);

    public static LockOwner[] Scan(string[] paths, bool recursive, CancellationToken cancellation)
    {
        var owners = new Dictionary<(int Pid, long StartTime), LockOwner>();
        var files = paths.SelectMany(path => GetFiles(path, recursive, cancellation));
        foreach (var batch in files.Chunk(BatchSize))
        {
            cancellation.ThrowIfCancellationRequested();
            foreach (var info in QueryBatch(batch.Distinct(StringComparer.Ordinal).ToArray()))
            {
                var owner = new LockOwner(info.Process.Pid, info.Process.StartTime, info.Name, info.Type);
                owners.TryAdd((owner.Pid, owner.StartTime), owner);
            }
        }
        cancellation.ThrowIfCancellationRequested();
        return owners.Values.OrderBy(owner => owner.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(owner => owner.Pid).ToArray();
    }

    private static IEnumerable<string> GetFiles(string path, bool recursive, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        path = Path.GetFullPath(path);
        return File.GetAttributes(path).HasFlag(FileAttributes.Directory)
            ? Directory.EnumerateFiles(path, "*", new EnumerationOptions
            {
                RecurseSubdirectories = recursive,
                IgnoreInaccessible = false,
                AttributesToSkip = FileAttributes.ReparsePoint
            })
            : [path];
    }

    private static ProcessInfo[] QueryBatch(string[] files)
    {
        var key = new StringBuilder(33);
        Check(RmStartSession(out uint session, 0, key));
        try
        {
            Check(RmRegisterResources(session, (uint)files.Length, files, 0, IntPtr.Zero, 0, IntPtr.Zero));
            ProcessInfo[]? buffer = null;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                uint count = (uint)(buffer?.Length ?? 0);
                int result = RmGetList(session, out uint needed, ref count, buffer, out _);
                if (result == MoreData)
                {
                    buffer = new ProcessInfo[needed];
                    continue;
                }
                Check(result);
                return buffer is null ? [] : buffer[..(int)count];
            }
            throw new Win32Exception(MoreData);
        }
        finally
        {
            RmEndSession(session);
        }
    }

    private static void Check(int result)
    {
        if (result != 0)
            throw new Win32Exception(result);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UniqueProcess
    {
        public int Pid;
        // FILETIME is two DWORDs with 4-byte alignment, including in x64 structures.
        public uint StartLow;
        public uint StartHigh;
        public readonly long StartTime => (long)StartHigh << 32 | StartLow;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessInfo
    {
        public UniqueProcess Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string ServiceName;
        public int Type;
        public uint Status;
        public uint SessionId;
        [MarshalAs(UnmanagedType.Bool)]
        public bool Restartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int RmStartSession(out uint session, int flags, StringBuilder key);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int RmRegisterResources(uint session, uint fileCount, string[] files, uint appCount,
        IntPtr apps, uint serviceCount, IntPtr services);

    [DllImport("rstrtmgr.dll", ExactSpelling = true)]
    private static extern int RmGetList(uint session, out uint needed, ref uint count, [Out] ProcessInfo[]? apps,
        out uint rebootReasons);

    [DllImport("rstrtmgr.dll", ExactSpelling = true)]
    private static extern int RmEndSession(uint session);
}

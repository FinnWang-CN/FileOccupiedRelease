using System.Diagnostics;
using FileOccupiedRelease;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 0)
        {
            using var file = File.Open(args[1], FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            if (args[0] == "window")
            {
                using var window = new Form { Text = "File lock smoke test", ShowInTaskbar = true, Opacity = 0 };
                window.Shown += (_, _) => Console.WriteLine("ready");
                Application.Run(window);
            }
            else
            {
                Console.WriteLine("ready");
                Thread.Sleep(TimeSpan.FromMinutes(1));
            }
            return;
        }

        string root = Directory.CreateTempSubdirectory("file-lock-test-").FullName;
        try
        {
            string nested = Directory.CreateDirectory(Path.Combine(root, "目录")).FullName;
            string path = Path.Combine(nested, "占用 file !.txt");
            File.WriteAllText(path, "test");
            Check(LockScanner.Scan(path, default).Length == 0, "An unlocked file has no detected owners.");
            Check(LockScanner.Scan(Directory.CreateDirectory(Path.Combine(root, "empty")).FullName, default).Length == 0,
                "An empty directory has no detected owners.");
            TestRelease(path, root, force: false);
            TestRelease(path, root, force: true);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
        Console.WriteLine("PASS: Unicode file, recursive scan, empty folder, PID identity, graceful close, forced release.");
    }

    private static void TestRelease(string path, string root, bool force)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, UseShellExecute = false };
        start.ArgumentList.Add(force ? "headless" : "window");
        start.ArgumentList.Add(path);
        using var child = Process.Start(start)!;
        try
        {
            Check(child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult() == "ready",
                "Fixture acquired the file lock.");
            var owner = LockScanner.Scan(path, default).Single(item => item.Pid == child.Id);
            Check(LockScanner.Scan(root, default).Count(item => item.Pid == child.Id) == 1,
                "Recursive scan identifies the same process once.");
            Check(LockScanner.Scan([root], recursive: false, default).All(item => item.Pid != child.Id),
                "A shallow scan does not enumerate nested files.");
            Check(LockScanner.Scan([root, path], recursive: true, default).Count(item => item.Pid == child.Id) == 1,
                "Overlapping paths only show the process once.");
            Check(ProcessRelease.Release(owner with { StartTime = owner.StartTime - 10_000 }, force) == ReleaseResult.Gone,
                "A different process identity is rejected.");
            Check(!child.HasExited, "Identity mismatch left the process running.");
            var outcome = ProcessRelease.Release(owner, force);
            Check(outcome == ReleaseResult.Closed, $"Release returned {outcome}; force={force}; type={owner.ApplicationType}; HWND={child.MainWindowHandle}.");
            Check(child.WaitForExit(5000), "The fixture process exited.");
            Check(ProcessRelease.Release(owner, force) == ReleaseResult.Gone, "An exited process is handled.");
            Check(LockScanner.Scan(path, default).Length == 0, "A rescan finds no file owner.");
            using var reopened = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            if (!child.HasExited)
            {
                child.Kill();
                child.WaitForExit();
            }
        }
    }

    private static void Check(bool success, string message)
    {
        if (!success)
            throw new InvalidOperationException(message);
    }
}

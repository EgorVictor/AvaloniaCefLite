namespace MyBrowser.Subprocess
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using MyBrowser;
    using MyBrowser.Interop;

    internal class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            var pid = Environment.ProcessId;
            var logDir = AppContext.BaseDirectory;
            var startupLog = Path.Combine(logDir, $"subprocess-startup-{pid}.log");
            try
            {
                File.AppendAllText(startupLog,
                    $"[{DateTime.Now:HH:mm:ss.fff}] PID={pid} CommandLine={Environment.CommandLine}\n" +
                    $"BaseDir={logDir}\n" +
                    $"Args={string.Join(" | ", args)}\n");
            }
            catch { }

            ConfigureCefNativeSearchPath();

            var options = new CefRuntimeOptions
            {
                RuntimePath = GetRuntimePath(),
                DisableGpu = true,
                MultiThreadedMessageLoop = true,
                CompatibilityMode = CefCompatibilityMode.ModernWindows,
                Win7RenderMode = CefWin7RenderMode.SafeNoGpu
            };

            var exitCode = CefRuntime.ExecuteMainProcess(GetModuleHandle(null), options);
            try { File.AppendAllText(startupLog, $"[{DateTime.Now:HH:mm:ss.fff}] ExecuteMainProcess returned: {exitCode}\n"); } catch { }
            return exitCode < 0 ? 0 : exitCode;
        }

        private static string GetRuntimePath()
        {
            var driverName = "Cef109";
            var baseDir = AppContext.BaseDirectory;
            var runtimePath = Path.Combine(baseDir, "Runtimes", driverName);

            if (Directory.Exists(runtimePath))
            {
                return runtimePath;
            }

            var altPath = Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "MyBrowser.Demo", "bin", "Debug", "net8.0", "Runtimes", driverName);
            if (Directory.Exists(altPath))
            {
                return altPath;
            }

            return string.Empty;
        }

        private static void ConfigureCefNativeSearchPath()
        {
            var runtimePath = GetRuntimePath();

            if (!string.IsNullOrEmpty(runtimePath) && Directory.Exists(runtimePath))
            {
                SetDllDirectory(runtimePath);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}

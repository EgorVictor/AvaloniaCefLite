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

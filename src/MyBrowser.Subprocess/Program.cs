namespace MyBrowser.Subprocess
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using MyBrowser.Interop;

    internal class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            ConfigureCefNativeSearchPath();

            var exitCode = CefRuntime.ExecuteMainProcess(GetModuleHandle(null));
            return exitCode < 0 ? 0 : exitCode;
        }

        private static void ConfigureCefNativeSearchPath()
        {
            var driverName = "Cef109";
            var baseDir = AppContext.BaseDirectory;
            var runtimePath = Path.Combine(baseDir, "Runtimes", driverName);

            if (Directory.Exists(runtimePath))
            {
                SetDllDirectory(runtimePath);
            }
            else
            {
                var altPath = Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "MyBrowser.Demo", "bin", "Debug", "net8.0", "Runtimes", driverName);
                if (Directory.Exists(altPath))
                {
                    SetDllDirectory(altPath);
                }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}

namespace MyBrowser.Demo
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using MyBrowser.Interop;

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ConfigureCefNativeSearchPath();

            var cefExitCode = CefRuntime.ExecuteMainProcess(GetModuleHandle(null));
            if (cefExitCode >= 0)
            {
                Environment.Exit(cefExitCode);
                return;
            }

            ResetLogFile();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static void ConfigureCefNativeSearchPath()
        {
            var driverName = Environment.OSVersion.Version.Major < 10 ? "Legacy" : "Modern";
            var runtimePath = Path.Combine(AppContext.BaseDirectory, "Runtimes", driverName);

            if (Directory.Exists(runtimePath))
            {
                SetDllDirectory(runtimePath);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private static void ResetLogFile()
        {
            var logFile = LogHelper.GetLogPath();
            var debugFile = Path.Combine(AppContext.BaseDirectory, "cef_debug.log");
            try
            {
                if (File.Exists(logFile))
                {
                    File.Delete(logFile);
                    File.Delete(debugFile);
                }
            }
            catch
            {
            }
        }
    }
}

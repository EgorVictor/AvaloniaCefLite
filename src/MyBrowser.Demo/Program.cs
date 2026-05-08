namespace MyBrowser.Demo
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using MyBrowser;
    using MyBrowser.Interop;

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var isCefSubprocess = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--type=", StringComparison.OrdinalIgnoreCase))
                {
                    isCefSubprocess = true;
                    break;
                }
            }

            if (isCefSubprocess)
            {
                ConfigureCefNativeSearchPath();
                var subExitCode = CefRuntime.ExecuteMainProcess(GetModuleHandle(null));
                Environment.Exit(subExitCode);
                return;
            }

            WriteEarlyLog("[Program] === Application starting ===");
            WriteEarlyLog("[Program] OS Version: {0}", Environment.OSVersion.VersionString);
            WriteEarlyLog("[Program] CLR Version: {0}", Environment.Version);
            WriteEarlyLog("[Program] Base Directory: {0}", AppContext.BaseDirectory);

            ConfigureCefNativeSearchPath();
            WriteEarlyLog("[Program] After ConfigureCefNativeSearchPath");

            WriteEarlyLog("[Program] Calling CefRuntime.ExecuteMainProcess...");
            var cefExitCode = CefRuntime.ExecuteMainProcess(GetModuleHandle(null));
            WriteEarlyLog("[Program] CefRuntime.ExecuteMainProcess returned: {0}", cefExitCode);

            if (cefExitCode >= 0)
            {
                WriteEarlyLog("[Program] Exiting with code: {0}", cefExitCode);
                Environment.Exit(cefExitCode);
                return;
            }

            ResetLogFile();
            WriteEarlyLog("[Program] Log file reset, starting Avalonia...");

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }

        private static void ConfigureCefNativeSearchPath()
        {
            var driverName = "Cef109";
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

        private static void WriteEarlyLog(string format, params object[] args)
        {
            var earlyLog = Path.Combine(AppContext.BaseDirectory, "early_startup.log");
            var message = string.Format(format, args);
            var line = string.Format("[{0:HH:mm:ss.fff}] {1}", DateTime.Now, message);
            try
            {
                System.IO.File.AppendAllText(earlyLog, line + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}

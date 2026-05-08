namespace MyBrowser.Demo
{
    using System;
    using System.Diagnostics;
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

            // 主进程：创建 Job Object，主进程退出/崩溃时自动杀所有子进程
            AttachToJobWithKillOnClose();

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

        private static IntPtr _jobHandle;

        private static void AttachToJobWithKillOnClose()
        {
            _jobHandle = CreateJobObject(IntPtr.Zero, null);
            if (_jobHandle == IntPtr.Zero)
            {
                WriteEarlyLog("[Program] CreateJobObject failed: {0}", Marshal.GetLastWin32Error());
                return;
            }

            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            var length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            var ptr = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(info, ptr, false);
                if (!SetInformationJobObject(_jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, ptr, (uint)length))
                {
                    WriteEarlyLog("[Program] SetInformationJobObject failed: {0}", Marshal.GetLastWin32Error());
                    return;
                }

                if (!AssignProcessToJobObject(_jobHandle, Process.GetCurrentProcess().Handle))
                {
                    WriteEarlyLog("[Program] AssignProcessToJobObject failed: {0} (可能已在job内)", Marshal.GetLastWin32Error());
                }
                else
                {
                    WriteEarlyLog("[Program] Job Object created with KILL_ON_JOB_CLOSE - 子进程将随主进程退出自动终止");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr hJob, JOBOBJECTINFOCLASS JobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        private enum JOBOBJECTINFOCLASS
        {
            JobObjectExtendedLimitInformation = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public long Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

        private static void ResetLogFile()
        {
            var logFile = LogHelper.GetLogPath("mybrowser-main.log");
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

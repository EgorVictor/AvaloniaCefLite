namespace MyBrowser.Interop
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using MyBrowser.Interop.Internal;
    using Serilog;

    /// <summary>
    /// CEF 运行时管理器
    /// 负责 CEF 初始化、关闭和消息循环
    /// </summary>
    public sealed unsafe class CefRuntime
    {
        private static readonly string _logFile = @"F:\mybrowser.log";
        private static bool _initialized;
        private static bool _shutdown;
        private static CefRuntime? _instance;

        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(_logFile, shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        static CefRuntime()
        {
            // 每次程序启动时删除旧日志
            try
            {
                if (File.Exists(_logFile))
                {
                    File.Delete(_logFile);
                }
            }
            catch { }
        }

        public static CefRuntime Instance => _instance ??= new CefRuntime();
        public static bool IsInitialized => _initialized;
        public static bool IsShutdown => _shutdown;

        /// <summary>
        /// 执行主进程入口
        /// </summary>
        public static int ExecuteMainProcess(IntPtr instanceHandle)
        {
            var args = new CefMainArgs { Instance = instanceHandle };
            var app = new CefApp { Base = new CefBase() };
            return -1;
        }

        /// <summary>
        /// 初始化 CEF
        /// </summary>
        public static bool Initialize(IntPtr instanceHandle, bool multiThreadedMessageLoop = true)
        {
            if (_initialized)
            {
                _log.Information("[CefRuntime] Already initialized");
                return true;
            }

            _log.Information("[CefRuntime] Initializing CEF...");
            _log.Information("[CefRuntime] sizeof(CefSettings) = {0}", sizeof(CefSettings));

            // Initialize ALL fields of CefSettings to proper values
            var settings = new CefSettings
            {
                size = (UIntPtr)sizeof(CefSettings),  // CRITICAL - must match!
                no_sandbox = 1,                        // Disable sandbox
                multi_threaded_message_loop = multiThreadedMessageLoop ? 1 : 0,
                windowless_rendering_enabled = 0,
                command_line_args_disabled = 0,
                persist_session_cookies = 0,
                persist_user_preferences = 0,
                pack_loading_disabled = 0,
                remote_debugging_port = 0,
                uncaught_exception_stack_size = 0,
                background_color = 0xFFFFFFFF,        // White background
                log_severity = 3,                     // LOGSEVERITY_ERROR
                cookieable_schemes_exclude_defaults = 0
            };

            // Initialize app with proper base
            var app = new CefApp
            {
                Base = new CefBase
                {
                    size = (UIntPtr)sizeof(CefBase)
                }
            };

            var args = new CefMainArgs { Instance = instanceHandle };

            _log.Information("[CefRuntime] Calling cef_initialize...");
            _log.Information("[CefRuntime] settings.size = {0}", settings.size);
            _log.Information("[CefRuntime] settings.no_sandbox = {0}", settings.no_sandbox);

            // Pass NULL for app - it's optional and we don't have proper callbacks
            // For unmanaged structs, we can take address directly without 'fixed'
            int result = CefNative.CefInitialize(&args, &settings, null, IntPtr.Zero);
            _log.Information("[CefRuntime] cef_initialize returned: {0}", result);

            if (result != 0)
            {
                _initialized = true;
                _log.Information("[CefRuntime] CEF initialized OK");
                return true;
            }

            _log.Information("[CefRuntime] CEF initialization FAILED!");
            return false;
        }

        /// <summary>
        /// 关闭 CEF
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized || _shutdown) return;

            _log.Information("[CefRuntime] Shutting down CEF...");
            CefNative.CefShutdown();
            _shutdown = true;
            _log.Information("[CefRuntime] CEF shut down");
        }

        /// <summary>
        /// 运行消息循环
        /// </summary>
        public static void RunMessageLoop()
        {
            CefNative.CefRunMessageLoop();
        }

        /// <summary>
        /// 执行消息循环工作
        /// </summary>
        public static void DoMessageLoopWork()
        {
            CefNative.CefDoMessageLoopWork();
        }
    }
}
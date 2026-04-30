namespace MyBrowser.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using MyBrowser.Interop.Internal;

    /// <summary>
    /// CEF 运行时管理器
    /// 负责 CEF 初始化、关闭和消息循环
    /// </summary>
    public sealed unsafe class CefRuntime
    {
        private static bool _initialized;
        private static bool _shutdown;
        private static CefRuntime? _instance;

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
                System.Diagnostics.Debug.WriteLine("[CefRuntime] Already initialized");
                return true;
            }

            System.Diagnostics.Debug.WriteLine("[CefRuntime] Initializing CEF...");
            System.Diagnostics.Debug.WriteLine($"[CefRuntime] sizeof(CefSettings) = {sizeof(CefSettings)}");

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

            System.Diagnostics.Debug.WriteLine($"[CefRuntime] Calling cef_initialize...");
            System.Diagnostics.Debug.WriteLine($"[CefRuntime] settings.size = {settings.size}");
            System.Diagnostics.Debug.WriteLine($"[CefRuntime] settings.no_sandbox = {settings.no_sandbox}");

            // Pass NULL for app - it's optional and we don't have proper callbacks
            // For unmanaged structs, we can take address directly without 'fixed'
            int result = CefNative.CefInitialize(&args, &settings, null, IntPtr.Zero);
            System.Diagnostics.Debug.WriteLine($"[CefRuntime] cef_initialize returned: {result}");

            if (result != 0)
            {
                _initialized = true;
                System.Diagnostics.Debug.WriteLine("[CefRuntime] CEF initialized OK");
                return true;
            }

            System.Diagnostics.Debug.WriteLine("[CefRuntime] CEF initialization FAILED!");
            return false;
        }

        /// <summary>
        /// 关闭 CEF
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized || _shutdown) return;

            System.Diagnostics.Debug.WriteLine("[CefRuntime] Shutting down CEF...");
            CefNative.CefShutdown();
            _shutdown = true;
            System.Diagnostics.Debug.WriteLine("[CefRuntime] CEF shut down");
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
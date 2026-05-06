namespace MyBrowser.Interop
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using MyBrowser.Interop.cef.capi;
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
        private static readonly object _initLock = new object();

        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(_logFile, shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private static CefApp? _app;

        public static CefRuntime Instance => _instance ??= new CefRuntime();
        public static bool IsInitialized => _initialized;
        public static bool IsShutdown => _shutdown;

        /// <summary>
        /// CEF 上下文初始化完成事件。浏览器创建必须在此事件之后进行。
        /// </summary>
        public static event EventHandler? ContextInitialized;

        public static int ExecuteMainProcess(IntPtr instanceHandle)
        {
            var args = new cef_main_args_t { instance = instanceHandle };
            _log.Information("[CefRuntime] Calling cef_execute_process...");
            var result = NativeMethods.cef_execute_process(&args, null, IntPtr.Zero);
            _log.Information("[CefRuntime] cef_execute_process returned: {0}", result);
            return result;
        }

        public static bool Initialize(IntPtr instanceHandle, string driverDir, bool multiThreadedMessageLoop = true)
        {
            lock (_initLock)
            {
                if (_initialized)
                {
                    _log.Information("[CefRuntime] Already initialized");
                    return true;
                }

                _log.Information("[CefRuntime] Initializing CEF...");
                _log.Information("[CefRuntime] DriverDir: {DriverDir}", driverDir);

                var settings = new cef_settings_t
                {
                    size = (UIntPtr)sizeof(cef_settings_t),
                    no_sandbox = 1,
                    multi_threaded_message_loop = multiThreadedMessageLoop ? 1 : 0,
                    windowless_rendering_enabled = 0,
                    command_line_args_disabled = 0,
                    persist_session_cookies = 0,
                    persist_user_preferences = 0,
                    pack_loading_disabled = 0,
                    remote_debugging_port = 9222,
                    uncaught_exception_stack_size = 0,
                    background_color = 0xFFFFFFFF,
                    log_severity = 0, // verbose
                    cookieable_schemes_exclude_defaults = 0
                };

                var resourcesDir = Path.Combine(driverDir, "resources");
                var localesDir = Path.Combine(driverDir, "locales");

                if (File.Exists(Path.Combine(driverDir, "resources.pak")))
                {
                    SetCefString(ref settings.resources_dir_path, driverDir);
                    _log.Information("[CefRuntime] Resources dir: {DriverDir} (contains resources.pak)", driverDir);
                }
                else if (Directory.Exists(resourcesDir))
                {
                    SetCefString(ref settings.resources_dir_path, resourcesDir);
                    _log.Information("[CefRuntime] Resources dir: {ResourcesDir}", resourcesDir);
                }
                else
                {
                    _log.Information("[CefRuntime] WARNING: No resources directory found");
                }

                if (Directory.Exists(localesDir))
                {
                    SetCefString(ref settings.locales_dir_path, localesDir);
                    _log.Information("[CefRuntime] Locales dir: {LocalesDir}", localesDir);
                }
                else
                {
                    _log.Information("[CefRuntime] WARNING: No locales directory found");
                }

                SetCefString(ref settings.log_file, @"F:\cef_debug.log");

                _app = new CefApp();
                _app.ContextInitialized += (s, e) =>
                {
                    _log.Information("[CefRuntime] Forwarding ContextInitialized event");
                    ContextInitialized?.Invoke(s, e);
                };

                cef_app_t* appPtr = (cef_app_t*)_app.Handle;

                var args = new cef_main_args_t { instance = instanceHandle };

                _log.Information("[CefRuntime] Calling cef_initialize...");
                _log.Information("[CefRuntime] settings.size = {0}", settings.size);
                _log.Information("[CefRuntime] settings.no_sandbox = {0}", settings.no_sandbox);
                _log.Information("[CefRuntime] settings.resources_dir = {ResourcesDir}", GetString(settings.resources_dir_path));
                _log.Information("[CefRuntime] settings.locales_dir = {LocalesDir}", GetString(settings.locales_dir_path));

                int result = NativeMethods.cef_initialize(&args, &settings, appPtr, IntPtr.Zero);
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
        }

        private static unsafe void SetCefString(ref cef_string_t cefStr, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                cefStr.str = null;
                cefStr.length = UIntPtr.Zero;
                cefStr.dtor = IntPtr.Zero;
                return;
            }

            var ptr = Marshal.StringToHGlobalUni(value);
            cefStr.str = (char*)ptr;
            cefStr.length = (UIntPtr)value.Length;
            cefStr.dtor = Marshal.GetFunctionPointerForDelegate<CefStringDtor>(FreeCefString);
        }

        private static CefStringDtor? _stringDtor;
        private static CefStringDtor CefStringDtorDelegate => _stringDtor ??= new CefStringDtor(FreeCefString);
        private static void FreeCefString(char* str) => Marshal.FreeHGlobal((IntPtr)str);
        public delegate void CefStringDtor(char* str);

        private static unsafe string GetString(cef_string_t cefStr)
        {
            if (cefStr.str == null || cefStr.length == UIntPtr.Zero)
                return "(null)";
            return Marshal.PtrToStringUni((IntPtr)cefStr.str, (int)cefStr.length) ?? "(empty)";
        }

        /// <summary>
        /// 关闭 CEF
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized || _shutdown) return;

            _log.Information("[CefRuntime] Shutting down CEF...");
            _app?.Dispose();
            _app = null;
            NativeMethods.cef_shutdown();
            _shutdown = true;
            _initialized = false;
            _log.Information("[CefRuntime] CEF shut down");
        }

        public static void RunMessageLoop()
        {
            NativeMethods.cef_run_message_loop();
        }

        public static void DoMessageLoopWork()
        {
            NativeMethods.cef_do_message_loop_work();
        }
    }
}

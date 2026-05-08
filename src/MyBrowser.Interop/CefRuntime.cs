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
        private static readonly string _logFile = LogHelper.GetLogPath("mybrowser-main.log");
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
        public static bool IsContextReady { get; private set; }

        /// <summary>
        /// CEF 上下文初始化完成事件。浏览器创建必须在此事件之后进行。
        /// </summary>
        public static event EventHandler? ContextInitialized;

        private static bool _executeMainProcessCalled;
        private static CefApp? _executeApp;

        public static int ExecuteMainProcess(IntPtr instanceHandle, CefRuntimeOptions? options = null)
        {
            var os = Environment.OSVersion;
            _log.Information("[CefRuntime] ExecuteMainProcess: OS={OsVersion}, CLR={ClrVersion}", os.VersionString, Environment.Version);

            if (_executeMainProcessCalled)
            {
                _log.Warning("[CefRuntime] ExecuteMainProcess called twice! Stack: {0}", Environment.StackTrace);
                return -1;
            }
            _executeMainProcessCalled = true;

            var isWin7 = options != null && options.CompatibilityMode == CefCompatibilityMode.Win7Compatible;
            var disableGpu = options != null && options.DisableGpu;
            var win7RenderMode = options != null ? options.Win7RenderMode : CefWin7RenderMode.SafeNoGpu;
            var ignoreCert = options != null && options.IgnoreCertificateErrors;

            _log.Information("[CefRuntime] ExecuteMainProcess options: isWin7={IsWin7}, disableGpu={DisableGpu}, win7RenderMode={Win7RenderMode}, ignoreCert={IgnoreCert}", isWin7, disableGpu, win7RenderMode, ignoreCert);

            _executeApp = new CefApp(isWin7Or8: isWin7, hardwareAcceleration: !disableGpu, ignoreCertificateErrors: ignoreCert, win7RenderMode: win7RenderMode);

            var args = new cef_main_args_t { instance = instanceHandle };
            _log.Information("[CefRuntime] Calling cef_execute_process with CefApp (disableGpu={DisableGpu}, win7RenderMode={Win7RenderMode})...", disableGpu, win7RenderMode);
            var result = NativeMethods.cef_execute_process(&args, (cef_app_t*)_executeApp.Handle, IntPtr.Zero);
            _log.Information("[CefRuntime] cef_execute_process returned: {0}", result);

            if (result < 0)
            {
                // Browser process: _executeApp was created with caller's options,
                // don't reuse - Initialize will create the real CefApp from BrowserConfig
                _executeApp = null;
            }

            return result;
        }

        public static bool Initialize(IntPtr instanceHandle, CefRuntimeOptions options)
        {
            lock (_initLock)
            {
                if (_initialized)
                {
                    _log.Information("[CefRuntime] Already initialized");
                    return true;
                }

                _log.Information("[CefRuntime] === Initialize START ===");
                _log.Information("[CefRuntime] RuntimePath: {RuntimePath}", options.RuntimePath);
                _log.Information("[CefRuntime] DisableGpu: {DisableGpu}", options.DisableGpu);
                _log.Information("[CefRuntime] MultiThreadedMessageLoop: {MTML}", options.MultiThreadedMessageLoop);
                _log.Information("[CefRuntime] CompatibilityMode: {CompatibilityMode}", options.CompatibilityMode);
                _log.Information("[CefRuntime] Win7RenderMode: {Win7RenderMode}", options.Win7RenderMode);
                _log.Information("[CefRuntime] instanceHandle: {0}", (long)instanceHandle);

                var isWin7 = options.CompatibilityMode == CefCompatibilityMode.Win7Compatible;
                var logSeverity = options.LogSeverity switch
                {
                    CefLogLevel.Verbose => cef_log_severity_t.LOGSEVERITY_VERBOSE,
                    CefLogLevel.Info => cef_log_severity_t.LOGSEVERITY_INFO,
                    CefLogLevel.Warning => cef_log_severity_t.LOGSEVERITY_WARNING,
                    CefLogLevel.Error => cef_log_severity_t.LOGSEVERITY_ERROR,
                    CefLogLevel.Disabled => cef_log_severity_t.LOGSEVERITY_DISABLE,
                    _ => cef_log_severity_t.LOGSEVERITY_DEFAULT
                };

                var settings = new cef_settings_t
                {
                    size = (UIntPtr)sizeof(cef_settings_t),
                    no_sandbox = 1,
                    multi_threaded_message_loop = options.MultiThreadedMessageLoop ? 1 : 0,
                    windowless_rendering_enabled = 0,
                    command_line_args_disabled = 0,
                    persist_session_cookies = 0,
                    persist_user_preferences = 0,
                    pack_loading_disabled = 0,
                    remote_debugging_port = options.RemoteDebuggingPort,
                    uncaught_exception_stack_size = 0,
                    background_color = 0xFFFFFFFF,
                    log_severity = logSeverity,
                    cookieable_schemes_exclude_defaults = 0
                };

                if (!string.IsNullOrEmpty(options.CachePath))
                {
                    SetCefString(ref settings.cache_path, options.CachePath);
                    SetCefString(ref settings.root_cache_path, options.CachePath);
                    var cacheExists = Directory.Exists(options.CachePath);
                    _log.Information("[CefRuntime] Cache path: {CachePath} (exists={CacheExists})", options.CachePath, cacheExists);
                }
                else
                {
                    _log.Information("[CefRuntime] No cache path set - CEF will use default");
                }

                if (!string.IsNullOrEmpty(options.BrowserSubprocessPath))
                {
                    var subExists = File.Exists(options.BrowserSubprocessPath);
                    _log.Information("[CefRuntime] Browser subprocess path: {Path} (exists={SubExists})", options.BrowserSubprocessPath, subExists);
                    if (!subExists)
                        _log.Warning("[CefRuntime] Subprocess EXE not found at: {Path}", options.BrowserSubprocessPath);
                    SetCefString(ref settings.browser_subprocess_path, options.BrowserSubprocessPath);
                }
                else
                {
                    _log.Information("[CefRuntime] No subprocess path set - CEF defaults to main exe");
                }

                var driverDir = options.RuntimePath;
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

                var logFile = Path.Combine(AppContext.BaseDirectory, "cef_debug.log");
                SetCefString(ref settings.log_file, logFile);

                if (_executeApp != null)
                {
                    _app = _executeApp;
                    _log.Information("[CefRuntime] Reusing CefApp from ExecuteMainProcess (subprocess)");
                }
                else
                {
                    _app = new CefApp(isWin7Or8: isWin7, hardwareAcceleration: !options.DisableGpu, ignoreCertificateErrors: options.IgnoreCertificateErrors, win7RenderMode: options.Win7RenderMode);
                    _log.Information("[CefRuntime] Created fresh CefApp from BrowserConfig: hwAccel={0}, win7RenderMode={1}", !options.DisableGpu, options.Win7RenderMode);
                }
                _app.ContextInitialized += (s, e) =>
                {
                    IsContextReady = true;
                    _log.Information("[CefRuntime] ContextInitialized - Forwarding event");
                    ContextInitialized?.Invoke(s, e);
                };

                cef_app_t* appPtr = (cef_app_t*)_app.Handle;

                var args = new cef_main_args_t { instance = instanceHandle };

                _log.Information("[CefRuntime] Calling cef_initialize...");
                _log.Information("[CefRuntime] settings.size = {0}", settings.size);
                _log.Information("[CefRuntime] settings.no_sandbox = {0}", settings.no_sandbox);
                _log.Information("[CefRuntime] settings.multi_threaded_message_loop = {0}", settings.multi_threaded_message_loop);
                _log.Information("[CefRuntime] settings.resources_dir = {ResourcesDir}", GetString(settings.resources_dir_path));
                _log.Information("[CefRuntime] settings.locales_dir = {LocalesDir}", GetString(settings.locales_dir_path));
                _log.Information("[CefRuntime] settings.log_severity = {LogSeverity}", logSeverity);
                _log.Information("[CefRuntime] settings.remote_debugging_port = {Port}", options.RemoteDebuggingPort);
                _log.Information("[CefRuntime] appPtr = {0}, args.instance = {1}", (long)appPtr, (long)args.instance);
                _log.Information("[CefRuntime] >>> Calling native cef_initialize <<<");

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
            cefStr.dtor = Marshal.GetFunctionPointerForDelegate(CefStringDtorDelegate);
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

            // 1. First call cef_shutdown() while native callbacks are still valid
            NativeMethods.cef_shutdown();

            // 2. Then dispose the callback objects
            _app?.Dispose();
            _app = null;

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

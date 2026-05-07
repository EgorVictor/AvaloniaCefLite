namespace MyBrowser.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using MyBrowser.Interop.cef.capi;
    using Serilog;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// CEF 应用对象，实现 cef_app_t 和 cef_browser_process_handler_t。
    /// 两个 native ref-counted 对象使用独立的引用计数。
    /// Release() 从不主动 Dispose() - native 内存仅在 Shutdown 时释放。
    /// </summary>
    public sealed unsafe class CefApp : IDisposable
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(LogHelper.GetLogPath(), shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        // App ref-count (for cef_app_t)
        private int _appRefCount = 1;
        // BrowserProcessHandler ref-count (for cef_browser_process_handler_t)
        private int _bphRefCount = 1;

        // GCHandles to prevent GC from collecting this object while CEF holds native pointers
        private GCHandle _selfHandle;
        private GCHandle _bphSelfHandle;

        // Native structs and their pointers
        private cef_app_t _app;
        private cef_browser_process_handler_t _browserProcessHandler;
        private IntPtr _appPtr;
        private IntPtr _browserProcessHandlerPtr;

        private bool _disposed;

        // Delegate fields - MUST be kept alive to prevent GC
        private cef_base_add_ref _appAddRef;
        private cef_base_release _appRelease;
        private cef_base_has_one_ref _appHasOneRef;
        private cef_base_has_at_least_one_ref _appHasAtLeastOneRef;
        private cef_base_add_ref _bphAddRef;
        private cef_base_release _bphRelease;
        private cef_base_has_one_ref _bphHasOneRef;
        private cef_base_has_at_least_one_ref _bphHasAtLeastOneRef;
        private cef_app_get_browser_process_handler _getBrowserProcessHandler;
        private cef_browser_process_handler_on_context_initialized _onContextInitialized;
        private cef_app_on_before_command_line_processing _onBeforeCommandLineProcessing;

        public event EventHandler? ContextInitialized;
        public IntPtr Handle => _appPtr;

        public CefApp()
        {
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            _bphSelfHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            // --- App delegates (operate on _appRefCount) ---
            _appAddRef = AppAddRef;
            _appRelease = AppRelease;
            _appHasOneRef = AppHasOneRef;
            _appHasAtLeastOneRef = AppHasAtLeastOneRef;

            // --- BPH delegates (operate on _bphRefCount) ---
            _bphAddRef = BphAddRef;
            _bphRelease = BphRelease;
            _bphHasOneRef = BphHasOneRef;
            _bphHasAtLeastOneRef = BphHasAtLeastOneRef;

            _getBrowserProcessHandler = GetBrowserProcessHandler;
            _onContextInitialized = OnContextInitialized;
            _onBeforeCommandLineProcessing = OnBeforeCommandLineProcessing;

            // --- cef_browser_process_handler_t ---
            _browserProcessHandler = new cef_browser_process_handler_t
            {
                base_ = new cef_base_ref_counted_t
                {
                    size = (UIntPtr)sizeof(cef_browser_process_handler_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate(_bphAddRef),
                    release = Marshal.GetFunctionPointerForDelegate(_bphRelease),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate(_bphHasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate(_bphHasAtLeastOneRef)
                },
                on_register_custom_preferences = IntPtr.Zero,
                on_context_initialized = Marshal.GetFunctionPointerForDelegate(_onContextInitialized),
                on_before_child_process_launch = IntPtr.Zero,
                on_schedule_message_pump_work = IntPtr.Zero,
                get_default_client = IntPtr.Zero
            };

            _browserProcessHandlerPtr = Marshal.AllocHGlobal(sizeof(cef_browser_process_handler_t));
            Marshal.StructureToPtr(_browserProcessHandler, _browserProcessHandlerPtr, false);

            // --- cef_app_t ---
            _app = new cef_app_t
            {
                base_ = new cef_base_ref_counted_t
                {
                    size = (UIntPtr)sizeof(cef_app_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate(_appAddRef),
                    release = Marshal.GetFunctionPointerForDelegate(_appRelease),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate(_appHasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate(_appHasAtLeastOneRef)
                },
                on_before_command_line_processing = Marshal.GetFunctionPointerForDelegate(_onBeforeCommandLineProcessing),
                on_register_custom_schemes = IntPtr.Zero,
                get_resource_bundle_handler = IntPtr.Zero,
                get_browser_process_handler = Marshal.GetFunctionPointerForDelegate(_getBrowserProcessHandler),
                get_render_process_handler = IntPtr.Zero
            };

            _appPtr = Marshal.AllocHGlobal(sizeof(cef_app_t));
            Marshal.StructureToPtr(_app, _appPtr, false);

            _log.Information("[CefApp] Created, app={App}, bph={BPH}", _appPtr, _browserProcessHandlerPtr);
        }

        // ===== cef_app_t ref-count (separate from BPH) =====
        private void AppAddRef(IntPtr self) => Interlocked.Increment(ref _appRefCount);

        private int AppRelease(IntPtr self)
        {
            var newCount = Interlocked.Decrement(ref _appRefCount);
            return (newCount == 0) ? 1 : 0;
        }

        private int AppHasOneRef(IntPtr self) => _appRefCount == 1 ? 1 : 0;
        private int AppHasAtLeastOneRef(IntPtr self) => _appRefCount >= 1 ? 1 : 0;

        // ===== cef_browser_process_handler_t ref-count (separate from App) =====
        private void BphAddRef(IntPtr self) => Interlocked.Increment(ref _bphRefCount);

        private int BphRelease(IntPtr self)
        {
            var newCount = Math.Max(0, Interlocked.Decrement(ref _bphRefCount));
            _bphRefCount = newCount;
            return (newCount == 0) ? 1 : 0;
        }

        private int BphHasOneRef(IntPtr self) => _bphRefCount == 1 ? 1 : 0;
        private int BphHasAtLeastOneRef(IntPtr self) => _bphRefCount >= 1 ? 1 : 0;

        // ===== cef_app_t callbacks =====
        private IntPtr GetBrowserProcessHandler(IntPtr self) => _browserProcessHandlerPtr;

        private void OnContextInitialized(IntPtr self)
        {
            _log.Information("[CefApp] OnContextInitialized - CEF context is ready");
            ContextInitialized?.Invoke(this, EventArgs.Empty);
        }

        private unsafe void OnBeforeCommandLineProcessing(IntPtr self, cef_string_t* processType, cef_command_line_t* commandLine)
        {
            var process = GetCefString(processType);
            _log.Information("[CefApp] OnBeforeCommandLineProcessing process={Process}", process);
            
            // Win7 compatibility: completely disable GPU to prevent render process crashes
            CommandLineAppendSwitch(commandLine, "disable-gpu");
            CommandLineAppendSwitch(commandLine, "disable-gpu-compositing");
            CommandLineAppendSwitch(commandLine, "disable-gpu-process");
            CommandLineAppendSwitchWithValue(commandLine, "use-gl", "swiftshader");
            
            // SSL compatibility for Win7
            CommandLineAppendSwitch(commandLine, "ignore-certificate-errors");
        }


        private static unsafe string GetCefString(cef_string_t* cefStr)
        {
            if (cefStr == null || cefStr->str == null || cefStr->length == UIntPtr.Zero) return "";
            return Marshal.PtrToStringUni((IntPtr)cefStr->str, (int)cefStr->length) ?? "";
        }

        private static unsafe void CommandLineAppendSwitch(cef_command_line_t* commandLine, string switchName)
        {
            if (commandLine == null) return;
            
            var funcPtr = commandLine->append_switch;
            if (funcPtr == IntPtr.Zero) return;

            var appendSwitch = Marshal.GetDelegateForFunctionPointer<cef_command_line_append_switch>(funcPtr);
            
            var cefStr = stackalloc cef_string_t[1];
            fixed (char* chars = switchName)
            {
                cefStr->str = chars;
                cefStr->length = (UIntPtr)switchName.Length;
                cefStr->dtor = IntPtr.Zero;
                appendSwitch((IntPtr)commandLine, cefStr);
            }
        }

        private static unsafe void CommandLineAppendSwitchWithValue(cef_command_line_t* commandLine, string switchName, string value)
        {
            if (commandLine == null) return;
            
            var funcPtr = commandLine->append_switch_with_value;
            if (funcPtr == IntPtr.Zero) return;

            var appendSwitchWithValue = Marshal.GetDelegateForFunctionPointer<cef_command_line_append_switch_with_value>(funcPtr);
            
            var cefName = stackalloc cef_string_t[1];
            var cefValue = stackalloc cef_string_t[1];
            fixed (char* nameChars = switchName)
            fixed (char* valueChars = value)
            {
                cefName->str = nameChars;
                cefName->length = (UIntPtr)switchName.Length;
                cefName->dtor = IntPtr.Zero;
                cefValue->str = valueChars;
                cefValue->length = (UIntPtr)value.Length;
                cefValue->dtor = IntPtr.Zero;
                appendSwitchWithValue((IntPtr)commandLine, cefName, cefValue);
            }
        }

        // ===== Disposal - only called during Shutdown =====
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _log.Information("[CefApp] Disposing native resources...");

            if (_browserProcessHandlerPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_browserProcessHandlerPtr);
                _browserProcessHandlerPtr = IntPtr.Zero;
            }

            if (_appPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_appPtr);
                _appPtr = IntPtr.Zero;
            }

            if (_bphSelfHandle.IsAllocated)
            {
                _bphSelfHandle.Free();
            }

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }

            _log.Information("[CefApp] Disposed");
        }
    }

    // Delegate types for cef_base_ref_counted_t callbacks
    public delegate void cef_base_add_ref(IntPtr self);
    public delegate int cef_base_release(IntPtr self);
    public delegate int cef_base_has_one_ref(IntPtr self);
    public delegate int cef_base_has_at_least_one_ref(IntPtr self);
    public delegate IntPtr cef_app_get_browser_process_handler(IntPtr self);
    public delegate void cef_browser_process_handler_on_context_initialized(IntPtr self);
}
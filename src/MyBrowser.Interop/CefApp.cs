namespace MyBrowser.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using MyBrowser.Interop.cef.capi;
    using Serilog;

    public sealed unsafe class CefApp : IDisposable
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private GCHandle _selfHandle;
        private GCHandle _browserProcessHandlerSelfHandle;
        private cef_app_t _app;
        private cef_browser_process_handler_t _browserProcessHandler;
        private IntPtr _appPtr;
        private IntPtr _browserProcessHandlerPtr;
        private bool _disposed;
        private int _refCount = 1;
        private int _browserProcessHandlerRefCount = 1;

        private cef_base_add_ref _addRef;
        private cef_base_release _release;
        private cef_base_has_one_ref _hasOneRef;
        private cef_base_has_at_least_one_ref _hasAtLeastOneRef;
        private cef_app_get_browser_process_handler _getBrowserProcessHandler;
        private cef_browser_process_handler_on_context_initialized _onContextInitialized;

        public event EventHandler? ContextInitialized;

        public IntPtr Handle => _appPtr;

        public CefApp()
        {
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            _browserProcessHandlerSelfHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            _addRef = AddRef;
            _release = Release;
            _hasOneRef = HasOneRef;
            _hasAtLeastOneRef = HasAtLeastOneRef;
            _getBrowserProcessHandler = GetBrowserProcessHandler;
            _onContextInitialized = OnContextInitialized;

            _browserProcessHandler = new cef_browser_process_handler_t
            {
                base_ = new cef_base_ref_counted_t
                {
                    size = (UIntPtr)sizeof(cef_browser_process_handler_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate(_addRef),
                    release = Marshal.GetFunctionPointerForDelegate(_release),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate(_hasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate(_hasAtLeastOneRef)
                },
                on_register_custom_preferences = IntPtr.Zero,
                on_context_initialized = Marshal.GetFunctionPointerForDelegate(_onContextInitialized),
                on_before_child_process_launch = IntPtr.Zero,
                on_schedule_message_pump_work = IntPtr.Zero,
                get_default_client = IntPtr.Zero
            };

            _browserProcessHandlerPtr = Marshal.AllocHGlobal(sizeof(cef_browser_process_handler_t));
            Marshal.StructureToPtr(_browserProcessHandler, _browserProcessHandlerPtr, false);

            _app = new cef_app_t
            {
                base_ = new cef_base_ref_counted_t
                {
                    size = (UIntPtr)sizeof(cef_app_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate(_addRef),
                    release = Marshal.GetFunctionPointerForDelegate(_release),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate(_hasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate(_hasAtLeastOneRef)
                },
                on_before_command_line_processing = IntPtr.Zero,
                on_register_custom_schemes = IntPtr.Zero,
                get_resource_bundle_handler = IntPtr.Zero,
                get_browser_process_handler = Marshal.GetFunctionPointerForDelegate(_getBrowserProcessHandler),
                get_render_process_handler = IntPtr.Zero
            };

            _appPtr = Marshal.AllocHGlobal(sizeof(cef_app_t));
            Marshal.StructureToPtr(_app, _appPtr, false);

            _log.Information("[CefApp] Created, Handle: {Handle}, BrowserProcessHandler: {BPH}", _appPtr, _browserProcessHandlerPtr);
        }

        private IntPtr GetBrowserProcessHandler(IntPtr self)
        {
            _log.Information("[CefApp] GetBrowserProcessHandler called, returning: {Ptr}", _browserProcessHandlerPtr);
            return _browserProcessHandlerPtr;
        }

        private void AddRef(IntPtr self) => Interlocked.Increment(ref _refCount);

        private int Release(IntPtr self)
        {
            int newCount = Interlocked.Decrement(ref _refCount);
            if (newCount == 0)
            {
                Dispose();
                return 1;
            }
            return 0;
        }

        private int HasOneRef(IntPtr self) => _refCount == 1 ? 1 : 0;
        private int HasAtLeastOneRef(IntPtr self) => _refCount >= 1 ? 1 : 0;

        private void OnContextInitialized(IntPtr self)
        {
            _log.Information("[CefApp] OnContextInitialized - CEF context is ready");
            ContextInitialized?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_appPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_appPtr);
                _appPtr = IntPtr.Zero;
            }

            if (_browserProcessHandlerPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_browserProcessHandlerPtr);
                _browserProcessHandlerPtr = IntPtr.Zero;
            }

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }

            if (_browserProcessHandlerSelfHandle.IsAllocated)
            {
                _browserProcessHandlerSelfHandle.Free();
            }

            _log.Information("[CefApp] Disposed");
        }
    }

    public delegate void cef_base_add_ref(IntPtr self);
    public delegate int cef_base_release(IntPtr self);
    public delegate int cef_base_has_one_ref(IntPtr self);
    public delegate int cef_base_has_at_least_one_ref(IntPtr self);
    public delegate IntPtr cef_app_get_browser_process_handler(IntPtr self);
    public delegate void cef_browser_process_handler_on_context_initialized(IntPtr self);
}
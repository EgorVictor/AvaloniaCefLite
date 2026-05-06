namespace MyBrowser.Interop.cef
{
    using System;
    using System.Runtime.InteropServices;
    using MyBrowser.Interop.cef.capi;
    using Serilog;

    public sealed unsafe class CefClient : IDisposable
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private GCHandle _selfHandle;
        private cef_client_t _client;
        private IntPtr _clientPtr;
        private bool _disposed;

        private CefLifeSpanHandler? _lifeSpanHandler;
        private CefLoadHandler? _loadHandler;

        public IntPtr Handle => _clientPtr;
        public IntPtr BrowserHandle { get; set; }
        public IntPtr BrowserHostHandle { get; set; }

        public event EventHandler<string>? TitleChanged;
        public event EventHandler<string>? AddressChanged;
        public event EventHandler? LoadStart;
        public event EventHandler<int>? LoadEnd;
        public event EventHandler<string>? LoadError;

        public CefClient()
        {
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            _lifeSpanHandler = new CefLifeSpanHandler(this);
            _loadHandler = new CefLoadHandler(this);

            _client = new cef_client_t
            {
                base_ = new cef_base_t
                {
                    size = (UIntPtr)sizeof(cef_client_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_add_ref>(AddRef),
                    release = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_release>(Release),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_has_one_ref>(HasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_has_at_least_one_ref>(HasAtLeastOneRef)
                },
                get_life_span_handler = Marshal.GetFunctionPointerForDelegate<cef_client_get_life_span_handler>(GetLifeSpanHandler),
                get_load_handler = Marshal.GetFunctionPointerForDelegate<cef_client_get_load_handler>(GetLoadHandler),
                get_browser_handler = IntPtr.Zero,
                get_context_menu_handler = IntPtr.Zero,
                get_dialog_handler = IntPtr.Zero,
                get_keyboard_handler = IntPtr.Zero,
                get_render_handler = IntPtr.Zero,
                get_find_handler = IntPtr.Zero,
                get_jsdialog_handler = IntPtr.Zero,
                get_electron_bindings = IntPtr.Zero,
                get_audio_handler = IntPtr.Zero
            };

            _clientPtr = Marshal.AllocHGlobal(sizeof(cef_client_t));
            Marshal.StructureToPtr(_client, _clientPtr, false);

            _log.Information("[CefClient] Created, Handle: {Handle}", _clientPtr);
        }

        private IntPtr GetLifeSpanHandler(IntPtr client)
        {
            return _lifeSpanHandler?.Handle ?? IntPtr.Zero;
        }

        private IntPtr GetLoadHandler(IntPtr client)
        {
            return _loadHandler?.Handle ?? IntPtr.Zero;
        }

        private static int AddRef(IntPtr ptr) => 1;
        private static int Release(IntPtr ptr) => 1;
        private static int HasOneRef(IntPtr ptr) => 1;
        private static int HasAtLeastOneRef(IntPtr ptr) => 1;

        public void OnTitleChanged(string title) => TitleChanged?.Invoke(this, title);
        public void OnAddressChanged(string url) => AddressChanged?.Invoke(this, url);
        public void OnLoadStart() => LoadStart?.Invoke(this, EventArgs.Empty);
        public void OnLoadEnd(int httpStatusCode) => LoadEnd?.Invoke(this, httpStatusCode);
        public void OnLoadError(string error) => LoadError?.Invoke(this, error);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _lifeSpanHandler?.Dispose();
            _loadHandler?.Dispose();

            if (_clientPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_clientPtr);
                _clientPtr = IntPtr.Zero;
            }

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }

            _log.Information("[CefClient] Disposed");
        }
    }

    #region LifeSpanHandler

    public sealed unsafe class CefLifeSpanHandler : IDisposable
    {
        private readonly CefClient _parent;
        private GCHandle _selfHandle;
        private cef_life_span_handler_t _handler;
        private IntPtr _handlerPtr;
        private bool _disposed;

        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        public IntPtr Handle => _handlerPtr;

        public CefLifeSpanHandler(CefClient parent)
        {
            _parent = parent;
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            _handler = new cef_life_span_handler_t
            {
                base_ = new cef_base_t
                {
                    size = (UIntPtr)sizeof(cef_life_span_handler_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_add_ref>(AddRef),
                    release = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_release>(Release),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_has_one_ref>(HasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_has_at_least_one_ref>(HasAtLeastOneRef)
                },
                on_before_popup = OnBeforePopup,
                on_after_created = OnAfterCreated,
                on_before_close = OnBeforeClose,
                on_render_view_ready = OnRenderViewReady
            };

            _handlerPtr = Marshal.AllocHGlobal(sizeof(cef_life_span_handler_t));
            Marshal.StructureToPtr(_handler, _handlerPtr, false);

            _log.Information("[CefLifeSpanHandler] Created");
        }

        private static int AddRef(IntPtr ptr) => 1;
        private static int Release(IntPtr ptr) => 1;
        private static int HasOneRef(IntPtr ptr) => 1;
        private static int HasAtLeastOneRef(IntPtr ptr) => 1;

        private int OnBeforePopup(IntPtr self, IntPtr browser, IntPtr frame, IntPtr targetUrl, IntPtr targetFrameName, int targetDisposition, int userGesture, IntPtr popupFeatures, IntPtr windowInfo, IntPtr client, IntPtr settings, IntPtr extraInfo, int* noJavascriptAccess)
        {
            _log.Information("[CefLifeSpanHandler] OnBeforePopup");
            *noJavascriptAccess = 0;
            return 0;
        }

        private void OnAfterCreated(IntPtr self, IntPtr browser, IntPtr popupBrowser)
        {
            _log.Information("[CefLifeSpanHandler] OnAfterCreated");
        }

        private void OnBeforeClose(IntPtr self, IntPtr browser)
        {
            _log.Information("[CefLifeSpanHandler] OnBeforeClose");
        }

        private void OnRenderViewReady(IntPtr self, IntPtr browser)
        {
            _log.Information("[CefLifeSpanHandler] OnRenderViewReady");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_handlerPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_handlerPtr);
                _handlerPtr = IntPtr.Zero;
            }

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }
        }
    }

    #endregion

    #region LoadHandler

    public sealed unsafe class CefLoadHandler : IDisposable
    {
        private readonly CefClient _parent;
        private GCHandle _selfHandle;
        private cef_load_handler_t _handler;
        private IntPtr _handlerPtr;
        private bool _disposed;

        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        public IntPtr Handle => _handlerPtr;

        public CefLoadHandler(CefClient parent)
        {
            _parent = parent;
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            _handler = new cef_load_handler_t
            {
                base_ = new cef_base_t
                {
                    size = (UIntPtr)sizeof(cef_load_handler_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_add_ref>(AddRef),
                    release = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_release>(Release),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_has_one_ref>(HasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate<cef_base_ref_counted_has_at_least_one_ref>(HasAtLeastOneRef)
                },
                on_load_start = OnLoadStart,
                on_load_end = OnLoadEnd,
                on_load_error = OnLoadError,
                on_load_progress_change = OnLoadProgressChange
            };

            _handlerPtr = Marshal.AllocHGlobal(sizeof(cef_load_handler_t));
            Marshal.StructureToPtr(_handler, _handlerPtr, false);

            _log.Information("[CefLoadHandler] Created");
        }

        private static int AddRef(IntPtr ptr) => 1;
        private static int Release(IntPtr ptr) => 1;
        private static int HasOneRef(IntPtr ptr) => 1;
        private static int HasAtLeastOneRef(IntPtr ptr) => 1;

        private void OnLoadStart(IntPtr self, IntPtr browser, IntPtr frame, int transitionType)
        {
            _log.Information("[CefLoadHandler] OnLoadStart");
            _parent.OnLoadStart();
        }

        private void OnLoadEnd(IntPtr self, IntPtr browser, IntPtr frame, int httpStatusCode)
        {
            _log.Information("[CefLoadHandler] OnLoadEnd, status: {Status}", httpStatusCode);
            _parent.OnLoadEnd(httpStatusCode);
        }

        private void OnLoadError(IntPtr self, IntPtr browser, IntPtr frame, int errorCode, IntPtr errorText, IntPtr failedUrl)
        {
            _log.Information("[CefLoadHandler] OnLoadError, code: {Code}", errorCode);
            _parent.OnLoadError($"Error {errorCode}");
        }

        private void OnLoadProgressChange(IntPtr self, IntPtr browser, double progress)
        {
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_handlerPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_handlerPtr);
                _handlerPtr = IntPtr.Zero;
            }

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }
        }
    }

    #endregion

    #region CEF Delegates

    public delegate IntPtr cef_client_get_life_span_handler(IntPtr client);
    public delegate IntPtr cef_client_get_load_handler(IntPtr client);

    #endregion
}

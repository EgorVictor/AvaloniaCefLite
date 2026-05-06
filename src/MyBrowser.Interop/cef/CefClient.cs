namespace MyBrowser.Interop.cef
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
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

        // Ref-counted fields (must be kept alive for the lifetime of this object)
        private int _refCount = 1;

        // Delegate fields - MUST be kept alive to prevent GC
        private cef_base_ref_counted_add_ref _addRef;
        private cef_base_ref_counted_release _release;
        private cef_base_ref_counted_has_one_ref _hasOneRef;
        private cef_base_ref_counted_has_at_least_one_ref _hasAtLeastOneRef;
        private cef_client_get_audio_handler _getAudioHandler;
        private cef_client_get_command_handler _getCommandHandler;
        private cef_client_get_context_menu_handler _getContextMenuHandler;
        private cef_client_get_dialog_handler _getDialogHandler;
        private cef_client_get_display_handler _getDisplayHandler;
        private cef_client_get_download_handler _getDownloadHandler;
        private cef_client_get_drag_handler _getDragHandler;
        private cef_client_get_find_handler _getFindHandler;
        private cef_client_get_focus_handler _getFocusHandler;
        private cef_client_get_frame_handler _getFrameHandler;
        private cef_client_get_permission_handler _getPermissionHandler;
        private cef_client_get_jsdialog_handler _getJsDialogHandler;
        private cef_client_get_keyboard_handler _getKeyboardHandler;
        private cef_client_get_life_span_handler _getLifeSpanHandler;
        private cef_client_get_load_handler _getLoadHandler;
        private cef_client_get_print_handler _getPrintHandler;
        private cef_client_get_render_handler _getRenderHandler;
        private cef_client_get_request_handler _getRequestHandler;
        private cef_client_on_process_message_received _onProcessMessageReceived;

        private CefLifeSpanHandler _lifeSpanHandler;
        private CefLoadHandler _loadHandler;

        public IntPtr Handle => _clientPtr;
        public IntPtr BrowserHandle { get; private set; }
        public IntPtr BrowserHostHandle { get; private set; }

        public event EventHandler<string>? TitleChanged;
        public event EventHandler<string>? AddressChanged;
        public event EventHandler? LoadStart;
        public event EventHandler<int>? LoadEnd;
        public event EventHandler<string>? LoadError;
        public event EventHandler<BrowserCreatedEventArgs>? BrowserCreated;
        public event EventHandler? BrowserClosing;
        public event EventHandler<bool>? LoadingStateChanged;
        public event EventHandler<bool>? CanGoBackChanged;
        public event EventHandler<bool>? CanGoForwardChanged;

        public CefClient()
        {
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            // Create child handlers (they hold references to us)
            _lifeSpanHandler = new CefLifeSpanHandler(this);
            _loadHandler = new CefLoadHandler(this);

            // Store delegates as fields to prevent GC
            _addRef = AddRef;
            _release = Release;
            _hasOneRef = HasOneRef;
            _hasAtLeastOneRef = HasAtLeastOneRef;
            _getAudioHandler = GetAudioHandler;
            _getCommandHandler = GetCommandHandler;
            _getContextMenuHandler = GetContextMenuHandler;
            _getDialogHandler = GetDialogHandler;
            _getDisplayHandler = GetDisplayHandler;
            _getDownloadHandler = GetDownloadHandler;
            _getDragHandler = GetDragHandler;
            _getFindHandler = GetFindHandler;
            _getFocusHandler = GetFocusHandler;
            _getFrameHandler = GetFrameHandler;
            _getPermissionHandler = GetPermissionHandler;
            _getJsDialogHandler = GetJsDialogHandler;
            _getKeyboardHandler = GetKeyboardHandler;
            _getLifeSpanHandler = GetLifeSpanHandler;
            _getLoadHandler = GetLoadHandler;
            _getPrintHandler = GetPrintHandler;
            _getRenderHandler = GetRenderHandler;
            _getRequestHandler = GetRequestHandler;
            _onProcessMessageReceived = OnProcessMessageReceived;

            _client = new cef_client_t
            {
                base_ = new cef_base_ref_counted_t
                {
                    size = (UIntPtr)sizeof(cef_client_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate(_addRef),
                    release = Marshal.GetFunctionPointerForDelegate(_release),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate(_hasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate(_hasAtLeastOneRef)
                },
                get_audio_handler = Marshal.GetFunctionPointerForDelegate(_getAudioHandler),
                get_command_handler = Marshal.GetFunctionPointerForDelegate(_getCommandHandler),
                get_context_menu_handler = Marshal.GetFunctionPointerForDelegate(_getContextMenuHandler),
                get_dialog_handler = Marshal.GetFunctionPointerForDelegate(_getDialogHandler),
                get_display_handler = Marshal.GetFunctionPointerForDelegate(_getDisplayHandler),
                get_download_handler = Marshal.GetFunctionPointerForDelegate(_getDownloadHandler),
                get_drag_handler = Marshal.GetFunctionPointerForDelegate(_getDragHandler),
                get_find_handler = Marshal.GetFunctionPointerForDelegate(_getFindHandler),
                get_focus_handler = Marshal.GetFunctionPointerForDelegate(_getFocusHandler),
                get_frame_handler = Marshal.GetFunctionPointerForDelegate(_getFrameHandler),
                get_permission_handler = Marshal.GetFunctionPointerForDelegate(_getPermissionHandler),
                get_jsdialog_handler = Marshal.GetFunctionPointerForDelegate(_getJsDialogHandler),
                get_keyboard_handler = Marshal.GetFunctionPointerForDelegate(_getKeyboardHandler),
                get_life_span_handler = Marshal.GetFunctionPointerForDelegate(_getLifeSpanHandler),
                get_load_handler = Marshal.GetFunctionPointerForDelegate(_getLoadHandler),
                get_print_handler = Marshal.GetFunctionPointerForDelegate(_getPrintHandler),
                get_render_handler = Marshal.GetFunctionPointerForDelegate(_getRenderHandler),
                get_request_handler = Marshal.GetFunctionPointerForDelegate(_getRequestHandler),
                on_process_message_received = Marshal.GetFunctionPointerForDelegate(_onProcessMessageReceived)
            };

            _clientPtr = Marshal.AllocHGlobal(sizeof(cef_client_t));
            Marshal.StructureToPtr(_client, _clientPtr, false);

            _log.Information("[CefClient] Created, Handle: {Handle}", _clientPtr);
        }

        // Handler getters - return the appropriate handler pointer
        private IntPtr GetAudioHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetCommandHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetContextMenuHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetDialogHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetDisplayHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetDownloadHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetDragHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetFindHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetFocusHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetFrameHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetPermissionHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetJsDialogHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetKeyboardHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetLifeSpanHandler(IntPtr self) => _lifeSpanHandler.Handle;
        private IntPtr GetLoadHandler(IntPtr self) => _loadHandler.Handle;
        private IntPtr GetPrintHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetRenderHandler(IntPtr self) => IntPtr.Zero;
        private IntPtr GetRequestHandler(IntPtr self) => IntPtr.Zero;
        private int OnProcessMessageReceived(IntPtr self, IntPtr browser, IntPtr frame, int sourceProcess, IntPtr message) => 0;

        // Real ref-counting
        private void AddRef(IntPtr self)
        {
            Interlocked.Increment(ref _refCount);
            _log.Information("[CefClient] AddRef -> {RefCount}", _refCount);
        }

        private int Release(IntPtr self)
        {
            int newCount = Interlocked.Decrement(ref _refCount);
            _log.Information("[CefClient] Release -> {RefCount}", newCount);
            if (newCount == 0)
            {
                Dispose();
                return 1; // Returns true (1) if reference count is 0
            }
            return 0;
        }

        private int HasOneRef(IntPtr self)
        {
            int result = _refCount == 1 ? 1 : 0;
            _log.Information("[CefClient] HasOneRef -> {Result}", result);
            return result;
        }

        private int HasAtLeastOneRef(IntPtr self)
        {
            int result = _refCount >= 1 ? 1 : 0;
            _log.Information("[CefClient] HasAtLeastOneRef -> {Result}", result);
            return result;
        }

        public void OnTitleChanged(string title) => TitleChanged?.Invoke(this, title);
        public void OnAddressChanged(string url) => AddressChanged?.Invoke(this, url);
        public void OnLoadStart() => LoadStart?.Invoke(this, EventArgs.Empty);
        public void OnLoadEnd(int httpStatusCode) => LoadEnd?.Invoke(this, httpStatusCode);
        public void OnLoadError(string error) => LoadError?.Invoke(this, error);
        public void OnBrowserCreated(IntPtr browser, IntPtr host)
        {
            BrowserHandle = browser;
            BrowserHostHandle = host;
            BrowserCreated?.Invoke(this, new BrowserCreatedEventArgs(browser, host));
        }
        public void OnBrowserClosing()
        {
            BrowserHandle = IntPtr.Zero;
            BrowserHostHandle = IntPtr.Zero;
            BrowserClosing?.Invoke(this, EventArgs.Empty);
        }
        public void OnLoadingStateChanged(bool isLoading) => LoadingStateChanged?.Invoke(this, isLoading);
        public void OnCanGoBackChanged(bool canGoBack) => CanGoBackChanged?.Invoke(this, canGoBack);
        public void OnCanGoForwardChanged(bool canGoForward) => CanGoForwardChanged?.Invoke(this, canGoForward);

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

    public sealed class BrowserCreatedEventArgs : EventArgs
    {
        public BrowserCreatedEventArgs(IntPtr browserHandle, IntPtr browserHostHandle)
        {
            BrowserHandle = browserHandle;
            BrowserHostHandle = browserHostHandle;
        }

        public IntPtr BrowserHandle { get; }
        public IntPtr BrowserHostHandle { get; }
    }

    #region LifeSpanHandler

    public sealed unsafe class CefLifeSpanHandler : IDisposable
    {
        private readonly CefClient _parent;
        private GCHandle _selfHandle;
        private cef_life_span_handler_t _handler;
        private IntPtr _handlerPtr;
        private bool _disposed;
        private int _refCount = 1;

        // Delegate fields - MUST be kept alive
        private cef_base_ref_counted_add_ref _addRef;
        private cef_base_ref_counted_release _release;
        private cef_base_ref_counted_has_one_ref _hasOneRef;
        private cef_base_ref_counted_has_at_least_one_ref _hasAtLeastOneRef;
        private cef_life_span_handler_on_before_popup _onBeforePopup;
        private cef_life_span_handler_on_after_created _onAfterCreated;
        private cef_life_span_handler_do_close _doClose;
        private cef_life_span_handler_on_before_close _onBeforeClose;

        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        public IntPtr Handle => _handlerPtr;

        public CefLifeSpanHandler(CefClient parent)
        {
            _parent = parent;
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            // Store delegates as fields
            _addRef = AddRef;
            _release = Release;
            _hasOneRef = HasOneRef;
            _hasAtLeastOneRef = HasAtLeastOneRef;
            _onBeforePopup = OnBeforePopup;
            _onAfterCreated = OnAfterCreated;
            _doClose = DoClose;
            _onBeforeClose = OnBeforeClose;

            _handler = new cef_life_span_handler_t
            {
                base_ = new cef_base_ref_counted_t
                {
                    size = (UIntPtr)sizeof(cef_life_span_handler_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate(_addRef),
                    release = Marshal.GetFunctionPointerForDelegate(_release),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate(_hasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate(_hasAtLeastOneRef)
                },
                on_before_popup = Marshal.GetFunctionPointerForDelegate(_onBeforePopup),
                on_after_created = Marshal.GetFunctionPointerForDelegate(_onAfterCreated),
                do_close = Marshal.GetFunctionPointerForDelegate(_doClose),
                on_before_close = Marshal.GetFunctionPointerForDelegate(_onBeforeClose)
            };

            _handlerPtr = Marshal.AllocHGlobal(sizeof(cef_life_span_handler_t));
            Marshal.StructureToPtr(_handler, _handlerPtr, false);

            _log.Information("[CefLifeSpanHandler] Created");
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

        private int OnBeforePopup(IntPtr self, IntPtr browser, IntPtr frame, IntPtr targetUrl, IntPtr targetFrameName, int targetDisposition, int userGesture, IntPtr popupFeatures, IntPtr windowInfo, IntPtr client, IntPtr settings, IntPtr extraInfo, int* noJavascriptAccess)
        {
            _log.Information("[CefLifeSpanHandler] OnBeforePopup");
            *noJavascriptAccess = 0;
            return 0;
        }

        private void OnAfterCreated(IntPtr self, IntPtr browser)
        {
            var host = browser != IntPtr.Zero ? NativeMethods.cef_browser_get_host(browser) : IntPtr.Zero;
            _log.Information("[CefLifeSpanHandler] OnAfterCreated, Browser: {Browser}, Host: {Host}", browser, host);
            _parent.OnBrowserCreated(browser, host);
        }

        private int DoClose(IntPtr self, IntPtr browser)
        {
            _log.Information("[CefLifeSpanHandler] DoClose");
            return 0; // Let CEF handle the close
        }

        private void OnBeforeClose(IntPtr self, IntPtr browser)
        {
            _log.Information("[CefLifeSpanHandler] OnBeforeClose");
            _parent.OnBrowserClosing();
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
        private int _refCount = 1;

        // Delegate fields - MUST be kept alive
        private cef_base_ref_counted_add_ref _addRef;
        private cef_base_ref_counted_release _release;
        private cef_base_ref_counted_has_one_ref _hasOneRef;
        private cef_base_ref_counted_has_at_least_one_ref _hasAtLeastOneRef;
        private cef_load_handler_on_loading_state_change _onLoadingStateChange;
        private cef_load_handler_on_load_start _onLoadStart;
        private cef_load_handler_on_load_end _onLoadEnd;
        private cef_load_handler_on_load_error _onLoadError;

        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        public IntPtr Handle => _handlerPtr;

        public CefLoadHandler(CefClient parent)
        {
            _parent = parent;
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            // Store delegates as fields
            _addRef = AddRef;
            _release = Release;
            _hasOneRef = HasOneRef;
            _hasAtLeastOneRef = HasAtLeastOneRef;
            _onLoadingStateChange = OnLoadingStateChange;
            _onLoadStart = OnLoadStart;
            _onLoadEnd = OnLoadEnd;
            _onLoadError = OnLoadError;

            _handler = new cef_load_handler_t
            {
                base_ = new cef_base_ref_counted_t
                {
                    size = (UIntPtr)sizeof(cef_load_handler_t),
                    add_ref = Marshal.GetFunctionPointerForDelegate(_addRef),
                    release = Marshal.GetFunctionPointerForDelegate(_release),
                    has_one_ref = Marshal.GetFunctionPointerForDelegate(_hasOneRef),
                    has_at_least_one_ref = Marshal.GetFunctionPointerForDelegate(_hasAtLeastOneRef)
                },
                on_loading_state_change = Marshal.GetFunctionPointerForDelegate(_onLoadingStateChange),
                on_load_start = Marshal.GetFunctionPointerForDelegate(_onLoadStart),
                on_load_end = Marshal.GetFunctionPointerForDelegate(_onLoadEnd),
                on_load_error = Marshal.GetFunctionPointerForDelegate(_onLoadError)
            };

            _handlerPtr = Marshal.AllocHGlobal(sizeof(cef_load_handler_t));
            Marshal.StructureToPtr(_handler, _handlerPtr, false);

            _log.Information("[CefLoadHandler] Created");
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

        private void OnLoadingStateChange(IntPtr self, IntPtr browser, int isLoading, int canGoBack, int canGoForward)
        {
            _log.Information("[CefLoadHandler] OnLoadingStateChange: isLoading={IsLoading}, canGoBack={CanGoBack}, canGoForward={CanGoForward}", isLoading, canGoBack, canGoForward);
            _parent.OnLoadingStateChanged(isLoading != 0);
            _parent.OnCanGoBackChanged(canGoBack != 0);
            _parent.OnCanGoForwardChanged(canGoForward != 0);
        }

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

    public delegate IntPtr cef_client_get_audio_handler(IntPtr self);
    public delegate IntPtr cef_client_get_command_handler(IntPtr self);
    public delegate IntPtr cef_client_get_context_menu_handler(IntPtr self);
    public delegate IntPtr cef_client_get_dialog_handler(IntPtr self);
    public delegate IntPtr cef_client_get_display_handler(IntPtr self);
    public delegate IntPtr cef_client_get_download_handler(IntPtr self);
    public delegate IntPtr cef_client_get_drag_handler(IntPtr self);
    public delegate IntPtr cef_client_get_find_handler(IntPtr self);
    public delegate IntPtr cef_client_get_focus_handler(IntPtr self);
    public delegate IntPtr cef_client_get_frame_handler(IntPtr self);
    public delegate IntPtr cef_client_get_permission_handler(IntPtr self);
    public delegate IntPtr cef_client_get_jsdialog_handler(IntPtr self);
    public delegate IntPtr cef_client_get_keyboard_handler(IntPtr self);
    public delegate IntPtr cef_client_get_life_span_handler(IntPtr self);
    public delegate IntPtr cef_client_get_load_handler(IntPtr self);
    public delegate IntPtr cef_client_get_print_handler(IntPtr self);
    public delegate IntPtr cef_client_get_render_handler(IntPtr self);
    public delegate IntPtr cef_client_get_request_handler(IntPtr self);
    public delegate int cef_client_on_process_message_received(IntPtr self, IntPtr browser, IntPtr frame, int sourceProcess, IntPtr message);

    #endregion
}

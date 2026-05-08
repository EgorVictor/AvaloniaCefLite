namespace MyBrowser.Interop
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using MyBrowser.Interop.Internal;
    using MyBrowser.Interop.cef;
    using MyBrowser.Interop.cef.capi;
    using Serilog;

    /// <summary>
    /// CEF 浏览器工厂
    /// 创建和管理浏览器实例 - 使用 CEF C API
    /// </summary>
    public sealed unsafe class CefBrowserFactory : IDisposable
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(LogHelper.GetLogPath(), shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private CefClient _client;
        private cef_browser_settings_t _settings;
        private bool _initialized;
        private IntPtr _browserHostHandle;
        private IntPtr _containerHwnd;

        public void SetContainerHwnd(IntPtr hwnd)
        {
            _containerHwnd = hwnd;
        }

        public IntPtr BrowserHandle { get; private set; }
        public IntPtr BrowserHostHandle => _browserHostHandle;
        public bool IsInitialized => _initialized;
        public bool DisableWebGL { get; set; }
        
        /// <summary>
        /// 获取CEF客户端以订阅事件
        /// </summary>
        public CefClient Client => _client;

        public CefBrowserFactory()
        {
            _client = new CefClient();
            _client.BrowserCreated += (_, e) =>
            {
                BrowserHandle = e.BrowserHandle;
                _browserHostHandle = e.BrowserHostHandle;
                _initialized = e.BrowserHandle != IntPtr.Zero;
                _log.Information("[CefBrowserFactory] Browser handle updated, Browser: {Browser}, Host: {Host}", BrowserHandle, _browserHostHandle);
            };
            _client.BrowserClosing += (_, _) =>
            {
                BrowserHandle = IntPtr.Zero;
                _browserHostHandle = IntPtr.Zero;
                _initialized = false;
            };
            _settings = CreateDefaultSettings();
            _initialized = false;
            _browserHostHandle = IntPtr.Zero;
        }

        /// <summary>
        /// 创建浏览器窗口（异步）
        /// </summary>
        public bool CreateBrowser(IntPtr parentHwnd, string initialUrl, out IntPtr browserHandle)
        {
            _log.Information("[CefBrowserFactory] CreateBrowser: URL={InitialUrl}, ParentHWND={ParentHWND}", initialUrl, parentHwnd);
            browserHandle = IntPtr.Zero;
            _browserHostHandle = IntPtr.Zero;

            if (!CefRuntime.IsInitialized)
            {
                _log.Information("[CefBrowserFactory] CEF not initialized!");
                return false;
            }

            // Use parent's actual client size for initial bounds
            GetClientRect(parentHwnd, out RECT parentRect);
            var initW = Math.Max(1, parentRect.Right - parentRect.Left);
            var initH = Math.Max(1, parentRect.Bottom - parentRect.Top);

            var windowInfo = new cef_window_info_t
            {
                ex_style = 0,
                window_name = new cef_string_t { str = null, length = UIntPtr.Zero, dtor = IntPtr.Zero },
                style = 0x40000000 | 0x10000000,  // WS_CHILD | WS_VISIBLE
                bounds = new cef_rect_t
                {
                    x = 0,
                    y = 0,
                    width = initW,
                    height = initH
                },
                parent_window = parentHwnd,
                menu = IntPtr.Zero,
                windowless_rendering_enabled = 0,
                shared_texture_enabled = 0,
                external_begin_frame_enabled = 0,
                window = IntPtr.Zero,
            };

            var url = new cef_string_t();
            if (!string.IsNullOrEmpty(initialUrl))
            {
                var urlStr = Marshal.StringToHGlobalUni(initialUrl + "\0");
                url.str = (char*)urlStr;
                url.length = (UIntPtr)initialUrl.Length;
                url.dtor = IntPtr.Zero;
            }

            try
            {
                // Apply WebGL setting just before creation
                if (DisableWebGL)
                {
                    _settings.webgl = cef_state_t.STATE_DISABLED;
                }

                cef_client_t* clientPtr = (cef_client_t*)_client.Handle;
                fixed (cef_browser_settings_t* settingsPtr = &_settings)
                {
                    _log.Information("[CefBrowserFactory] Calling cef_browser_host_create_browser...");
                    _log.Information("[CefBrowserFactory]   clientPtr = {ClientPtr}", (long)clientPtr);
                    _log.Information("[CefBrowserFactory]   url.str = {UrlStr}, url.length = {UrlLen}", (long)url.str, (ulong)url.length);
                    _log.Information("[CefBrowserFactory]   settingsPtr = {SettingsPtr}", (long)settingsPtr);
                    _log.Information("[CefBrowserFactory]   windowInfo.parent_window = {ParentHwnd}", windowInfo.parent_window);
                    _log.Information("[CefBrowserFactory]   windowInfo.size = {WinInfoSize}", sizeof(cef_window_info_t));

                    var result = NativeMethods.cef_browser_host_create_browser(
                        &windowInfo,
                        clientPtr,
                        &url,
                        settingsPtr,
                        IntPtr.Zero,  // extra_info
                        IntPtr.Zero); // request_context

                    _log.Information("[CefBrowserFactory] Result: {Result}", result);

                    if (result != 0)
                    {
                        _initialized = true;
                        _log.Information("[CefBrowserFactory] Browser created successfully");
                        return true;
                    }
                }

                _log.Information("[CefBrowserFactory] FAILED to create browser!");
                return false;
            }
            catch (Exception ex)
            {
                _log.Information("[CefBrowserFactory] Exception: {Message}", ex.Message);
                _log.Information("[CefBrowserFactory] StackTrace: {StackTrace}", ex.StackTrace);
                return false;
            }
            finally
            {
                if (url.str != null)
                {
                    Marshal.FreeHGlobal((IntPtr)url.str);
                }
            }
        }

        /// <summary>
        /// 创建浏览器窗口（同步）
        /// </summary>
        public bool CreateBrowserSync(IntPtr parentHwnd, string initialUrl, out IntPtr browserHandle)
        {
            _log.Information("[CefBrowserFactory] CreateBrowserSync: URL={InitialUrl}, ParentHWND={ParentHWND}", initialUrl, parentHwnd);
            browserHandle = IntPtr.Zero;
            _browserHostHandle = IntPtr.Zero;

            if (!CefRuntime.IsInitialized)
            {
                _log.Information("[CefBrowserFactory] CEF not initialized!");
                return false;
            }

            // 使用 WS_CHILD 样式将浏览器嵌入父窗口
            var windowInfo = new cef_window_info_t
            {
                ex_style = 0,
                window_name = new cef_string_t { str = null, length = UIntPtr.Zero, dtor = IntPtr.Zero },
                style = 0x40000000 | 0x04000000 | 0x02000000 | 0x00010000 | 0x10000000,
                bounds = new cef_rect_t
                {
                    x = 0,
                    y = 0,
                    width = 1024,
                    height = 768
                },
                parent_window = parentHwnd,
                menu = IntPtr.Zero,
                windowless_rendering_enabled = 0,
                shared_texture_enabled = 0,
                external_begin_frame_enabled = 0,
                window = IntPtr.Zero,
            };

            var url = new cef_string_t();
            if (!string.IsNullOrEmpty(initialUrl))
            {
                var urlStr = Marshal.StringToHGlobalUni(initialUrl + "\0");
                url.str = (char*)urlStr;
                url.length = (UIntPtr)initialUrl.Length;
                url.dtor = IntPtr.Zero;
            }

            try
            {
                cef_client_t* clientPtr = (cef_client_t*)_client.Handle;
                fixed (cef_browser_settings_t* settingsPtr = &_settings)
                {
                    var browserPtr = NativeMethods.cef_browser_host_create_browser_sync(
                        &windowInfo,
                        clientPtr,
                        &url,
                        settingsPtr,
                        IntPtr.Zero,  // extra_info
                        IntPtr.Zero); // request_context

                    if (browserPtr != IntPtr.Zero)
                    {
                        browserHandle = browserPtr;
                        _browserHostHandle = NativeMethods.cef_browser_get_host(browserPtr);
                        _initialized = true;
                        _log.Information("[CefBrowserFactory] Browser created (sync) OK, BrowserHost: {HostHandle}", _browserHostHandle);
                        return true;
                    }
                }

                _log.Information("[CefBrowserFactory] Sync create FAILED!");
                return false;
            }
            finally
            {
                if (url.str != null) Marshal.FreeHGlobal((IntPtr)url.str);
            }
        }

        /// <summary>
        /// 后退
        /// </summary>
        public void GoBack()
        {
            if (BrowserHandle == IntPtr.Zero) return;
            // cef_browser_t vtable: go_back = offset 64
            var ptr = Marshal.ReadIntPtr(BrowserHandle, Cef109VTableOffsets.BrowserGoBack);
            if (ptr != IntPtr.Zero)
                Marshal.GetDelegateForFunctionPointer<cef_browser_go_back>(ptr)(BrowserHandle);
        }

        /// <summary>
        /// 前进
        /// </summary>
        public void GoForward()
        {
            if (BrowserHandle == IntPtr.Zero) return;
            // cef_browser_t vtable: go_forward = offset 80
            var ptr = Marshal.ReadIntPtr(BrowserHandle, Cef109VTableOffsets.BrowserGoForward);
            if (ptr != IntPtr.Zero)
                Marshal.GetDelegateForFunctionPointer<cef_browser_go_forward>(ptr)(BrowserHandle);
        }

        /// <summary>
        /// 重新加载
        /// </summary>
        public void Reload()
        {
            if (BrowserHandle == IntPtr.Zero) return;
            // cef_browser_t vtable: reload = offset 96
            var ptr = Marshal.ReadIntPtr(BrowserHandle, Cef109VTableOffsets.BrowserReload);
            if (ptr != IntPtr.Zero)
                Marshal.GetDelegateForFunctionPointer<cef_browser_reload>(ptr)(BrowserHandle);
        }

        /// <summary>
        /// 重新加载（忽略缓存）
        /// </summary>
        public void ReloadIgnoreCache()
        {
            if (BrowserHandle == IntPtr.Zero) return;
            // cef_browser_t vtable: reload_ignore_cache = offset 104
            var ptr = Marshal.ReadIntPtr(BrowserHandle, Cef109VTableOffsets.BrowserReloadIgnoreCache);
            if (ptr != IntPtr.Zero)
                Marshal.GetDelegateForFunctionPointer<cef_browser_reload_ignore_cache>(ptr)(BrowserHandle);
        }

        /// <summary>
        /// 停止加载
        /// </summary>
        public void StopLoad()
        {
            if (BrowserHandle == IntPtr.Zero) return;
            // cef_browser_t vtable: stop_load = offset 112
            var ptr = Marshal.ReadIntPtr(BrowserHandle, Cef109VTableOffsets.BrowserStopLoad);
            if (ptr != IntPtr.Zero)
                Marshal.GetDelegateForFunctionPointer<cef_browser_stop_load>(ptr)(BrowserHandle);
        }

        /// <summary>
        /// 执行 JavaScript
        /// </summary>
        public void ExecuteJavaScript(string code, string url = "", int line = 0)
        {
            if (_browserHostHandle != IntPtr.Zero && !string.IsNullOrEmpty(code))
            {
                var codeStr = Marshal.StringToHGlobalUni(code + "\0");
                var urlStr = string.IsNullOrEmpty(url) ? IntPtr.Zero : Marshal.StringToHGlobalUni(url + "\0");

                var cefCode = new cef_string_t
                {
                    str = (char*)codeStr,
                    length = (UIntPtr)code.Length,
                    dtor = IntPtr.Zero
                };

                var cefUrl = new cef_string_t();
                if (urlStr != IntPtr.Zero)
                {
                    cefUrl.str = (char*)urlStr;
                    cefUrl.length = (UIntPtr)url.Length;
                    cefUrl.dtor = IntPtr.Zero;
                }

                NativeMethods.cef_browser_host_execute_javascript(_browserHostHandle, &cefCode, &cefUrl, line);

                Marshal.FreeHGlobal(codeStr);
                if (urlStr != IntPtr.Zero) Marshal.FreeHGlobal(urlStr);
            }
        }

        /// <summary>
        /// 导航到指定URL
        /// </summary>
        public void LoadUrl(string url)
        {
            if (BrowserHandle == IntPtr.Zero)
            {
                return;
            }

            // get_main_frame is vtable offset 152 in cef_browser_t
            var getMainFramePtr = Marshal.ReadIntPtr(BrowserHandle, Cef109VTableOffsets.BrowserGetMainFrame);
            if (getMainFramePtr == IntPtr.Zero)
            {
                return;
            }
            var getMainFrame = Marshal.GetDelegateForFunctionPointer<cef_browser_get_main_frame>(getMainFramePtr);
            var mainFrame = getMainFrame(BrowserHandle);
            if (mainFrame == IntPtr.Zero)
            {
                return;
            }

            // load_url is vtable offset 136 in cef_frame_t
            var loadUrlPtr = Marshal.ReadIntPtr(mainFrame, Cef109VTableOffsets.FrameLoadUrl);
            if (loadUrlPtr == IntPtr.Zero)
            {
                return;
            }
            var loadUrl = Marshal.GetDelegateForFunctionPointer<cef_frame_load_url>(loadUrlPtr);

            var urlStr = Marshal.StringToHGlobalUni(url);
            var cefUrl = new cef_string_t
            {
                str = (char*)urlStr,
                length = (UIntPtr)url.Length,
                dtor = IntPtr.Zero
            };

            loadUrl(mainFrame, &cefUrl);
            Marshal.FreeHGlobal(urlStr);

            _log.Information("[CefBrowserFactory] LoadUrl: {Url}", url);
        }

        /// <summary>
        /// 检查是否正在加载
        /// </summary>
        public bool IsLoading()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                return NativeMethods.cef_browser_host_is_loading(_browserHostHandle) != 0;
            }
            return false;
        }

        /// <summary>
        /// 关闭浏览器
        /// </summary>
        public void CloseBrowser(bool forceClose = false)
        {
            if (_browserHostHandle == IntPtr.Zero) return;
            var ptr = Marshal.ReadIntPtr(_browserHostHandle, Cef109VTableOffsets.BrowserHostCloseBrowser);
            if (ptr != IntPtr.Zero)
            {
                var closeBrowser = Marshal.GetDelegateForFunctionPointer<cef_browser_host_close_browser>(ptr);
                closeBrowser(_browserHostHandle, forceClose ? 1 : 0);
            }
        }

        /// <summary>
        /// 创建默认浏览器设置
        /// </summary>
        private static cef_browser_settings_t CreateDefaultSettings()
        {
            return new cef_browser_settings_t
            {
                size = (UIntPtr)sizeof(cef_browser_settings_t),
                windowless_frame_rate = 30,
                standard_font_family = new cef_string_t(),
                fixed_font_family = new cef_string_t(),
                serif_font_family = new cef_string_t(),
                sans_serif_font_family = new cef_string_t(),
                cursive_font_family = new cef_string_t(),
                fantasy_font_family = new cef_string_t(),
                default_font_size = 0,
                default_fixed_font_size = 0,
                minimum_font_size = 0,
                minimum_logical_font_size = 0,
                default_encoding = new cef_string_t(),
                remote_fonts = cef_state_t.STATE_DEFAULT,
                javascript = cef_state_t.STATE_ENABLED,
                javascript_close_windows = cef_state_t.STATE_DEFAULT,
                javascript_access_clipboard = cef_state_t.STATE_DEFAULT,
                javascript_dom_paste = cef_state_t.STATE_DEFAULT,
                image_loading = cef_state_t.STATE_DEFAULT,
                image_shrink_standalone_to_fit = cef_state_t.STATE_DEFAULT,
                text_area_resize = cef_state_t.STATE_DEFAULT,
                tab_to_links = cef_state_t.STATE_DEFAULT,
                local_storage = cef_state_t.STATE_DEFAULT,
                databases = cef_state_t.STATE_DEFAULT,
                webgl = cef_state_t.STATE_DEFAULT,
                background_color = 0xFFFFFFFF,
                accept_language_list = new cef_string_t(),
                chrome_status_bubble = cef_state_t.STATE_DEFAULT
            };
        }

        public void SetFocus()
        {
            if (_browserHostHandle == IntPtr.Zero) return;
            var ptr = Marshal.ReadIntPtr(_browserHostHandle, Cef109VTableOffsets.BrowserHostSetFocus);
            if (ptr != IntPtr.Zero)
            {
                var setFocus = Marshal.GetDelegateForFunctionPointer<cef_browser_host_set_focus>(ptr);
                setFocus(_browserHostHandle, 1);
            }
        }

        public void NotifyMoveOrResizeStarted()
        {
            if (_browserHostHandle == IntPtr.Zero) return;
            var ptr = Marshal.ReadIntPtr(_browserHostHandle, Cef109VTableOffsets.BrowserHostNotifyMoveOrResizeStarted);
            if (ptr != IntPtr.Zero)
            {
                var notify = Marshal.GetDelegateForFunctionPointer<cef_browser_host_notify_move_or_resize_started>(ptr);
                notify(_browserHostHandle);
            }
        }

        public void NotifyBrowserResized()
        {
            if (_browserHostHandle == IntPtr.Zero || _containerHwnd == IntPtr.Zero)
            {
                return;
            }

            // Get the container size
            GetClientRect(_containerHwnd, out RECT rc);

            // Get the browser's native window handle
            var browserHwnd = GetBrowserHwnd();
            if (browserHwnd == IntPtr.Zero)
            {
                return;
            }

            // Resize browser window to fill the parent container
            SetWindowPos(browserHwnd, IntPtr.Zero, rc.Left, rc.Top,
                rc.Right - rc.Left, rc.Bottom - rc.Top, SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);

            // Force show and invalidate
            ShowWindow(browserHwnd, SW_SHOW);
            InvalidateRect(browserHwnd, IntPtr.Zero, true);
            UpdateWindow(browserHwnd);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_SHOW = 5;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateWindow(IntPtr hWnd);

        private IntPtr GetBrowserHwnd()
        {
            // cef_browser_host_t vtable:
            // 0: base_
            // 40: get_browser
            // 48: close_browser
            // 56: try_close_browser
            // 64: set_focus
            // 72: get_window_handle
            var ptr = Marshal.ReadIntPtr(_browserHostHandle, Cef109VTableOffsets.BrowserHostGetWindowHandle);
            if (ptr == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            var getWindowHandle = Marshal.GetDelegateForFunctionPointer<cef_browser_host_get_window_handle>(ptr);
            return getWindowHandle(_browserHostHandle);
        }

        public void Dispose()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                CloseBrowser(true);
            }
            _browserHostHandle = IntPtr.Zero;
            _initialized = false;
        }
    }
}

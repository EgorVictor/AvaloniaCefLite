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
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private CefClient _client;
        private cef_browser_settings_t _settings;
        private bool _initialized;
        private IntPtr _browserHostHandle;

        public IntPtr BrowserHandle { get; private set; }
        public IntPtr BrowserHostHandle => _browserHostHandle;
        public bool IsInitialized => _initialized;
        
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
                    _log.Information("[CefBrowserFactory] Calling cef_browser_host_create_browser...");

                    var result = NativeMethods.cef_browser_host_create_browser(
                        &windowInfo,
                        clientPtr,
                        &url,
                        settingsPtr,
                        IntPtr.Zero,
                        IntPtr.Zero);

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
                        IntPtr.Zero,
                        IntPtr.Zero);

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
            if (_browserHostHandle != IntPtr.Zero)
            {
                NativeMethods.cef_browser_host_go_back(_browserHostHandle);
            }
        }

        /// <summary>
        /// 前进
        /// </summary>
        public void GoForward()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                NativeMethods.cef_browser_host_go_forward(_browserHostHandle);
            }
        }

        /// <summary>
        /// 重新加载
        /// </summary>
        public void Reload()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                NativeMethods.cef_browser_host_reload(_browserHostHandle);
            }
        }

        /// <summary>
        /// 重新加载（忽略缓存）
        /// </summary>
        public void ReloadIgnoreCache()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                NativeMethods.cef_browser_host_reload_ignore_cache(_browserHostHandle);
            }
        }

        /// <summary>
        /// 停止加载
        /// </summary>
        public void StopLoad()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                NativeMethods.cef_browser_host_stop_load(_browserHostHandle);
            }
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
                _log.Information("[CefBrowserFactory] Browser handle is zero, cannot load URL");
                return;
            }

            var mainFrame = NativeMethods.cef_browser_get_main_frame(BrowserHandle);
            if (mainFrame == IntPtr.Zero)
            {
                _log.Information("[CefBrowserFactory] Main frame is zero");
                return;
            }

            var urlStr = Marshal.StringToHGlobalUni(url + "\0");
            var cefUrl = new cef_string_t
            {
                str = (char*)urlStr,
                length = (UIntPtr)url.Length,
                dtor = IntPtr.Zero
            };

            NativeMethods.cef_frame_load_url(mainFrame, &cefUrl);
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
            if (_browserHostHandle != IntPtr.Zero)
            {
                NativeMethods.cef_browser_host_close_browser(_browserHostHandle, forceClose ? 1 : 0);
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
                javascript = 1,
                local_storage = 1,
                databases = 1,
                webgl = 1,
                background_color = 0xFFFFFFFF
            };
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

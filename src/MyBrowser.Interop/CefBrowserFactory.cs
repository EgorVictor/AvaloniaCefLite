namespace MyBrowser.Interop
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using MyBrowser.Interop.Internal;
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
        private CefBrowserSettings _settings;
        private bool _initialized;
        private IntPtr _browserHostHandle;

        public IntPtr BrowserHandle { get; private set; }
        public IntPtr BrowserHostHandle => _browserHostHandle;
        public bool IsInitialized => _initialized;

        public CefBrowserFactory()
        {
            // Initialize client with reference counting
            _client = new CefClient
            {
                Base = new CefBase
                {
                    size = (UIntPtr)sizeof(CefBase),
                    add_ref = IntPtr.Zero,
                    release = IntPtr.Zero,
                    has_one_ref = IntPtr.Zero,
                    has_at_least_one_ref = IntPtr.Zero
                }
            };

            // Initialize browser settings
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

            // Create window info
            var windowInfo = new CefWindowInfo
            {
                size = (UIntPtr)sizeof(CefWindowInfo),
                parent_window = parentHwnd,
                windowless_rendering_enabled = 0,
                style = 0xCF0000, // WS_OVERLAPPEDWINDOW
                ex_style = 0,
                bounds = new CefRect { x = 0, y = 0, width = 1024, height = 768 }
            };

            // Create URL string
            var url = new CefString();
            if (!string.IsNullOrEmpty(initialUrl))
            {
                var urlStr = Marshal.StringToHGlobalUni(initialUrl);
                url.str = (char*)urlStr;
                url.length = (UIntPtr)initialUrl.Length;
                url.dtor = IntPtr.Zero;
            }

            try
            {
                fixed (CefClient* clientPtr = &_client)
                fixed (CefBrowserSettings* settingsPtr = &_settings)
                {
                    _log.Information("[CefBrowserFactory] Calling cef_browser_host_create_browser...");

                    var result = CefNative.CefBrowserHost_CreateBrowser(
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
            // 参考 CefGlue: style = WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS | WS_TABSTOP | WS_VISIBLE
            var windowInfo = new CefWindowInfo
            {
                size = (UIntPtr)sizeof(CefWindowInfo),
                parent_window = parentHwnd,
                bounds = new CefRect { x = 0, y = 0, width = 1024, height = 768 },
                style = 0x40000000 | 0x04000000 | 0x02000000 | 0x00090000 | 0x10000000, // WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS | WS_TABSTOP | WS_VISIBLE
                ex_style = 0
            };

            var url = new CefString();
            if (!string.IsNullOrEmpty(initialUrl))
            {
                var urlStr = Marshal.StringToHGlobalUni(initialUrl + "\0");
                url.str = (char*)urlStr;
                url.length = (UIntPtr)initialUrl.Length;
            }

            try
            {
                fixed (CefClient* clientPtr = &_client)
                fixed (CefBrowserSettings* settingsPtr = &_settings)
                {
                    var browserPtr = CefNative.CefBrowserHost_CreateBrowserSync(
                        &windowInfo,
                        clientPtr,
                        &url,
                        settingsPtr,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    if (browserPtr != IntPtr.Zero)
                    {
                        browserHandle = browserPtr;
                        // 从 browser 获取 host 指针
                        _browserHostHandle = CefNative.CefBrowser_GetHost(browserPtr);
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
                CefNative.CefBrowserHost_GoBack(_browserHostHandle);
            }
        }

        /// <summary>
        /// 前进
        /// </summary>
        public void GoForward()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                CefNative.CefBrowserHost_GoForward(_browserHostHandle);
            }
        }

        /// <summary>
        /// 重新加载
        /// </summary>
        public void Reload()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                CefNative.CefBrowserHost_Reload(_browserHostHandle);
            }
        }

        /// <summary>
        /// 重新加载（忽略缓存）
        /// </summary>
        public void ReloadIgnoreCache()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                CefNative.CefBrowserHost_ReloadIgnoreCache(_browserHostHandle);
            }
        }

        /// <summary>
        /// 停止加载
        /// </summary>
        public void StopLoad()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                CefNative.CefBrowserHost_StopLoad(_browserHostHandle);
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

                var cefCode = new CefString
                {
                    str = (char*)codeStr,
                    length = (UIntPtr)code.Length,
                    dtor = IntPtr.Zero
                };

                var cefUrl = new CefString();
                if (urlStr != IntPtr.Zero)
                {
                    cefUrl.str = (char*)urlStr;
                    cefUrl.length = (UIntPtr)url.Length;
                    cefUrl.dtor = IntPtr.Zero;
                }

                CefNative.CefBrowserHost_ExecuteJavaScript(_browserHostHandle, &cefCode, &cefUrl, line);

                Marshal.FreeHGlobal(codeStr);
                if (urlStr != IntPtr.Zero) Marshal.FreeHGlobal(urlStr);
            }
        }

        /// <summary>
        /// 检查是否正在加载
        /// </summary>
        public bool IsLoading()
        {
            if (_browserHostHandle != IntPtr.Zero)
            {
                return CefNative.CefBrowserHost_IsLoading(_browserHostHandle) != 0;
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
                CefNative.CefBrowserHost_CloseBrowser(_browserHostHandle, forceClose ? 1 : 0);
            }
        }

        /// <summary>
        /// 创建默认浏览器设置
        /// </summary>
        private static CefBrowserSettings CreateDefaultSettings()
        {
            return new CefBrowserSettings
            {
                size = (UIntPtr)sizeof(CefBrowserSettings),
                windowless_frame_rate = 30,
                javascript = 1,
                local_storage = 1,
                databases = 1,
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
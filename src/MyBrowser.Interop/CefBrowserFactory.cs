namespace MyBrowser.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using MyBrowser.Interop.Internal;

    /// <summary>
    /// CEF 浏览器工厂
    /// 创建和管理浏览器实例 - 使用 CEF C API
    /// </summary>
    public sealed unsafe class CefBrowserFactory : IDisposable
    {
        private CefClient _client;
        private CefBrowserSettings _settings;
        private bool _initialized;

        public IntPtr BrowserHandle { get; private set; }
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
        }

        /// <summary>
        /// 创建浏览器窗口（异步）
        /// </summary>
        public bool CreateBrowser(IntPtr parentHwnd, string initialUrl, out IntPtr browserHandle)
        {
            System.Diagnostics.Debug.WriteLine($"[CefBrowserFactory] CreateBrowser: URL={initialUrl}, ParentHWND={parentHwnd}");
            browserHandle = IntPtr.Zero;

            if (!CefRuntime.IsInitialized)
            {
                System.Diagnostics.Debug.WriteLine("[CefBrowserFactory] CEF not initialized!");
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
                    System.Diagnostics.Debug.WriteLine("[CefBrowserFactory] Calling cef_browser_host_create_browser...");
                    
                    var result = CefNative.CefBrowserHost_CreateBrowser(
                        &windowInfo,
                        clientPtr,
                        &url,
                        settingsPtr,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    System.Diagnostics.Debug.WriteLine($"[CefBrowserFactory] Result: {result}");

                    if (result != 0)
                    {
                        _initialized = true;
                        System.Diagnostics.Debug.WriteLine("[CefBrowserFactory] Browser created successfully");
                        return true;
                    }
                }

                System.Diagnostics.Debug.WriteLine("[CefBrowserFactory] FAILED to create browser!");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CefBrowserFactory] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
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
            System.Diagnostics.Debug.WriteLine($"[CefBrowserFactory] CreateBrowserSync: URL={initialUrl}");
            browserHandle = IntPtr.Zero;

            if (!CefRuntime.IsInitialized)
            {
                System.Diagnostics.Debug.WriteLine("[CefBrowserFactory] CEF not initialized!");
                return false;
            }

            var windowInfo = new CefWindowInfo
            {
                size = (UIntPtr)sizeof(CefWindowInfo),
                parent_window = parentHwnd,
                bounds = new CefRect { width = 1024, height = 768 }
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
                        _initialized = true;
                        System.Diagnostics.Debug.WriteLine("[CefBrowserFactory] Browser created (sync) OK");
                        return true;
                    }
                }

                System.Diagnostics.Debug.WriteLine("[CefBrowserFactory] Sync create FAILED!");
                return false;
            }
            finally
            {
                if (url.str != null) Marshal.FreeHGlobal((IntPtr)url.str);
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
            _initialized = false;
        }
    }
}
namespace MyBrowser.Driver.Modern
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using MyBrowser;
    using MyBrowser.Interop;
    using Serilog;

    /// <summary>
    /// 现代CEF驱动 - Windows 10+兼容性
    /// 物理隔离在Runtimes/Modern文件夹中
    /// </summary>
    public sealed class CefFactory : IBrowserFactory
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private BrowserConfig _config;
        private bool _initialized;
        private CefBrowserFactory _browserFactory;
        private IntPtr _browserHandle;
        
        /// <summary>
        /// 驱动目录路径（libcef.dll所在位置）
        /// </summary>
        public static string DriverDirectory { get; private set; }

        public string Version => "109.1.11";
        public string DriverType => "Modern";
        public bool IsInitialized => _initialized;

        public void Initialize(BrowserConfig config)
        {
            if (_initialized)
            {
                _log.Information("[CefFactory.Modern] 已经初始化，跳过");
                return;
            }

            _config = config ?? new BrowserConfig();

            DriverDirectory = Path.GetDirectoryName(typeof(CefFactory).Assembly.Location)
                ?? throw new InvalidOperationException("无法确定驱动目录");

            _log.Information("[CefFactory.Modern] 正在初始化现代CEF...");
            _log.Information("[CefFactory.Modern] 驱动目录: {DriverDirectory}", DriverDirectory);
            _log.Information("[CefFactory.Modern] 无窗口模式: {WindowlessRendering}", _config.WindowlessRendering);
            _log.Information("[CefFactory.Modern] 硬件加速: {HardwareAcceleration}", _config.HardwareAcceleration);
            _log.Information("[CefFactory.Modern] 缓存路径: {CachePath}", _config.CachePath);
            _log.Information("[CefFactory.Modern] 初始URL: {InitialUrl}", _config.InitialUrl);

            SetDllDirectory(DriverDirectory);

            // 使用多线程消息循环，避免时序问题导致浏览器创建失败
            CefRuntime.Initialize(GetModuleHandle(null), multiThreadedMessageLoop: true);

            _browserFactory = new CefBrowserFactory();
            _initialized = true;

            _log.Information("[CefFactory.Modern] CEF 初始化完成");
        }

        public object CreateControl()
        {
            if (!_initialized)
                throw new InvalidOperationException("工厂未初始化。请先调用Initialize()方法。");

            _log.Information("[CefFactory.Modern] 正在创建浏览器控件...");

            // 传递 CefBrowserFactory 以便 ModernBrowserControl 能够创建真实浏览器
            return new ModernBrowserControl(_config, DriverDirectory, _browserFactory);
        }

        /// <summary>
        /// 创建浏览器窗口
        /// </summary>
        public bool CreateBrowser(IntPtr parentHwnd, string initialUrl)
        {
            if (!_initialized || _browserFactory == null)
            {
                _log.Information("[CefFactory.Modern] 工厂未初始化");
                return false;
            }

            _log.Information("[CefFactory.Modern] 创建浏览器, URL: {InitialUrl}", initialUrl);

            var success = _browserFactory.CreateBrowser(parentHwnd, initialUrl, out _browserHandle);

            if (success)
            {
                _log.Information("[CefFactory.Modern] 浏览器创建成功, Handle: {BrowserHandle}", _browserHandle);
            }
            else
            {
                _log.Information("[CefFactory.Modern] 浏览器创建失败");
            }

            return success;
        }

        public void Shutdown()
        {
            _log.Information("[CefFactory.Modern] 正在关闭...");

            _browserFactory?.Dispose();
            _browserFactory = null;

            CefRuntime.Shutdown();
            _initialized = false;

            _log.Information("[CefFactory.Modern] 关闭完成");
        }

        /// <summary>
        /// 设置 DLL 搜索目录
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        /// <summary>
        /// 获取模块句柄
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    /// <summary>
    /// Modern驱动浏览器控件
    /// </summary>
    public class ModernBrowserControl : IBrowserControl
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private readonly BrowserConfig _config;
        private readonly string _driverDir;
        private readonly CefBrowserFactory _browserFactory;
        private IntPtr _browserHandle;
        private IntPtr _windowHandle;
        private string _url = "about:blank";
        private string _title = "Modern Browser";
        private bool _isLoading;
        private bool _canGoBack;
        private bool _canGoForward;

        public ModernBrowserControl(BrowserConfig config, string driverDir, CefBrowserFactory browserFactory)
        {
            _config = config;
            _driverDir = driverDir;
            _browserFactory = browserFactory;
            _url = config.InitialUrl;

            // 订阅CEF客户端事件
            _browserFactory.Client.LoadStart += (s, e) => OnCefLoadStart();
            _browserFactory.Client.LoadEnd += (s, code) => OnCefLoadEnd(code);
            _browserFactory.Client.LoadError += (s, error) => OnCefLoadError(error);
            _browserFactory.Client.TitleChanged += (s, title) => OnCefTitleChanged(title);
            _browserFactory.Client.AddressChanged += (s, url) => OnCefAddressChanged(url);
            _browserFactory.Client.BrowserCreated += (s, e) =>
            {
                _browserHandle = e.BrowserHandle;
                _canGoBack = false;
                _canGoForward = false;
                BrowserInitialized?.Invoke(this, EventArgs.Empty);
            };

            _log.Information("[ModernBrowserControl] 已创建, 驱动目录: {DriverDir}", _driverDir);
        }

        public bool IsLoading => _isLoading;
        public string Url => _url;
        public string Title => _title;
        public bool CanGoBack => _canGoBack;
        public bool CanGoForward => _canGoForward;

        public event EventHandler BrowserInitialized;
        public event EventHandler<LoadStartEventArgs> LoadStart;
        public event EventHandler<LoadEndEventArgs> LoadEnd;
        public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;
        public event EventHandler<TitleChangedEventArgs> TitleChanged;
        public event EventHandler<AddressChangedEventArgs> AddressChanged;

        /// <summary>
        /// 设置父窗口句柄并创建浏览器
        /// </summary>
        public void SetWindowHandle(IntPtr parentHwnd)
        {
            _windowHandle = parentHwnd;
            _log.Information("[ModernBrowserControl] 设置窗口句柄: {HWND}", parentHwnd);

            // 如果已经有URL，则创建浏览器
            if (_windowHandle != IntPtr.Zero && !string.IsNullOrEmpty(_url))
            {
                CreateBrowser();
            }
        }

        /// <summary>
        /// 创建CEF浏览器
        /// </summary>
        private bool CreateBrowser()
        {
            if (_windowHandle == IntPtr.Zero)
            {
                _log.Information("[ModernBrowserControl] 窗口句柄无效，无法创建浏览器");
                return false;
            }

            _log.Information("[ModernBrowserControl] 正在创建浏览器, URL: {Url}, HWND: {HWND}", _url, _windowHandle);

            // 使用异步方法创建浏览器，因为同步方法在消息循环未运行时可能失败
            var success = _browserFactory.CreateBrowser(_windowHandle, _url, out _browserHandle);

            if (success)
            {
                _log.Information("[ModernBrowserControl] 浏览器创建请求已提交");
                _canGoBack = false;
                _canGoForward = false;
            }
            else
            {
                _log.Information("[ModernBrowserControl] 浏览器创建失败");
            }

            return success;
        }

        public void LoadUrl(string url)
        {
            _log.Information("[ModernBrowserControl] LoadUrl: {Url}", url);
            _url = url;
            _isLoading = true;
            LoadStart?.Invoke(this, new LoadStartEventArgs { IsMainFrame = true });

            // 如果窗口句柄已设置但浏览器还未创建，则创建浏览器
            if (_windowHandle != IntPtr.Zero && _browserHandle == IntPtr.Zero)
            {
                CreateBrowser();
            }

            // 触发加载完成事件（模拟）
            _isLoading = false;
            LoadEnd?.Invoke(this, new LoadEndEventArgs { IsMainFrame = true, HttpStatusCode = 200 });
            AddressChanged?.Invoke(this, new AddressChangedEventArgs { Address = url, IsMainFrame = true });
        }

        public void GoBack()
        {
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _log.Information("[ModernBrowserControl] 后退");
                _browserFactory.GoBack();
            }
        }

        public void GoForward()
        {
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _log.Information("[ModernBrowserControl] 前进");
                _browserFactory.GoForward();
            }
        }

        public void Reload()
        {
            _log.Information("[ModernBrowserControl] 重新加载");
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _browserFactory.Reload();
            }
        }

        public void Stop()
        {
            _log.Information("[ModernBrowserControl] 停止");
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _browserFactory.StopLoad();
            }
            _isLoading = false;
        }

        public void ExecuteJavaScript(string script)
        {
            _log.Information("[ModernBrowserControl] 执行脚本: {Script}", script);
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _browserFactory.ExecuteJavaScript(script, _url, 0);
            }
        }

        /// <summary>
        /// CEF 事件处理程序
        /// </summary>
        private void OnCefLoadStart()
        {
            _isLoading = true;
            _log.Information("[ModernBrowserControl] CEF OnLoadStart");
            LoadStart?.Invoke(this, new LoadStartEventArgs { IsMainFrame = true });
        }

        private void OnCefLoadEnd(int httpStatusCode)
        {
            _isLoading = false;
            _log.Information("[ModernBrowserControl] CEF OnLoadEnd, StatusCode: {Code}", httpStatusCode);
            LoadEnd?.Invoke(this, new LoadEndEventArgs { IsMainFrame = true, HttpStatusCode = httpStatusCode });
        }

        private void OnCefLoadError(string error)
        {
            _isLoading = false;
            _log.Information("[ModernBrowserControl] CEF OnLoadError: {Error}", error);
            // 可在此处转发到自定义错误事件
        }

        private void OnCefTitleChanged(string title)
        {
            _title = title;
            _log.Information("[ModernBrowserControl] CEF OnTitleChange: {Title}", title);
            TitleChanged?.Invoke(this, new TitleChangedEventArgs { Title = title });
        }

        private void OnCefAddressChanged(string url)
        {
            _url = url;
            _log.Information("[ModernBrowserControl] CEF OnAddressChange: {Url}", url);
            AddressChanged?.Invoke(this, new AddressChangedEventArgs { Address = url, IsMainFrame = true });
        }

        public void Dispose()
        {
            _log.Information("[ModernBrowserControl] 已释放");
            _browserHandle = IntPtr.Zero;
        }
    }
}

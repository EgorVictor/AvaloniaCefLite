namespace MyBrowser.Driver.Cef109
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using MyBrowser;
    using MyBrowser.Interop;
    using Serilog;

    /// <summary>
    /// CEF 109 驱动 - 支持 Win7 / Win10+
    /// 通过 CefCompatibilityMode 决定运行策略
    /// </summary>
    public sealed class Cef109Factory : IBrowserFactory
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(LogHelper.GetLogPath(), shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private BrowserConfig _config;
        private CefCompatibilityMode _policy;
        private bool _initialized;
        private CefBrowserFactory _browserFactory;
        private IntPtr _browserHandle;
        
        public static string DriverDirectory { get; private set; }

        public string Version => "109.1.11";
        public string DriverType => "Cef109";
        public bool IsInitialized => _initialized;

        public void Initialize(BrowserConfig config, CefCompatibilityMode policy)
        {
            if (_initialized)
            {
                _log.Information("[Cef109Factory] 已经初始化，跳过");
                return;
            }

            _config = config ?? new BrowserConfig();
            _policy = policy;

            DriverDirectory = !string.IsNullOrWhiteSpace(_config.RuntimePath)
                ? _config.RuntimePath
                : Path.GetDirectoryName(typeof(Cef109Factory).Assembly.Location)
                    ?? throw new InvalidOperationException("无法确定驱动目录");

            _log.Information("[Cef109Factory] 正在初始化 CEF 109...");
            _log.Information("[Cef109Factory] 驱动目录: {DriverDirectory}", DriverDirectory);
            _log.Information("[Cef109Factory] 兼容策略: {Policy}", _policy);
            _log.Information("[Cef109Factory] 硬件加速: {HardwareAcceleration}", _config.HardwareAcceleration);
            _log.Information("[Cef109Factory] 缓存路径: {CachePath}", _config.CachePath);
            _log.Information("[Cef109Factory] 初始URL: {InitialUrl}", _config.InitialUrl);

            SetDllDirectory(DriverDirectory);
            _log.Information("[Cef109Factory] SetDllDirectory done");

            var options = CefRuntimeOptions.FromConfig(_config, Environment.OSVersion.Version);
            options.RuntimePath = DriverDirectory;
            options.CompatibilityMode = _policy;
            options.IgnoreCertificateErrors = _config.IgnoreCertificateErrors;

            _log.Information("[Cef109Factory] >>> Calling CefRuntime.Initialize <<<");
            CefRuntime.Initialize(GetModuleHandle(null), options);
            _log.Information("[Cef109Factory] CefRuntime.Initialize returned");

            _browserFactory = new CefBrowserFactory { DisableWebGL = options.DisableWebGL };
            _initialized = true;

            _log.Information("[Cef109Factory] CEF 109 初始化完成");
        }

        public object CreateControl()
        {
            if (!_initialized)
                throw new InvalidOperationException("工厂未初始化。请先调用Initialize()方法。");

            _log.Information("[Cef109Factory] 正在创建浏览器控件...");

            return new Cef109BrowserControl(_config, DriverDirectory, _browserFactory);
        }

        public void Shutdown()
        {
            _log.Information("[Cef109Factory] 正在关闭...");

            _browserFactory?.Dispose();
            _browserFactory = null;

            CefRuntime.Shutdown();
            _initialized = false;

            _log.Information("[Cef109Factory] 关闭完成");
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    /// <summary>
    /// Cef109 浏览器控件
    /// </summary>
    public class Cef109BrowserControl : IBrowserControl
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(LogHelper.GetLogPath(), shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private readonly BrowserConfig _config;
        private readonly string _driverDir;
        private readonly CefBrowserFactory _browserFactory;
        private IntPtr _browserHandle;
        private IntPtr _windowHandle;
        private string _url = "about:blank";
        private string _title = "Browser";
        private bool _isLoading;
        private bool _canGoBack;
        private bool _canGoForward;

        public Cef109BrowserControl(BrowserConfig config, string driverDir, CefBrowserFactory browserFactory)
        {
            _config = config;
            _driverDir = driverDir;
            _browserFactory = browserFactory;
            _url = config.InitialUrl;

            _browserFactory.Client.LoadStart += (s, e) => OnCefLoadStart();
            _browserFactory.Client.LoadEnd += (s, code) => OnCefLoadEnd(code);
            _browserFactory.Client.LoadError += (s, error) => OnCefLoadError(error);
            _browserFactory.Client.TitleChanged += (s, title) => OnCefTitleChanged(title);
            _browserFactory.Client.AddressChanged += (s, url) => OnCefAddressChanged(url);
            _browserFactory.Client.LoadingStateChanged += (s, isLoading) => OnCefLoadingStateChanged(isLoading);
            _browserFactory.Client.CanGoBackChanged += (s, canGoBack) => OnCefCanGoBackChanged(canGoBack);
            _browserFactory.Client.CanGoForwardChanged += (s, canGoForward) => OnCefCanGoForwardChanged(canGoForward);
            _browserFactory.Client.PopupRequested += (s, url) => OnCefPopupRequested(url);
            _browserFactory.Client.BrowserCreated += (s, e) =>
            {
                _browserHandle = e.BrowserHandle;
                _canGoBack = false;
                _canGoForward = false;
                BrowserInitialized?.Invoke(this, EventArgs.Empty);
            };

            _log.Information("[Cef109BrowserControl] 已创建, 驱动目录: {DriverDir}", _driverDir);
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
        public event EventHandler<string> PopupRequested;

        public void SetWindowHandle(IntPtr parentHwnd)
        {
            _windowHandle = parentHwnd;
            _log.Information("[Cef109BrowserControl] 设置窗口句柄: {HWND}", parentHwnd);

            if (_windowHandle != IntPtr.Zero && !string.IsNullOrEmpty(_url))
            {
                if (CefRuntime.IsContextReady)
                {
                    CreateBrowser();
                }
                else
                {
                    _log.Information("[Cef109BrowserControl] CEF Context 未就绪，等待 ContextInitialized");
                    CefRuntime.ContextInitialized += OnCefContextInitialized;
                }
            }
        }

        private void OnCefContextInitialized(object? sender, EventArgs e)
        {
            CefRuntime.ContextInitialized -= OnCefContextInitialized;
            if (_windowHandle != IntPtr.Zero && !string.IsNullOrEmpty(_url) && _browserHandle == IntPtr.Zero)
            {
                _log.Information("[Cef109BrowserControl] CEF Context 已就绪，创建浏览器");
                CreateBrowser();
            }
        }

        private bool CreateBrowser()
        {
            if (_windowHandle == IntPtr.Zero)
            {
                _log.Information("[Cef109BrowserControl] 窗口句柄无效，无法创建浏览器");
                return false;
            }

            _log.Information("[Cef109BrowserControl] 正在创建浏览器, URL: {Url}, HWND: {HWND}", _url, _windowHandle);

            _browserFactory.SetContainerHwnd(_windowHandle);

            var success = _browserFactory.CreateBrowser(_windowHandle, _url, out _browserHandle);

            if (success)
            {
                _log.Information("[Cef109BrowserControl] 浏览器创建请求已提交");
                _canGoBack = false;
                _canGoForward = false;
            }
            else
            {
                _log.Information("[Cef109BrowserControl] 浏览器创建失败");
            }

            return success;
        }

        public void LoadUrl(string url)
        {
            _log.Information("[Cef109BrowserControl] LoadUrl: {Url}", url);
            _url = url;

            if (_windowHandle != IntPtr.Zero && _browserHandle == IntPtr.Zero)
            {
                if (CefRuntime.IsContextReady)
                {
                    CreateBrowser();
                }
                else
                {
                    _log.Information("[Cef109BrowserControl] CEF Context 未就绪，等待 ContextInitialized 后创建");
                    CefRuntime.ContextInitialized += OnCefContextInitialized;
                }
                return;
            }

            if (_browserHandle != IntPtr.Zero)
            {
                _browserFactory.LoadUrl(url);
            }
        }

        public void GoBack()
        {
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _log.Information("[Cef109BrowserControl] 后退");
                _browserFactory.GoBack();
            }
        }

        public void GoForward()
        {
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _log.Information("[Cef109BrowserControl] 前进");
                _browserFactory.GoForward();
            }
        }

        public void Reload()
        {
            _log.Information("[Cef109BrowserControl] 重新加载");
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _browserFactory.Reload();
            }
        }

        public void Stop()
        {
            _log.Information("[Cef109BrowserControl] 停止");
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _browserFactory.StopLoad();
            }
            _isLoading = false;
        }

        public void ExecuteJavaScript(string script)
        {
            _log.Information("[Cef109BrowserControl] 执行脚本: {Script}", script);
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _browserFactory.ExecuteJavaScript(script, _url, 0);
            }
        }

        public void NotifyResized()
        {
            if (_browserFactory.BrowserHostHandle != IntPtr.Zero)
            {
                _browserFactory.NotifyBrowserResized();
            }
        }

        private void OnCefLoadStart()
        {
            _isLoading = true;
            _log.Information("[Cef109BrowserControl] CEF OnLoadStart");
            LoadStart?.Invoke(this, new LoadStartEventArgs { IsMainFrame = true });
        }

        private void OnCefLoadEnd(int httpStatusCode)
        {
            _isLoading = false;
            _log.Information("[Cef109BrowserControl] CEF OnLoadEnd, StatusCode: {Code}", httpStatusCode);
            LoadEnd?.Invoke(this, new LoadEndEventArgs { IsMainFrame = true, HttpStatusCode = httpStatusCode });
        }

        private void OnCefLoadError(string error)
        {
            _isLoading = false;
            _log.Information("[Cef109BrowserControl] CEF OnLoadError: {Error}", error);
        }

        private void OnCefTitleChanged(string title)
        {
            _title = title;
            _log.Information("[Cef109BrowserControl] CEF OnTitleChange: {Title}", title);
            TitleChanged?.Invoke(this, new TitleChangedEventArgs { Title = title });
        }

        private void OnCefAddressChanged(string url)
        {
            _url = url;
            _log.Information("[Cef109BrowserControl] CEF OnAddressChange: {Url}", url);
            AddressChanged?.Invoke(this, new AddressChangedEventArgs { Address = url, IsMainFrame = true });
        }

        private void OnCefLoadingStateChanged(bool isLoading)
        {
            _isLoading = isLoading;
            _log.Information("[Cef109BrowserControl] CEF OnLoadingStateChanged: {IsLoading}", isLoading);
        }

        private void OnCefCanGoBackChanged(bool canGoBack)
        {
            if (_canGoBack != canGoBack)
            {
                _canGoBack = canGoBack;
                _log.Information("[Cef109BrowserControl] CEF CanGoBack: {CanGoBack}", canGoBack);
            }
        }

        private void OnCefCanGoForwardChanged(bool canGoForward)
        {
            if (_canGoForward != canGoForward)
            {
                _canGoForward = canGoForward;
                _log.Information("[Cef109BrowserControl] CEF CanGoForward: {CanGoForward}", canGoForward);
            }
        }

        private void OnCefPopupRequested(string url)
        {
            _log.Information("[Cef109BrowserControl] CEF Popup intercepted, URL: {Url}", url);
            PopupRequested?.Invoke(this, url);
        }

        public void Dispose()
        {
            _log.Information("[Cef109BrowserControl] 已释放");
            _browserHandle = IntPtr.Zero;
        }
    }
}

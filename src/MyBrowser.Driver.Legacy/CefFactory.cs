namespace MyBrowser.Driver.Legacy
{
    using System;
    using System.IO;
    using System.Diagnostics;
    using MyBrowser;
    using MyBrowser.Interop;

    /// <summary>
    /// CEF 109驱动 - Windows 7兼容性
    /// 物理隔离在Runtimes/Legacy文件夹中
    /// </summary>
    public sealed class CefFactory : IBrowserFactory
    {
        private BrowserConfig _config;
        private bool _initialized;
        
        /// <summary>
        /// 驱动目录路径（libcef.dll所在位置）
        /// </summary>
        public static string DriverDirectory { get; private set; }

        public string Version => "109.1.36";
        public string DriverType => "Legacy";
        public bool IsInitialized => _initialized;

        public void Initialize(BrowserConfig config)
        {
            _config = config ?? new BrowserConfig();
            
            // 获取此驱动DLL所在的目录
            DriverDirectory = Path.GetDirectoryName(typeof(CefFactory).Assembly.Location) 
                ?? throw new InvalidOperationException("无法确定驱动目录");

            Logger.Log("[CefFactory.Legacy] 正在初始化CEF 109...");
            Logger.Log("[CefFactory.Legacy] 驱动目录: {DriverDirectory}", DriverDirectory);
            Logger.Log("[CefFactory.Legacy] 无窗口模式: {WindowlessRendering}", _config.WindowlessRendering);
            Logger.Log("[CefFactory.Legacy] 硬件加速: {HardwareAcceleration}", _config.HardwareAcceleration);
            Logger.Log("[CefFactory.Legacy] 缓存路径: {CachePath}", _config.CachePath);
            Logger.Log("[CefFactory.Legacy] 初始URL: {InitialUrl}", _config.InitialUrl);

            // Win7 配置：禁用 GPU 和沙箱
            // 实际实现中需要配置 CefSettings:
            // - settings.NoSandbox = true
            // - settings.BrowserSubprocessPath = 子进程路径
            // - settings.CommandLineArgs["disable-gpu"] = ""
            // - settings.CommandLineArgs["disable-software-rasterizer"] = ""

            if (!_config.HardwareAcceleration)
            {
                Logger.Log("[CefFactory.Legacy] GPU已禁用(Win7模式)");
            }

            Logger.Log("[CefFactory.Legacy] CEF 109配置:");
            Logger.Log("  --no-sandbox");
            Logger.Log("  --disable-gpu");
            Logger.Log("  --disable-software-rasterizer");
            Logger.Log("  BrowserSubprocessPath: {BrowserSubprocessPath}", Path.Combine(DriverDirectory, "CefRenderProcess.exe"));

            _initialized = true;
        }

        public object CreateControl()
        {
            if (!_initialized)
                throw new InvalidOperationException("工厂未初始化。请先调用Initialize()方法。");

            Logger.Log("[CefFactory.Legacy] 正在创建浏览器控件...");

            // TODO: 返回真实的 AvaloniaCefBrowser 包装控件
            // 目前返回占位符
            return new LegacyBrowserControl(_config, DriverDirectory);
        }

        public void Shutdown()
        {
            Logger.Log("[CefFactory.Legacy] 正在关闭...");
            _initialized = false;
        }
    }

    /// <summary>
    /// Legacy驱动浏览器控件
    /// 
    /// 注意：目前是占位符实现
    /// 要实现真正的浏览器功能，需要:
    /// 1. 添加 CefGlue NuGet 包引用
    /// 2. 使用 AvaloniaCefBrowser 作为底层控件
    /// 3. 配置 CefRuntimeLoader.Initialize()
    /// </summary>
    public class LegacyBrowserControl : IBrowserControl
    {
        private readonly BrowserConfig _config;
        private readonly string _driverDir;
        private string _url = "about:blank";
        private string _title = "Legacy Browser";
        private bool _isLoading;

        public LegacyBrowserControl(BrowserConfig config, string driverDir)
        {
            _config = config;
            _driverDir = driverDir;
            _url = config.InitialUrl;

            Logger.Log("[LegacyBrowserControl] 已创建, 驱动目录: {DriverDir}", _driverDir);
        }

        public bool IsLoading => _isLoading;
        public string Url => _url;
        public string Title => _title;
        public bool CanGoBack => false;
        public bool CanGoForward => false;

        public event EventHandler BrowserInitialized;
        public event EventHandler<LoadStartEventArgs> LoadStart;
        public event EventHandler<LoadEndEventArgs> LoadEnd;
        public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;
        public event EventHandler<TitleChangedEventArgs> TitleChanged;
        public event EventHandler<AddressChangedEventArgs> AddressChanged;

        public void LoadUrl(string url)
        {
            Logger.Log("[LegacyBrowserControl] LoadUrl: {Url}", url);
            _url = url;
            _isLoading = true;
            LoadStart?.Invoke(this, new LoadStartEventArgs { IsMainFrame = true });

            // TODO: 调用真实的浏览器加载
            // CefBrowserHost.CreateBrowser(windowHandle, settings, url);

            _isLoading = false;
            LoadEnd?.Invoke(this, new LoadEndEventArgs { IsMainFrame = true, HttpStatusCode = 200 });
            AddressChanged?.Invoke(this, new AddressChangedEventArgs { Address = url, IsMainFrame = true });
        }

        public void GoBack()
        {
            Logger.Log("[LegacyBrowserControl] 后退 - 未实现");
        }

        public void GoForward()
        {
            Logger.Log("[LegacyBrowserControl] 前进 - 未实现");
        }

        public void Reload()
        {
            Logger.Log("[LegacyBrowserControl] 重新加载");
            LoadUrl(_url);
        }

        public void Stop()
        {
            Logger.Log("[LegacyBrowserControl] 停止");
            _isLoading = false;
        }

        public void ExecuteJavaScript(string script)
        {
            Logger.Log("[LegacyBrowserControl] 执行脚本: {Script}", script);
            // TODO: CefBrowserHost.ExecuteJavaScript(script, url, 0);
        }

        public void Dispose()
        {
            Logger.Log("[LegacyBrowserControl] 已释放");
        }
    }
}
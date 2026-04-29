namespace MyBrowser.Driver.Legacy
{
    using System;
    using System.IO;
    using MyBrowser;

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

            Console.WriteLine("[CefFactory.Legacy] 正在初始化CEF 109...");
            Console.WriteLine($"[CefFactory.Legacy] 驱动目录: {DriverDirectory}");
            Console.WriteLine($"[CefFactory.Legacy] 无窗口模式: {_config.WindowlessRendering}");
            Console.WriteLine($"[CefFactory.Legacy] 硬件加速: {_config.HardwareAcceleration}");
            Console.WriteLine($"[CefFactory.Legacy] 缓存路径: {_config.CachePath}");
            Console.WriteLine($"[CefFactory.Legacy] 初始URL: {_config.InitialUrl}");

            // 为Win7配置CEF
            // 实际实现中需要配置:
            // - settings.NoSandbox = true
            // - settings.BrowserSubprocessPath = Path.Combine(DriverDirectory, "CefRenderProcess.exe")
            // - settings.CommandLineArgs["disable-gpu"] = ""
            // - settings.CommandLineArgs["disable-software-rasterizer"] = ""
            
            if (!_config.HardwareAcceleration)
            {
                Console.WriteLine("[CefFactory.Legacy] GPU已禁用(Win7模式)");
            }
            
            Console.WriteLine("[CefFactory.Legacy] CEF 109配置:");
            Console.WriteLine($"  --no-sandbox");
            Console.WriteLine($"  --disable-gpu");
            Console.WriteLine($"  --disable-software-rasterizer");
            Console.WriteLine($"  BrowserSubprocessPath: {Path.Combine(DriverDirectory, "CefRenderProcess.exe")}");
            
            _initialized = true;
        }

        public object CreateControl()
        {
            if (!_initialized)
                throw new InvalidOperationException("工厂未初始化。请先调用Initialize()方法。");

            Console.WriteLine("[CefFactory.Legacy] 正在创建浏览器控件...");

            return new LegacyBrowserControl(_config, DriverDirectory);
        }

        public void Shutdown()
        {
            Console.WriteLine("[CefFactory.Legacy] 正在关闭...");
            _initialized = false;
        }
    }

    /// <summary>
    /// Legacy驱动浏览器控件
    /// 生产环境中应包装AvaloniaCefBrowser
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
            
            Console.WriteLine($"[LegacyBrowserControl] 已创建, 驱动目录: {_driverDir}");
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
            Console.WriteLine($"[LegacyBrowserControl] LoadUrl: {url}");
            _url = url;
            _isLoading = true;
            LoadStart?.Invoke(this, new LoadStartEventArgs { IsMainFrame = true });
            
            // 实际实现中应调用:
            // CefBrowserHost.CreateBrowser(windowHandle, settings, url);
            
            _isLoading = false;
            LoadEnd?.Invoke(this, new LoadEndEventArgs { IsMainFrame = true, HttpStatusCode = 200 });
            AddressChanged?.Invoke(this, new AddressChangedEventArgs { Address = url, IsMainFrame = true });
        }

        public void GoBack() 
        {
            Console.WriteLine("[LegacyBrowserControl] 后退 - 未实现");
        }
        
        public void GoForward() 
        {
            Console.WriteLine("[LegacyBrowserControl] 前进 - 未实现");
        }
        
        public void Reload() 
        {
            Console.WriteLine("[LegacyBrowserControl] 重新加载");
            LoadUrl(_url);
        }
        
        public void Stop() 
        {
            Console.WriteLine("[LegacyBrowserControl] 停止");
            _isLoading = false;
        }
        
        public void ExecuteJavaScript(string script)
        {
            Console.WriteLine($"[LegacyBrowserControl] 执行脚本: {script}");
            // 实际实现中应调用: CefBrowserHost.ExecuteJavaScript(script, url, 0);
        }

        public void Dispose()
        {
            Console.WriteLine("[LegacyBrowserControl] 已释放");
        }
    }
}
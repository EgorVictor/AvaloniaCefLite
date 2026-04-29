namespace MyBrowser.Driver.Modern
{
    using System;
    using System.IO;
    using MyBrowser;

    /// <summary>
    /// 现代CEF驱动 - Windows 10+兼容性
    /// 物理隔离在Runtimes/Modern文件夹中
    /// </summary>
    public sealed class CefFactory : IBrowserFactory
    {
        private BrowserConfig _config;
        private bool _initialized;
        
        /// <summary>
        /// 驱动目录路径（libcef.dll所在位置）
        /// </summary>
        public static string DriverDirectory { get; private set; }

        public string Version => "120.2.70";
        public string DriverType => "Modern";
        public bool IsInitialized => _initialized;

        public void Initialize(BrowserConfig config)
        {
            _config = config ?? new BrowserConfig();
            
            // 获取此驱动DLL所在的目录
            DriverDirectory = Path.GetDirectoryName(typeof(CefFactory).Assembly.Location) 
                ?? throw new InvalidOperationException("无法确定驱动目录");

            Console.WriteLine("[CefFactory.Modern] 正在初始化现代CEF...");
            Console.WriteLine($"[CefFactory.Modern] 驱动目录: {DriverDirectory}");
            Console.WriteLine($"[CefFactory.Modern] 无窗口模式: {_config.WindowlessRendering}");
            Console.WriteLine($"[CefFactory.Modern] 硬件加速: {_config.HardwareAcceleration}");
            Console.WriteLine($"[CefFactory.Modern] 缓存路径: {_config.CachePath}");
            Console.WriteLine($"[CefFactory.Modern] 初始URL: {_config.InitialUrl}");

            // 为Win10+配置CEF
            // 实际实现中需要配置:
            // - settings.NoSandbox = false (现代Windows不需要)
            // - settings.BrowserSubprocessPath = Path.Combine(DriverDirectory, "CefRenderProcess.exe")
            // - settings.CommandLineArgs["enable-gpu"] = "" (如果启用了硬件加速)
            
            if (_config.HardwareAcceleration)
            {
                Console.WriteLine("[CefFactory.Modern] 硬件加速已启用");
            }
            else
            {
                Console.WriteLine("[CefFactory.Modern] GPU已禁用");
            }
            
            Console.WriteLine("[CefFactory.Modern] 现代CEF配置:");
            Console.WriteLine($"  BrowserSubprocessPath: {Path.Combine(DriverDirectory, "CefRenderProcess.exe")}");
            Console.WriteLine($"  硬件加速: {_config.HardwareAcceleration}");
            
            _initialized = true;
        }

        public object CreateControl()
        {
            if (!_initialized)
                throw new InvalidOperationException("工厂未初始化。请先调用Initialize()方法。");

            Console.WriteLine("[CefFactory.Modern] 正在创建浏览器控件...");

            return new ModernBrowserControl(_config, DriverDirectory);
        }

        public void Shutdown()
        {
            Console.WriteLine("[CefFactory.Modern] 正在关闭...");
            _initialized = false;
        }
    }

    /// <summary>
    /// Modern驱动浏览器控件
    /// 生产环境中应包装AvaloniaCefBrowser
    /// </summary>
    public class ModernBrowserControl : IBrowserControl
    {
        private readonly BrowserConfig _config;
        private readonly string _driverDir;
        private string _url = "about:blank";
        private string _title = "Modern Browser";
        private bool _isLoading;

        public ModernBrowserControl(BrowserConfig config, string driverDir)
        {
            _config = config;
            _driverDir = driverDir;
            _url = config.InitialUrl;
            
            Console.WriteLine($"[ModernBrowserControl] 已创建, 驱动目录: {_driverDir}");
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
            Console.WriteLine($"[ModernBrowserControl] LoadUrl: {url}");
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
            Console.WriteLine("[ModernBrowserControl] 后退 - 未实现");
        }
        
        public void GoForward() 
        {
            Console.WriteLine("[ModernBrowserControl] 前进 - 未实现");
        }
        
        public void Reload() 
        {
            Console.WriteLine("[ModernBrowserControl] 重新加载");
            LoadUrl(_url);
        }
        
        public void Stop() 
        {
            Console.WriteLine("[ModernBrowserControl] 停止");
            _isLoading = false;
        }
        
        public void ExecuteJavaScript(string script)
        {
            Console.WriteLine($"[ModernBrowserControl] 执行脚本: {script}");
            // 实际实现中应调用: CefBrowserHost.ExecuteJavaScript(script, url, 0);
        }

        public void Dispose()
        {
            Console.WriteLine("[ModernBrowserControl] 已释放");
        }
    }
}
namespace MyBrowser.Driver.Modern
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using MyBrowser;
    using MyBrowser.Interop;

    /// <summary>
    /// 现代CEF驱动 - Windows 10+兼容性
    /// 物理隔离在Runtimes/Modern文件夹中
    /// </summary>
    public sealed class CefFactory : IBrowserFactory
    {
        private BrowserConfig _config;
        private bool _initialized;
        private CefBrowserFactory? _browserFactory;
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
            _config = config ?? new BrowserConfig();
            
            // 获取此驱动DLL所在的目录
            DriverDirectory = Path.GetDirectoryName(typeof(CefFactory).Assembly.Location) 
                ?? throw new InvalidOperationException("无法确定驱动目录");

            System.Diagnostics.Debug.WriteLine("[CefFactory.Modern] 正在初始化现代CEF...");
            System.Diagnostics.Debug.WriteLine($"[CefFactory.Modern] 驱动目录: {DriverDirectory}");
            System.Diagnostics.Debug.WriteLine($"[CefFactory.Modern] 无窗口模式: {_config.WindowlessRendering}");
            System.Diagnostics.Debug.WriteLine($"[CefFactory.Modern] 硬件加速: {_config.HardwareAcceleration}");
            System.Diagnostics.Debug.WriteLine($"[CefFactory.Modern] 缓存路径: {_config.CachePath}");
            System.Diagnostics.Debug.WriteLine($"[CefFactory.Modern] 初始URL: {_config.InitialUrl}");

            // 设置 DLL 搜索路径
            SetDllDirectory(DriverDirectory);

            // 初始化 CEF
            CefRuntime.Initialize(GetModuleHandle(null), multiThreadedMessageLoop: true);

            _browserFactory = new CefBrowserFactory();
            _initialized = true;

            System.Diagnostics.Debug.WriteLine("[CefFactory.Modern] CEF 初始化完成");
        }

        public object CreateControl()
        {
            if (!_initialized)
                throw new InvalidOperationException("工厂未初始化。请先调用Initialize()方法。");

            System.Diagnostics.Debug.WriteLine("[CefFactory.Modern] 正在创建浏览器控件...");

            // TODO: 返回真实的浏览器控件
            // 目前返回占位符
            return new ModernBrowserControl(_config, DriverDirectory);
        }

        /// <summary>
        /// 创建浏览器窗口
        /// </summary>
        public bool CreateBrowser(IntPtr parentHwnd, string initialUrl)
        {
            if (!_initialized || _browserFactory == null)
            {
                System.Diagnostics.Debug.WriteLine("[CefFactory.Modern] 工厂未初始化");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[CefFactory.Modern] 创建浏览器, URL: {initialUrl}");
            
            var success = _browserFactory.CreateBrowser(parentHwnd, initialUrl, out _browserHandle);
            
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"[CefFactory.Modern] 浏览器创建成功, Handle: {_browserHandle}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[CefFactory.Modern] 浏览器创建失败");
            }
            
            return success;
        }

        public void Shutdown()
        {
            System.Diagnostics.Debug.WriteLine("[CefFactory.Modern] 正在关闭...");
            
            _browserFactory?.Dispose();
            _browserFactory = null;
            
            CefRuntime.Shutdown();
            _initialized = false;
            
            System.Diagnostics.Debug.WriteLine("[CefFactory.Modern] 关闭完成");
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
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }

    /// <summary>
    /// Modern驱动浏览器控件
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
            
            System.Diagnostics.Debug.WriteLine($"[ModernBrowserControl] 已创建, 驱动目录: {_driverDir}");
        }

        public bool IsLoading => _isLoading;
        public string Url => _url;
        public string Title => _title;
        public bool CanGoBack => false;
        public bool CanGoForward => false;

        public event EventHandler? BrowserInitialized;
        public event EventHandler<LoadStartEventArgs>? LoadStart;
        public event EventHandler<LoadEndEventArgs>? LoadEnd;
        public event EventHandler<ConsoleMessageEventArgs>? ConsoleMessage;
        public event EventHandler<TitleChangedEventArgs>? TitleChanged;
        public event EventHandler<AddressChangedEventArgs>? AddressChanged;

        public void LoadUrl(string url)
        {
            System.Diagnostics.Debug.WriteLine($"[ModernBrowserControl] LoadUrl: {url}");
            _url = url;
            _isLoading = true;
            LoadStart?.Invoke(this, new LoadStartEventArgs { IsMainFrame = true });
            
            _isLoading = false;
            LoadEnd?.Invoke(this, new LoadEndEventArgs { IsMainFrame = true, HttpStatusCode = 200 });
            AddressChanged?.Invoke(this, new AddressChangedEventArgs { Address = url, IsMainFrame = true });
        }

        public void GoBack() 
        {
            System.Diagnostics.Debug.WriteLine("[ModernBrowserControl] 后退 - 未实现");
        }
        
        public void GoForward() 
        {
            System.Diagnostics.Debug.WriteLine("[ModernBrowserControl] 前进 - 未实现");
        }
        
        public void Reload() 
        {
            System.Diagnostics.Debug.WriteLine("[ModernBrowserControl] 重新加载");
            LoadUrl(_url);
        }
        
        public void Stop() 
        {
            System.Diagnostics.Debug.WriteLine("[ModernBrowserControl] 停止");
            _isLoading = false;
        }
        
        public void ExecuteJavaScript(string script)
        {
            System.Diagnostics.Debug.WriteLine($"[ModernBrowserControl] 执行脚本: {script}");
        }

        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("[ModernBrowserControl] 已释放");
        }
    }
}
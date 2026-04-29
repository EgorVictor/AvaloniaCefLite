namespace MyBrowser.Driver.Legacy
{
    using System;
    using System.IO;
    using MyBrowser;

    /// <summary>
    /// CEF 109 driver for Windows 7 compatibility.
    /// Physically isolated in Runtimes/Legacy folder.
    /// </summary>
    public sealed class CefFactory : IBrowserFactory
    {
        private BrowserConfig _config;
        private bool _initialized;
        
        /// <summary>
        /// Path to this driver directory (where libcef.dll resides).
        /// </summary>
        public static string DriverDirectory { get; private set; }

        public string Version => "109.1.36";
        public string DriverType => "Legacy";
        public bool IsInitialized => _initialized;

        public void Initialize(BrowserConfig config)
        {
            _config = config ?? new BrowserConfig();
            
            // Get the directory where this driver DLL resides
            DriverDirectory = Path.GetDirectoryName(typeof(CefFactory).Assembly.Location) 
                ?? throw new InvalidOperationException("Cannot determine driver directory");

            Console.WriteLine("[CefFactory.Legacy] Initializing CEF 109...");
            Console.WriteLine($"[CefFactory.Legacy] Driver Directory: {DriverDirectory}");
            Console.WriteLine($"[CefFactory.Legacy] Windowless: {_config.WindowlessRendering}");
            Console.WriteLine($"[CefFactory.Legacy] HardwareAcceleration: {_config.HardwareAcceleration}");
            Console.WriteLine($"[CefFactory.Legacy] CachePath: {_config.CachePath}");
            Console.WriteLine($"[CefFactory.Legacy] InitialUrl: {_config.InitialUrl}");

            // Configure CEF for Win7
            // In real implementation, this would configure:
            // - settings.NoSandbox = true
            // - settings.BrowserSubprocessPath = Path.Combine(DriverDirectory, "CefRenderProcess.exe")
            // - settings.CommandLineArgs["disable-gpu"] = ""
            // - settings.CommandLineArgs["disable-software-rasterizer"] = ""
            
            if (!_config.HardwareAcceleration)
            {
                Console.WriteLine("[CefFactory.Legacy] GPU disabled (Win7 mode)");
            }
            
            Console.WriteLine("[CefFactory.Legacy] CEF 109 settings:");
            Console.WriteLine($"  --no-sandbox");
            Console.WriteLine($"  --disable-gpu");
            Console.WriteLine($"  --disable-software-rasterizer");
            Console.WriteLine($"  BrowserSubprocessPath: {Path.Combine(DriverDirectory, "CefRenderProcess.exe")}");
            
            _initialized = true;
        }

        public object CreateControl()
        {
            if (!_initialized)
                throw new InvalidOperationException("Factory not initialized. Call Initialize() first.");

            Console.WriteLine("[CefFactory.Legacy] Creating browser control...");

            return new LegacyBrowserControl(_config, DriverDirectory);
        }

        public void Shutdown()
        {
            Console.WriteLine("[CefFactory.Legacy] Shutting down...");
            _initialized = false;
        }
    }

    /// <summary>
    /// Browser control for Legacy driver.
    /// In production, this wraps AvaloniaCefBrowser.
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
            
            Console.WriteLine($"[LegacyBrowserControl] Created with driver dir: {_driverDir}");
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
            
            // In real implementation, this would call:
            // CefBrowserHost.CreateBrowser(windowHandle, settings, url);
            
            _isLoading = false;
            LoadEnd?.Invoke(this, new LoadEndEventArgs { IsMainFrame = true, HttpStatusCode = 200 });
            AddressChanged?.Invoke(this, new AddressChangedEventArgs { Address = url, IsMainFrame = true });
        }

        public void GoBack() 
        {
            Console.WriteLine("[LegacyBrowserControl] GoBack - not implemented");
        }
        
        public void GoForward() 
        {
            Console.WriteLine("[LegacyBrowserControl] GoForward - not implemented");
        }
        
        public void Reload() 
        {
            Console.WriteLine("[LegacyBrowserControl] Reload");
            LoadUrl(_url);
        }
        
        public void Stop() 
        {
            Console.WriteLine("[LegacyBrowserControl] Stop");
            _isLoading = false;
        }
        
        public void ExecuteJavaScript(string script)
        {
            Console.WriteLine($"[LegacyBrowserControl] ExecuteScript: {script}");
            // In real implementation: CefBrowserHost.ExecuteJavaScript(script, url, 0);
        }

        public void Dispose()
        {
            Console.WriteLine("[LegacyBrowserControl] Disposed");
        }
    }
}
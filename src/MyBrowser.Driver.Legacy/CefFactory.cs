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

        public string Version => "109.1.36";
        public string DriverType => "Legacy";
        public bool IsInitialized => _initialized;

        public void Initialize(BrowserConfig config)
        {
            _config = config ?? new BrowserConfig();
            _initialized = true;

            Console.WriteLine("[CefFactory.Legacy] Initializing CEF 109...");
            Console.WriteLine($"[CefFactory.Legacy] Windowless: {_config.WindowlessRendering}");
            Console.WriteLine($"[CefFactory.Legacy] HardwareAcceleration: {_config.HardwareAcceleration}");
            Console.WriteLine($"[CefFactory.Legacy] CachePath: {_config.CachePath}");
            Console.WriteLine($"[CefFactory.Legacy] InitialUrl: {_config.InitialUrl}");

            // For Win7/8: Disable GPU and sandbox
            if (!_config.HardwareAcceleration)
            {
                Console.WriteLine("[CefFactory.Legacy] GPU disabled (Win7 mode)");
            }
        }

        public object CreateControl()
        {
            if (!_initialized)
                throw new InvalidOperationException("Factory not initialized. Call Initialize() first.");

            Console.WriteLine("[CefFactory.Legacy] Creating browser control...");

            // TODO: Return actual AvaloniaCefBrowser wrapped control
            // For now, return a placeholder
            return new LegacyBrowserControl(_config);
        }

        public void Shutdown()
        {
            Console.WriteLine("[CefFactory.Legacy] Shutting down...");
            _initialized = false;
        }
    }

    /// <summary>
    /// Placeholder browser control for Legacy driver.
    /// TODO: Replace with actual AvaloniaCefBrowser wrapper.
    /// </summary>
    public class LegacyBrowserControl : IBrowserControl
    {
        private readonly BrowserConfig _config;
        private string _url = "about:blank";
        private string _title = "Legacy Browser";
        private bool _isLoading;

        public LegacyBrowserControl(BrowserConfig config)
        {
            _config = config;
            _url = config.InitialUrl;
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
            // Simulate load completion
            _isLoading = false;
            LoadEnd?.Invoke(this, new LoadEndEventArgs { IsMainFrame = true, HttpStatusCode = 200 });
            AddressChanged?.Invoke(this, new AddressChangedEventArgs { Address = url, IsMainFrame = true });
        }

        public void GoBack() { }
        public void GoForward() { }
        public void Reload() { }
        public void Stop() { }
        public void ExecuteJavaScript(string script)
        {
            Console.WriteLine($"[LegacyBrowserControl] ExecuteScript: {script}");
        }

        public void Dispose()
        {
            Console.WriteLine("[LegacyBrowserControl] Disposed");
        }
    }
}
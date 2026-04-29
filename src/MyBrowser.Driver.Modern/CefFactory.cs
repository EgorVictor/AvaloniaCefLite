namespace MyBrowser.Driver.Modern
{
    using System;
    using MyBrowser;

    /// <summary>
    /// Modern CEF driver for Windows 10+.
    /// </summary>
    public sealed class CefFactory : IBrowserFactory
    {
        public string Version => "120.2.70";
        public string DriverType => "Modern";
        public bool IsInitialized { get; private set; }

        public void Initialize(BrowserConfig config)
        {
            IsInitialized = true;
            Console.WriteLine("[CefFactory.Modern] Initialized with modern CEF");
        }

        public object CreateControl()
        {
            // TODO: Return AvaloniaCefBrowser wrapped control
            throw new NotImplementedException("Modern driver not yet implemented");
        }

        public void Shutdown()
        {
            IsInitialized = false;
            Console.WriteLine("[CefFactory.Modern] Shutdown");
        }
    }
}
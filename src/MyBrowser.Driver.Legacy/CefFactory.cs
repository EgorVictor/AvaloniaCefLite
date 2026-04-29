namespace MyBrowser.Driver.Legacy
{
    using System;
    using MyBrowser;

    /// <summary>
    /// CEF 109 driver for Windows 7 compatibility.
    /// </summary>
    public sealed class CefFactory : IBrowserFactory
    {
        public string Version => "109.1.36";
        public string DriverType => "Legacy";
        public bool IsInitialized { get; private set; }

        public void Initialize(BrowserConfig config)
        {
            IsInitialized = true;
            Console.WriteLine("[CefFactory.Legacy] Initialized with CEF 109");
        }

        public object CreateControl()
        {
            // TODO: Return AvaloniaCefBrowser wrapped control
            throw new NotImplementedException("Legacy driver not yet implemented");
        }

        public void Shutdown()
        {
            IsInitialized = false;
            Console.WriteLine("[CefFactory.Legacy] Shutdown");
        }
    }
}
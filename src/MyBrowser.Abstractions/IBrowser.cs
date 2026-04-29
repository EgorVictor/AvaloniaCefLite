namespace MyBrowser
{
    using System;

    /// <summary>
    /// Browser initialization configuration.
    /// </summary>
    public class BrowserConfig
    {
        public string RuntimePath { get; set; }
        public string CachePath { get; set; }
        public bool WindowlessRendering { get; set; }
        public bool HardwareAcceleration { get; set; } = true;
        public string InitialUrl { get; set; } = "about:blank";
    }

    /// <summary>
    /// Event args for load start.
    /// </summary>
    public class LoadStartEventArgs : EventArgs
    {
        public bool IsMainFrame { get; set; }
    }

    /// <summary>
    /// Event args for load end.
    /// </summary>
    public class LoadEndEventArgs : EventArgs
    {
        public bool IsMainFrame { get; set; }
        public int HttpStatusCode { get; set; }
    }

    /// <summary>
    /// Event args for console messages.
    /// </summary>
    public class ConsoleMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string Source { get; set; }
        public int Line { get; set; }
    }

    /// <summary>
    /// Event args for title change.
    /// </summary>
    public class TitleChangedEventArgs : EventArgs
    {
        public string Title { get; set; }
    }

    /// <summary>
    /// Event args for address change.
    /// </summary>
    public class AddressChangedEventArgs : EventArgs
    {
        public string Address { get; set; }
        public bool IsMainFrame { get; set; }
    }

    /// <summary>
    /// Core browser factory interface.
    /// </summary>
    public interface IBrowserFactory
    {
        string Version { get; }
        string DriverType { get; }
        bool IsInitialized { get; }
        void Initialize(BrowserConfig config);
        object CreateControl();
        void Shutdown();
    }

    /// <summary>
    /// Core browser control interface.
    /// </summary>
    public interface IBrowserControl : IDisposable
    {
        bool IsLoading { get; }
        string Url { get; }
        string Title { get; }
        bool CanGoBack { get; }
        bool CanGoForward { get; }

        void LoadUrl(string url);
        void GoBack();
        void GoForward();
        void Reload();
        void Stop();
        void ExecuteJavaScript(string script);

        event EventHandler BrowserInitialized;
        event EventHandler<LoadStartEventArgs> LoadStart;
        event EventHandler<LoadEndEventArgs> LoadEnd;
        event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;
        event EventHandler<TitleChangedEventArgs> TitleChanged;
        event EventHandler<AddressChangedEventArgs> AddressChanged;
    }
}
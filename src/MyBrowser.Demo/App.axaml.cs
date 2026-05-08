namespace MyBrowser.Demo
{
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Controls;
    using System;
    using System.IO;

    /// <summary>
    /// Avalonia应用程序入口
    /// </summary>
    public partial class App : Application
    {
        private IBrowserFactory? _factory;

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var baseDir = AppContext.BaseDirectory;
                _factory = CefDispatcher.Boot(new BrowserConfig
                {
                    CompatibilityMode = CefCompatibilityMode.Win7Compatible,
                    Win7RenderMode = CefWin7RenderMode.SafeNoGpu,
                    InitialUrl = "about:blank",
                    BrowserSubprocessPath = Path.Combine(baseDir, "MyBrowser.Subprocess.exe"),
                    CachePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MyBrowser",
                        "Cef109",
                        "Cache")
                });

                desktop.Exit += (_, _) =>
                {
                    try { _factory?.Shutdown(); } catch { }
                    _factory = null;
                };

                var window = new MainWindow();
                window.SetFactory(_factory);

                desktop.MainWindow = window;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                window.Show();
            }
        }
    }
}

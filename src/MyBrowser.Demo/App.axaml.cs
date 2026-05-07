namespace MyBrowser.Demo
{
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Controls;
    using System;

    /// <summary>
    /// Avalonia应用程序入口
    /// </summary>
    public partial class App : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var factory = CefDispatcher.Boot(new BrowserConfig
                {
                    InitialUrl = "about:blank",
                    CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyBrowser", "Cef109", "Cache")
                });

                var window = new MainWindow();
                window.SetFactory(factory);

                desktop.MainWindow = window;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                window.Show();
            }
        }
    }
}

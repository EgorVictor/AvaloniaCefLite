namespace MyBrowser.Demo
{
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Controls;

    public partial class App : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Boot dispatcher FIRST
                var factory = CefDispatcher.Boot();
                factory.Initialize(new BrowserConfig
                {
                    InitialUrl = "https://www.google.com"
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
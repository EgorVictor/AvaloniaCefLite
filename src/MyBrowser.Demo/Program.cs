namespace MyBrowser.Demo
{
    using System;
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
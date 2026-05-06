namespace MyBrowser.Demo
{
    using System;
    using System.IO;
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 删除旧日志文件
            var logFile = @"F:\mybrowser.log";
            try
            {
                if (File.Exists(logFile))
                {
                    File.Delete(logFile);
                }
            }
            catch { }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
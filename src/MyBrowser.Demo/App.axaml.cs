namespace MyBrowser.Demo
{
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Controls;
    using System.Threading;

    /// <summary>
    /// Avalonia应用程序入口
    /// </summary>
    public partial class App : Application
    {
        private Timer? _cefMessagePump;

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 第一步：启动调度器（在任何UI之前）
                var factory = CefDispatcher.Boot();

                // 第二步：初始化配置
                factory.Initialize(new BrowserConfig
                {
                    InitialUrl = "https://www.google.com"
                });

                // 第三步：启动CEF消息循环泵
                _cefMessagePump = new Timer(_ =>
                {
                    MyBrowser.Interop.CefRuntime.DoMessageLoopWork();
                }, null, 10, 10);

                // 第四步：创建主窗口
                var window = new MainWindow();
                window.SetFactory(factory);

                desktop.MainWindow = window;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                window.Show();
            }
        }
    }
}
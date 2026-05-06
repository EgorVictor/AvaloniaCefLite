namespace MyBrowser.Demo
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Platform;
    using MyBrowser;
    using MyBrowser.Interop;
    using Serilog;

    /// <summary>
    /// 主窗口
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private IBrowserFactory _factory;
        private IBrowserControl _browser;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置浏览器工厂
        /// </summary>
        public void SetFactory(IBrowserFactory factory)
        {
            _factory = factory;
            CreateBrowserControl();
        }

        /// <summary>
        /// 创建浏览器控件
        /// </summary>
        private void CreateBrowserControl()
        {
            if (_factory == null) return;

            // 从工厂创建浏览器控件
            var control = _factory.CreateControl();
            _browser = control as IBrowserControl;

            if (_browser == null && control != null)
            {
                _log.Information($"[MainWindow] 已创建控件: {control.GetType().Name}");
            }

                // 订阅事件
            if (_browser != null)
            {
                _browser.TitleChanged += (s, e) => Title = e.Title ?? "MyBrowser";
                _browser.AddressChanged += (s, e) => UrlTextBox.Text = e.Address;
                _browser.LoadStart += (s, e) => _log.Information("[MainWindow] 开始加载");
                _browser.LoadEnd += (s, e) => _log.Information($"[MainWindow] 加载结束: {e.HttpStatusCode}");

                // 加载初始URL
                _browser.LoadUrl("https://www.baidu.com");
            }

            // 创建标签页
            var tab = new TabItem
            {
                Header = "新标签",
                Content = new BrowserView(_browser, this)
            };
            Tabs.Items.Add(tab);
            Tabs.SelectedItem = tab;
        }

        /// <summary>
        /// 创建新标签页
        /// </summary>
        private void CreateNewTab()
        {
            if (_factory == null) return;

            var browser = _factory.CreateControl() as IBrowserControl;
            if (browser == null) return;

            var tab = new TabItem
            {
                Header = "新标签",
                Content = new BrowserView(browser, this)
            };

            browser.TitleChanged += (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    tab.Header = e.Title ?? "标签";
                });
            };

            Tabs.Items.Add(tab);
            Tabs.SelectedItem = tab;

            browser.LoadUrl("about:blank");
        }

        private BrowserView ActiveBrowser => (Tabs.SelectedContent as TabItem)?.Content as BrowserView;

        private void OnNewTab(object sender, RoutedEventArgs e) => CreateNewTab();

        private void OnExit(object sender, RoutedEventArgs e) => Close();

        private void OnBack(object sender, RoutedEventArgs e) => ActiveBrowser?.GoBack();

        private void OnForward(object sender, RoutedEventArgs e) => ActiveBrowser?.GoForward();

        private void OnReload(object sender, RoutedEventArgs e) => ActiveBrowser?.Reload();

        private void OnGo(object sender, RoutedEventArgs e)
        {
            var url = UrlTextBox.Text;
            if (!string.IsNullOrWhiteSpace(url))
            {
                ActiveBrowser?.LoadUrl(url);
            }
        }
    }

    /// <summary>
    /// 浏览器视图 - 嵌入CEF浏览器到Avalonia窗口
    /// </summary>
    public class BrowserView : Panel
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private readonly IBrowserControl _browser;
        private IntPtr _containerHwnd;
        private bool _browserCreated;

        public BrowserView(IBrowserControl browser, Window parentWindow)
        {
            _browser = browser;
            
            if (_browser == null)
            {
                return;
            }

            // 获取浏览器控件的HWND并创建浏览器
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_browserCreated) return;

            var hwnd = GetNativeWindowHandle(this);
            if (hwnd != IntPtr.Zero)
            {
                _containerHwnd = hwnd;
                _log.Information($"[BrowserView] 获取到容器HWND: {hwnd}");

                // 设置窗口句柄并创建浏览器
                _browser.SetWindowHandle(hwnd);
                _browserCreated = true;
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            
            // 浏览器窗口大小调整由CEF自动处理
            // 如果需要手动调整，可以在这里实现
        }

        /// <summary>
        /// 获取Avalonia控件的原生HWND
        /// </summary>
        private IntPtr GetNativeWindowHandle(Control control)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(control);
                if (topLevel != null)
                {
                    var platformHandle = topLevel.TryGetPlatformHandle();
                    if (platformHandle != null)
                    {
                        _log.Information($"[BrowserView] PlatformHandle kind: {platformHandle.HandleDescriptor}");
                        return platformHandle.Handle;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Information($"[BrowserView] 获取HWND失败: {ex.Message}");
            }
            return IntPtr.Zero;
        }

        public void LoadUrl(string url) => _browser?.LoadUrl(url);
        public void GoBack() => _browser?.GoBack();
        public void GoForward() => _browser?.GoForward();
        public void Reload() => _browser?.Reload();
        public void Stop() => _browser?.Stop();
    }
}
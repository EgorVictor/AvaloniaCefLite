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

    /// <summary>
    /// 主窗口
    /// </summary>
    public partial class MainWindow : Window
    {
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
                Logger.Log($"[MainWindow] 已创建控件: {control.GetType().Name}");
            }

                // 订阅事件
            if (_browser != null)
            {
                _browser.TitleChanged += (s, e) => Title = e.Title ?? "MyBrowser";
                _browser.AddressChanged += (s, e) => UrlTextBox.Text = e.Address;
                _browser.LoadStart += (s, e) => Logger.Log("[MainWindow] 开始加载");
                _browser.LoadEnd += (s, e) => Logger.Log($"[MainWindow] 加载结束: {e.HttpStatusCode}");

                // 加载初始URL
                _browser.LoadUrl("https://www.google.com");
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
    /// 浏览器视图包装器
    /// </summary>
    public class BrowserView : StackPanel
    {
        private readonly IBrowserControl? _browser;
        private readonly TextBlock _placeholder;
        private readonly Window _parentWindow;

        public BrowserView(IBrowserControl? browser, Window parentWindow)
        {
            _browser = browser;
            _parentWindow = parentWindow;

            if (_browser == null)
            {
                _placeholder = new TextBlock
                {
                    Text = "无浏览器控件",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Children.Add(_placeholder);
            }
            else
            {
                // TODO: 在此处添加实际的浏览器控件
                // 目前显示状态信息
                _placeholder = new TextBlock
                {
                    Text = $"浏览器: {_browser.GetType().Name}",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Children.Add(_placeholder);

                // 获取原生窗口句柄并传递给浏览器控件
                // 这应该在布局完成后再调用
                Loaded += OnLoaded;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 获取原生 HWND 并传递给浏览器控件
            if (_browser != null && _parentWindow != null)
            {
                var hwnd = GetNativeWindowHandle(_parentWindow);
                if (hwnd != IntPtr.Zero)
                {
                    Logger.Log($"[BrowserView] 获取到原生HWND: {hwnd}");

                    // 如果浏览器控件有 SetWindowHandle 方法，则调用它
                    var setHandleMethod = _browser.GetType().GetMethod("SetWindowHandle");
                    setHandleMethod?.Invoke(_browser, new object[] { hwnd });
                }
            }
        }

        /// <summary>
        /// 获取 Avalonia 窗口的原生 HWND
        /// </summary>
        private IntPtr GetNativeWindowHandle(Window window)
        {
            try
            {
                // 使用 Avalonia 的平台特定接口获取原生句柄
                var platformHandle = window.TryGetPlatformHandle();
                if (platformHandle != null)
                {
                    Logger.Log($"[BrowserView] PlatformHandle kind: {platformHandle.HandleDescriptor}");
                    return platformHandle.Handle;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[BrowserView] 获取HWND失败: {ex.Message}");
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
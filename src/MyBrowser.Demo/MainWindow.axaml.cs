namespace MyBrowser.Demo
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Layout;
    using Avalonia.VisualTree;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
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
            .WriteTo.File(LogHelper.GetLogPath(), shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private IBrowserFactory _factory;
        private IBrowserControl _browser;
        private int _tabCounter;
        private Dictionary<TabItem, BrowserView> _tabBrowserMap = new();

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

                // 订阅事件 - 所有回调通过 Dispatcher 切回 UI 线程
            if (_browser != null)
            {
                _browser.TitleChanged += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => Title = e.Title ?? "MyBrowser");
                _browser.AddressChanged += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => UrlTextBox.Text = e.Address);
                _browser.LoadStart += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => _log.Information("[MainWindow] 开始加载"));
                _browser.LoadEnd += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => _log.Information($"[MainWindow] 加载结束: {e.HttpStatusCode}"));
                _browser.PopupRequested += (s, url) => Avalonia.Threading.Dispatcher.UIThread.Post(() => CreateTabWithUrl(url));

                // 加载初始URL
                _browser.LoadUrl("https://www.baidu.com");
            }

            // 创建标签页
            _tabCounter++;
            var tab = new TabItem
            {
                Header = $"标签{_tabCounter}",
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
            CreateTabWithUrl("about:blank");
        }

        /// <summary>
        /// 创建新标签页并加载指定URL
        /// </summary>
        private void CreateTabWithUrl(string url)
        {
            if (_factory == null) return;

            var browser = _factory.CreateControl() as IBrowserControl;
            if (browser == null) return;

            // 订阅弹窗事件 - 新标签页的弹窗也创建新标签
            browser.PopupRequested += (s, popupUrl) => Avalonia.Threading.Dispatcher.UIThread.Post(() => CreateTabWithUrl(popupUrl));

            _tabCounter++;
            var tabNumber = _tabCounter;
            var tab = new TabItem
            {
                Header = $"标签{tabNumber}",
                Content = new BrowserView(browser, this)
            };

            var browserView = tab.Content as BrowserView;
            if (browserView != null)
            {
                _tabBrowserMap[tab] = browserView;
            }

            // 添加关闭按钮到 Header
            var headerPanel = CreateTabHeader($"标签{tabNumber}", tab);
            tab.Header = headerPanel;

            browser.TitleChanged += (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var title = string.IsNullOrWhiteSpace(e.Title) ? $"标签{tabNumber}" : $"{e.Title} - 标签{tabNumber}";
                    UpdateTabHeader(headerPanel, title);
                });
            };

            Tabs.Items.Add(tab);
            Tabs.SelectedItem = tab;

            browser.LoadUrl(url);
        }

        private StackPanel CreateTabHeader(string title, TabItem tab)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var textBlock = new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            panel.Children.Add(textBlock);

            var closeButton = new Button
            {
                Content = "×",
                Padding = new Thickness(4, 0, 4, 0),
                Margin = new Thickness(0, 0, 4, 0),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeButton.Click += (s, e) => CloseTab(tab);
            panel.Children.Add(closeButton);

            panel.Tag = tab;
            return panel;
        }

        private void UpdateTabHeader(StackPanel panel, string title)
        {
            if (panel?.Children[0] is TextBlock textBlock)
            {
                textBlock.Text = title;
            }
        }

        private void CloseTab(TabItem tab)
        {
            if (_tabBrowserMap.TryGetValue(tab, out var browserView))
            {
                browserView.Cleanup();
                _tabBrowserMap.Remove(tab);
            }

            Tabs.Items.Remove(tab);
        }

        private BrowserView ActiveBrowser => Tabs.SelectedContent as BrowserView;

        private void OnNewTab(object sender, RoutedEventArgs e) => CreateNewTab();

        private void OnExit(object sender, RoutedEventArgs e)
        {
            foreach (var kvp in _tabBrowserMap)
            {
                kvp.Value.Cleanup();
            }
            _tabBrowserMap.Clear();
            Close();
        }

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
            .WriteTo.File(LogHelper.GetLogPath(), shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private readonly IBrowserControl _browser;
        private readonly Window _parentWindow;
        private IntPtr _containerHwnd;
        private bool _browserCreated;
        private bool _disposed;
        private Rect _lastBounds;

        public BrowserView(IBrowserControl browser, Window parentWindow)
        {
            _browser = browser;
            _parentWindow = parentWindow;

            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            if (_browser == null)
            {
                return;
            }

            _browser.BrowserInitialized += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateNativeBounds());

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnsureContainerWindow();
            TryCreateBrowser();
            UpdateNativeBounds();
        }

        public void Cleanup()
        {
            if (_disposed) return;
            _disposed = true;

            if (_browser != null)
            {
                _browser.Dispose();
            }

            if (_containerHwnd != IntPtr.Zero)
            {
                DestroyWindow(_containerHwnd);
                _containerHwnd = IntPtr.Zero;
            }

            _browserCreated = false;
        }

        private void EnsureContainerWindow()
        {
            if (_containerHwnd != IntPtr.Zero)
            {
                return;
            }

            var platformHandle = _parentWindow.TryGetPlatformHandle();
            var parentHwnd = platformHandle?.Handle ?? IntPtr.Zero;
            if (parentHwnd == IntPtr.Zero)
            {
                _log.Information("[BrowserView] 顶层窗口 HWND 还不可用");
                return;
            }

            _containerHwnd = CreateWindowEx(
                0,
                "STATIC",
                string.Empty,
                WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
                0,
                0,
                Math.Max(1, (int)Bounds.Width),
                Math.Max(1, (int)Bounds.Height),
                parentHwnd,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            _log.Information("[BrowserView] 创建原生容器 HWND: {ContainerHwnd}, Parent: {ParentHwnd}", _containerHwnd, parentHwnd);
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateNativeBounds();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var width = double.IsInfinity(availableSize.Width) ? 800 : availableSize.Width;
            var height = double.IsInfinity(availableSize.Height) ? 600 : availableSize.Height;
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateNativeBounds();
            return finalSize;
        }

        private void UpdateNativeBounds()
        {
            if (_containerHwnd == IntPtr.Zero)
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            var origin = topLevel == null ? null : this.TranslatePoint(new Point(0, 0), topLevel);
            if (topLevel == null || origin == null)
            {
                return;
            }

            var scale = topLevel.RenderScaling;
            var x = (int)Math.Round(origin.Value.X * scale);
            var y = (int)Math.Round(origin.Value.Y * scale);
            var width = Math.Max(1, (int)Math.Round(Bounds.Width * scale));
            var height = Math.Max(1, (int)Math.Round(Bounds.Height * scale));

            var newBounds = new Rect(x, y, width, height);
            if (newBounds.Equals(_lastBounds))
            {
                return;
            }
            _lastBounds = newBounds;

            _log.Information("[BrowserView] UpdateNativeBounds: pos={X},{Y} size={W}x{H} scale={Scale} bounds={BoundsW}x{BoundsH}",
                x, y, width, height, scale, Bounds.Width, Bounds.Height);

            SetWindowPos(
                _containerHwnd,
                IntPtr.Zero,
                x,
                y,
                width,
                height,
                SWP_NOZORDER | SWP_NOACTIVATE);

            // 通知浏览器大小已改变（仅在浏览器已创建时）
            if (_browser != null)
            {
                (_browser as IBrowserControl)?.NotifyResized();
            }
        }

        private void TryCreateBrowser()
        {
            if (_browserCreated || _browser == null || _containerHwnd == IntPtr.Zero)
            {
                return;
            }

            _log.Information("[BrowserView] 设置浏览器父 HWND: {ContainerHwnd}", _containerHwnd);
            _browser.SetWindowHandle(_containerHwnd);
            _browserCreated = true;
        }

        public void LoadUrl(string url) => _browser?.LoadUrl(url);
        public void GoBack() => _browser?.GoBack();
        public void GoForward() => _browser?.GoForward();
        public void Reload() => _browser?.Reload();
        public void Stop() => _browser?.Stop();

        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);
    }
}

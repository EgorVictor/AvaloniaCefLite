namespace MyBrowser.Demo
{
    using System;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Layout;
    using Avalonia.Threading;
    using Avalonia.VisualTree;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using MyBrowser;
    using MyBrowser.Interop;
    using Serilog;

    public partial class MainWindow : Window
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(LogHelper.GetLogPath("mybrowser-main.log"), shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private IBrowserFactory _factory;
        private IBrowserControl _browser;
        private int _tabCounter;
        private Dictionary<TabItem, CefNativeHost> _tabHostMap = new();

        public MainWindow()
        {
            InitializeComponent();
            Closing += OnClosing;
            Tabs.SelectionChanged += OnTabSelectionChanged;
        }

        public void SetFactory(IBrowserFactory factory)
        {
            _factory = factory;
            CreateBrowserControl();
        }

        private void CreateBrowserControl()
        {
            if (_factory == null) return;

            var control = _factory.CreateControl();
            _browser = control as IBrowserControl;

            if (_browser == null && control != null)
            {
                _log.Information($"[MainWindow] 已创建控件: {control.GetType().Name}");
            }

            if (_browser != null)
            {
                _browser.TitleChanged += (s, e) => Dispatcher.UIThread.Post(() => Title = e.Title ?? "MyBrowser");
                _browser.AddressChanged += (s, e) => Dispatcher.UIThread.Post(() => UrlTextBox.Text = e.Address);
                _browser.LoadStart += (s, e) => Dispatcher.UIThread.Post(() => _log.Information("[MainWindow] 开始加载"));
                _browser.LoadEnd += (s, e) => Dispatcher.UIThread.Post(() => _log.Information($"[MainWindow] 加载结束: {e.HttpStatusCode}"));
                _browser.PopupRequested += (s, url) => Dispatcher.UIThread.Post(() => CreateTabWithUrl(url));

                _browser.LoadUrl("about:blank");
            }

            _tabCounter++;
            var tab = new TabItem
            {
                Header = $"标签{_tabCounter}",
            };

            var host = CreateHostForBrowser(_browser);
            tab.Content = host;
            _tabHostMap[tab] = host;

            Tabs.Items.Add(tab);
            Tabs.SelectedItem = tab;
        }

        private CefNativeHost CreateHostForBrowser(IBrowserControl browser)
        {
            var host = new CefNativeHost();
            host.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            host.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            if (browser != null)
            {
                browser.BrowserInitialized += (s, e) => Dispatcher.UIThread.Post(() =>
                {
                    host.ForceRefresh();
                });
            }

            host.AttachBrowser(browser);
            return host;
        }

        private void CreateNewTab()
        {
            CreateTabWithUrl("about:blank");
        }

        private void CreateTabWithUrl(string url)
        {
            if (_factory == null) return;

            var browser = _factory.CreateControl() as IBrowserControl;
            if (browser == null) return;

            browser.PopupRequested += (s, popupUrl) => Dispatcher.UIThread.Post(() => CreateTabWithUrl(popupUrl));

            _tabCounter++;
            var tabNumber = _tabCounter;
            var tab = new TabItem
            {
                Header = $"标签{tabNumber}",
            };

            var host = CreateHostForBrowser(browser);
            tab.Content = host;
            _tabHostMap[tab] = host;

            var headerPanel = CreateTabHeader($"标签{tabNumber}", tab);
            tab.Header = headerPanel;

            browser.TitleChanged += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
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
            if (_tabHostMap.TryGetValue(tab, out var host))
            {
                host.Browser?.Dispose();
                _tabHostMap.Remove(tab);
            }

            Tabs.Items.Remove(tab);
        }

        private CefNativeHost ActiveHost => Tabs.SelectedContent as CefNativeHost;

        private void OnClosing(object sender, WindowClosingEventArgs e)
        {
            _log.Information("[MainWindow] Window closing...");
            foreach (var kvp in _tabHostMap)
            {
                kvp.Value.Browser?.Dispose();
            }
            _tabHostMap.Clear();
            _log.Information("[MainWindow] All browsers disposed, calling CefDispatcher.Shutdown...");
            CefDispatcher.Shutdown();
            _log.Information("[MainWindow] Shutdown complete");
        }

        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var host = ActiveHost;
            if (host?.Browser != null)
            {
                Dispatcher.UIThread.Post(() => host.Browser.SetFocus(), DispatcherPriority.Input);
            }
        }

        private void OnNewTab(object sender, RoutedEventArgs e) => CreateNewTab();

        private void OnExit(object sender, RoutedEventArgs e)
        {
            foreach (var kvp in _tabHostMap)
            {
                kvp.Value.Browser?.Dispose();
            }
            _tabHostMap.Clear();
            Close();
        }

        private void OnBack(object sender, RoutedEventArgs e) => ActiveHost?.GoBack();

        private void OnForward(object sender, RoutedEventArgs e) => ActiveHost?.GoForward();

        private void OnReload(object sender, RoutedEventArgs e) => ActiveHost?.Reload();

        private void OnGo(object sender, RoutedEventArgs e)
        {
            var url = UrlTextBox.Text;
            if (!string.IsNullOrWhiteSpace(url))
            {
                ActiveHost?.LoadUrl(url);
            }
        }
    }
}

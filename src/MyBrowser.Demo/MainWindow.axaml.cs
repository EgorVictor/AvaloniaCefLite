namespace MyBrowser.Demo
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using MyBrowser;

    public partial class MainWindow : Window
    {
        private IBrowserFactory _factory;
        private IBrowserControl _browser;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetFactory(IBrowserFactory factory)
        {
            _factory = factory;
            CreateBrowserControl();
        }

        private void CreateBrowserControl()
        {
            if (_factory == null) return;

            // Create browser control from factory
            var control = _factory.CreateControl();
            _browser = control as IBrowserControl;

            if (_browser == null && control != null)
            {
                // If control is not IBrowserControl, wrap it
                Console.WriteLine($"[MainWindow] Created control: {control.GetType().Name}");
            }

            // Subscribe to events
            if (_browser != null)
            {
                _browser.TitleChanged += (s, e) => Title = e.Title ?? "MyBrowser";
                _browser.AddressChanged += (s, e) => UrlTextBox.Text = e.Address;
                _browser.LoadStart += (s, e) => Console.WriteLine("[MainWindow] Load started");
                _browser.LoadEnd += (s, e) => Console.WriteLine($"[MainWindow] Load ended: {e.HttpStatusCode}");

                // Load initial URL
                _browser.LoadUrl("https://www.google.com");
            }

            // Create tab with browser
            var tab = new TabItem
            {
                Header = "New Tab",
                Content = new BrowserView(_browser)
            };
            Tabs.Items.Add(tab);
            Tabs.SelectedItem = tab;
        }

        private void CreateNewTab()
        {
            if (_factory == null) return;

            var browser = _factory.CreateControl() as IBrowserControl;
            if (browser == null) return;

            var tab = new TabItem
            {
                Header = "New Tab",
                Content = new BrowserView(browser)
            };

            browser.TitleChanged += (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    tab.Header = e.Title ?? "Tab";
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
    /// Browser view wrapper for Avalonia.
    /// </summary>
    public class BrowserView : StackPanel
    {
        private readonly IBrowserControl? _browser;
        private readonly TextBlock _placeholder;

        public BrowserView(IBrowserControl? browser)
        {
            _browser = browser;

            if (_browser == null)
            {
                _placeholder = new TextBlock
                {
                    Text = "No browser control",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Children.Add(_placeholder);
            }
            else
            {
                // TODO: Add actual browser control here
                // For now, show status
                _placeholder = new TextBlock
                {
                    Text = $"Browser: {_browser.GetType().Name}",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Children.Add(_placeholder);
            }
        }

        public void LoadUrl(string url) => _browser?.LoadUrl(url);
        public void GoBack() => _browser?.GoBack();
        public void GoForward() => _browser?.GoForward();
        public void Reload() => _browser?.Reload();
        public void Stop() => _browser?.Stop();
    }
}
namespace MyBrowser.Demo
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using MyBrowser;

    public partial class MainWindow : Window
    {
        private IBrowserFactory _factory;

        public MainWindow()
        {
            InitializeComponent();
            CreateTab();
        }

        public void SetFactory(IBrowserFactory factory)
        {
            _factory = factory;
        }

        private void CreateTab()
        {
            var tab = new TabItem
            {
                Header = "New Tab",
                Content = new BrowserView()
            };
            Tabs.Items.Add(tab);
            Tabs.SelectedItem = tab;
        }

        private BrowserView ActiveBrowser => (Tabs.SelectedContent as TabItem)?.Content as BrowserView;

        private void OnNewTab(object sender, RoutedEventArgs e) => CreateTab();
        
        private void OnExit(object sender, RoutedEventArgs e) => Close();
        
        private void OnBack(object sender, RoutedEventArgs e) => ActiveBrowser?.GoBack();
        
        private void OnForward(object sender, RoutedEventArgs e) => ActiveBrowser?.GoForward();
        
        private void OnReload(object sender, RoutedEventArgs e) => ActiveBrowser?.Reload();

        private void OnGo(object sender, RoutedEventArgs e)
        {
            ActiveBrowser?.LoadUrl(UrlTextBox.Text);
        }
    }

    /// <summary>
    /// Browser view wrapper - placeholder until drivers are implemented.
    /// </summary>
    public class BrowserView : StackPanel
    {
        private readonly TextBlock _placeholder;

        public BrowserView()
        {
            _placeholder = new TextBlock
            {
                Text = "Browser view - drivers not yet implemented",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Children.Add(_placeholder);
        }

        public void LoadUrl(string url) { }
        public void GoBack() { }
        public void GoForward() { }
        public void Reload() { }
    }
}
namespace MyBrowser
{
    using System;
    using System.IO;

    /// <summary>
    /// 日志路径辅助类
    /// </summary>
    public static class LogHelper
    {
        public static string GetLogPath(string fileName = "mybrowser.log")
        {
            return Path.Combine(AppContext.BaseDirectory, fileName);
        }
    }

    /// <summary>
    /// 浏览器初始化配置
    /// </summary>
    public class BrowserConfig
    {
        /// <summary>
        /// 运行时路径
        /// </summary>
        public string RuntimePath { get; set; }
        
        /// <summary>
        /// 缓存路径
        /// </summary>
        public string CachePath { get; set; }
        
        /// <summary>
        /// 无窗口渲染模式
        /// </summary>
        public bool WindowlessRendering { get; set; }
        
        /// <summary>
        /// 硬件加速（默认启用）
        /// </summary>
        public bool HardwareAcceleration { get; set; } = true;
        
        /// <summary>
        /// 初始URL（默认about:blank）
        /// </summary>
        public string InitialUrl { get; set; } = "about:blank";
    }

    /// <summary>
    /// 加载开始事件参数
    /// </summary>
    public class LoadStartEventArgs : EventArgs
    {
        /// <summary>
        /// 是否为主框架
        /// </summary>
        public bool IsMainFrame { get; set; }
    }

    /// <summary>
    /// 加载结束事件参数
    /// </summary>
    public class LoadEndEventArgs : EventArgs
    {
        /// <summary>
        /// 是否为主框架
        /// </summary>
        public bool IsMainFrame { get; set; }
        
        /// <summary>
        /// HTTP状态码
        /// </summary>
        public int HttpStatusCode { get; set; }
    }

    /// <summary>
    /// 控制台消息事件参数
    /// </summary>
    public class ConsoleMessageEventArgs : EventArgs
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 来源
        /// </summary>
        public string Source { get; set; }
        
        /// <summary>
        /// 行号
        /// </summary>
        public int Line { get; set; }
    }

    /// <summary>
    /// 标题变更事件参数
    /// </summary>
    public class TitleChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 新标题
        /// </summary>
        public string Title { get; set; }
    }

    /// <summary>
    /// 地址变更事件参数
    /// </summary>
    public class AddressChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 新地址
        /// </summary>
        public string Address { get; set; }
        
        /// <summary>
        /// 是否为主框架
        /// </summary>
        public bool IsMainFrame { get; set; }
    }

    /// <summary>
    /// 浏览器工厂接口 - 创建浏览器控件的工厂
    /// </summary>
    public interface IBrowserFactory
    {
        /// <summary>
        /// 驱动版本
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// 驱动类型（Legacy或Modern）
        /// </summary>
        string DriverType { get; }
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// 初始化工厂
        /// </summary>
        void Initialize(BrowserConfig config);
        
        /// <summary>
        /// 创建浏览器控件
        /// </summary>
        object CreateControl();
        
        /// <summary>
        /// 关闭工厂并释放资源
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// 浏览器控件接口
    /// </summary>
    public interface IBrowserControl : IDisposable
    {
        /// <summary>
        /// 是否正在加载
        /// </summary>
        bool IsLoading { get; }
        
        /// <summary>
        /// 当前URL
        /// </summary>
        string Url { get; }
        
        /// <summary>
        /// 当前标题
        /// </summary>
        string Title { get; }
        
        /// <summary>
        /// 是否可以后退
        /// </summary>
        bool CanGoBack { get; }
        
        /// <summary>
        /// 是否可以前进
        /// </summary>
        bool CanGoForward { get; }

        /// <summary>
        /// 设置原生窗口句柄
        /// </summary>
        void SetWindowHandle(IntPtr hwnd);

        /// <summary>
        /// 加载URL
        /// </summary>
        void LoadUrl(string url);
        
        /// <summary>
        /// 后退
        /// </summary>
        void GoBack();
        
        /// <summary>
        /// 前进
        /// </summary>
        void GoForward();
        
        /// <summary>
        /// 重新加载
        /// </summary>
        void Reload();
        
        /// <summary>
        /// 停止加载
        /// </summary>
        void Stop();

        /// <summary>
        /// 通知浏览器窗口大小已改变
        /// </summary>
        void NotifyResized();

        /// <summary>
        /// 执行JavaScript
        /// </summary>
        void ExecuteJavaScript(string script);

        /// <summary>
        /// 浏览器初始化完成事件
        /// </summary>
        event EventHandler BrowserInitialized;
        
        /// <summary>
        /// 开始加载事件
        /// </summary>
        event EventHandler<LoadStartEventArgs> LoadStart;
        
        /// <summary>
        /// 结束加载事件
        /// </summary>
        event EventHandler<LoadEndEventArgs> LoadEnd;
        
        /// <summary>
        /// 控制台消息事件
        /// </summary>
        event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;
        
        /// <summary>
        /// 标题变更事件
        /// </summary>
        event EventHandler<TitleChangedEventArgs> TitleChanged;
        
        /// <summary>
        /// 地址变更事件
        /// </summary>
        event EventHandler<AddressChangedEventArgs> AddressChanged;

        /// <summary>
        /// 弹窗请求事件（页面请求打开新窗口）
        /// </summary>
        event EventHandler<string> PopupRequested;
    }
}
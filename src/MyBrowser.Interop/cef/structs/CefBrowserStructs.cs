namespace MyBrowser.Interop.cef.structs
{
    using System;
    using System.Runtime.InteropServices;

    // CefBrowserSettings 移到了 MyBrowser.Interop.Internal.CefBrowserSettingsNative

    /// <summary>
    /// CEF 浏览器设置
    /// </summary>
    public struct CefBrowserSettings
    {
        public int Size;
        public int WindowlessFrameRate;
        public int BackgroundColor;
        public int AcceptLangList;
        public int RemotFontList;
        public IntPtr UserAgent;
        public IntPtr UserAgentProduct;
        public IntPtr Locale;
        public IntPtr PrefetchLangCountryList;
        public IntPtr CookieableTypesExcludeValidation;
        public int PersistSessionCookies;
        public int PersistUserPreferences;
        public int JavaScriptEnabled;
        public int JavaScriptCloseWindows;
        public int JavaScriptAccessClipboard;
        public int JavaScriptDOMPaste;
        public int AllowUniversalAccessFromFileUrls;
        public int AllowFileAccessFromFileUrls;
        public int AllowInsecureLocalhost;
        public int EnableWebSecurity;
        public int EnableXSSAuditor;
        public int EnableSameSiteCookie;
        public int ImageLoading;
        public int ImageShrinkStandaloneToFavicon;
        public int TextAreaResize;
        public int TabToLinks;
        public int NewWindowsFromAlerts;
        public int AlwaysAuthorizePdst;
        public int AlwaysOpenIncognitoMode;
        public int DownloadDisabled;
        public int ContextIsolation;
        public int ApplyWebSecurityEnabled;
        public int LoadDropsEnabled;
        public int SpellcheckEnabled;
        public int IsolateCorruptedNetworkStack;
        public IntPtr FileStoragePath;
        public int CacheDisabled;
        public int StandardFontFamily;
        public IntPtr FixedPositionCreatesStackingContext;
        public IntPtr TransparentPaintingEnabled;
        public int IncognitoEnabled;
        public int FileURLFileAccessFromFileURLsAllowed;
        public int FileURLStrictSecurityEnabled;
        public int FileURLAlwaysAllowFileAccess;
        public int ServiceWorkerBotEnabled;
        public int ServiceWorkerSkipCORSActually;
        public int ServiceWorkerAllowed;
        public int FullscreenEnabled;
        public IntPtr GlobalError;
        public int ValidateStaleScriptEnabled;
        public IntPtr ScriptTimeoutMs;
    }

    /// <summary>
    /// CEF 窗口信息（用于 CreateBrowser）
    /// </summary>
    public struct CefWindowInfo
    {
        public IntPtr ParentHandle;        // HWND
        public IntPtr WindowHandle;       // 窗口句柄
        public int X;                      // X 坐标
        public int Y;                      // Y 坐标
        public int Width;                  // 宽度
        public int Height;                 // 高度
        public int Style;                 // 窗口样式
        public int ExStyle;               // 扩展窗口样式
        public IntPtr MenuHandle;         // 菜单句柄
        public int Transparency;          // 透明度
        public int RectsProvided;         // 是否提供矩形区域
    }

    /// <summary>
    /// CEF 指向屏幕信息
    /// </summary>
    public struct CefScreenInfo
    {
        public int Depth;
        public int DepthPerComponent;
        public int IsMonochrome;
        public IntPtr Rect;
        public IntPtr AvailableRect;
    }

    /// <summary>
    /// CEF 指针设置
    /// </summary>
    public struct CefPointerSettings
    {
        public int TouchEventTimeoutMs;
        public int HoverEventTimeoutMs;
        public int HasTouchInput;
        public int GestureScrollHorizontalEnabled;
    }
}
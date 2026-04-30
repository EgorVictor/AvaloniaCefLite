namespace MyBrowser.Interop.cef.enums
{
    /// <summary>
    /// CEF 错误代码
    /// </summary>
    public enum CefErrorCode
    {
        None = 0,
        Failed = -2,
        Aborted = -3,
        InvalidArgument = -4,
        InvalidHandle = -5,
        FileNotFound = -6,
        TimedOut = -7,
        FileTooBig = -8,
        Unexpected = -9,
        AccessDenied = -10,
        NotImplemented = -11,
        ConnectionClosed = -100,
        ConnectionReset = -101,
        ConnectionRefused = -102,
        ConnectionAborted = -103,
        ConnectionResetByPeer = -104,
        ConnectionClosedOnRedirect = -105,
        ConnectionClosedOnError = -106,
        LostConnection = -107,
        ConnectionClosedGracefully = -108,
        InvalidResponse = -200,
        InvalidURL = -201,
        Disconnected = -202,
        TooManyRedirects = -203,
        InvalidRequest = -204,
        InvalidResponseHeader = -205,
        InvalidCertificate = -206,
        CertificateCommonNameInvalid = -207,
        CertificateExpired = -208,
        CertificateRevoked = -209,
        CertificateInvalid = -210,
        RequestProvisionalResponse = -300,
        RequestNoResponse = -301,
        RequestRetryWithNewAuthInfo = -302,
        CookieEmpty = -400,
        CookieTooLarge = -401,
        CookieBlocked = -402,
        CookieStateViolation = -403,
        CookieBlockedByPreferences = -404,
        CookieFailure = -405,
    }

    /// <summary>
    /// CEF 加载状态
    /// </summary>
    public enum CefLoadStatus
    {
        None = 0,
        Loading = 1,
        Loaded = 2,
        Error = 3,
    }

    /// <summary>
    /// CEF 转换状态
    /// </summary>
    public enum CefTransitionType
    {
        LinkClicked = 0,
        Typed = 1,
        AutoSubframe = 2,
        ManualSubframe = 3,
        FormSubmitted = 4,
        Reloaded = 5,
        FormResubmitted = 6,
        NavigationHandled = 7,
        Download = 8,
        Redirect = 9,
        ManualRequest = 10,
        ManualConcurrentRequest = 11,
    }

    /// <summary>
    /// CEF URL 请求状态
    /// </summary>
    public enum CefURLRequestStatus
    {
        Unknown = 0,
        Success = 1,
        Pending = 2,
        Canceled = 3,
        Failed = 4,
    }

    /// <summary>
    /// CEF 日志级别
    /// </summary>
    public enum CefLogSeverity
    {
        Default = 0,
        Verbose = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Disable = 5,
        Enable = 6,
        Always = 7,
    }

    /// <summary>
    /// CEF 颜色配置文件
    /// </summary>
    public enum CefColorProfile
    {
        None = 0,
        Custom = 1,
        SRGB = 2,
        DisplayP3 = 3,
    }

    /// <summary>
    /// CEF Cookie 优先级
    /// </summary>
    public enum CefCookiePriority
    {
        Default = 0,
        Low = 1,
        Medium = 2,
        High = 3,
    }

    /// <summary>
    /// CEF 资源类型
    /// </summary>
    public enum CefResourceType
    {
        MainFrame = 0,
        Subframe = 1,
        Stylesheet = 2,
        Script = 3,
        Image = 4,
        Font = 5,
        Subresource = 6,
        XHR = 7,
        Ping = 8,
        CSPViolationReport = 9,
        ServiceWorkerManifest = 10,
        ServiceWorker = 11,
        SharedWorker = 12,
        DedicatedWorker = 13,
    }

    /// <summary>
    /// CEF 导航类型
    /// </summary>
    public enum CefNavigationType
    {
        LinkClicked = 0,
        FormSubmitted = 1,
        FormResubmitted = 2,
        Initial = 3,
        ManualSubframe = 4,
        AutoSubframe = 5,
        Reloaded = 6,
        Typed = 7,
    }

    /// <summary>
    /// CEF 菜单命令 ID
    /// </summary>
    public enum CefMenuCommand
    {
        NotFound = -1,
        Back = 100,
        Forward = 101,
        Reload = 102,
        ReloadIgnoringCache = 103,
        Stop = 104,
        EditCut = 105,
        EditCopy = 106,
        EditPaste = 107,
        EditDelete = 108,
        EditSelectAll = 109,
        EditUndo = 110,
        EditRedo = 111,
        CustomFirst = 200,
        CustomLast = 250,
    }

    /// <summary>
    /// CEF 上下文菜单类型标志
    /// </summary>
    public enum CefContextMenuTypeFlags
    {
        None = 0,
        Page = 1 << 0,
        Frame = 1 << 1,
        Link = 1 << 2,
        Media = 1 << 3,
        Selection = 1 << 4,
        Editable = 1 << 5,
    }

    /// <summary>
    /// CEF 上下文菜单媒体类型
    /// </summary>
    public enum CefContextMenuMediaType
    {
        None = 0,
        Image = 1,
        Video = 2,
        Audio = 3,
        Plugin = 4,
    }

    /// <summary>
    /// CEF 上下文菜单媒体状态标志
    /// </summary>
    public enum CefContextMenuMediaStateFlags
    {
        None = 0,
        InError = 1 << 0,
        Paused = 1 << 1,
        Muted = 1 << 2,
        Loop = 1 << 3,
        CanSaveAs = 1 << 4,
        HasAudio = 1 << 5,
        HasVideo = 1 << 6,
    }

    /// <summary>
    /// CEF 上下文菜单编辑状态标志
    /// </summary>
    public enum CefContextMenuEditStateFlags
    {
        None = 0,
        CanUndo = 1 << 0,
        CanRedo = 1 << 1,
        CanCut = 1 << 2,
        CanCopy = 1 << 3,
        CanPaste = 1 << 4,
        CanDelete = 1 << 5,
        CanSelectAll = 1 << 6,
        CanEditRich = 1 << 7,
    }

    /// <summary>
    /// CEF 焦点源
    /// </summary>
    public enum CefFocusSource
    {
        None = 0,
        Navigation = 1,
        Shortcut = 2,
    }

    /// <summary>
    /// CEF 鼠标按钮
    /// </summary>
    public enum CefMouseButtonType
    {
        Left = 0,
        Middle = 1,
        Right = 2,
    }

    /// <summary>
    /// CEF 拖动操作类型
    /// </summary>
    public enum CefDragOperationMask
    {
        None = 0,
        Generic = 1,
        Copy = 2,
        Link = 4,
        GenericType1 = 8,
        GenericType2 = 16,
        Move = 32,
        Delete = 64,
        Every = int.MaxValue,
    }

    /// <summary>
    /// CEF 设备类型
    /// </summary>
    public enum CefTextInputMode
    {
        Default = 0,
        None = 1,
        Text = 2,
        Tel = 3,
        Url = 4,
        Email = 5,
        Numeric = 6,
        Decimal = 7,
        Search = 8,
        IsPassword = 9,
        TelOrUrl = 10,
        Max = 11,
    }

    /// <summary>
    /// CEF 窗口_open 模式
    /// </summary>
    public enum CefWindowOpenDisposition
    {
        Unknown = 0,
        CurrentTab = 1,
        SingletonTab = 2,
        NewForegroundTab = 3,
        NewBackgroundTab = 4,
        NewPopup = 5,
        NewWindow = 6,
        SaveToDisk = 7,
        OffTheRecord = 8,
        IgnoreAction = 9,
        SwitchToTab = 10,
        WsPopup = 11,
        TrustedPopup = 12,
    }

    /// <summary>
    /// CEF UI 鼠标按钮
    /// </summary>
    public enum CefMouseEventButton
    {
        None = 0,
        Left = 1,
        Middle = 2,
        Right = 3,
    }

    /// <summary>
    /// CEF 开发者工具消息目的
    /// </summary>
    public enum CefDevToolsMessageTarget
    {
        Default = 0,
        New = 1,
        Active = 2,
    }
}
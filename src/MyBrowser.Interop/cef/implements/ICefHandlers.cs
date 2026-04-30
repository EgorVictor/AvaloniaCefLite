namespace MyBrowser.Interop.cef.implements
{
    using System;
    using MyBrowser.Interop.Internal;

    /// <summary>
    /// CEF 回调接口 - 生命周期处理器
    /// </summary>
    public interface ILifeSpanHandler
    {
        /// <summary>
        /// 浏览器即将打开弹窗
        /// </summary>
        /// <returns>true 表示阻止默认行为</returns>
        bool OnBeforePopup(
            IntPtr browser,
            IntPtr frame,
            IntPtr targetUrl,
            IntPtr targetFrameName,
            IntPtr targetDisposition,
            bool userGesture,
            IntPtr popupFeatures,
            IntPtr windowInfo,
            IntPtr client,
            IntPtr settings,
            IntPtr extraInfo,
            ref bool noJavascriptAccess);

        /// <summary>
        /// 弹窗已创建
        /// </summary>
        void OnCreated(IntPtr browser, IntPtr popupBrowser);

        /// <summary>
        /// 弹窗即将关闭
        /// </summary>
        void OnBeforeClose(IntPtr browser);

        /// <summary>
        /// 弹窗地址改变
        /// </summary>
        void OnAddressChange(IntPtr browser, IntPtr frame, IntPtr url);

        /// <summary>
        /// 标题改变
        /// </summary>
        void OnTitleChange(IntPtr browser, IntPtr title);

        /// <summary>
        /// 鼠标光标改变
        /// </summary>
        void OnCursorChange(IntPtr browser, IntPtr cursor, int type, IntPtr customCursorInfo);
    }

    /// <summary>
    /// CEF 回调接口 - 加载处理器
    /// </summary>
    public interface ILoadHandler
    {
        /// <summary>
        /// 开始加载
        /// </summary>
        void OnLoadStart(IntPtr browser, IntPtr frame, int transitionType);

        /// <summary>
        /// 结束加载
        /// </summary>
        void OnLoadEnd(IntPtr browser, IntPtr frame, int httpStatusCode);

        /// <summary>
        /// 加载失败
        /// </summary>
        void OnLoadError(IntPtr browser, IntPtr frame, int errorCode, IntPtr errorText, IntPtr failedUrl);

        /// <summary>
        /// 加载状态改变
        /// </summary>
        void OnLoadStateChange(IntPtr browser, IntPtr frame, bool isLoading, bool canGoBack, bool canGoForward);
    }

    /// <summary>
    /// CEF 回调接口 - 浏览器处理器
    /// </summary>
    public interface IBrowserHandler
    {
        /// <summary>
        /// 浏览器已创建
        /// </summary>
        void OnBrowserCreated(IntPtr browser);

        /// <summary>
        /// 浏览器即将关闭
        /// </summary>
        void OnBrowserDestroyed(IntPtr browser);

        /// <summary>
        /// 标题改变
        /// </summary>
        void OnTitleChange(IntPtr title);

        /// <summary>
        /// 地址改变
        /// </summary>
        void OnAddressChange(IntPtr url);

        /// <summary>
        /// 焦点改变
        /// </summary>
        void OnFaviconUrlChange(IntPtr browser, IntPtr urls);

        /// <summary>
        /// 网页内容状态
        /// </summary>
        void OnTooltip(IntPtr browser, IntPtr text);

        /// <summary>
        /// 状态消息
        /// </summary>
        void OnStatusMessage(IntPtr browser, IntPtr value);

        /// <summary>
        /// 加载进度
        /// </summary>
        void OnProgress(IntPtr browser, double progress);

        /// <summary>
        /// 加载异常
        /// </summary>
        void OnLoadError(IntPtr browser, IntPtr frame, int errorCode, IntPtr errorMsg, IntPtr failedUrl);
    }

    /// <summary>
    /// CEF 回调接口 - JavaScript  dialog
    /// </summary>
    public interface IDialogHandler
    {
        bool OnJSMessage(
            IntPtr browser,
            IntPtr originUrl,
            int type,
            IntPtr message,
            IntPtr defaultPromptText,
            IntPtr callback,
            ref bool suppressMessage);

        bool OnFileDialog(
            IntPtr browser,
            int mode,
            IntPtr title,
            IntPtr defaultFilePath,
            IntPtr acceptFilters,
            int selectedAcceptFilter,
            IntPtr callback);
    }

    /// <summary>
    /// CEF 回调接口 - 上下文菜单
    /// </summary>
    public interface IContextMenuHandler
    {
        void OnBeforeContextMenu(IntPtr browser, IntPtr frame, IntPtr contextMenuParams, IntPtr model);

        bool OnContextMenuCommand(IntPtr browser, IntPtr frame, IntPtr contextMenuParams, int commandId, int eventFlags);

        void OnContextMenuDismissed(IntPtr browser, IntPtr frame);
    }

    /// <summary>
    /// CEF 回调接口 - 键盘输入
    /// </summary>
    public interface IKeyboardHandler
    {
        bool OnPreKeyEvent(IntPtr browser, IntPtr keyEvent, int osEvent, bool isKeyboardShort);

        bool OnKeyEvent(IntPtr browser, IntPtr keyEvent, int osEvent);
    }

    /// <summary>
    /// CEF 回调接口 - 权限请求
    /// </summary>
    public interface IPermissionHandler
    {
        bool OnRequestPermission(
            IntPtr browser,
            IntPtr originUrl,
            int type,
            int requestedPermissions,
            IntPtr callback);

        bool OnShowPermissionPrompt(IntPtr browser, int id, IntPtr requestedOrigin, int type, IntPtr callback);

        void OnDismissPermissionPrompt(IntPtr browser, int id, int result);
    }

    /// <summary>
    /// CEF 回调接口 - 查找
    /// </summary>
    public interface IFindHandler
    {
        void OnFindResult(IntPtr browser, int identifier, int count, int selectionRect, int activeMatchOrdinal, bool finalUpdate);
    }
}
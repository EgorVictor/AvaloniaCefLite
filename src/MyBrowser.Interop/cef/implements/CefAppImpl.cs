namespace MyBrowser.Interop.cef.implements
{
    using System;
    using System.Runtime.InteropServices;
    using MyBrowser.Interop.Internal;

    /// <summary>
    /// CEF 应用实现
    /// 实现所有 CEF 回调接口
    /// </summary>
    public sealed class CefAppImpl
    {
        private GCHandle _selfHandle;

        public CefBase Base { get; private set; }

        public ILifeSpanHandler? LifeSpanHandler { get; set; }
        public ILoadHandler? LoadHandler { get; set; }
        public IBrowserHandler? BrowserHandler { get; set; }
        public IDialogHandler? DialogHandler { get; set; }
        public IContextMenuHandler? ContextMenuHandler { get; set; }
        public IKeyboardHandler? KeyboardHandler { get; set; }
        public IPermissionHandler? PermissionHandler { get; set; }
        public IFindHandler? FindHandler { get; set; }

        public CefAppImpl()
        {
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            Base = new CefBase();
        }

        /// <summary>
        /// 获取自己的指针
        /// </summary>
        public IntPtr SelfPtr => GCHandle.ToIntPtr(_selfHandle);

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Free()
        {
            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }
        }
    }

    /// <summary>
    /// CEF 客户端实现
    /// 包含所有处理器
    /// </summary>
    public sealed class CefClientImpl
    {
        private GCHandle _selfHandle;

        public CefBase Base { get; private set; }

        public ILifeSpanHandler? LifeSpanHandler { get; set; }
        public ILoadHandler? LoadHandler { get; set; }
        public IBrowserHandler? BrowserHandler { get; set; }
        public IDialogHandler? DialogHandler { get; set; }
        public IContextMenuHandler? ContextMenuHandler { get; set; }
        public IKeyboardHandler? KeyboardHandler { get; set; }
        public IPermissionHandler? PermissionHandler { get; set; }
        public IFindHandler? FindHandler { get; set; }

        public CefClientImpl()
        {
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            Base = new CefBase();
        }

        public IntPtr SelfPtr => GCHandle.ToIntPtr(_selfHandle);

        public void Free()
        {
            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }
        }
    }
}
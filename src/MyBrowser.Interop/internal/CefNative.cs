namespace MyBrowser.Interop.Internal
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// CEF Native Library P/Invoke declarations
    /// </summary>
    public static unsafe class CefNative
    {
        public const string DllName = "libcef.dll";

        #region Library Functions

        [DllImport(DllName, EntryPoint = "cef_initialize")]
        public static extern int CefInitialize(
            CefMainArgs* args,
            CefSettings* settings,
            CefApp* app,
            IntPtr windowsSandboxInfo);

        [DllImport(DllName, EntryPoint = "cef_shutdown")]
        public static extern void CefShutdown();

        [DllImport(DllName, EntryPoint = "cef_execute_process")]
        public static extern int CefExecuteProcess(
            CefMainArgs* args,
            CefApp* app,
            IntPtr windowsSandboxInfo);

        [DllImport(DllName, EntryPoint = "cef_version_info")]
        public static extern int CefVersionInfo(int entry);

        [DllImport(DllName, EntryPoint = "cef_run_message_loop")]
        public static extern void CefRunMessageLoop();

        [DllImport(DllName, EntryPoint = "cef_do_message_loop_work")]
        public static extern void CefDoMessageLoopWork();

        #endregion

        #region String Functions

        [DllImport(DllName, EntryPoint = "cef_string_utf16_set")]
        public static extern int CefString_Set(char* value, int length, CefString* output, int copy);

        [DllImport(DllName, EntryPoint = "cef_string_utf16_clear")]
        public static extern void CefString_Clear(CefString* str);

        [DllImport(DllName, EntryPoint = "cef_string_utf8_set")]
        public static extern int CefString_Utf8_Set(byte* value, int length, CefString* output, int copy);

        #endregion

        #region Browser Functions

        [DllImport(DllName, EntryPoint = "cef_browser_host_create_browser")]
        public static extern int CefBrowserHost_CreateBrowser(
            CefWindowInfo* windowInfo,
            CefClient* client,
            CefString* url,
            CefBrowserSettings* settings,
            IntPtr extraInfo,
            IntPtr requestContext);

        [DllImport(DllName, EntryPoint = "cef_browser_host_create_browser_sync")]
        public static extern IntPtr CefBrowserHost_CreateBrowserSync(
            CefWindowInfo* windowInfo,
            CefClient* client,
            CefString* url,
            CefBrowserSettings* settings,
            IntPtr extraInfo,
            IntPtr requestContext);

        #endregion
    }
}
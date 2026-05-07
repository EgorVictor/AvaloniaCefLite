namespace MyBrowser.Interop
{
    /// <summary>
    /// CEF 109 vtable offsets for x64.
    /// These offsets are specific to CEF 109 x64 and may vary between versions.
    /// Only valid for CEF 109 x64.
    /// </summary>
    public static class Cef109VTableOffsets
    {
        // cef_browser_t vtable offsets (after cef_base_ref_counted_t = 40 bytes)
        public const int BrowserIsValid = 40;
        public const int BrowserGetHost = 48;
        public const int BrowserGoBack = 64;
        public const int BrowserGoForward = 80;
        public const int BrowserReload = 96;
        public const int BrowserReloadIgnoreCache = 104;
        public const int BrowserStopLoad = 112;
        public const int BrowserGetMainFrame = 152;

        // cef_frame_t vtable offsets
        public const int FrameLoadUrl = 136;

        // cef_browser_host_t vtable offsets
        public const int BrowserHostGetWindowHandle = 72;
    }
}

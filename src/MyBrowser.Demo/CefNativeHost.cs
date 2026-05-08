#nullable enable
namespace MyBrowser.Demo
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Platform;
    using System.Runtime.InteropServices;

    public class CefNativeHost : NativeControlHost
    {
        private IntPtr _hostHwnd;
        private IBrowserControl? _browser;
        private bool _browserAttached;
        private Rect _lastBounds;

        public IBrowserControl? Browser
        {
            get => _browser;
            set
            {
                _browser = value;
                if (_browser != null && _hostHwnd != IntPtr.Zero && !_browserAttached)
                {
                    _browser.SetWindowHandle(_hostHwnd);
                    _browserAttached = true;
                }
            }
        }

        public void AttachBrowser(IBrowserControl browser)
        {
            Browser = browser;
            if (_browser != null)
            {
                _browser.BrowserInitialized += (s, e) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (_hostHwnd != IntPtr.Zero && _browser != null && !_browserAttached)
                        {
                            _browser.SetWindowHandle(_hostHwnd);
                            _browserAttached = true;
                            _browser.NotifyResized();
                        }
                    });
                };
            }
        }

        public void LoadUrl(string url) => _browser?.LoadUrl(url);
        public void GoBack() => _browser?.GoBack();
        public void GoForward() => _browser?.GoForward();
        public void Reload() => _browser?.Reload();
        public void Stop() => _browser?.Stop();

        public void ForceRefresh()
        {
            _lastBounds = default;
            UpdateNativeBounds(force: true);
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            _hostHwnd = CreateHostWindow(parent.Handle);
            if (_hostHwnd != IntPtr.Zero && _browser != null && !_browserAttached)
            {
                _browser.SetWindowHandle(_hostHwnd);
                _browserAttached = true;
            }
            return new PlatformHandle(_hostHwnd, "HWND");
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (_browser != null)
            {
                _browser.Dispose();
                _browser = null;
            }

            if (_hostHwnd != IntPtr.Zero)
            {
                DestroyWindow(_hostHwnd);
                _hostHwnd = IntPtr.Zero;
            }
            _browserAttached = false;
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateNativeBounds();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateNativeBounds();
            return finalSize;
        }

        private void UpdateNativeBounds(bool force = false)
        {
            if (_hostHwnd == IntPtr.Zero) return;

            var topLevel = TopLevel.GetTopLevel(this);
            var origin = this.TranslatePoint(new Point(0, 0), topLevel);
            if (topLevel == null || origin == null) return;

            var scale = topLevel.RenderScaling;
            var x = (int)Math.Round(origin.Value.X * scale);
            var y = (int)Math.Round(origin.Value.Y * scale);
            var width = Math.Max(1, (int)Math.Round(Bounds.Width * scale));
            var height = Math.Max(1, (int)Math.Round(Bounds.Height * scale));

            var newBounds = new Rect(x, y, width, height);
            if (!force && newBounds == _lastBounds) return;
            _lastBounds = newBounds;

            SetWindowPos(_hostHwnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            _browser?.NotifyResized();
        }

        private IntPtr CreateHostWindow(IntPtr parentHwnd)
        {
            RegisterHostWindowClass();

            return CreateWindowEx(
                0,
                HOST_CLASS_NAME,
                string.Empty,
                WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
                0,
                0,
                800,
                600,
                parentHwnd,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        private static IntPtr _wndProcPtr;
        private static WNDPROC? _wndProc;
        private static bool _classRegistered = RegisterHostWindowClass();

        private static bool RegisterHostWindowClass()
        {
            _wndProc = new WNDPROC(HostWndProc);
            _wndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProc);

            var wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                style = CS_HREDRAW | CS_VREDRAW,
                lpfnWndProc = _wndProcPtr,
                hInstance = GetModuleHandle(null),
                lpszClassName = HOST_CLASS_NAME
            };

            var atom = RegisterClassEx(ref wndClass);
            return atom != 0;
        }

        private const string HOST_CLASS_NAME = "MyBrowserCefHostWindow";
        private const int CS_HREDRAW = 0x0002;
        private const int CS_VREDRAW = 0x0001;
        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;

        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_PAINT = 0x000F;
        private const int WM_SIZE = 0x0005;
        private const int WM_SETFOCUS = 0x0007;
        private const int WM_DESTROY = 0x0002;

        private static IntPtr HostWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_ERASEBKGND:
                    return new IntPtr(1);
                case WM_PAINT:
                    var ps = new PAINTSTRUCT();
                    BeginPaint(hWnd, ref ps);
                    FillRect(ps.hdc, ref ps.rcPaint, GetStockObject(NULL_BRUSH));
                    EndPaint(hWnd, ref ps);
                    return IntPtr.Zero;
                case WM_SIZE:
                    if (wParam != (IntPtr)1)
                    {
                        var width = (short)(lParam.ToInt32() & 0xFFFF);
                        var height = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
                        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
                    }
                    return IntPtr.Zero;
                case WM_SETFOCUS:
                    return IntPtr.Zero;
                case WM_DESTROY:
                    return IntPtr.Zero;
                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpWndClass);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int fnObject);

        private const int NULL_BRUSH = 5;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private delegate IntPtr WNDPROC(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string? lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}

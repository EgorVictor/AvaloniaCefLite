namespace MyBrowser.Demo
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Native;
    using Avalonia.Platform;
    using System.Runtime.InteropServices;
    using System.Reflection;

    public class CefNativeHost : NativeControlHost
    {
        private IntPtr _browserHwnd;
        private IBrowserControl? _browser;
        private bool _browserCreated;

        public void AttachBrowser(IBrowserControl browser)
        {
            _browser = browser;
            if (_browser != null)
            {
                _browser.BrowserInitialized += (s, e) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _browserCreated = true;
                        if (_browserHwnd != IntPtr.Zero)
                        {
                            _browser.SetWindowHandle(_browserHwnd);
                        }
                    });
                };
            }
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var hwnd = CreateHostWindow(parent.Handle);
            if (hwnd != IntPtr.Zero)
            {
                _browserHwnd = hwnd;
                if (_browser != null && _browserCreated)
                {
                    _browser.SetWindowHandle(hwnd);
                }
            }
            return new PlatformHandle(hwnd, "HWND");
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (_browser != null)
            {
                _browser.Dispose();
                _browser = null;
            }

            if (_browserHwnd != IntPtr.Zero)
            {
                DestroyWindow(_browserHwnd);
                _browserHwnd = IntPtr.Zero;
            }
            _browserCreated = false;
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

        private static readonly bool _classRegistered = RegisterHostWindowClass();

        private static IntPtr _wndProcPtr;

        private static bool RegisterHostWindowClass()
        {
            var wndProc = new WNDPROC(HostWndProc);
            _wndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProc);

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
                        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, width, height, 0x0040);
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

        [DllImport("user32.dll")]
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

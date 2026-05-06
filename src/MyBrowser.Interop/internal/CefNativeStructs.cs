namespace MyBrowser.Interop.Internal
{
    using System;
    using System.Runtime.InteropServices;

    // ==================== Native types (pointer-compatible) ====================
    // These match CEF C API exactly and are used for P/Invoke

    public unsafe struct CefMainArgs
    {
        public IntPtr Instance; // HINSTANCE on Windows
    }

    public unsafe struct CefSettings
    {
        public UIntPtr size;                    // 0: size_t
        public int no_sandbox;                // 8: int
        public CefString browser_subprocess_path; // 16: cef_string_t
        public CefString framework_dir_path;     // 32: cef_string_t
        public CefString main_bundle_path;     // 48: cef_string_t
        public int chrome_runtime;              // 64: int
        public int multi_threaded_message_loop; // 68: int
        public int external_message_pump;       // 72: int
        public int windowless_rendering_enabled; // 76: int
        public int command_line_args_disabled;  // 80: int
        public CefString cache_path;            // 88: cef_string_t
        public CefString root_cache_path;      // 104: cef_string_t
        public CefString user_data_path;       // 120: cef_string_t
        public int persist_session_cookies;     // 136: int
        public int persist_user_preferences;    // 140: int
        public CefString user_agent;           // 144: cef_string_t
        public CefString user_agent_product;   // 160: cef_string_t
        public CefString locale;              // 176: cef_string_t
        public CefString log_file;            // 192: cef_string_t
        public int log_severity;              // 208: int (cef_log_severity_t)
        public CefString javascript_flags;     // 216: cef_string_t
        public CefString resources_dir_path;   // 232: cef_string_t
        public CefString locales_dir_path;     // 248: cef_string_t
        public int pack_loading_disabled;     // 264: int
        public int remote_debugging_port;      // 268: int
        public int uncaught_exception_stack_size; // 272: int
        public uint background_color;          // 276: cef_color_t
        public CefString accept_language_list; // 288: cef_string_t
        public CefString cookieable_schemes_list; // 304: cef_string_t
        public int cookieable_schemes_exclude_defaults; // 320: int
    }

    public unsafe struct CefApp
    {
        public CefBase Base;
    }

    public unsafe struct CefBase
    {
        public UIntPtr size;
        public IntPtr add_ref;
        public IntPtr release;
        public IntPtr has_one_ref;
        public IntPtr has_at_least_one_ref;
    }

    public unsafe struct CefString
    {
        public char* str;
        public UIntPtr length;
        public IntPtr dtor;
    }

    public struct CefRect
    {
        public int x, y, width, height;
    }

    public unsafe struct CefWindowInfo
    {
        public UIntPtr size;
        public CefString window_name;
        public CefRect bounds;
        public IntPtr parent_window;
        public int windowless_rendering_enabled;
        public int shared_texture_enabled;
        public int external_begin_frame_enabled;
        public IntPtr window;
        public int hidden;
        public IntPtr parent_view;
        public IntPtr view;
        public uint style;
        public uint ex_style;
        public IntPtr menu;
    }

    public unsafe struct CefBrowserSettings
    {
        public UIntPtr size;
        public int windowless_frame_rate;
        public CefString standard_font_family;
        public CefString fixed_font_family;
        public CefString serif_font_family;
        public CefString sans_serif_font_family;
        public CefString cursive_font_family;
        public CefString fantasy_font_family;
        public int default_font_size;
        public int default_font_family;
        public int minimum_font_size;
        public int minimum_logical_font_size;
        public CefString default_encoding;
        public int remote_fonts;
        public int javascript;
        public int javascript_close_windows;
        public int javascript_access_clipboard;
        public int javascript_dom_paste;
        public int javascript_ui;
        public int local_storage;
        public int databases;
        public int webgl;
        public uint background_color;
        public int accept_ssl_certificates;
        public int spellcheck;
        public int spellcheck_dictionaries;
    }
}
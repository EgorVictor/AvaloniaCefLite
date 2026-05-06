namespace MyBrowser.Interop.cef.capi
{
    using System;
    using System.Runtime.InteropServices;

    public static unsafe class cef_capi
    {
        public const string DllName = "libcef.dll";
    }

    #region Base Types

    // CEF 109: add_ref returns void, others return int
    public unsafe delegate void cef_base_ref_counted_add_ref(IntPtr self);
    public unsafe delegate int cef_base_ref_counted_release(IntPtr self);
    public unsafe delegate int cef_base_ref_counted_has_one_ref(IntPtr self);
    public unsafe delegate int cef_base_ref_counted_has_at_least_one_ref(IntPtr self);

    // CEF 109 cef_base_ref_counted_t
    [StructLayout(LayoutKind.Sequential)]
    public struct cef_base_ref_counted_t
    {
        public UIntPtr size;
        public IntPtr add_ref;
        public IntPtr release;
        public IntPtr has_one_ref;
        public IntPtr has_at_least_one_ref;
    }

    // Alias for backward compatibility
    public struct cef_base_t
    {
        public UIntPtr size;
        public IntPtr add_ref;
        public IntPtr release;
        public IntPtr has_one_ref;
        public IntPtr has_at_least_one_ref;
    }

    #endregion

    #region String

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_string_t
    {
        public char* str;
        public UIntPtr length;
        public IntPtr dtor;
    }

    public unsafe delegate int cef_stringvisitor_visit(IntPtr self, cef_string_t* str);

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct _cef_string_visitor_t
    {
        public cef_base_t base_;
        public cef_stringvisitor_visit visit;
    }

    #endregion

    #region Browser

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_browser_t
    {
        public cef_base_t base_;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_browser_host_t
    {
        public cef_base_t base_;
    }

    #endregion

    #region Frame

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_frame_t
    {
        public cef_base_t base_;
    }

    #endregion

    #region Request

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_request_t
    {
        public cef_base_t base_;
    }

    #endregion

    #region Response

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_response_t
    {
        public cef_base_t base_;
    }

    #endregion

    #region LifeSpan Handler

    public unsafe delegate int cef_life_span_handler_on_before_popup(
        IntPtr self,
        IntPtr browser,
        IntPtr frame,
        IntPtr target_url,
        IntPtr target_frame_name,
        int target_disposition,
        int user_gesture,
        IntPtr popupFeatures,
        IntPtr windowInfo,
        IntPtr client,
        IntPtr settings,
        IntPtr extra_info,
        int* no_javascript_access);

    public unsafe delegate void cef_life_span_handler_on_after_created(IntPtr self, IntPtr browser);
    public unsafe delegate int cef_life_span_handler_do_close(IntPtr self, IntPtr browser);
    public unsafe delegate void cef_life_span_handler_on_before_close(IntPtr self, IntPtr browser);

    // CEF 109 actual field order from cef_life_span_handler_capi.h
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_life_span_handler_t
    {
        public cef_base_ref_counted_t base_;
        public IntPtr on_before_popup;
        public IntPtr on_after_created;
        public IntPtr do_close;
        public IntPtr on_before_close;
    }

    #endregion

    #region Load Handler

    public unsafe delegate void cef_load_handler_on_loading_state_change(IntPtr self, IntPtr browser, int isLoading, int canGoBack, int canGoForward);
    public unsafe delegate void cef_load_handler_on_load_start(IntPtr self, IntPtr browser, IntPtr frame, int transition_type);
    public unsafe delegate void cef_load_handler_on_load_end(IntPtr self, IntPtr browser, IntPtr frame, int httpStatusCode);
    public unsafe delegate void cef_load_handler_on_load_error(IntPtr self, IntPtr browser, IntPtr frame, int errorCode, IntPtr errorText, IntPtr failedUrl);

    // CEF 109 actual field order from cef_load_handler_capi.h
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_load_handler_t
    {
        public cef_base_ref_counted_t base_;
        public IntPtr on_loading_state_change;
        public IntPtr on_load_start;
        public IntPtr on_load_end;
        public IntPtr on_load_error;
    }

    #endregion

    #region Browser Handler (Display)

    public unsafe delegate void cef_browser_handler_on_title_change(IntPtr self, IntPtr browser, IntPtr title);
    public unsafe delegate void cef_browser_handler_on_address_change(IntPtr self, IntPtr browser, IntPtr frame, IntPtr url);
    public unsafe delegate void cef_browser_handler_on_favicon_urlchange(IntPtr self, IntPtr browser, IntPtr urls);
    public unsafe delegate void cef_browser_handler_on_tooltip(IntPtr self, IntPtr browser, IntPtr text);
    public unsafe delegate void cef_browser_handler_on_status_message(IntPtr self, IntPtr browser, IntPtr value);
    public unsafe delegate void cef_browser_handler_on_load_progress_change(IntPtr self, IntPtr browser, double progress);
    public unsafe delegate int cef_browser_handler_on_console_message(IntPtr self, IntPtr browser, int level, IntPtr message, IntPtr source, int line);
    public unsafe delegate int cef_browser_handler_on_auto_fill(IntPtr self, IntPtr browser, IntPtr frame, IntPtr form, int row);

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_browser_handler_t
    {
        public cef_base_t base_;
        public cef_browser_handler_on_title_change on_title_change;
        public cef_browser_handler_on_address_change on_address_change;
        public cef_browser_handler_on_favicon_urlchange on_favicon_urlchange;
        public cef_browser_handler_on_tooltip on_tooltip;
        public cef_browser_handler_on_status_message on_status_message;
        public cef_browser_handler_on_load_progress_change on_load_progress_change;
        public cef_browser_handler_on_console_message on_console_message;
        public cef_browser_handler_on_auto_fill on_auto_fill;
    }

    #endregion

    #region Client

    // CEF 109 actual field order from cef_client_capi.h
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_client_t
    {
        public cef_base_ref_counted_t base_;
        public IntPtr get_audio_handler;
        public IntPtr get_command_handler;
        public IntPtr get_context_menu_handler;
        public IntPtr get_dialog_handler;
        public IntPtr get_display_handler;
        public IntPtr get_download_handler;
        public IntPtr get_drag_handler;
        public IntPtr get_find_handler;
        public IntPtr get_focus_handler;
        public IntPtr get_frame_handler;
        public IntPtr get_permission_handler;
        public IntPtr get_jsdialog_handler;
        public IntPtr get_keyboard_handler;
        public IntPtr get_life_span_handler;
        public IntPtr get_load_handler;
        public IntPtr get_print_handler;
        public IntPtr get_render_handler;
        public IntPtr get_request_handler;
        public IntPtr on_process_message_received;
    }

    #endregion

    #region Browser Settings

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_browser_settings_t
    {
        public UIntPtr size;
        public int windowless_frame_rate;
        public cef_string_t standard_font_family;
        public cef_string_t fixed_font_family;
        public cef_string_t serif_font_family;
        public cef_string_t sans_serif_font_family;
        public cef_string_t cursive_font_family;
        public cef_string_t fantasy_font_family;
        public int default_font_size;
        public int default_font_family;
        public int minimum_font_size;
        public int minimum_logical_font_size;
        public cef_string_t default_encoding;
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

    #endregion

    #region Window Info

    [StructLayout(LayoutKind.Sequential)]
    public struct cef_rect_t
    {
        public int x;
        public int y;
        public int width;
        public int height;
    }

    // CEF 109 actual Windows window info from cef_types_win.h
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_window_info_t
    {
        public uint ex_style;
        public cef_string_t window_name;
        public uint style;
        public cef_rect_t bounds;
        public IntPtr parent_window;
        public IntPtr menu;
        public int windowless_rendering_enabled;
        public int shared_texture_enabled;
        public int external_begin_frame_enabled;
        public IntPtr window;
    }

    #endregion

    #region Main Args

    [StructLayout(LayoutKind.Sequential)]
    public struct cef_main_args_t
    {
        public IntPtr instance;
    }

    #endregion

    #region Settings

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_settings_t
    {
        public UIntPtr size;
        public int no_sandbox;
        public cef_string_t browser_subprocess_path;
        public cef_string_t framework_dir_path;
        public cef_string_t main_bundle_path;
        public int chrome_runtime;
        public int multi_threaded_message_loop;
        public int external_message_pump;
        public int windowless_rendering_enabled;
        public int command_line_args_disabled;
        public cef_string_t cache_path;
        public cef_string_t root_cache_path;
        public cef_string_t user_data_path;
        public int persist_session_cookies;
        public int persist_user_preferences;
        public cef_string_t user_agent;
        public cef_string_t user_agent_product;
        public cef_string_t locale;
        public cef_string_t log_file;
        public int log_severity;
        public cef_string_t javascript_flags;
        public cef_string_t resources_dir_path;
        public cef_string_t locales_dir_path;
        public int pack_loading_disabled;
        public int remote_debugging_port;
        public int uncaught_exception_stack_size;
        public uint background_color;
        public cef_string_t accept_language_list;
        public cef_string_t cookieable_schemes_list;
        public int cookieable_schemes_exclude_defaults;
    }

    #endregion

    #region App

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct cef_app_t
    {
        public cef_base_t base_;
        public IntPtr on_before_command_line_processing;
        public IntPtr on_render_process_thread_created;
        public IntPtr on_web_view_created;
        public IntPtr on_context_initialized;
        public IntPtr on_before_cookies;
        public IntPtr get_auth_credentials;
    }

    #endregion

    #region P/Invoke Declarations

    public static unsafe class NativeMethods
    {
        [DllImport(cef_capi.DllName, EntryPoint = "cef_execute_process")]
        public static extern int cef_execute_process(cef_main_args_t* args, cef_app_t* app, IntPtr windows_sandbox_info);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_initialize")]
        public static extern int cef_initialize(cef_main_args_t* args, cef_settings_t* settings, cef_app_t* app, IntPtr windows_sandbox_info);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_shutdown")]
        public static extern void cef_shutdown();

        [DllImport(cef_capi.DllName, EntryPoint = "cef_run_message_loop")]
        public static extern void cef_run_message_loop();

        [DllImport(cef_capi.DllName, EntryPoint = "cef_do_message_loop_work")]
        public static extern void cef_do_message_loop_work();

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_create_browser")]
        public static extern int cef_browser_host_create_browser(cef_window_info_t* window_info, cef_client_t* client, cef_string_t* url, cef_browser_settings_t* settings, IntPtr extra_info, IntPtr request_context);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_create_browser_sync")]
        public static extern IntPtr cef_browser_host_create_browser_sync(cef_window_info_t* window_info, cef_client_t* client, cef_string_t* url, cef_browser_settings_t* settings, IntPtr extra_info, IntPtr request_context);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_get_host")]
        public static extern IntPtr cef_browser_get_host(IntPtr browser);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_close_browser")]
        public static extern void cef_browser_host_close_browser(IntPtr host, int force_close);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_go_back")]
        public static extern void cef_browser_host_go_back(IntPtr host);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_go_forward")]
        public static extern void cef_browser_host_go_forward(IntPtr host);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_reload")]
        public static extern void cef_browser_host_reload(IntPtr host);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_reload_ignore_cache")]
        public static extern void cef_browser_host_reload_ignore_cache(IntPtr host);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_stop_load")]
        public static extern void cef_browser_host_stop_load(IntPtr host);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_is_loading")]
        public static extern int cef_browser_host_is_loading(IntPtr host);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_execute_javascript")]
        public static extern void cef_browser_host_execute_javascript(IntPtr host, cef_string_t* code, cef_string_t* url, int line);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_was_resized")]
        public static extern void cef_browser_host_was_resized(IntPtr host);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_get_size")]
        public static extern void cef_browser_host_get_size(IntPtr host, int* width, int* height);

        [DllImport(cef_capi.DllName, EntryPoint = "cef_browser_host_set_size")]
        public static extern void cef_browser_host_set_size(IntPtr host, int width, int height);
    }

    #endregion
}

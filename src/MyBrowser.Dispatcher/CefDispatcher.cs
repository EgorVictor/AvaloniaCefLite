namespace MyBrowser
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Serilog;

    /// <summary>
    /// 浏览器驱动调度器
    /// 根据操作系统版本动态选择并加载对应的浏览器驱动
    ///
    /// 选择逻辑：
    /// 1. 第一层：选择 CEF 版本（Cef109 / CefLatest）
    /// 2. 第二层：选择运行策略（Win7Compatible / ModernWindows）
    /// </summary>
    public static class CefDispatcher
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(LogHelper.GetLogPath("mybrowser-main.log"), shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private static IBrowserFactory _factory;
        private static bool _assemblyResolveRegistered;
        
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        public static string DriverType => _factory?.DriverType ?? "Unknown";
        public static string Version => _factory?.Version ?? "Unknown";
        public static string DriverPath { get; private set; }
        public static string DriverName { get; private set; }
        public static CefCompatibilityMode CurrentPolicy { get; private set; }

        public static void Shutdown()
        {
            _factory?.Shutdown();
            _factory = null;
        }

        /// <summary>
        /// 启动调度器并返回浏览器工厂
        /// 必须在UI初始化之前调用
        /// </summary>
        public static IBrowserFactory Boot()
        {
            return BootInternal(Environment.OSVersion.Version, null);
        }

        /// <summary>
        /// 带配置启动
        /// </summary>
        public static IBrowserFactory Boot(BrowserConfig config)
        {
            return BootInternal(Environment.OSVersion.Version, config);
        }

        internal static IBrowserFactory BootInternal(Version osVersion, BrowserConfig config = null)
        {
            config = config ?? new BrowserConfig();

            var selection = CefRuntimeSelector.Select(osVersion, config);
            DriverName = selection.DriverName;
            DriverPath = selection.RuntimePath;
            CurrentPolicy = selection.Policy;

            _log.Information("===========================================");
            _log.Information("[CefDispatcher] 浏览器驱动调度器");
            _log.Information("===========================================");
            _log.Information("[CefDispatcher] 操作系统版本: {OsVersion}", osVersion);
            _log.Information("[CefDispatcher] CEF 版本: {DriverName}", DriverName);
            _log.Information("[CefDispatcher] 兼容策略: {Policy}", CurrentPolicy);
            _log.Information("[CefDispatcher] 驱动路径: {DriverPath}", DriverPath);

            if (!string.IsNullOrEmpty(DriverPath) && Directory.Exists(DriverPath))
            {
                bool result = SetDllDirectory(DriverPath);
                _log.Information("[CefDispatcher] SetDllDirectory({DriverPath}) = {Result}", DriverPath, result);
                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    _log.Information("[CefDispatcher] 警告: SetDllDirectory失败, error={Error}", error);
                }
            }
            else
            {
                _log.Information("[CefDispatcher] 警告: 驱动路径不存在: {DriverPath}", DriverPath);
            }

            if (!_assemblyResolveRegistered)
            {
                _log.Information("[CefDispatcher] 挂载AssemblyResolve");
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                _assemblyResolveRegistered = true;
            }

            _log.Information("[CefDispatcher] 定位驱动DLL");
            string driverDllName = $"MyBrowser.Driver.{DriverName}.dll";
            string driverDllPath = FindDriverDll(DriverPath, driverDllName);

            _log.Information("[CefDispatcher] 驱动DLL: {DriverDllPath}", driverDllPath);

            _log.Information("[CefDispatcher] 加载驱动程序集");
            var assembly = Assembly.LoadFrom(driverDllPath);
            _log.Information("[CefDispatcher] 程序集已加载: {AssemblyFullName}", assembly.FullName);

            _log.Information("[CefDispatcher] 查找IBrowserFactory");
            Type factoryType = FindFactoryType(assembly);

            _log.Information("[CefDispatcher] 工厂类型: {FactoryTypeFullName}", factoryType.FullName);
            _log.Information("[CefDispatcher] 创建工厂实例");
            _factory = (IBrowserFactory)Activator.CreateInstance(factoryType);
            _log.Information("[CefDispatcher] 工厂已创建: {FactoryTypeName}", _factory.GetType().Name);

            config.RuntimePath = DriverPath;
            _factory.Initialize(config, CurrentPolicy);

            _log.Information("===========================================");

            return _factory;
        }

        private static string FindDriverDll(string primaryPath, string dllName)
        {
            string primaryDllPath = Path.Combine(primaryPath, dllName);
            if (File.Exists(primaryDllPath))
            {
                return primaryDllPath;
            }

            string[] fallbackPaths = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", DriverName)),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "src", DriverName)),
                Path.Combine(AppContext.BaseDirectory, DriverName),
            };

            foreach (var fallback in fallbackPaths)
            {
                var testPath = Path.Combine(fallback, dllName);
                _log.Information("[CefDispatcher] 尝试: {TestPath}", testPath);
                if (File.Exists(testPath))
                {
                    DriverPath = fallback;
                    _log.Information("[CefDispatcher] 已找到! 更新DriverPath为: {FallbackPath}", fallback);
                    return testPath;
                }
            }

            throw new FileNotFoundException(
                $"未找到驱动DLL: {dllName}\n" +
                $"已搜索路径:\n" +
                $"  主路径: {primaryDllPath}\n" +
                $"  备用路径: {string.Join("\n  ", fallbackPaths)}");
        }

        private static Type FindFactoryType(Assembly assembly)
        {
            Type factoryType = null;
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IBrowserFactory).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    factoryType = type;
                    break;
                }
            }

            if (factoryType == null)
            {
                throw new TypeLoadException(
                    $"在 {assembly.Location} 中未找到IBrowserFactory实现\n" +
                    $"可用类型: {string.Join(", ", assembly.GetTypes().Select(t => t.FullName))}");
            }

            return factoryType;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
        {
            var assemblyName = new AssemblyName(e.Name);
            string dllName = assemblyName.Name + ".dll";
            string dllPath = Path.Combine(DriverPath, dllName);

            _log.Information("[AssemblyResolve] 正在解析: {AssemblyName}", assemblyName.Name);
            _log.Information("[AssemblyResolve] 在以下路径查找: {DriverPath}", DriverPath);

            if (File.Exists(dllPath))
            {
                _log.Information("[AssemblyResolve] 已找到: {DllPath}", dllPath);
                return Assembly.LoadFrom(dllPath);
            }

            _log.Information("[AssemblyResolve] 未找到");
            return null;
        }
    }

    /// <summary>
    /// CEF 运行时选择器
    /// 根据 OS 版本和配置选择合适的 CEF 版本和运行策略
    /// </summary>
    public sealed class CefRuntimeSelection
    {
        public string DriverName { get; init; }
        public string RuntimePath { get; init; }
        public CefCompatibilityMode Policy { get; init; }
    }

    public static class CefRuntimeSelector
    {
        public static CefRuntimeSelection Select(Version osVersion, BrowserConfig config)
        {
            var isWin7Or8 = osVersion.Major < 10;

            var policy = config.CompatibilityMode == CefCompatibilityMode.Auto
                ? (isWin7Or8 ? CefCompatibilityMode.Win7Compatible : CefCompatibilityMode.ModernWindows)
                : config.CompatibilityMode;

            string driverName;
            if (config.VersionPreference == CefVersionPreference.Cef109)
            {
                driverName = "Cef109";
            }
            else if (config.VersionPreference == CefVersionPreference.Latest)
            {
                var latestPath = Path.Combine(AppContext.BaseDirectory, "Runtimes", "CefLatest");
                driverName = Directory.Exists(latestPath) ? "CefLatest" : "Cef109";
            }
            else
            {
                driverName = "Cef109";
            }

            var runtimePath = Path.Combine(AppContext.BaseDirectory, "Runtimes", driverName);

            return new CefRuntimeSelection
            {
                DriverName = driverName,
                RuntimePath = runtimePath,
                Policy = policy
            };
        }
    }
}

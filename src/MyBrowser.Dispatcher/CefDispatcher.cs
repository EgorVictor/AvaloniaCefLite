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
    /// 路径劫持策略：
    /// 1. SetDllDirectory: 将原生DLL(libcef.dll)的加载重定向到子目录
    /// 2. AssemblyResolve: 将托管DLL(CefGlue.dll)的加载重定向到子目录
    /// </summary>
    public static class CefDispatcher
    {
        private static readonly ILogger _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"F:\mybrowser.log", shared: true, encoding: System.Text.Encoding.UTF8, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Message}\n")
            .CreateLogger();

        private static IBrowserFactory _factory;
        
        /// <summary>
        /// 设置原生DLL搜索路径的Win32 API
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        /// <summary>
        /// 当前驱动的类型
        /// </summary>
        public static string DriverType => _factory?.DriverType ?? "Unknown";
        
        /// <summary>
        /// 当前驱动的版本
        /// </summary>
        public static string Version => _factory?.Version ?? "Unknown";
        
        /// <summary>
        /// 驱动目录路径
        /// </summary>
        public static string DriverPath { get; private set; }
        
        /// <summary>
        /// 驱动名称（Legacy或Modern）
        /// </summary>
        public static string DriverName { get; private set; }

        /// <summary>
        /// 关闭调度器并释放资源
        /// </summary>
        public static void Shutdown()
        {
            _factory?.Shutdown();
            _factory = null;
        }

        /// <summary>
        /// 启动调度器并返回浏览器工厂
        /// 必须在UI初始化之前调用
        /// 
        /// 此方法执行路径劫持：
        /// 1. 检测操作系统版本以选择驱动（Win7用Legacy，Win10+用Modern）
        /// 2. 调用SetDllDirectory重定向原生DLL加载
        /// 3. 挂载AssemblyResolve重定向托管DLL加载
        /// 4. 通过反射加载驱动DLL
        /// </summary>
        public static IBrowserFactory Boot()
        {
            return BootInternal(Environment.OSVersion.Version);
        }

        /// <summary>
        /// 内部启动方法（供测试使用）
        /// </summary>
        internal static IBrowserFactory BootInternal(Version osVersion)
        {
            // 1. 检测操作系统版本（Win7/8=Legacy，Win10+=Modern）
            bool isLegacy = osVersion.Major < 10;
            
            DriverName = isLegacy ? "Legacy" : "Modern";
            DriverPath = Path.Combine(AppContext.BaseDirectory, "Runtimes", DriverName);

            _log.Information("===========================================");
            _log.Information("[CefDispatcher] 浏览器驱动调度器");
            _log.Information("===========================================");
            _log.Information("[CefDispatcher] 操作系统版本: {OsVersion}", osVersion);
            _log.Information("[CefDispatcher] 操作系统主版本号: {OsVersionMajor}", osVersion.Major);
            _log.Information("[CefDispatcher] 选择的驱动: {DriverName}", DriverName);
            _log.Information("[CefDispatcher] 驱动路径: {DriverPath}", DriverPath);

            // 2. SetDllDirectory - 原生DLL搜索路径劫持
            // 这对libcef.dll的加载至关重要
            _log.Information("[CefDispatcher] 步骤1: SetDllDirectory()");
            if (!string.IsNullOrEmpty(DriverPath) && Directory.Exists(DriverPath))
            {
                bool result = SetDllDirectory(DriverPath);
                _log.Information("[CefDispatcher]   SetDllDirectory({DriverPath}) = {Result}", DriverPath, result);
                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    _log.Information("[CefDispatcher]   警告: SetDllDirectory失败, error={Error}", error);
                }
            }
            else
            {
                _log.Information("[CefDispatcher]   警告: 驱动路径不存在: {DriverPath}", DriverPath);
                _log.Information("[CefDispatcher]   将使用备用路径发现...");
            }

            // 3. 挂载AssemblyResolve - 托管DLL搜索路径劫持
            // 这对CefGlue.dll的加载至关重要
            _log.Information("[CefDispatcher] 步骤2: 挂载AssemblyResolve");
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            _log.Information("[CefDispatcher]   AssemblyResolve处理器已注册");

            // 4. 查找驱动DLL路径
            _log.Information("[CefDispatcher] 步骤3: 定位驱动DLL");
            string driverDllName = $"MyBrowser.Driver.{DriverName}.dll";
            string driverDllPath = FindDriverDll(DriverPath, driverDllName);

            _log.Information("[CefDispatcher]   驱动DLL: {DriverDllPath}", driverDllPath);

            // 5. 通过反射加载驱动程序集
            _log.Information("[CefDispatcher] 步骤4: 加载驱动程序集");
            var assembly = Assembly.LoadFrom(driverDllPath);
            _log.Information("[CefDispatcher]   程序集已加载: {AssemblyFullName}", assembly.FullName);

            // 6. 查找IBrowserFactory实现
            _log.Information("[CefDispatcher] 步骤5: 查找IBrowserFactory");
            Type factoryType = FindFactoryType(assembly);

            _log.Information("[CefDispatcher]   工厂类型: {FactoryTypeFullName}", factoryType.FullName);
            _log.Information("[CefDispatcher] 步骤6: 创建工厂实例");
            _factory = (IBrowserFactory)Activator.CreateInstance(factoryType);
            _log.Information("[CefDispatcher]   工厂已创建: {FactoryTypeName}", _factory.GetType().Name);
            _log.Information("===========================================");

            return _factory;
        }

        /// <summary>
        /// 查找驱动DLL，检查多个可能的位置
        /// </summary>
        private static string FindDriverDll(string primaryPath, string dllName)
        {
            // 先尝试主路径
            string primaryDllPath = Path.Combine(primaryPath, dllName);
            if (File.Exists(primaryDllPath))
            {
                return primaryDllPath;
            }

            // 尝试开发时的备用路径
            string[] fallbackPaths = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", DriverName)),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "src", DriverName)),
                Path.Combine(AppContext.BaseDirectory, DriverName),
            };

            foreach (var fallback in fallbackPaths)
            {
                var testPath = Path.Combine(fallback, dllName);
                _log.Information("[CefDispatcher]   尝试: {TestPath}", testPath);
                if (File.Exists(testPath))
                {
                    DriverPath = fallback;
                    _log.Information("[CefDispatcher]   已找到! 更新DriverPath为: {FallbackPath}", fallback);
                    return testPath;
                }
            }

            throw new FileNotFoundException(
                $"未找到驱动DLL: {dllName}\n" +
                $"已搜索路径:\n" +
                $"  主路径: {primaryDllPath}\n" +
                $"  备用路径: {string.Join("\n  ", fallbackPaths)}");
        }

        /// <summary>
        /// 在程序集中查找IBrowserFactory实现
        /// </summary>
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

        /// <summary>
        /// 程序集解析处理器 - 将托管DLL加载重定向到驱动目录
        /// 当.NET无法在默认位置找到程序集时调用此方法
        /// </summary>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
        {
            var assemblyName = new AssemblyName(e.Name);
            string dllName = assemblyName.Name + ".dll";
            string dllPath = Path.Combine(DriverPath, dllName);

            _log.Information("[AssemblyResolve] 正在解析: {AssemblyName}", assemblyName.Name);
            _log.Information("[AssemblyResolve]   在以下路径查找: {DriverPath}", DriverPath);

            if (File.Exists(dllPath))
            {
                _log.Information("[AssemblyResolve]   已找到: {DllPath}", dllPath);
                return Assembly.LoadFrom(dllPath);
            }

            _log.Information("[AssemblyResolve]   未找到");
            return null;
        }
    }
}
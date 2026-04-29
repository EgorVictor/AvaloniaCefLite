namespace MyBrowser
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Dispatches to the appropriate browser driver based on OS version.
    /// Uses reflection to load driver DLLs at runtime - NO compile-time dependencies.
    /// </summary>
    public static class CefDispatcher
    {
        private static IBrowserFactory _factory;
        
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        public static string DriverType => _factory?.DriverType ?? "Unknown";
        public static string Version => _factory?.Version ?? "Unknown";
        public static string DriverPath { get; private set; }
        public static string DriverName { get; private set; }

        /// <summary>
        /// Shutdown the dispatcher and release resources.
        /// </summary>
        public static void Shutdown()
        {
            _factory?.Shutdown();
            _factory = null;
        }

        /// <summary>
        /// Boot the dispatcher and return a browser factory.
        /// Must be called BEFORE any UI initialization.
        /// </summary>
        public static IBrowserFactory Boot()
        {
            // 1. Detect OS version (Win7/8=Legacy, Win10+=Modern)
            var osVersion = Environment.OSVersion.Version;
            bool isLegacy = osVersion.Major < 10;
            
            DriverName = isLegacy ? "Legacy" : "Modern";
            DriverPath = Path.Combine(AppContext.BaseDirectory, "Runtimes", DriverName);

            Console.WriteLine($"[CefDispatcher] OS Version: {osVersion}");
            Console.WriteLine($"[CefDispatcher] Selected Driver: {DriverName}");
            Console.WriteLine($"[CefDispatcher] Driver Path: {DriverPath}");

            // 2. Inject Native DLL search path (fix libcef.dll loading)
            if (!string.IsNullOrEmpty(DriverPath) && Directory.Exists(DriverPath))
            {
                SetDllDirectory(DriverPath);
            }

            // 3. Hook AssemblyResolve to handle managed DLLs
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            // 4. Load the driver assembly via reflection
            string driverDllName = $"MyBrowser.Driver.{DriverName}.dll";
            string driverDllPath = Path.Combine(DriverPath, driverDllName);

            if (!File.Exists(driverDllPath))
            {
                // Fallback: look in parent directories (for development)
                var devPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", DriverName));
                if (File.Exists(Path.Combine(devPath, driverDllName)))
                {
                    driverDllPath = Path.Combine(devPath, driverDllName);
                    DriverPath = devPath;
                }
                else
                {
                    throw new FileNotFoundException($"Driver DLL not found: {driverDllPath}");
                }
            }

            Console.WriteLine($"[CefDispatcher] Loading: {driverDllPath}");

            var assembly = Assembly.LoadFrom(driverDllPath);

            // 5. Find IBrowserFactory implementation
            Type factoryType = null;
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IBrowserFactory).IsAssignableFrom(type) && !type.IsInterface)
                {
                    factoryType = type;
                    break;
                }
            }

            if (factoryType == null)
            {
                throw new TypeLoadException($"No IBrowserFactory implementation found in {driverDllPath}");
            }

            Console.WriteLine($"[CefDispatcher] Factory: {factoryType.FullName}");

            // 6. Create factory instance
            _factory = (IBrowserFactory)Activator.CreateInstance(factoryType);
            return _factory;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
        {
            var assemblyName = new AssemblyName(e.Name);
            string dllName = assemblyName.Name + ".dll";
            string dllPath = Path.Combine(DriverPath, dllName);

            if (File.Exists(dllPath))
            {
                return Assembly.LoadFrom(dllPath);
            }

            return null;
        }
    }
}
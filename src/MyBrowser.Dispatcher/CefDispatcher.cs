namespace MyBrowser
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Dispatches to the appropriate browser driver based on OS version.
    /// Uses reflection to load driver DLLs at runtime - NO compile-time dependencies.
    /// 
    /// Path Hijacking Strategy:
    /// 1. SetDllDirectory: Redirects native DLL (libcef.dll) loading to subdirectory
    /// 2. AssemblyResolve: Redirects managed DLL (CefGlue.dll) loading to subdirectory
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
        /// 
        /// This method performs path hijacking:
        /// 1. Detects OS version to select driver (Legacy for Win7, Modern for Win10+)
        /// 2. Calls SetDllDirectory to redirect native DLL loading
        /// 3. Hooks AssemblyResolve to redirect managed DLL loading
        /// 4. Loads driver DLL via reflection
        /// </summary>
        public static IBrowserFactory Boot()
        {
            return BootInternal(Environment.OSVersion.Version);
        }

        /// <summary>
        /// Internal boot with OS version parameter (for testing).
        /// </summary>
        internal static IBrowserFactory BootInternal(Version osVersion)
        {
            // 1. Detect OS version (Win7/8=Legacy, Win10+=Modern)
            bool isLegacy = osVersion.Major < 10;
            
            DriverName = isLegacy ? "Legacy" : "Modern";
            DriverPath = Path.Combine(AppContext.BaseDirectory, "Runtimes", DriverName);

            Console.WriteLine("===========================================");
            Console.WriteLine("[CefDispatcher] Browser Driver Dispatcher");
            Console.WriteLine("===========================================");
            Console.WriteLine($"[CefDispatcher] OS Version: {osVersion}");
            Console.WriteLine($"[CefDispatcher] OS Major: {osVersion.Major}");
            Console.WriteLine($"[CefDispatcher] Selected Driver: {DriverName}");
            Console.WriteLine($"[CefDispatcher] Driver Path: {DriverPath}");

            // 2. SetDllDirectory - Native DLL search path hijacking
            // This is CRITICAL for libcef.dll loading
            Console.WriteLine("[CefDispatcher] Step 1: SetDllDirectory()");
            if (!string.IsNullOrEmpty(DriverPath) && Directory.Exists(DriverPath))
            {
                bool result = SetDllDirectory(DriverPath);
                Console.WriteLine($"[CefDispatcher]   SetDllDirectory({DriverPath}) = {result}");
                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"[CefDispatcher]   WARNING: SetDllDirectory failed, error={error}");
                }
            }
            else
            {
                Console.WriteLine($"[CefDispatcher]   WARNING: Driver path does not exist: {DriverPath}");
                Console.WriteLine($"[CefDispatcher]   Will use fallback path discovery...");
            }

            // 3. Hook AssemblyResolve - Managed DLL search path hijacking
            // This is CRITICAL for CefGlue.dll loading
            Console.WriteLine("[CefDispatcher] Step 2: Hook AssemblyResolve");
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            Console.WriteLine("[CefDispatcher]   AssemblyResolve handler registered");

            // 4. Find driver DLL path
            Console.WriteLine("[CefDispatcher] Step 3: Locate driver DLL");
            string driverDllName = $"MyBrowser.Driver.{DriverName}.dll";
            string driverDllPath = FindDriverDll(DriverPath, driverDllName);

            Console.WriteLine($"[CefDispatcher]   Driver DLL: {driverDllPath}");

            // 5. Load the driver assembly via reflection
            Console.WriteLine("[CefDispatcher] Step 4: Load driver assembly");
            var assembly = Assembly.LoadFrom(driverDllPath);
            Console.WriteLine($"[CefDispatcher]   Assembly loaded: {assembly.FullName}");

            // 6. Find IBrowserFactory implementation
            Console.WriteLine("[CefDispatcher] Step 5: Find IBrowserFactory");
            Type factoryType = FindFactoryType(assembly);

            Console.WriteLine($"[CefDispatcher]   Factory type: {factoryType.FullName}");

            // 7. Create factory instance
            Console.WriteLine("[CefDispatcher] Step 6: Create factory instance");
            _factory = (IBrowserFactory)Activator.CreateInstance(factoryType);
            Console.WriteLine($"[CefDispatcher]   Factory created: {_factory.GetType().Name}");
            Console.WriteLine("===========================================");

            return _factory;
        }

        /// <summary>
        /// Find driver DLL, checking multiple possible locations.
        /// </summary>
        private static string FindDriverDll(string primaryPath, string dllName)
        {
            // Try primary path first
            string primaryDllPath = Path.Combine(primaryPath, dllName);
            if (File.Exists(primaryDllPath))
            {
                return primaryDllPath;
            }

            // Try development fallback paths
            string[] fallbackPaths = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", DriverName)),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "src", DriverName)),
                Path.Combine(AppContext.BaseDirectory, DriverName),
            };

            foreach (var fallback in fallbackPaths)
            {
                var testPath = Path.Combine(fallback, dllName);
                Console.WriteLine($"[CefDispatcher]   Trying: {testPath}");
                if (File.Exists(testPath))
                {
                    DriverPath = fallback;
                    Console.WriteLine($"[CefDispatcher]   Found! Updated DriverPath to: {fallback}");
                    return testPath;
                }
            }

            throw new FileNotFoundException(
                $"Driver DLL not found: {dllName}\n" +
                $"Searched paths:\n" +
                $"  Primary: {primaryDllPath}\n" +
                $"  Fallbacks: {string.Join("\n  ", fallbackPaths)}");
        }

        /// <summary>
        /// Find IBrowserFactory implementation in assembly.
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
                    $"No IBrowserFactory implementation found in {assembly.Location}\n" +
                    $"Available types: {string.Join(", ", assembly.GetTypes().Select(t => t.FullName))}");
            }

            return factoryType;
        }

        /// <summary>
        /// Assembly resolve handler - redirects managed DLL loading to driver directory.
        /// This is called when .NET can't find an assembly in the default locations.
        /// </summary>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
        {
            var assemblyName = new AssemblyName(e.Name);
            string dllName = assemblyName.Name + ".dll";
            string dllPath = Path.Combine(DriverPath, dllName);

            Console.WriteLine($"[AssemblyResolve] Resolving: {assemblyName.Name}");
            Console.WriteLine($"[AssemblyResolve]   Looking in: {DriverPath}");

            if (File.Exists(dllPath))
            {
                Console.WriteLine($"[AssemblyResolve]   FOUND: {dllPath}");
                return Assembly.LoadFrom(dllPath);
            }

            Console.WriteLine($"[AssemblyResolve]   NOT FOUND");
            return null;
        }
    }
}
namespace MyBrowser.Interop
{
    using System;
    using System.IO;

    /// <summary>
    /// 统一日志助手
    /// 每次运行前删除旧日志文件
    /// </summary>
    public static class Logger
    {
        private static readonly string _logFile = @"F:\mybrowser.log";
        private static readonly object _lock = new object();

        static Logger()
        {
            // 每次程序启动时删除旧日志
            try
            {
                if (File.Exists(_logFile))
                {
                    File.Delete(_logFile);
                }
            }
            catch
            {
                // 忽略删除失败
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        public static void Log(string msg)
        {
            try
            {
                var s = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}";
                System.Diagnostics.Debug.WriteLine(s);
                lock (_lock)
                {
                    File.AppendAllText(_logFile, s + "\r\n");
                }
            }
            catch
            {
                // 忽略日志写入失败
            }
        }

        /// <summary>
        /// 记录格式化日志
        /// </summary>
        public static void Log(string fmt, params object[] args)
        {
            Log(string.Format(fmt, args));
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(string msg, Exception ex = null)
        {
            if (ex != null)
            {
                Log($"[ERROR] {msg}: {ex.Message}\n{ex.StackTrace}");
            }
            else
            {
                Log($"[ERROR] {msg}");
            }
        }
    }
}
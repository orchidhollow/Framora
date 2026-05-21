using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Framora.Utilities;

/// <summary>
/// 日志级别。
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}

/// <summary>
/// 线程安全的文件 + 控制台日志记录器。
/// 默认在应用基础目录下的 <c>Logs</c> 文件夹里创建带时间戳的日志文件。
/// </summary>
public static class Logger
{
    private static readonly object _lock = new();
    private static string? _logFilePath;
    private static string? _logsFolder;

    /// <summary>
    /// 获取当前日志文件路径。
    /// </summary>
    public static string? CurrentLogFilePath
    {
        get
        {
            lock (_lock)
            {
                return _logFilePath;
            }
        }
    }

    /// <summary>
    /// 判断日志器是否已经初始化。
    /// </summary>
    public static bool IsInitialized
    {
        get
        {
            lock (_lock)
            {
                return !string.IsNullOrWhiteSpace(_logFilePath);
            }
        }
    }

    /// <summary>
    /// 初始化日志记录器。如果 <paramref name="logsFolder"/> 为 null，则使用应用基础目录下的 <c>Logs</c> 文件夹。
    /// </summary>
    public static void Initialize(string? logsFolder = null)
    {
        try
        {
            string? logFilePath;

            lock (_lock)
            {
                if (!string.IsNullOrWhiteSpace(_logFilePath))
                {
                    return;
                }

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _logsFolder = string.IsNullOrWhiteSpace(logsFolder)
                    ? Path.Combine(baseDir, "Logs")
                    : logsFolder;

                Directory.CreateDirectory(_logsFolder);
                var fileName = $"Framora_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                _logFilePath = Path.Combine(_logsFolder, fileName);
                logFilePath = _logFilePath;
            }

            WriteInternal(LogLevel.Info, $"日志记录器已初始化，日志文件：{logFilePath}");
        }
        catch (Exception ex)
        {
            // 最后手段输出到控制台，避免日志系统本身导致应用崩溃。
            Console.Error.WriteLine("Logger 初始化失败: " + ex);
        }
    }

    public static void Trace(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => WriteInternal(LogLevel.Trace, message, null, memberName, filePath, lineNumber);

    public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => WriteInternal(LogLevel.Debug, message, null, memberName, filePath, lineNumber);

    public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => WriteInternal(LogLevel.Info, message, null, memberName, filePath, lineNumber);

    public static void Warn(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => WriteInternal(LogLevel.Warn, message, null, memberName, filePath, lineNumber);

    public static void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => WriteInternal(LogLevel.Error, message, null, memberName, filePath, lineNumber);

    public static void Error(Exception ex, string? message = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => WriteInternal(LogLevel.Error, message ?? ex.Message, ex, memberName, filePath, lineNumber);

    public static void Fatal(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => WriteInternal(LogLevel.Fatal, message, null, memberName, filePath, lineNumber);

    public static void Fatal(Exception ex, string? message = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => WriteInternal(LogLevel.Fatal, message ?? ex.Message, ex, memberName, filePath, lineNumber);

    private static void WriteInternal(LogLevel level, string message, Exception? exception = null, string memberName = "", string filePath = "", int lineNumber = 0)
    {
        var now = DateTime.Now;
        var source = string.IsNullOrWhiteSpace(filePath)
            ? memberName
            : $"{Path.GetFileName(filePath)}:{lineNumber} {memberName}";

        var text = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        var line = $"[{now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{source}] {text}";

        // 先输出到控制台，便于调试时即时查看。
        if (level is LogLevel.Error or LogLevel.Fatal)
            Console.Error.WriteLine(line);
        else
            Console.WriteLine(line);

        // 再追加到日志文件中。
        try
        {
            lock (_lock)
            {
                EnsureLogFilePathLocked();

                if (string.IsNullOrWhiteSpace(_logFilePath))
                {
                    return;
                }

                File.AppendAllText(_logFilePath, line + Environment.NewLine, new UTF8Encoding(false));
            }
        }
        catch
        {
            // 忽略文件写入错误，避免因日志导致应用崩溃。
        }
    }

    private static void EnsureLogFilePathLocked()
    {
        if (!string.IsNullOrWhiteSpace(_logFilePath))
        {
            return;
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _logsFolder ??= Path.Combine(baseDir, "Logs");
        Directory.CreateDirectory(_logsFolder);

        var fileName = $"Framora_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        _logFilePath = Path.Combine(_logsFolder, fileName);
    }
}


using System;
using System.IO;
using System.Runtime.InteropServices;
using FFMpegCore;
using Framora.Utilities;

namespace Framora.Services;

/// <summary>
/// FFmpeg 跨平台服务，负责定位 ffmpeg 二进制并初始化 FFMpegCore
/// </summary>
public static class FFmpegService
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            Logger.Debug("FFmpeg 已经初始化");
            return;
        }

        Logger.Info("正在解析 FFmpeg 二进制路径");

        var ffmpegPath = GetFFmpegBinaryPath();
        Logger.Debug($"项目候选 FFmpeg 路径: {ffmpegPath}");

        // 如果项目内没有提供二进制，尝试从系统 PATH 找到 ffmpeg 可执行文件
        if (!File.Exists(ffmpegPath))
        {
            Logger.Warn($"未在项目路径找到 FFmpeg 二进制: {ffmpegPath}");
            var pathFromEnv = FindFfmpegInPath();
            if (!string.IsNullOrEmpty(pathFromEnv) && File.Exists(pathFromEnv))
            {
                ffmpegPath = pathFromEnv;
                Logger.Info($"使用系统 PATH 中的 FFmpeg: {ffmpegPath}");
            }
        }

        if (!File.Exists(ffmpegPath))
        {
            // 不抛出致命异常，抛出后将中断应用启动；这里抛出以便尽早发现问题
            Logger.Error($"未找到 FFmpeg 二进制，路径: {ffmpegPath}");
            throw new FileNotFoundException($"FFmpeg 二进制未找到，路径: {ffmpegPath}", ffmpegPath);
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Logger.Debug($"尝试设置 FFmpeg 可执行权限: {ffmpegPath}");
            TrySetUnixExecutablePermission(ffmpegPath);
        }

        GlobalFFOptions.Configure(options =>
        {
            options.BinaryFolder = Path.GetDirectoryName(ffmpegPath)!;
            options.TemporaryFilesFolder = Path.GetTempPath();
        });

        _initialized = true;
        Logger.Info($"FFmpeg 初始化成功。二进制目录: {Path.GetDirectoryName(ffmpegPath)}");
    }

    /// <summary>
    /// 尝试初始化 FFmpeg，如果失败返回 false 并输出错误信息（便于 UI 层友好提示）。
    /// </summary>
    public static bool TryInitialize(out string? errorMessage)
    {
        try
        {
            Initialize();
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Logger.Error(ex, "FFmpeg 初始化失败");
            return false;
        }
    }

    private static string GetFFmpegBinaryPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var ffmpegDir = Path.Combine(baseDir, "FFmpeg");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(ffmpegDir, "win-x64", "ffmpeg.exe");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => Path.Combine(ffmpegDir, "osx-arm64", "ffmpeg"),
                Architecture.X64 => Path.Combine(ffmpegDir, "osx-x64", "ffmpeg"),
                _ => throw new PlatformNotSupportedException($"Unsupported macOS architecture: {RuntimeInformation.OSArchitecture}")
            };
        }

        // 默认未支持 Linux（可扩展）
        throw new PlatformNotSupportedException("Linux is not supported by FFmpegService yet.");
    }

    private static void TrySetUnixExecutablePermission(string path)
    {
        try
        {
            var p = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            p.Start();
            p.WaitForExit();

            if (p.ExitCode == 0)
            {
                Logger.Debug($"chmod +x 成功，FFmpeg: {path}");
            }
            else
            {
                Logger.Warn($"chmod +x 返回退出码 {p.ExitCode}，FFmpeg: {path}");
            }
        }
        catch
        {
            // 忽略任何 chmod 错误，但保留日志记录，以便诊断权限问题。
            Logger.Warn($"设置可执行权限失败，FFmpeg: {path}");
        }
    }

    private static string? FindFfmpegInPath()
    {
        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var parts = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            try
            {
                var candidate = Path.Combine(part, fileName);
                if (File.Exists(candidate))
                {
                    Logger.Debug($"在 PATH 中找到 FFmpeg: {candidate}");
                    return candidate;
                }
            }
            catch
            {
                // ignore malformed PATH entries
            }
        }
        return null;
    }
}

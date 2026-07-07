using System;
using System.IO;
using System.Runtime.InteropServices;
using FFMpegCore;
using Framora.Utilities;

namespace Framora.Services;

/// <summary>
/// FFmpeg 跨平台服务（静态类），负责定位 ffmpeg 二进制并初始化 FFMpegCore 全局配置。
/// 支持从项目内嵌路径或系统 PATH 中查找 FFmpeg。
/// 支持平台：Windows x64、macOS x64、macOS ARM64。Linux 暂未实现。
/// </summary>
public static class FFmpegService
{
    private static bool _initialized;

    /// <summary>
    /// 初始化 FFmpeg 运行时。同时验证 ffmpeg 和 ffprobe 二进制均存在。
    /// 按以下优先级查找：
    /// 1. 应用目录下 FFmpeg/{platform}/（内嵌模式）
    /// 2. 系统 PATH 环境变量（两个二进制必须在同一目录才认可）
    /// 若任一二进制缺失，抛出 <see cref="FileNotFoundException"/>。
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            Logger.Debug("FFmpeg 已经初始化");
            return;
        }

        Logger.Info("正在解析 FFmpeg/FFProbe 二进制路径");

        var binDir = GetFFmpegBinaryDirectory();
        Logger.Debug($"项目候选二进制目录: {binDir}");

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var ffmpegName  = isWindows ? "ffmpeg.exe"  : "ffmpeg";
        var ffprobeName = isWindows ? "ffprobe.exe" : "ffprobe";

        var ffmpegPath  = Path.Combine(binDir, ffmpegName);
        var ffprobePath = Path.Combine(binDir, ffprobeName);

        // 优先检查内嵌目录，若缺失则回退到 PATH 搜索
        if (!File.Exists(ffmpegPath) || !File.Exists(ffprobePath))
        {
            Logger.Warn($"内嵌二进制目录不完整: ffmpeg={File.Exists(ffmpegPath)}, ffprobe={File.Exists(ffprobePath)}");
            var (pathFfmpeg, pathFfprobe) = FindBinariesInPath(ffmpegName, ffprobeName);

            if (pathFfmpeg != null && pathFfprobe != null)
            {
                ffmpegPath  = pathFfmpeg;
                ffprobePath = pathFfprobe;
                Logger.Info($"使用系统 PATH 中的二进制: {Path.GetDirectoryName(ffmpegPath)}");
            }
            else
            {
                // 明确报告哪个二进制缺失，便于排查
                if (!File.Exists(ffmpegPath))
                    Logger.Error($"未找到 ffmpeg 二进制，路径: {ffmpegPath}");
                if (!File.Exists(ffprobePath))
                    Logger.Error($"未找到 ffprobe 二进制，路径: {ffprobePath}");

                throw new FileNotFoundException(
                    $"FFmpeg 二进制不完整，缺少: ffmpeg={File.Exists(ffmpegPath)}, ffprobe={File.Exists(ffprobePath)}，目录: {binDir}",
                    ffmpegPath);
            }
        }

        if (!isWindows)
        {
            Logger.Debug($"尝试设置可执行权限: {ffmpegPath}, {ffprobePath}");
            TrySetUnixExecutablePermission(ffmpegPath);
            TrySetUnixExecutablePermission(ffprobePath);
        }

        var resolvedDir = Path.GetDirectoryName(ffmpegPath)!;
        GlobalFFOptions.Configure(options =>
        {
            options.BinaryFolder = resolvedDir;
            options.TemporaryFilesFolder = Path.GetTempPath();
        });

        _initialized = true;
        Logger.Info($"FFmpeg 初始化成功。二进制目录: {resolvedDir}");
    }

    /// <summary>
    /// 安全地尝试初始化 FFmpeg。失败时返回 false 并输出错误信息，
    /// 便于 UI 层向用户展示友好提示，而非抛出未捕获异常。
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

    /// <summary>
    /// 根据当前操作系统和 CPU 架构，返回项目内嵌的 FFmpeg 二进制所在目录。
    /// 路径格式：{AppBaseDir}/FFmpeg/{platform}/
    /// </summary>
    private static string GetFFmpegBinaryDirectory()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var ffmpegDir = Path.Combine(baseDir, "FFmpeg");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(ffmpegDir, "win-x64");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => Path.Combine(ffmpegDir, "osx-arm64"),
                Architecture.X64   => Path.Combine(ffmpegDir, "osx-x64"),
                _ => throw new PlatformNotSupportedException($"Unsupported macOS architecture: {RuntimeInformation.OSArchitecture}")
            };
        }

        throw new PlatformNotSupportedException("Linux is not supported by FFmpegService yet.");
    }

    /// <summary>
    /// 在 Unix 系统上为 FFmpeg 二进制文件设置可执行权限（chmod +x）。
    /// 失败时静默处理，仅记录警告日志。
    /// </summary>
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

    /// <summary>
    /// 在系统 PATH 环境变量的所有目录中，同时搜索 ffmpeg 和 ffprobe。
    /// 仅当两者在同一目录均存在时才返回结果。
    /// </summary>
    private static (string? ffmpeg, string? ffprobe) FindBinariesInPath(string ffmpegName, string ffprobeName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var parts = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            try
            {
                var ffmpegCandidate  = Path.Combine(part, ffmpegName);
                var ffprobeCandidate = Path.Combine(part, ffprobeName);

                if (File.Exists(ffmpegCandidate) && File.Exists(ffprobeCandidate))
                {
                    Logger.Debug($"在 PATH 中找到 FFmpeg 二进制: {part}");
                    return (ffmpegCandidate, ffprobeCandidate);
                }
            }
            catch
            {
                // ignore malformed PATH entries
            }
        }

        return (null, null);
    }
}

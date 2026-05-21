using System;
using System.IO;
using System.Runtime.InteropServices;
using FFMpegCore;

namespace Framora.Services;

/// <summary>
/// FFmpeg 跨平台服务，负责定位 ffmpeg 二进制并初始化 FFMpegCore
/// </summary>
public static class FFmpegService
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;

        var ffmpegPath = GetFFmpegBinaryPath();

        if (!File.Exists(ffmpegPath))
        {
            // 不抛出致命异常，抛出后将中断应用启动；这里抛出以便尽早发现问题
            throw new FileNotFoundException($"FFmpeg binary not found at: {ffmpegPath}", ffmpegPath);
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TrySetUnixExecutablePermission(ffmpegPath);
        }

        GlobalFFOptions.Configure(options =>
        {
            options.BinaryFolder = Path.GetDirectoryName(ffmpegPath)!;
            options.TemporaryFilesFolder = Path.GetTempPath();
        });

        _initialized = true;
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
        }
        catch
        {
            // 忽略任何 chmod 错误
        }
    }
}

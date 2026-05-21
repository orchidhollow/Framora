using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using Framora.Models;
using Framora.Utilities;

namespace Framora.Services;

public class FrameExtractorService : IFrameExtractor
{
    public async Task<VideoInfo> GetVideoInfoAsync(string videoPath, CancellationToken ct = default)
    {
        Logger.Debug($"读取视频信息: {videoPath}");
        var info = await FFProbe.AnalyseAsync(videoPath);
        var videoInfo = new VideoInfo
        {
            Duration = info.Duration,
            Width = info.PrimaryVideoStream?.Width ?? 0,
            Height = info.PrimaryVideoStream?.Height ?? 0,
            OriginalFps = info.PrimaryVideoStream?.FrameRate ?? 0
        };

        Logger.Info($"视频信息加载完成: 时长={videoInfo.Duration}, 分辨率={videoInfo.Width}x{videoInfo.Height}, 帧率={videoInfo.OriginalFps}");
        return videoInfo;
    }

    public async Task ExtractFramesAsync(string inputVideoPath, string outputDirectory, double fps = 12, string outputFormat = "png", IProgress<(int current,int total)>? progress = null, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);

            Logger.Info($"开始提取帧: 输入={inputVideoPath}, 输出={outputDirectory}, fps={fps}, 格式={outputFormat}");

            var mediaInfo = await FFProbe.AnalyseAsync(inputVideoPath);
            var duration = mediaInfo.Duration;
            var totalFrames = (int)Math.Ceiling(duration.TotalSeconds * fps);

            Logger.Debug($"预计总帧数: {totalFrames}");

            var outputPattern = Path.Combine(outputDirectory, $"frame_%04d.{outputFormat}");

            var args = FFMpegArguments
                .FromFileInput(inputVideoPath)
                .OutputToFile(outputPattern, overwrite: true, options => options
                    .WithVideoCodec(outputFormat == "png" ? "png" : "mjpeg")
                    .WithFramerate((int)Math.Round(fps)));

            await args.ProcessAsynchronously();

            progress?.Report((totalFrames, totalFrames));
            Logger.Info($"帧提取完成，输出模式: {outputPattern}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"帧提取失败: 输入={inputVideoPath}, 输出={outputDirectory}");
            throw;
        }
    }
}

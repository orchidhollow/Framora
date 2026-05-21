using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using Framora.Models;

namespace Framora.Services;

public class FrameExtractorService : IFrameExtractor
{
    public async Task<VideoInfo> GetVideoInfoAsync(string videoPath, CancellationToken ct = default)
    {
        var info = await FFProbe.AnalyseAsync(videoPath);
        return new VideoInfo
        {
            Duration = info.Duration,
            Width = info.PrimaryVideoStream?.Width ?? 0,
            Height = info.PrimaryVideoStream?.Height ?? 0,
            OriginalFps = info.PrimaryVideoStream?.FrameRate ?? 0
        };
    }

    public async Task ExtractFramesAsync(string inputVideoPath, string outputDirectory, double fps = 12, string outputFormat = "png", IProgress<(int current,int total)>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var mediaInfo = await FFProbe.AnalyseAsync(inputVideoPath);
        var duration = mediaInfo.Duration;
        var totalFrames = (int)Math.Ceiling(duration.TotalSeconds * fps);

        var outputPattern = Path.Combine(outputDirectory, $"frame_%04d.{outputFormat}");


        var args = FFMpegArguments
            .FromFileInput(inputVideoPath)
            .OutputToFile(outputPattern, overwrite: true, options => options
                .WithVideoCodec(outputFormat == "png" ? "png" : "mjpeg")
                .WithFramerate((int)Math.Round(fps)));

        await args.ProcessAsynchronously();

        progress?.Report((totalFrames, totalFrames));
    }
}

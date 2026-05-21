using System;
using System.Threading;
using System.Threading.Tasks;
using Framora.Models;

namespace Framora.Services;

public interface IFrameExtractor
{
    Task<VideoInfo> GetVideoInfoAsync(string videoPath, CancellationToken ct = default);

    Task ExtractFramesAsync(string inputVideoPath, string outputDirectory, double fps = 12, string outputFormat = "png", IProgress<(int current,int total)>? progress = null, CancellationToken ct = default);
}

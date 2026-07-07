using System;
using System.Threading;
using System.Threading.Tasks;
using Framora.Models;

namespace Framora.Services;

/// <summary>
/// 帧提取服务接口。定义视频信息探测与帧提取的核心契约，
/// 便于通过依赖注入替换实现（如用于单元测试 Mock）。
/// </summary>
public interface IFrameExtractor
{
    /// <summary>
    /// 异步探测视频文件的元数据（时长、分辨率、原始帧率）。
    /// </summary>
    Task<VideoInfo> GetVideoInfoAsync(string videoPath, CancellationToken ct = default);

    /// <summary>
    /// 异步从视频中提取帧序列并保存为图片文件。
    /// </summary>
    /// <param name="inputVideoPath">输入视频文件的完整路径。</param>
    /// <param name="outputDirectory">输出图片的目录路径，不存在时自动创建。</param>
    /// <param name="fps">提取帧率，默认 12fps。</param>
    /// <param name="outputFormat">输出图片格式，支持 "png" 和 "mjpeg"。</param>
    /// <param name="progress">进度回调，报告 (当前帧数, 总帧数)。</param>
    /// <param name="ct">取消令牌，支持中途取消提取操作。</param>
    Task ExtractFramesAsync(string inputVideoPath, string outputDirectory, double fps = 12, string outputFormat = "png", IProgress<(int current,int total)>? progress = null, CancellationToken ct = default);
}

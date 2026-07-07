using System;

namespace Framora.Models;

/// <summary>
/// 视频文件的元数据信息，由 FFProbe 探测得到。
/// </summary>
public class VideoInfo
{
    /// <summary>视频总时长。</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>水平分辨率（像素）。</summary>
    public int Width { get; set; }

    /// <summary>垂直分辨率（像素）。</summary>
    public int Height { get; set; }

    /// <summary>原始帧率（FPS）。</summary>
    public double OriginalFps { get; set; }

    public override string ToString() => $"{Width}x{Height}, {Duration:mm\\:ss}, {OriginalFps:F1} fps";
}

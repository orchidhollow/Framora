namespace Framora.Models;

/// <summary>
/// 帧提取操作的配置参数。
/// </summary>
public class ExtractionSettings
{
    /// <summary>输入视频文件的完整路径。</summary>
    public string InputVideoPath { get; set; } = string.Empty;

    /// <summary>输出图片的目标目录路径。</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>目标提取帧率，默认 12fps。</summary>
    public double Fps { get; set; } = 12;

    /// <summary>输出图片格式，支持 "png" 和 "mjpeg"，默认 "png"。</summary>
    public string OutputFormat { get; set; } = "png";
}

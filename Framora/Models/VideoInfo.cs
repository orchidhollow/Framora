using System;

namespace Framora.Models;

public class VideoInfo
{
    public TimeSpan Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double OriginalFps { get; set; }

    public override string ToString() => $"{Width}x{Height}, {Duration:mm\\:ss}, {OriginalFps:F1} fps";
}

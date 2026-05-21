namespace Framora.Models;

public class ExtractionSettings
{
    public string InputVideoPath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public double Fps { get; set; } = 12;
    public string OutputFormat { get; set; } = "png";
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Framora.Models;
using Framora.Services;
using Framora.Utilities;

namespace Framora.ViewModels;

/// <summary>
/// 主窗口的 ViewModel，驱动视频帧提取的完整交互流程。
/// 通过依赖注入获取 <see cref="IFrameExtractor"/> 服务。
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFrameExtractor _extractor;

    public MainWindowViewModel(IFrameExtractor extractor)
    {
        _extractor = extractor;

        // 在运行目录下创建默认的输入/输出文件夹
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var inputDir  = Path.Combine(baseDir, "VideoInput");
        var outputDir = Path.Combine(baseDir, "Output");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);
        _inputVideoPath  = inputDir;
        _outputDirectory = outputDir;

        Logger.Debug("MainWindowViewModel 已创建");
    }

    /// <summary>
    /// 供 XAML 设计器使用的无参构造函数，运行时不应调用。
    /// </summary>
    internal MainWindowViewModel() : this(new FrameExtractorService()) { }

    /// <summary>可用的输出格式列表，供 ComboBox 绑定。</summary>
    public string[] OutputFormats { get; } = ["png", "mjpeg"];

    // ────────────────────────── 可绑定属性 ──────────────────────────

    /// <summary>输入视频文件路径。</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadVideoInfoCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExtractFramesCommand))]
    private string _inputVideoPath = string.Empty;

    /// <summary>输出图片的目标目录。</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExtractFramesCommand))]
    private string _outputDirectory = string.Empty;

    /// <summary>目标提取帧率，默认 12fps。</summary>
    [ObservableProperty]
    private double _fps = 12;

    /// <summary>输出图片格式，"png" 或 "mjpeg"。</summary>
    [ObservableProperty]
    private string _outputFormat = "png";

    /// <summary>视频元数据展示文本（分辨率、时长、帧率）。</summary>
    [ObservableProperty]
    private string _videoInfoText = string.Empty;

    /// <summary>状态提示文本。</summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>提取进度当前值。</summary>
    [ObservableProperty]
    private int _progressCurrent;

    /// <summary>提取进度总值。</summary>
    [ObservableProperty]
    private int _progressTotal;

    /// <summary>是否正在提取中，用于禁用 UI 操作和显示取消按钮。</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelExtractingCommand))]
    private bool _isExtracting;

    private CancellationTokenSource? _extractCts;

    // ────────────────────────── 命令 ──────────────────────────

    /// <summary>
    /// 探测当前视频文件的元信息并更新 <see cref="VideoInfoText"/>。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLoadVideoInfo))]
    private async Task LoadVideoInfoAsync()
    {
        try
        {
            StatusText = "正在读取视频信息…";
            Logger.Info($"开始探测视频: {InputVideoPath}");

            var info = await _extractor.GetVideoInfoAsync(InputVideoPath);
            VideoInfoText = info.ToString();
            StatusText = "视频信息读取完成";

            Logger.Info($"视频信息: {VideoInfoText}");
        }
        catch (Exception ex)
        {
            VideoInfoText = string.Empty;
            StatusText = $"读取失败: {ex.Message}";
            Logger.Error(ex, "读取视频信息失败");
        }
    }

    private bool CanLoadVideoInfo() =>
        !IsExtracting && !string.IsNullOrWhiteSpace(InputVideoPath);

    /// <summary>
    /// 执行帧提取操作。支持通过 <see cref="CancelExtractingCommand"/> 取消。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExtractFrames))]
    private async Task ExtractFramesAsync()
    {
        _extractCts = new CancellationTokenSource();

        try
        {
            IsExtracting = true;
            StatusText = "正在提取帧…";
            ProgressCurrent = 0;
            ProgressTotal = 0;

            Logger.Info($"开始提取: 输入={InputVideoPath}, 输出={OutputDirectory}, FPS={Fps}, 格式={OutputFormat}");

            var progress = new Progress<(int current, int total)>(report =>
            {
                ProgressCurrent = report.current;
                ProgressTotal = report.total;
            });

            await _extractor.ExtractFramesAsync(
                InputVideoPath,
                OutputDirectory,
                Fps,
                OutputFormat,
                progress,
                _extractCts.Token);

            StatusText = $"提取完成，共 {ProgressTotal} 帧";
            Logger.Info(StatusText);
        }
        catch (OperationCanceledException)
        {
            StatusText = "提取已取消";
            Logger.Warn("用户取消了帧提取");
        }
        catch (Exception ex)
        {
            StatusText = $"提取失败: {ex.Message}";
            Logger.Error(ex, "帧提取异常");
        }
        finally
        {
            _extractCts.Dispose();
            _extractCts = null;
            IsExtracting = false;
        }
    }

    private bool CanExtractFrames() =>
        !IsExtracting
        && !string.IsNullOrWhiteSpace(InputVideoPath)
        && !string.IsNullOrWhiteSpace(OutputDirectory);

    /// <summary>
    /// 取消正在进行的帧提取。
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsExtracting))]
    private void CancelExtracting()
    {
        _extractCts?.Cancel();
        StatusText = "正在取消…";
        Logger.Info("发送取消请求");
    }
}

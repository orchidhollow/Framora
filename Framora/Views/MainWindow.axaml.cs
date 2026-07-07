using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Framora.ViewModels;

namespace Framora.Views;

/// <summary>
/// 主窗口的代码隐藏。仅处理需要 UI 引用的操作（文件/目录选择器），
/// 业务逻辑全部通过 DataContext 绑定到 MainWindowViewModel。
/// </summary>
public partial class MainWindow : Window
{
    private static readonly FilePickerFileType VideoFiles = new("视频文件")
    {
        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm", "*.m4v" }
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    /// <summary>打开视频文件选择对话框，将结果写入 ViewModel.InputVideoPath。</summary>
    private async void OnBrowseInputClick(object? sender, RoutedEventArgs e)
    {
        var files = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "选择视频文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { VideoFiles }
            });

        if (files.Count > 0)
            ViewModel.InputVideoPath = files[0].Path.LocalPath;
    }

    /// <summary>打开目录选择对话框，将结果写入 ViewModel.OutputDirectory。</summary>
    private async void OnBrowseOutputClick(object? sender, RoutedEventArgs e)
    {
        var folders = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "选择输出目录"
            });

        if (folders.Count > 0)
            ViewModel.OutputDirectory = folders[0].Path.LocalPath;
    }
}

using Avalonia;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Framora.ViewModels;
using Framora.Views;
using Microsoft.Extensions.DependencyInjection;
using Framora.Services;

namespace Framora;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 初始化 FFmpeg 运行时
        FFmpegService.Initialize();

        // 配置依赖注入
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // core services
        services.AddSingleton<IFrameExtractor, FrameExtractorService>();

        // viewmodels
        services.AddSingleton<MainWindowViewModel>();
    }
}
using Avalonia;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Framora.ViewModels;
using Framora.Views;
using Framora.Utilities;
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
        // 初始化日志
        Logger.Initialize();
        Logger.Info("应用启动中");
        Logger.Debug("准备应用服务与运行时依赖");

        // 初始化 FFmpeg 运行时（容错处理：防止 FFmpeg 缺失导致应用在启动阶段崩溃）
        try
        {
            Logger.Info("正在初始化 FFmpeg 运行时");
            FFmpegService.Initialize();
            Logger.Info("FFmpeg 运行时初始化成功");
        }
        catch (Exception ex)
        {
            // 记录错误，允许应用继续启动以便调试 UI。生产环境应展示友好提示或提供配置选项。
            Logger.Error(ex, "FFmpeg 初始化失败");

            // 在调试环境下，触发断点以便开发者能及时定位问题
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
#endif
        }

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

            Logger.Info("主窗口已创建并绑定数据上下文");
        }

        Logger.Info("应用初始化完成");
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
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

/// <summary>
/// 应用程序根类。负责初始化日志、FFmpeg 运行时、依赖注入容器，并创建主窗口。
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 应用级服务提供者，供全局访问已注册的依赖服务。
    /// </summary>
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Avalonia 框架初始化完成后的回调。按顺序执行：
    /// 1. 初始化日志系统
    /// 2. 初始化 FFmpeg 运行时（容错，失败不阻断启动）
    /// 3. 配置依赖注入
    /// 4. 创建主窗口并绑定 ViewModel
    /// </summary>
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

    /// <summary>
    /// 注册应用所需的所有依赖服务到 DI 容器。
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // core services
        services.AddSingleton<IFrameExtractor, FrameExtractorService>();

        // viewmodels
        services.AddSingleton<MainWindowViewModel>();
    }
}
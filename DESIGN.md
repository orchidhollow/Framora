# Framora 概要设计

## 项目概述

**Framora** 是一款跨平台视频帧提取桌面工具，基于 Avalonia UI + .NET 9 构建，通过 FFmpeg 将视频按指定帧率逐帧导出为图片序列。

**技术栈：**

| 层 | 技术 |
|---|------|
| UI 框架 | Avalonia 12.0.3 (Fluent Theme) |
| MVVM | CommunityToolkit.Mvvm 8.4.1 |
| 视频处理 | FFMpegCore 5.2.0 (FFmpeg C# 封装) |
| DI | Microsoft.Extensions.DependencyInjection 9.0.0 |
| 目标平台 | Windows x64、macOS x64、macOS ARM64 |

---

## 架构设计

```
┌─────────────────────────────────────────────────────────┐
│  Views            MainWindow.axaml                      │  ← XAML UI + 代码隐藏（文件选择器）
│                 MainWindow.axaml.cs                      │
├─────────────────────────────────────────────────────────┤
│  Converters       BoolNegationConverter                 │  ← XAML 值转换器
├─────────────────────────────────────────────────────────┤
│  ViewModels       MainWindowViewModel                   │  ← 状态、命令、数据绑定
├─────────────────────────────────────────────────────────┤
│  Services         IFrameExtractor                       │  ← 帧提取业务逻辑接口
│                   FrameExtractorService                  │  ← FFmpeg 实现
│                   FFmpegService (static)                 │  ← 运行时初始化（ffmpeg + ffprobe）
├─────────────────────────────────────────────────────────┤
│  Models           VideoInfo / ExtractionSettings         │  ← 数据结构
├─────────────────────────────────────────────────────────┤
│  Utilities        Logger                                │  ← 日志（文件+控制台）
└─────────────────────────────────────────────────────────┘
```

**依赖注入（App.axaml.cs 启动时注册）：**

```
IFrameExtractor  →  FrameExtractorService   (Singleton)
MainWindowViewModel                           (Singleton，自动注入 IFrameExtractor)
```

**启动流程：**

```
Program.Main()
  → App.OnFrameworkInitializationCompleted()
    → Logger.Initialize()
    → FFmpegService.Initialize()   [同时验证 ffmpeg + ffprobe，容错：失败不阻断启动]
    → ConfigureServices()          [注册 DI]
    → 创建 MainWindow + 绑定 ViewModel
    → ViewModel 构造函数自动创建 VideoInput/ 和 Output/ 默认目录
```

---

## MainWindowViewModel 结构

```
MainWindowViewModel : ViewModelBase
│
├── 构造函数
│   ├── 注入 IFrameExtractor
│   └── 创建默认目录（BaseDirectory/VideoInput, BaseDirectory/Output）
│
├── [ObservableProperty] 字段
│   ├── InputVideoPath      → 输入视频路径（默认 VideoInput/ 目录）
│   ├── OutputDirectory     → 输出目录（默认 Output/ 目录）
│   ├── Fps                 → 目标帧率，默认 12
│   ├── OutputFormat        → 输出格式，"png" 或 "mjpeg"
│   ├── VideoInfoText       → 视频元数据展示文本
│   ├── StatusText          → 状态栏提示文本
│   ├── ProgressCurrent     → 当前进度
│   ├── ProgressTotal       → 总进度
│   └── IsExtracting        → 是否正在提取
│
└── [RelayCommand] 命令
    ├── LoadVideoInfoCommand         → FFProbe 探测视频元数据
    ├── ExtractFramesCommand         → 执行帧提取（含 CanExecute 验证）
    └── CancelExtractingCommand      → 取消提取（通过 CancellationTokenSource）
```

---

## MainWindow UI 布局

```
DockPanel
├── [Bottom] 状态栏（ProgressBar + StatusText）
└── [Fill]  ScrollViewer → StackPanel
    ├── 输入视频：TextBox + 浏览按钮 + 探测信息按钮
    ├── VideoInfoText（视频元数据展示）
    ├── 输出目录：TextBox + 浏览按钮
    ├── 提取设置：NumericUpDown(FPS) + ComboBox(格式)
    └── 操作按钮：开始提取 / 取消（IsExtracting 控制切换）
```

文件/目录选择器通过代码后台 `IStorageProvider` 实现（Avalonia 标准做法）。
Extract/Cancel 按钮切换通过 `BoolNegationConverter` 实现。

---

## 模块完成度

### 已完成 ✅

| 模块 | 文件 | 说明 |
|------|------|------|
| 日志系统 | `Utilities/Logger.cs` | 文件+控制台双输出，线程安全，CallerInfo 记录源位置 |
| FFmpeg 运行时 | `Services/FFmpegService.cs` | 跨平台二进制查找（内嵌优先，PATH 回退），同时验证 ffmpeg + ffprobe |
| 帧提取服务 | `Services/FrameExtractorService.cs` | FFProbe 元数据探测、FFmpeg 逐帧提取（PNG/MJPEG） |
| 服务接口 | `Services/IFrameExtractor.cs` | 支持 CancellationToken、IProgress 进度回调 |
| 数据模型 | `Models/VideoInfo.cs` | 时长、分辨率、原始帧率 |
| 数据模型 | `Models/ExtractionSettings.cs` | 输入路径、输出目录、FPS、输出格式 |
| 视图定位器 | `ViewLocator.cs` | 命名约定自动映射 ViewModel → View |
| DI 容器 | `App.axaml.cs` | 服务注册与主窗口创建 |
| **主窗口 ViewModel** | `ViewModels/MainWindowViewModel.cs` | DI 注入、9 个 ObservableProperty、3 个命令（加载/提取/取消）、默认目录创建 |
| **主窗口 UI** | `Views/MainWindow.axaml` | DockPanel 布局，含文件选择、参数配置、进度条、提取/取消按钮 |
| **UI 代码隐藏** | `Views/MainWindow.axaml.cs` | IStorageProvider 文件/目录选择器 |
| **值转换器** | `Converters/BoolNegationConverter.cs` | IsExtracting 布尔取反，控制按钮切换 |

### 已知限制 ⚠️

| 项目 | 说明 |
|------|------|
| 进度非实时 | `IProgress` 仅在提取完成时报告 `(total, total)`，非逐帧更新 |
| FFmpeg 未安装 | 应用可正常启动但提取会报错，需手动安装 FFmpeg |
| 设计器构造函数 | `MainWindowViewModel` 含 internal 无参构造函数仅供 XAML 设计器使用 |

### 待实现 ❌

| 模块 | 优先级 | 说明 |
|------|--------|------|
| FFmpeg 不可用提示 | P1 | UI 层需在 FFmpeg 初始化失败时展示友好提示（而非用户点击后才报错） |
| 实时进度 | P1 | 解析 FFmpeg stderr 输出实现逐帧进度更新 |
| 提取结果预览 | P2 | 提取完成后展示输出目录中的图片列表或缩略图 |
| 拖拽支持 | P2 | 支持拖拽视频文件到窗口直接设置输入路径 |
| Linux 支持 | P2 | FFmpegService 需补充 linux-x64 路径 |
| 设置持久化 | P3 | 记住用户上次的输出目录、FPS 等偏好 |
| 应用图标 | P3 | 替换 Avalonia 默认图标为 Framora 自定义图标 |
| 打包发布 | P3 | dotnet publish 单文件/安装包配置 |

---

## 开发路线图

### 阶段 1：核心功能 ✅ 已完成
- [x] 实现 `MainWindowViewModel`：DI 注入 `IFrameExtractor`，绑定属性和命令
- [x] 实现主窗口 UI：文件选择、参数配置、进度条、提取/取消按钮
- [x] 进度条绑定 `IProgress` 回调
- [x] 取消提取功能（`CancellationTokenSource`）
- [x] FFmpeg 二进制查找同时验证 ffmpeg + ffprobe
- [x] 默认输入/输出目录自动创建

### 阶段 2：体验优化
- [ ] FFmpeg 不可用时 UI 友好提示
- [ ] 实时进度：解析 FFmpeg stderr 输出帧数
- [ ] 提取完成后结果预览（缩略图/文件列表）
- [ ] 拖拽视频文件到窗口
- [ ] 视频信息预览（时长、分辨率、帧率）— 已绑定命令，需 UI 交互优化

### 阶段 3：平台完善
- [ ] Linux x64 支持
- [ ] 用户偏好持久化（输出目录、FPS、格式）
- [ ] 应用图标替换
- [ ] 打包发布配置（dotnet publish 单文件/安装包）

---

## 环境依赖

| 依赖 | 安装方式 | 说明 |
|------|---------|------|
| .NET 9 SDK | 官网下载 | 构建和运行 |
| FFmpeg + FFprobe | `brew install ffmpeg` 或手动下载 | 运行时必须，需在 PATH 或项目 `FFmpeg/{platform}/` 目录中 |

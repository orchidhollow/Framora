using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Framora.ViewModels;

namespace Framora;

/// <summary>
/// Avalonia 视图定位器。根据 ViewModel 类型名称自动解析对应的 View 类型，
/// 通过命名约定（将 "ViewModel" 替换为 "View"）实现无配置的视图映射。
/// 仅在 View 和 ViewModel 位于同一程序集且遵循命名规范时有效。
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// 根据 ViewModel 实例类型名，反射查找并实例化对应的 View 控件。
    /// 未找到时返回显示类型名的 TextBlock，便于调试。
    /// </summary>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        
        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// 判断给定数据对象是否为 ViewModelBase 的实例，若是则由本定位器处理。
    /// </summary>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace Framora.ViewModels;

/// <summary>
/// 所有 ViewModel 的基类，继承自 CommunityToolkit.Mvvm 的 ObservableObject，
/// 提供 INotifyPropertyChanged 和 INotifyPropertyChanging 实现。
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}

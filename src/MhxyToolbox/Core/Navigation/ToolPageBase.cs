using System.Windows.Controls;

namespace MhxyToolbox.Core.Navigation;

/// <summary>
/// 所有工具页面的基类。具体工具的界面应放在继承本类的 UserControl 中，
/// 这样框架可以在页面切换时通知工具（保存状态、停止计时器等）。
/// </summary>
public abstract class ToolPageBase : UserControl
{
    /// <summary>页面被打开 / 返回时调用。parameter 预留给深链接场景。</summary>
    public virtual void OnNavigatedTo(object? parameter) { }

    /// <summary>页面离开可视区域时调用。</summary>
    public virtual void OnNavigatedFrom() { }
}

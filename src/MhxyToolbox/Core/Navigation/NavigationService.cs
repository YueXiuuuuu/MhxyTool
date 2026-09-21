namespace MhxyToolbox.Core.Navigation;

/// <summary>一条导航记录：页面实例 + 页面标题。</summary>
public record NavigationEntry(object Page, string Title);

/// <summary>
/// 全局页面导航服务：MainForm 只负责承载 ContentControl，
/// 页面切换、返回栈都由这里管理。
/// </summary>
public class NavigationService
{
    private readonly Stack<NavigationEntry> _backStack = new();

    /// <summary>当前页面发生变化（导航 / 返回 / 改标题）时触发。</summary>
    public event Action? CurrentChanged;

    public NavigationEntry? Current { get; private set; }

    public bool CanGoBack => _backStack.Count > 0;

    /// <summary>导航到新页面，当前页面压入返回栈。</summary>
    public void Navigate(object page, string title)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (Current is { } current)
            _backStack.Push(current);

        Current = new NavigationEntry(page, title);
        CurrentChanged?.Invoke();
    }

    /// <summary>返回上一页面。没有历史记录时返回 null。</summary>
    public NavigationEntry? GoBack()
    {
        if (_backStack.Count == 0)
            return null;

        Current = _backStack.Pop();
        CurrentChanged?.Invoke();
        return Current;
    }

    /// <summary>更新当前页面标题（不改变页面，例如同一页面换了筛选条件）。</summary>
    public void UpdateCurrentTitle(string title)
    {
        if (Current is null)
            return;
        Current = Current with { Title = title };
        CurrentChanged?.Invoke();
    }
}

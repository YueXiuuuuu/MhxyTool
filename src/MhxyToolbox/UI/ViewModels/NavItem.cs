namespace MhxyToolbox.UI.ViewModels;

public enum NavItemKind
{
    Home,
    Category,
    Favorites,
    Settings,
}

/// <summary>侧边栏导航项。分类项由 ToolManager 动态生成，首页不写死分类。</summary>
public class NavItem
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Icon { get; init; }
    public required NavItemKind Kind { get; init; }

    /// <summary>Kind == Category 时对应的分类名。</summary>
    public string? Category { get; init; }

    public override string ToString() => Label;
}

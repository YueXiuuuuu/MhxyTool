using MhxyToolbox.Infrastructure;

namespace MhxyToolbox.Core.Tools;

/// <summary>
/// 运行时的工具条目：ITool 的元数据 + 用户状态（是否收藏 / 是否启用）。
/// 首页、卡片等 UI 只依赖本类，完全不依赖任何具体工具模块。
/// </summary>
public class ToolDefinition : ObservableObject
{
    public ToolDefinition(ITool tool, bool isFavorite, bool isEnabled)
    {
        Tool = tool;
        _isFavorite = isFavorite;
        _isEnabled = isEnabled;
    }

    /// <summary>底层工具实现（CreateView 用）。</summary>
    public ITool Tool { get; }

    public string Id => Tool.Id;
    public string Name => Tool.Name;
    public string Description => Tool.Description;
    public string Category => Tool.Category;
    public string Icon => Tool.Icon;
    public string Version => Tool.Version;
    public int SortOrder => Tool.SortOrder;

    private bool _isFavorite;
    /// <summary>是否收藏（持久化到 settings.json）。</summary>
    public bool IsFavorite { get => _isFavorite; set => SetProperty(ref _isFavorite, value); }

    private bool _isEnabled;
    /// <summary>是否启用（禁用后首页不显示，持久化到 settings.json）。</summary>
    public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

    /// <summary>搜索标签的展示文本（最多取 3 个）。</summary>
    public string TagsDisplay => string.Join("  ", Tool.Tags.Take(3));
}

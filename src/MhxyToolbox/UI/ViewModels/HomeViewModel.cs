using System.Collections.ObjectModel;
using System.ComponentModel;
using MhxyToolbox.Core.Tools;
using MhxyToolbox.Services;
using MhxyToolbox.Infrastructure;

namespace MhxyToolbox.UI.ViewModels;

/// <summary>首页的一个分组：分组标题 + 该组工具卡片。</summary>
public class ToolSection
{
    public required string Title { get; init; }
    public required ObservableCollection<ToolDefinition> Tools { get; init; }
}

/// <summary>
/// 首页状态：搜索、分类筛选、收藏视图、动态分组。
/// 数据全部来自 ToolManager，不依赖任何具体工具。
/// </summary>
public class HomeViewModel : ObservableObject
{
    private readonly List<ToolDefinition> _allTools = new();

    /// <summary>用户点击某张工具卡片时触发（MainViewModel 负责打开页面）。</summary>
    public event Action<ToolDefinition>? OpenToolRequested;

    public ObservableCollection<ToolSection> Sections { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) RefreshSections(); }
    }

    private string? _selectedCategory;
    /// <summary>null = 全部。</summary>
    public string? SelectedCategory
    {
        get => _selectedCategory;
        set { if (SetProperty(ref _selectedCategory, value)) RefreshSections(); }
    }

    private bool _favoritesOnly;
    public bool FavoritesOnly
    {
        get => _favoritesOnly;
        set { if (SetProperty(ref _favoritesOnly, value)) RefreshSections(); }
    }

    /// <summary>当前可见（已启用）的工具数量。</summary>
    public int VisibleToolCount => _allTools.Count(t => t.IsEnabled);

    /// <summary>没有任何匹配结果时显示空状态。</summary>
    public bool HasNoResult { get; private set; }

    private string _emptyTitle = "没有找到匹配的工具";
    public string EmptyTitle { get => _emptyTitle; private set => SetProperty(ref _emptyTitle, value); }

    private string _emptyHint = "换个关键词试试，或从左侧分类进入";
    public string EmptyHint { get => _emptyHint; private set => SetProperty(ref _emptyHint, value); }

    public HomeViewModel()
    {
        foreach (var definition in ToolManager.CreateAllDefinitions())
        {
            definition.PropertyChanged += OnToolStateChanged;
            _allTools.Add(definition);
        }

        RefreshSections();
    }

    /// <summary>点击卡片 → 请求打开工具。</summary>
    public void OpenTool(ToolDefinition tool) => OpenToolRequested?.Invoke(tool);

    public void ResetFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = null;
        FavoritesOnly = false;
    }

    // ---------- 内部逻辑 ----------

    private void OnToolStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ToolDefinition.IsFavorite) or nameof(ToolDefinition.IsEnabled))
        {
            var tool = (ToolDefinition)sender!;
            SettingsService.Instance.SetFavorite(tool.Id, tool.IsFavorite);
            SettingsService.Instance.SetEnabled(tool.Id, tool.IsEnabled);
            RefreshSections(); // 收藏视图 / 禁用状态需要立即反映
        }
    }

    /// <summary>
    /// 根据当前筛选条件重建分组。这是首页展示的唯一数据源。
    /// 搜索优先级最高：只要有关键词，就在全部工具中搜索（不限于当前分类），
    /// 保证“搜索：内丹”能跨分类命中所有相关工具。
    /// </summary>
    private void RefreshSections()
    {
        var searching = !string.IsNullOrWhiteSpace(SearchText);
        IEnumerable<ToolDefinition> query = _allTools.Where(t => t.IsEnabled);
        string title;

        if (searching)
        {
            var keyword = SearchText.Trim();
            query = query.Where(t => Matches(t, keyword));
            title = $"搜索 “{keyword}”";
        }
        else if (FavoritesOnly)
        {
            query = query.Where(t => t.IsFavorite);
            title = "我的收藏";
        }
        else if (!string.IsNullOrWhiteSpace(SelectedCategory))
        {
            var category = SelectedCategory;
            query = query.Where(t => ToolManager.TopLevelCategory(t.Category) == category);
            title = category;
        }
        else
        {
            title = "全部工具";
        }

        var results = query.ToList();
        Sections.Clear();

        var isDefaultView = !FavoritesOnly
                         && SelectedCategory is null
                         && !searching;

        if (isDefaultView)
        {
            // 默认视图：收藏置顶，其余按顶级分类分组 —— 工具多了以后依然是这个结构。
            var favorites = results.Where(t => t.IsFavorite).ToList();
            if (favorites.Count > 0)
                AddSection("⭐ 我的收藏", favorites);

            foreach (var group in results
                         .GroupBy(t => ToolManager.TopLevelCategory(t.Category))
                         .OrderBy(g => g.Key, StringComparer.CurrentCulture))
            {
                AddSection(group.Key, group.ToList());
            }
        }
        else if (results.Count > 0)
        {
            AddSection(title, results);
        }

        HasNoResult = results.Count == 0;
        UpdateEmptyState(searching);
        OnPropertyChanged(nameof(HasNoResult));
    }

    /// <summary>空状态文案随场景变化，告诉用户下一步该做什么。</summary>
    private void UpdateEmptyState(bool searching)
    {
        if (searching)
        {
            EmptyTitle = "没有找到匹配的工具";
            EmptyHint = "换个关键词试试，搜索会匹配名称、描述、分类和标签";
        }
        else if (FavoritesOnly)
        {
            EmptyTitle = "还没有收藏任何工具";
            EmptyHint = "在工具卡片右上角点击 ☆ 即可收藏";
        }
        else
        {
            EmptyTitle = "这个分类下还没有工具";
            EmptyHint = "新增工具后会自动出现在这里";
        }
    }

    private void AddSection(string title, List<ToolDefinition> tools)
    {
        Sections.Add(new ToolSection
        {
            Title = $"{title}  ({tools.Count})",
            Tools = new ObservableCollection<ToolDefinition>(tools),
        });
    }

    /// <summary>搜索匹配：名称 / 描述 / 分类 / 标签。</summary>
    private static bool Matches(ToolDefinition tool, string keyword)
    {
        return tool.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || tool.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || tool.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || tool.Tool.Tags.Any(tag => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

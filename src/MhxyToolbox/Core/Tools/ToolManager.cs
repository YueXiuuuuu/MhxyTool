using System.Reflection;
using MhxyToolbox.Services;

namespace MhxyToolbox.Core.Tools;

/// <summary>
/// 工具管理器（框架门面）：负责工具的自动发现、注册与查询。
/// 首页与具体工具之间唯一的数据通道 —— 首页只知道 ToolManager，
/// 不知道任何具体工具的存在。
/// </summary>
public static class ToolManager
{
    private static readonly ToolRegistry Registry = new();

    /// <summary>扫描程序集中所有 ITool 实现（无参构造、非抽象）并自动注册。</summary>
    public static void DiscoverFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var toolTypes = assembly.GetTypes().Where(type =>
            !type.IsAbstract &&
            !type.IsInterface &&
            typeof(ITool).IsAssignableFrom(type) &&
            type.GetConstructor(Type.EmptyTypes) is not null);

        foreach (var type in toolTypes)
            Register((ITool)Activator.CreateInstance(type)!);
    }

    /// <summary>手动注册单个工具（自动发现之外的补充手段）。</summary>
    public static void Register(ITool tool) => Registry.Register(tool);

    /// <summary>所有已注册工具。</summary>
    public static IEnumerable<ITool> GetAll() => Registry.GetAll();

    /// <summary>顶级分类列表（由各工具声明汇总而来，首页不写死任何分类）。</summary>
    public static IReadOnlyList<string> GetCategories() =>
        Registry.GetAll()
            .Select(t => TopLevelCategory(t.Category))
            .Distinct()
            .OrderBy(c => c, StringComparer.CurrentCulture)
            .ToList();

    /// <summary>根据元数据构建运行时工具条目（合并收藏 / 禁用状态），供首页使用。</summary>
    public static IReadOnlyList<ToolDefinition> CreateAllDefinitions()
    {
        var settings = SettingsService.Instance;
        return Registry.GetAll()
            .Select(t => new ToolDefinition(t, settings.IsFavorite(t.Id), settings.IsEnabled(t.Id)))
            .ToList();
    }

    /// <summary>取分类的顶级部分："计算/收益计算" → "计算"。</summary>
    public static string TopLevelCategory(string category)
    {
        var slash = category.IndexOf('/');
        return slash < 0 ? category.Trim() : category[..slash].Trim();
    }
}

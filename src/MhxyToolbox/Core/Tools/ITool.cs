using System.Windows;

namespace MhxyToolbox.Core.Tools;

/// <summary>
/// 每个具体工具必须实现的统一接口。
/// 工具模块只负责声明自己的元数据并创建自己的页面；
/// 注册、发现、分类、搜索、收藏、首页展示全部由框架完成。
/// </summary>
public interface ITool
{
    /// <summary>全局唯一标识（持久化收藏 / 禁用状态用），例如 "tools.neidan-income"。</summary>
    string Id { get; }

    /// <summary>工具名称，例如 "内丹收益计算器"。</summary>
    string Name { get; }

    /// <summary>一句话描述，显示在卡片上。</summary>
    string Description { get; }

    /// <summary>所属分类，支持层级，用 "/" 分隔，例如 "计算/收益计算"。</summary>
    string Category { get; }

    /// <summary>图标（emoji 或字符），例如 "🧮"。</summary>
    string Icon { get; }

    /// <summary>搜索标签，参与搜索匹配。</summary>
    string[] Tags { get; }

    /// <summary>工具版本号，显示在卡片角标。</summary>
    string Version { get; }

    /// <summary>同一分类内的排序权重，越小越靠前。</summary>
    int SortOrder { get; }

    /// <summary>创建工具的界面页面（每次打开时调用）。</summary>
    FrameworkElement CreateView();
}

using System.Windows;
using MhxyToolbox.Core.Tools;

namespace MhxyToolbox.Tools.GemSynthesis;

/// <summary>
/// 宝石合成计算器：按合成规则推算 1~20 级宝石的材料、体力与成本。
/// 注册、首页展示、搜索、收藏全部由框架自动完成。
/// </summary>
public class GemSynthesisTool : ITool
{
    public string Id => "tools.gem-synthesis";
    public string Name => "宝石合成计算器";
    public string Description => "按合成规则推算 1~20 级宝石所需材料、体力与等效 1 级宝石数量，按一级宝石单价与金价折算合成成本。";
    public string Category => "计算/宝石计算";
    public string Icon => "💎";
    public string[] Tags => ["宝石", "合成", "体力", "成本", "计算器"];
    public string Version => "1.0.0";
    public int SortOrder => 11;

    public FrameworkElement CreateView() => new GemSynthesisView();
}

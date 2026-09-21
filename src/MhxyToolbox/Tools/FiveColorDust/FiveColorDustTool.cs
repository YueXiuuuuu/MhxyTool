using System.Windows;
using MhxyToolbox.Core.Tools;

namespace MhxyToolbox.Tools.FiveColorDust;

/// <summary>
/// 五色灵尘计算器：按合成规则推算 1~10 级所需材料与成本。
/// 注册、首页展示、搜索、收藏全部由框架自动完成。
/// </summary>
public class FiveColorDustTool : ITool
{
    public string Id => "tools.five-color-dust";
    public string Name => "五色灵尘计算器";
    public string Description => "按合成规则推算 1~10 级五色灵尘所需的一级灵尘数量，并按单价与金价折算梦幻币与人民币成本。";
    public string Category => "计算/宝石计算";
    public string Icon => "💠";
    public string[] Tags => ["五色灵尘", "灵尘", "宝石", "合成", "成本", "计算器"];
    public string Version => "1.0.0";
    public int SortOrder => 10;

    public FrameworkElement CreateView() => new FiveColorDustView();
}

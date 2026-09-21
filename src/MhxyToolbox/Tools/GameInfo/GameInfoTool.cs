using System.Windows;
using MhxyToolbox.Core.Tools;
using MhxyToolbox.Tools.GameInfo;

namespace MhxyToolbox.Tools.GameInfo;

/// <summary>
/// 示例工具 2：资料查询类工具，验证与计算器不同类型的工具页面打开机制。
/// </summary>
public class GameInfoTool : ITool
{
    public string Id => "tools.game-info";
    public string Name => "游戏资料查询";
    public string Description => "门派等游戏基础资料查询，验证资料类工具的页面机制。";
    public string Category => "查询/游戏资料";
    public string Icon => "📖";
    public string[] Tags => ["资料", "查询", "门派"];
    public string Version => "1.0.0";
    public int SortOrder => 200;

    public FrameworkElement CreateView() => new GameInfoView();
}

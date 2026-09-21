using System.Windows;
using MhxyToolbox.Core.Tools;
using MhxyToolbox.Tools.ExampleCalculator;

namespace MhxyToolbox.Tools.ExampleCalculator;

/// <summary>
/// 示例工具 1：验证 工具注册 → 自动发现 → 首页展示 → 点击打开 全流程。
/// 新增真实工具时，照这个类的写法实现 ITool 即可，框架和首页都不用改。
/// </summary>
public class ExampleCalculatorTool : ITool
{
    public string Id => "tools.example-calculator";
    public string Name => "示例计算器";
    public string Description => "四则运算演示工具，用于验证工具模块加载机制。";
    public string Category => "计算/示例";
    public string Icon => "🧮";
    public string[] Tags => ["示例", "计算器", "演示"];
    public string Version => "1.0.0";
    public int SortOrder => 100;

    public FrameworkElement CreateView() => new ExampleCalculatorView();
}

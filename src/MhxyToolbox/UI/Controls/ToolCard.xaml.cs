using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MhxyToolbox.Core.Tools;

namespace MhxyToolbox.UI.Controls;

/// <summary>
/// 首页工具卡片。纯展示控件：数据上下文是 ToolDefinition，
/// 点击卡片抛出 ToolActivated 冒泡事件，点击星星直接切换收藏。
/// </summary>
public partial class ToolCard : UserControl
{
    public ToolCard()
    {
        InitializeComponent();
    }

    /// <summary>卡片被激活（冒泡路由事件，由首页统一处理打开逻辑）。</summary>
    public static readonly RoutedEvent ToolActivatedEvent = EventManager.RegisterRoutedEvent(
        nameof(ToolActivated), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToolCard));

    public event RoutedEventHandler ToolActivated
    {
        add => AddHandler(ToolActivatedEvent, value);
        remove => RemoveHandler(ToolActivatedEvent, value);
    }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (IsWithinButton(e.OriginalSource as DependencyObject))
            return;

        RaiseEvent(new RoutedEventArgs(ToolActivatedEvent, this));
    }

    private void OnStarClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ToolDefinition tool)
            tool.IsFavorite = !tool.IsFavorite; // 持久化由 HomeViewModel 监听属性变化完成

        e.Handled = true;
    }

    /// <summary>点击落在星星等按钮上时不算激活卡片。</summary>
    private bool IsWithinButton(DependencyObject? source)
    {
        DependencyObject? current = source;
        while (current is not null && !ReferenceEquals(current, this))
        {
            if (current is ButtonBase)
                return true;
            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    /// <summary>
    /// 向 UI 自动化暴露 Invoke 模式：卡片在语义上就是一个按钮，
    /// 支持自动化测试与屏幕阅读器（无障碍）直接激活。
    /// </summary>
    protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new ToolCardAutomationPeer(this);

    private sealed class ToolCardAutomationPeer : System.Windows.Automation.Peers.UserControlAutomationPeer,
        System.Windows.Automation.Provider.IInvokeProvider
    {
        private readonly ToolCard _owner;

        public ToolCardAutomationPeer(ToolCard owner) : base(owner) => _owner = owner;

        protected override string GetClassNameCore() => "ToolCard";

        protected override string GetNameCore()
            => _owner.DataContext is ToolDefinition tool ? tool.Name : base.GetNameCore();

        public override object GetPattern(System.Windows.Automation.Peers.PatternInterface patternInterface)
            => patternInterface == System.Windows.Automation.Peers.PatternInterface.Invoke
                ? this
                : base.GetPattern(patternInterface);

        void System.Windows.Automation.Provider.IInvokeProvider.Invoke()
            => _owner.RaiseEvent(new RoutedEventArgs(ToolActivatedEvent, _owner));
    }
}

using System.Windows;
using System.Windows.Controls;
using MhxyToolbox.Core.Tools;
using MhxyToolbox.UI.ViewModels;

namespace MhxyToolbox.UI.Views;

public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
    }

    /// <summary>ToolCard 的 ToolActivated 冒泡到这里（不管卡片嵌在第几层分组里）。</summary>
    private void OnToolActivated(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: ToolDefinition tool }
            && DataContext is HomeViewModel viewModel)
        {
            viewModel.OpenTool(tool);
        }
    }
}

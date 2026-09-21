using System.Collections.ObjectModel;
using MhxyToolbox.Core.Navigation;
using MhxyToolbox.Core.Tools;
using MhxyToolbox.Infrastructure;

namespace MhxyToolbox.UI.ViewModels;

/// <summary>
/// 主窗口 ViewModel：持有侧边栏导航项、首页状态和导航服务。
/// 具体工具对它完全不可见 —— 它只知道 "ToolManager 里有这些条目"。
/// </summary>
public class MainViewModel : ObservableObject
{
    public NavigationService Navigation { get; } = new();

    public HomeViewModel Home { get; } = new();

    public ObservableCollection<NavItem> NavItems { get; } = new();

    public RelayCommand GoBackCommand { get; }

    private string _currentTitle = "首页";
    public string CurrentTitle { get => _currentTitle; private set => SetProperty(ref _currentTitle, value); }

    private bool _canGoBack;
    public bool CanGoBack { get => _canGoBack; private set => SetProperty(ref _canGoBack, value); }

    public MainViewModel()
    {
        GoBackCommand = new RelayCommand(_ => Navigation.GoBack(), _ => Navigation.CanGoBack);

        BuildNavItems();

        Navigation.CurrentChanged += () =>
        {
            CurrentTitle = Navigation.Current?.Title ?? string.Empty;
            CanGoBack = Navigation.CanGoBack;
        };
    }

    /// <summary>导航项 = 固定项（首页/收藏/设置）+ 动态分类项。</summary>
    private void BuildNavItems()
    {
        NavItems.Add(new NavItem { Id = "nav.home", Label = "首页", Icon = "🏠", Kind = NavItemKind.Home });

        foreach (var category in ToolManager.GetCategories())
        {
            NavItems.Add(new NavItem
            {
                Id = "nav.cat." + category,
                Label = category,
                Icon = CategoryIcon(category),
                Kind = NavItemKind.Category,
                Category = category,
            });
        }

        NavItems.Add(new NavItem { Id = "nav.favorites", Label = "我的收藏", Icon = "⭐", Kind = NavItemKind.Favorites });
        NavItems.Add(new NavItem { Id = "nav.settings", Label = "设置", Icon = "⚙", Kind = NavItemKind.Settings });
    }

    private static string CategoryIcon(string category) => category switch
    {
        "计算" => "🧮",
        "查询" => "📊",
        _ => "🔧",
    };
}

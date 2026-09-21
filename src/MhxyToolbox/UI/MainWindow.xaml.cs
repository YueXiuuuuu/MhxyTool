using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using MhxyToolbox.Core.Navigation;
using MhxyToolbox.Core.Tools;
using MhxyToolbox.UI.ViewModels;
using MhxyToolbox.UI.Views;
using System.Linq;

namespace MhxyToolbox.UI;

/// <summary>
/// 主窗口外壳：只负责导航栏、页面切换、全局主题，
/// 不包含任何具体工具的逻辑。
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly NavigationService _navigation;
    private readonly HomePage _homePage;
    private readonly SettingsPage _settingsPage;
    private object? _lastPage;
    private bool _ready;

    /// <summary>程序化切换导航项时为 true，避免顺带清空用户刚输入的搜索条件。</summary>
    private bool _suppressFilterReset;

    /// <summary>鼠标按下时命中的导航项，用于判断是否为“点击已选中项”。</summary>
    private NavItem? _pressedNavItem;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;
        _navigation = _vm.Navigation;

        _homePage = new HomePage { DataContext = _vm.Home };
        _settingsPage = new SettingsPage();

        _navigation.CurrentChanged += OnNavigationCurrentChanged;
        _vm.Home.OpenToolRequested += OnOpenToolRequested;
        _vm.Home.PropertyChanged += OnHomePropertyChanged;

        _ready = true;
        NavList.SelectedItem = _vm.NavItems.First(i => i.Kind == NavItemKind.Home);
    }

    // ---------- 侧边栏导航 ----------

    private void OnNavSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || NavList.SelectedItem is not NavItem item)
            return;

        ActivateNavItem(item, resetFilters: !_suppressFilterReset);
    }

    /// <summary>按下时记录被点的导航项，用于识别“点击已选中项”的情况。</summary>
    private void OnNavPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _pressedNavItem = ResolveNavItem(e.OriginalSource as DependencyObject);
    }

    /// <summary>
    /// 点击的如果是“已经选中”的导航项，SelectionChanged 不会触发，
    /// 但用户显然期望回到该页面（例如打开工具页后再点侧边栏“首页”）。
    /// 这里补上这条路径。
    /// </summary>
    private void OnNavPreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var released = ResolveNavItem(e.OriginalSource as DependencyObject);
        if (released is not null
            && ReferenceEquals(released, _pressedNavItem)
            && ReferenceEquals(released, NavList.SelectedItem))
        {
            ActivateNavItem(released, resetFilters: !_suppressFilterReset);
        }

        _pressedNavItem = null;
    }

    private NavItem? ResolveNavItem(DependencyObject? source)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: NavItem item })
                return item;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    /// <summary>
    /// 搜索是全局的（跨分类），所以一开始输入就把侧边栏切回“首页”，
    /// 否则会出现“显示全部结果、却高亮着某个分类”的矛盾状态。
    /// </summary>
    private void OnHomePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HomeViewModel.SearchText) || string.IsNullOrWhiteSpace(_vm.Home.SearchText))
            return;

        var homeItem = _vm.NavItems.First(i => i.Kind == NavItemKind.Home);
        if (ReferenceEquals(NavList.SelectedItem, homeItem))
            return;

        _suppressFilterReset = true;
        NavList.SelectedItem = homeItem;
        _suppressFilterReset = false;
        ShowPage(_homePage, "首页");
    }

    private void ActivateNavItem(NavItem item, bool resetFilters = true)
    {
        var home = _vm.Home;
        switch (item.Kind)
        {
            case NavItemKind.Home:
                if (resetFilters)
                    home.ResetFilters();
                ShowPage(_homePage, "首页");
                break;

            case NavItemKind.Category:
                home.FavoritesOnly = false;
                home.SearchText = string.Empty;
                home.SelectedCategory = item.Category;
                ShowPage(_homePage, item.Label);
                break;

            case NavItemKind.Favorites:
                home.SearchText = string.Empty;
                home.SelectedCategory = null;
                home.FavoritesOnly = true;
                ShowPage(_homePage, "我的收藏");
                break;

            case NavItemKind.Settings:
                ShowPage(_settingsPage, "设置");
                break;
        }
    }

    /// <summary>同一页面重复显示时只更新标题，不往返回栈压重复记录。</summary>
    private void ShowPage(object page, string title)
    {
        if (ReferenceEquals(_navigation.Current?.Page, page))
        {
            _navigation.UpdateCurrentTitle(title);
            return;
        }

        _navigation.Navigate(page, title);
    }

    // ---------- 打开工具 ----------

    private void OnOpenToolRequested(ToolDefinition tool)
    {
        // 工具页面由工具自己创建，外壳不关心它的内容。
        _navigation.Navigate(tool.Tool.CreateView(), tool.Name);
    }

    // ---------- 页面切换 ----------

    private void OnNavigationCurrentChanged()
    {
        var page = _navigation.Current?.Page;
        if (ReferenceEquals(_lastPage, page))
            return;

        if (_lastPage is ToolPageBase oldPage)
            oldPage.OnNavigatedFrom();

        _lastPage = page;
        ContentHost.Content = page;

        if (page is ToolPageBase newPage)
            newPage.OnNavigatedTo(null);
    }

    // ---------- 深色标题栏 ----------

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        EnableDarkTitleBar();
    }

    private void EnableDarkTitleBar()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int on = 1;
            _ = DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)); // Win10 1903+
            _ = DwmSetWindowAttribute(hwnd, 19, ref on, sizeof(int)); // 旧版本回退
        }
        catch
        {
            // 非 DWM 环境忽略。
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}

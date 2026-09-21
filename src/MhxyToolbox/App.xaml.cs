using System.Reflection;
using System.Windows;
using MhxyToolbox.Core.Tools;
using MhxyToolbox.Services;

namespace MhxyToolbox;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 启动顺序：先加载配置（收藏/禁用状态），再自动发现工具，最后创建主窗口（StartupUri）。
        SettingsService.Instance.Load();
        ToolManager.DiscoverFromAssembly(Assembly.GetExecutingAssembly());

        // 未来如果要支持外置插件 DLL，在这里额外扫描目录中的程序集：
        // foreach (var dll in Directory.GetFiles("plugins", "*.dll"))
        //     ToolManager.DiscoverFromAssembly(Assembly.LoadFrom(dll));
    }
}

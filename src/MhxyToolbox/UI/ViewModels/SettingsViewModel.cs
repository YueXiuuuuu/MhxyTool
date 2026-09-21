using MhxyToolbox.Core.Tools;
using MhxyToolbox.Services;
using MhxyToolbox.UI.ViewModels;

namespace MhxyToolbox.UI.ViewModels;

/// <summary>设置页数据：展示工具注册表与配置存储位置。</summary>
public class SettingsViewModel
{
    public IReadOnlyList<ToolDefinition> AllTools { get; } = ToolManager.CreateAllDefinitions();

    public int ToolCount => AllTools.Count;

    public string SettingsPath { get; } = SettingsService.Instance.FilePath;
}

using System.IO;
using System.Text.Json;

namespace MhxyToolbox.Services;

/// <summary>持久化的用户配置。</summary>
public class ToolboxSettings
{
    public List<string> FavoriteToolIds { get; set; } = new();
    public List<string> DisabledToolIds { get; set; } = new();
    public Dictionary<string, int> SortOverrides { get; set; } = new();
}

/// <summary>
/// 配置服务：JSON 文件保存到 %AppData%\MhxyToolbox\settings.json。
/// 后续的全局设置（主题、窗口尺寸等）也在这里扩展。
/// </summary>
public sealed class SettingsService
{
    public static SettingsService Instance { get; } = new();

    public ToolboxSettings Settings { get; private set; } = new();

    public string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MhxyToolbox",
        "settings.json");

    public void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                Settings = JsonSerializer.Deserialize<ToolboxSettings>(File.ReadAllText(FilePath)) ?? new ToolboxSettings();
        }
        catch
        {
            // 配置损坏时回退到默认值，不让工具箱无法启动。
            Settings = new ToolboxSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    public bool IsFavorite(string toolId) => Settings.FavoriteToolIds.Contains(toolId);

    public bool IsEnabled(string toolId) => !Settings.DisabledToolIds.Contains(toolId);

    public void SetFavorite(string toolId, bool favorite)
    {
        bool changed = false;
        if (favorite && !Settings.FavoriteToolIds.Contains(toolId))
        {
            Settings.FavoriteToolIds.Add(toolId);
            changed = true;
        }
        else if (!favorite && Settings.FavoriteToolIds.Remove(toolId))
        {
            changed = true;
        }

        if (changed)
            Save();
    }

    public void SetEnabled(string toolId, bool enabled)
    {
        bool changed = false;
        if (!enabled && !Settings.DisabledToolIds.Contains(toolId))
        {
            Settings.DisabledToolIds.Add(toolId);
            changed = true;
        }
        else if (enabled && Settings.DisabledToolIds.Remove(toolId))
        {
            changed = true;
        }

        if (changed)
            Save();
    }
}

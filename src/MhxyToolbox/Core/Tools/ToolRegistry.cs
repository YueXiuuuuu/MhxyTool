namespace MhxyToolbox.Core.Tools;

/// <summary>
/// 工具注册表：保存所有已注册的 ITool，按 Id 去重。
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.Ordinal);

    /// <summary>有新工具注册完成时触发。</summary>
    public event Action? ToolRegistered;

    /// <summary>注册一个工具。Id 重复视为程序错误，直接抛异常。</summary>
    public void Register(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        if (string.IsNullOrWhiteSpace(tool.Id))
            throw new ArgumentException($"工具 {tool.GetType().Name} 的 Id 不能为空", nameof(tool));

        if (!_tools.TryAdd(tool.Id, tool))
            throw new InvalidOperationException($"已存在相同 Id 的工具：{tool.Id}（{tool.Name}）");

        ToolRegistered?.Invoke();
    }

    /// <summary>所有已注册工具，按 SortOrder、名称排序。</summary>
    public IEnumerable<ITool> GetAll() =>
        _tools.Values
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name, StringComparer.CurrentCulture);

    public ITool? Find(string id) => _tools.GetValueOrDefault(id);

    public int Count => _tools.Count;
}

# 梦幻工具箱（MhxyToolbox）

一个可以**持续添加**《梦幻西游》工具的桌面工具平台。不是游戏本体，不是外挂，不修改游戏客户端。

核心目标只有一个：**可扩展性**。增加一个新工具时，不需要改首页、不需要改导航、不需要加 `if/else`。

---

## 一、技术栈

| 项目 | 选型 |
| --- | --- |
| 语言 / 运行时 | C# / .NET 8（`net8.0-windows`） |
| UI 框架 | WPF（XAML + 数据绑定） |
| 解决方案 | `MhxyToolbox.sln` |
| 主项目 | `src/MhxyToolbox/MhxyToolbox.csproj` |
| 构建 | `dotnet build src/MhxyToolbox/MhxyToolbox.csproj` |
| 运行 | `src/MhxyToolbox/bin/Debug/net8.0-windows/MhxyToolbox.exe` |

选择 WPF 的原因：深色主题、渐变、发光、悬停动画、自适应卡片布局都是原生能力，动态生成工具卡片只需要一个 `DataTemplate`，不需要手写绘制代码。

---

## 二、项目结构

```text
MhxyTool/
├─ MhxyToolbox.sln
├─ .tools/                          # 开发期的 UI 自动化验证脚本（不参与编译）
└─ src/MhxyToolbox/
   ├─ App.xaml(.cs)                 # 启动：先加载配置，再自动发现工具
   ├─ Core/                         # 平台核心，与具体工具完全解耦
   │  ├─ Tools/
   │  │  ├─ ITool.cs                # ★ 工具统一接口（元数据 + 创建页面）
   │  │  ├─ ToolDefinition.cs       # 运行时条目：元数据 + 收藏/启用状态
   │  │  ├─ ToolRegistry.cs         # 注册表（Id 去重、排序）
   │  │  └─ ToolManager.cs          # ★ 框架门面：发现 / 注册 / 查询 / 分类
   │  └─ Navigation/
   │     ├─ NavigationService.cs    # 页面导航 + 返回栈
   │     └─ ToolPageBase.cs         # ★ 所有工具页面的基类
   ├─ Infrastructure/
   │  ├─ ObservableObject.cs        # INotifyPropertyChanged 基类
   │  └─ RelayCommand.cs            # ICommand 轻量实现
   ├─ Services/
   │  └─ SettingsService.cs         # %AppData%\MhxyToolbox\settings.json
   ├─ Themes/
   │  └─ Dark.xaml                  # 深色主题（暖金点缀）全套样式
   ├─ UI/
   │  ├─ MainWindow.xaml(.cs)       # ★ 外壳：导航 / 页面切换 / 主题，无工具逻辑
   │  ├─ Controls/ToolCard.xaml(.cs)# ★ 首页工具卡片（含 UIA Invoke 支持）
   │  ├─ Views/
   │  │  ├─ HomePage.xaml(.cs)      # ★ 首页：搜索 / 分组 / 空状态
   │  │  └─ SettingsPage.xaml(.cs)  # 设置页
   │  └─ ViewModels/
   │     ├─ MainViewModel.cs        # 侧边栏导航项（分类动态生成）
   │     ├─ HomeViewModel.cs        # 首页筛选与分组逻辑
   │     ├─ SettingsViewModel.cs
   │     └─ NavItem.cs
   └─ Tools/                        # ★ 具体工具，一个工具一个文件夹
      ├─ FiveColorDust/             # 五色灵尘计算器（第一个真实工具）
      ├─ ExampleCalculator/         # 示例工具 1：计算器（验证加载机制）
      └─ GameInfo/                  # 示例工具 2：资料查询（验证另一类页面）
```

**依赖方向是单向的**：`Tools → Core`，`UI → Core`。`Core` 不认识任何具体工具，`UI` 也不认识。

---

## 三、核心类设计

### 1. `ITool` —— 工具统一接口

每个工具只需声明自己的元数据，并创建自己的页面：

```csharp
public interface ITool
{
    string Id { get; }            // 全局唯一，持久化收藏/禁用状态用
    string Name { get; }          // 显示名称，如 "内丹收益计算器"
    string Description { get; }   // 卡片上的一句话描述
    string Category { get; }      // 支持层级，用 "/" 分隔，如 "计算/收益计算"
    string Icon { get; }          // emoji 或字符
    string[] Tags { get; }        // 参与搜索匹配
    string Version { get; }       // 卡片角标
    int SortOrder { get; }        // 同分类内排序权重，越小越靠前

    FrameworkElement CreateView(); // 创建工具页面（每次打开时调用）
}
```

### 2. `ToolDefinition` —— 运行时条目

`ITool`（元数据） + 用户状态（`IsFavorite` / `IsEnabled`，可绑定、可持久化）。
**首页和卡片只依赖它**，因此永远不需要认识具体工具类。

### 3. `ToolManager` —— 框架门面

首页与工具之间唯一的数据通道：

```csharp
ToolManager.DiscoverFromAssembly(assembly);      // 自动扫描所有 ITool 实现并注册
ToolManager.Register(tool);                      // 手动补充注册
ToolManager.GetAll();                            // 所有工具
ToolManager.GetCategories();                     // 顶级分类（由工具声明汇总）
ToolManager.CreateAllDefinitions();              // 构建带用户状态的运行时条目
ToolManager.TopLevelCategory("计算/收益计算");    // → "计算"
```

自动发现规则：程序集中所有**非抽象、可实现、有无参构造函数**的 `ITool` 实现。

### 4. `NavigationService` + `ToolPageBase`

- `NavigationService`：持有当前页面与返回栈，只负责"显示哪个页面"。
- `ToolPageBase`：所有工具页面继承它，获得 `OnNavigatedTo` / `OnNavigatedFrom` 生命周期回调（保存状态、停止计时器等）。

### 5. `SettingsService`

JSON 持久化到 `%AppData%\MhxyToolbox\settings.json`：收藏列表、禁用列表、排序覆盖。配置损坏时回退到默认值，不影响启动。

---

## 四、首页 UI 结构

```text
┌──────────────────────────────────────────────────────────┐
│ 梦幻工具箱                    [🔍 搜索名称/描述/标签]      │  ← 标题 + 全局搜索
├──────────────┬───────────────────────────────────────────┤
│ 🏠 首页      │  ⭐ 我的收藏 (n)                          │
│ 📊 查询      │  ┌────────┐ ┌────────┐                   │  ← 收藏置顶分组
│ 🧮 计算      │  │ ToolCard│ │ ToolCard│                  │
│              │  └────────┘ └────────┘                   │
│ ⭐ 我的收藏  │  查询 (n)                                 │
│ ⚙ 设置       │  ┌────────┐                               │  ← 按分类自动分组
│              │  │ ToolCard│                              │
│              │  └────────┘                               │
└──────────────┴───────────────────────────────────────────┘
```

关键点：

- **侧边栏分类项是动态生成的**（`MainViewModel.BuildNavItems` 读取 `ToolManager.GetCategories()`），新增分类不需要改导航代码。
- **卡片用 `WrapPanel` 自动换行 + `ScrollViewer` 滚动**，不是固定的 2×3 / 3×3 网格，工具增到几十个也不会挤爆。
- **搜索是全局的**：只要输入关键词，就在全部工具中按「名称 / 描述 / 分类 / 标签」匹配，并自动把侧边栏切回"首页"，避免"显示全局结果却高亮某个分类"的矛盾状态。
- **空状态分场景提示**：搜索无结果 / 收藏为空 / 分类无工具，各有对应文案。
- 卡片交互：悬停上浮 + 金色描边 + 柔光，点击右上角 ☆ 收藏（立即写入配置文件），点击卡片主体打开工具页。

---

## 五、新增一个工具（三步）

以后你说"加一个召唤兽成长计算器"，实际改动只有：

**第 1 步**：新建文件夹 `src/MhxyToolbox/Tools/SummonGrowthCalculator/`

**第 2 步**：写工具类（声明元数据）

```csharp
public class SummonGrowthCalculatorTool : ITool
{
    public string Id => "tools.summon-growth";
    public string Name => "召唤兽成长计算器";
    public string Description => "根据资质与成长计算召唤兽最终属性。";
    public string Category => "计算/召唤兽计算";
    public string Icon => "🐾";
    public string[] Tags => ["召唤兽", "成长", "资质"];
    public string Version => "1.0.0";
    public int SortOrder => 300;

    public FrameworkElement CreateView() => new SummonGrowthView();
}
```

**第 3 步**：写页面（XAML + 继承 `ToolPageBase`）

```xml
<core:ToolPageBase x:Class="MhxyToolbox.Tools.SummonGrowthCalculator.SummonGrowthView" ...>
    <!-- 工具自己的界面 -->
</core:ToolPageBase>
```

**完成。** 重启程序后：工具自动注册 → 自动出现在「计算」分类 → 首页自动生成卡片 → 搜索/收藏自动可用。
**首页、导航、MainWindow 一行都不用改。**

如果新分类（如「召唤兽计算」）之前不存在，侧边栏会自动多出一项，无需手工维护分类列表。

---

## 六、设计红线（后续开发必须遵守）

- ❌ 不在 `MainForm` / `MainWindow` 里写任何具体工具逻辑
- ❌ 不用 `if (toolName == "xxx")` / `switch` 分发工具
- ❌ 不把工具按钮、工具页面写死在首页或外壳里
- ❌ 不让 `Core` 引用 `Tools`（依赖必须单向）
- ❌ 不为了演示效果破坏架构
- ✅ 新工具 = 新增文件夹 + 实现 `ITool` + 实现页面，其余全自动

---

## 七、已实现的工具

### 五色灵尘计算器（`Tools/FiveColorDust/`）

第一个真实工具，用于验证"平台 + 工具"架构在真实功能上的表现。

**合成规则**（用户提供）：

```text
1 级 = 1 个一级灵尘
2 级 = 2 个一级灵尘
N 级（N ≥ 3）= 2 个 (N-1) 级 + 1 个 (N-2) 级
```

由此合成 1 个 N 级所需的一级灵尘数量满足 `Q(1)=1, Q(2)=2, Q(N)=2×Q(N-1)+Q(N-2)`：

| 等级 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 需一级灵尘 | 1 | 2 | 5 | 12 | 29 | 70 | 169 | 408 | 985 | 2,378 |
| 累计数量 | 1 | 3 | 8 | 20 | 49 | 119 | 288 | 696 | 1,681 | 4,059 |

> 「累计数量」= 1 级到该级的数量之和。合成 1 个 N 级时，过程中涉及的各等级灵尘数量恰好等于 Q(1)…Q(N)，
> 所以累计数量也就是"整个过程需要处理的灵尘总数"。

**功能**：

- 打开即自动算出 1~10 级所需的一级灵尘数量与累计数量（无需任何输入）
- 输入「一级灵尘单价（万）」→ 显示每级的单级成本 / 综合成本（万梦幻币）
- 输入「金价：3000 万梦幻币折合人民币」→ 按比例折算出每级的人民币成本
- 非法输入给出提示且不崩溃；一键清空输入
- 单级成本 = 所需一级灵尘数 × 单价；综合成本 = 累计数量 × 单价

**计算模块是纯函数**（`FiveColorDustCalculator.cs`），与 UI 无关，可独立验证。
文件组织：`Calculator`（纯逻辑）/ `ViewModel`（输入与格式化）/ `View`（XAML 界面）/ `Tool`（注册元数据）。

---

## 八、第一阶段完成情况（已验证）

第一阶段只做三件事：整体 UI、可扩展模块架构、首页自动展示机制。已通过实际运行 + UI 自动化脚本逐项验证：

| 验证项 | 结果 |
| --- | --- |
| 工具注册 → 自动发现 → 首页自动生成卡片 | ✅ |
| 分类自动分组（计算 / 查询）+ 侧边栏分类动态生成 | ✅ |
| 搜索：名称 / 描述 / 分类 / 标签，跨分类全局匹配 | ✅ |
| 搜索无结果 → 空状态提示 | ✅ |
| 点击卡片 → 打开工具页面（含返回） | ✅ |
| 工具页内交互（示例计算器 6 + 3 = 9） | ✅ |
| 收藏（☆/★ 切换）+ 收藏置顶分组 | ✅ |
| 收藏状态持久化（重启后仍保留） | ✅ |
| 第二类工具页面（资料查询 + 页内搜索） | ✅ |
| 点侧边栏已选中项可从工具页返回（含 UIA 无障碍 Invoke） | ✅ |
| **新增工具不需要改首页/导航/MainWindow**（实测：加分类后自动出现） | ✅ |
| **五色灵尘计算器**：数列、成本、金价折算与独立复算一致 | ✅ |

前两个示例工具（示例计算器 / 游戏资料查询）**故意做得很简单**，它们的作用只是证明整条链路可用。

---

## 九、后续路线（按需逐步加）

平台层面（与具体工具无关）：

- 工具启用/禁用开关（`ToolDefinition.IsEnabled` 已具备，只需在设置页加 UI）
- 收藏排序、拖拽调整 `SortOrder`、最近使用
- 外置插件：`App.OnStartup` 中扫描 `plugins/*.dll` 并 `ToolManager.DiscoverFromAssembly`（入口已预留注释）
- 全局设置扩展（主题切换、窗口状态记忆）
- 工具级设置持久化（各工具自己的参数，例如五色灵尘的单价与金价记忆）

工具层面（真正开始攒功能，每次只加一个文件夹）：

- ✅ 五色灵尘计算器
- 内丹计算器 / 装备估价 / 宝石计算器 / 修炼计算器 / 技能计算器 / 灵饰计算器 / 召唤兽计算 /
  收益计算 / 五开收益 / 点化计算 / 打书计算 / 炼妖计算 / 经验计算 / 升级计算 / 资料查询 / 物品查询

---

## 十、开发期验证脚本

`.tools/` 下是 PowerShell + UI Automation 写的验证脚本，用于在无人工操作的情况下回归验证界面行为。需要时执行：

```powershell
powershell -ExecutionPolicy Bypass -File .tools\nav_state_test.ps1          # 导航 / 搜索状态
powershell -ExecutionPolicy Bypass -File .tools\final_test.ps1              # 首页 / 分组 / 设置页
powershell -ExecutionPolicy Bypass -File .tools\persist_test.ps1            # 收藏持久化 / 打开工具
powershell -ExecutionPolicy Bypass -File .tools\extensibility_test.ps1      # 新工具是否自动出现在首页
powershell -ExecutionPolicy Bypass -File .tools\verify_dust_math.ps1        # 五色灵尘数列独立复算
powershell -ExecutionPolicy Bypass -File .tools\dust_tool_test.ps1          # 五色灵尘计算器界面与成本
```

新增工具后可以用它们快速确认"新工具是否自动出现在首页"。

### 本机 git（免安装版）

本机没有安装 git（PATH 与常见安装位置均无），项目自带官方免安装版，不需要管理员权限、不写注册表、不改 PATH：

```powershell
# 位置
.tools\portablegit\cmd\git.exe

# 日常使用（在项目根目录）
& .tools\portablegit\cmd\git.exe status
& .tools\portablegit\cmd\git.exe log --oneline

# 换一台机器时重新获取（从 git-for-windows 官方 release 下载并解压到 .tools\portablegit）
powershell -ExecutionPolicy Bypass -File .tools\install_portablegit.ps1
```

仓库内的提交身份是本仓库独立配置（`git config user.name / user.email`），不改全局设置。
要改成自己的名字与邮箱：

```powershell
& .tools\portablegit\cmd\git.exe config user.name "你的名字"
& .tools\portablegit\cmd\git.exe config user.email "你的邮箱"
```

想在任何终端里直接用 `git` 命令，把 `.tools\portablegit\cmd` 加进系统 PATH 即可。

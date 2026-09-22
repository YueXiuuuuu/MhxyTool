namespace MhxyToolbox.Tools.GemSynthesis;

/// <summary>某一级宝石的合成消耗信息。</summary>
/// <param name="Level">等级（1~20）。</param>
/// <param name="ExtraMaterials">额外提交材料（12 级及以上），null 表示无需额外材料。</param>
/// <param name="ExtraMaterialText">额外提交材料的描述。</param>
/// <param name="Stamina">合成 1 个该级宝石消耗的体力。</param>
/// <param name="EquivalentOnes">总等效 1 级宝石数量（基础 + 额外）。</param>
public sealed record GemLevelInfo(
    int Level,
    IReadOnlyList<(int Level, int Count)>? ExtraMaterials,
    string ExtraMaterialText,
    int Stamina,
    int EquivalentOnes);

/// <summary>
/// 宝石合成计算（纯计算，不依赖 UI）。
///
/// 合成规则（2 颗上一级宝石合成 1 颗下一级宝石）：
///   1 级为基准宝石，无需合成；
///   N 级（N ≥ 2）= 2 颗 (N-1) 级宝石；
///   12 级及以上还需额外提交若干同类型宝石（见 ExtraMaterials 表）。
///
/// 总等效 1 级宝石数量（基础 + 额外）递推：
///   E(1) = 1, E(N) = 2 × E(N-1) + Σ 额外材料数量 × E(材料等级)
///   → 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024,
///     2100, 4456, 9680, 21716, 51012, 123740, 312628, 821724, 2392444
///
/// 体力消耗：合成 1 个 N 级宝石消耗的体力 = N × 10（1 级无合成，体力为 0）。
/// </summary>
public static class GemSynthesisCalculator
{
    /// <summary>最高等级。</summary>
    public const int MaxLevel = 20;

    /// <summary>体力消耗系数：合成体力 = 目标等级 × 10（1 级除外）。</summary>
    public const int StaminaPerLevel = 10;

    /// <summary>金价基准：3000 万梦幻币。</summary>
    public const decimal GoldPriceBaseCoinsWan = 3000m;

    /// <summary>12 级及以上的额外提交材料（同类型宝石）：等级 → (材料等级, 数量) 列表。</summary>
    private static readonly Dictionary<int, (int Level, int Count)[]> ExtraMaterialTable = new()
    {
        [12] = [(3, 1), (5, 1), (6, 1)],
        [13] = [(9, 1)],
        [14] = [(9, 1), (10, 1)],
        [15] = [(9, 1), (12, 1)],
        [16] = [(11, 1), (12, 1), (13, 1)],
        [17] = [(15, 1)],
        [18] = [(13, 1), (14, 1), (16, 1)],
        [19] = [(15, 1), (16, 1), (17, 1)],
        [20] = [(17, 1), (18, 2)],
    };

    private static readonly int[] EquivalentTable = BuildEquivalentOnes();

    private static readonly int[] CumulativeTable = BuildCumulativeEquivalents();

    /// <summary>1~20 级的消耗信息表。</summary>
    public static IReadOnlyList<GemLevelInfo> Levels { get; } = Enumerable
        .Range(1, MaxLevel)
        .Select(level =>
        {
            var extras = ExtraMaterialTable.GetValueOrDefault(level);
            return new GemLevelInfo(
                level,
                extras is null ? null : extras,
                DescribeExtras(extras),
                StaminaFor(level),
                EquivalentTable[level]);
        })
        .ToList();

    /// <summary>合成 1 个 level 级宝石消耗的体力（目标等级 × 10，1 级为 0）。</summary>
    public static int StaminaFor(int level) => level <= 1 ? 0 : level * StaminaPerLevel;

    /// <summary>总等效 1 级宝石数量（基础 + 额外）。</summary>
    public static int EquivalentOnes(int level) => EquivalentTable[level];

    /// <summary>1 级到 level 级各合成 1 颗的等效 1 级宝石数量累计。</summary>
    public static int CumulativeEquivalentOnes(int level) => CumulativeTable[level];

    /// <summary>等效 1 级宝石数量 × 一级宝石单价（万）→ 合成成本（万）。单价为空时返回 null。</summary>
    public static decimal? CostWan(int equivalentOnes, decimal? unitPriceWan)
        => unitPriceWan is null ? null : equivalentOnes * unitPriceWan.Value;

    /// <summary>
    /// 万梦幻币金额 → 人民币。金价含义：3000 万梦幻币折合 rmbPer30M 元。
    /// 即 1 万梦幻币 = rmbPer30M / 3000 元。
    /// </summary>
    public static decimal? CoinsWanToRmb(decimal? coinsWan, decimal? rmbPer30M)
        => coinsWan is null || rmbPer30M is null
            ? null
            : coinsWan.Value * rmbPer30M.Value / GoldPriceBaseCoinsWan;

    private static string DescribeExtras((int Level, int Count)[]? extras)
        => extras is null
            ? "—"
            : string.Join("、", extras.Select(e => e.Count == 1 ? $"{e.Level} 级 ×1" : $"{e.Level} 级 ×{e.Count}"));

    private static int[] BuildEquivalentOnes()
    {
        var equivalent = new int[MaxLevel + 1];
        equivalent[1] = 1;
        for (var level = 2; level <= MaxLevel; level++)
        {
            var total = 2 * equivalent[level - 1];
            if (ExtraMaterialTable.TryGetValue(level, out var extras))
                total += extras.Sum(e => e.Count * equivalent[e.Level]);
            equivalent[level] = total;
        }

        return equivalent;
    }

    private static int[] BuildCumulativeEquivalents()
    {
        var cumulative = new int[MaxLevel + 1];
        for (var level = 1; level <= MaxLevel; level++)
            cumulative[level] = cumulative[level - 1] + EquivalentTable[level];

        return cumulative;
    }
}

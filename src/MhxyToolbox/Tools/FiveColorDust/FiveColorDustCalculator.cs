namespace MhxyToolbox.Tools.FiveColorDust;

/// <summary>某一级五色灵尘的合成消耗信息。</summary>
/// <param name="Level">等级（1~10）。</param>
/// <param name="LevelOneNeeded">合成 1 个该级灵尘需要的一级灵尘数量。</param>
/// <param name="TotalQuantity">1 级到该级的数量累计（合成该级过程中涉及的全部灵尘数量）。</param>
/// <param name="Stamina">合成 1 个该级灵尘消耗的体力（目标等级 × 30）。</param>
public sealed record DustLevelInfo(int Level, int LevelOneNeeded, int TotalQuantity, int Stamina);

/// <summary>
/// 五色灵尘合成计算（纯计算，不依赖 UI）。
///
/// 合成规则：
///   1 级 = 1 个一级灵尘
///   2 级 = 2 个一级灵尘
///   N 级（N ≥ 3）= 2 个 (N-1) 级 + 1 个 (N-2) 级
/// 因此合成 1 个 N 级所需的一级灵尘数量 Q(N) 满足：
///   Q(1) = 1, Q(2) = 2, Q(N) = 2 × Q(N-1) + Q(N-2)
///   → 1, 2, 5, 12, 29, 70, 169, 408, 985, 2378
///
/// 体力消耗：合成 1 个 N 级灵尘消耗的体力 = N × 30。
/// </summary>
public static class FiveColorDustCalculator
{
    /// <summary>最高等级。</summary>
    public const int MaxLevel = 10;

    /// <summary>体力消耗系数：合成体力 = 目标等级 × 30。</summary>
    public const int StaminaPerLevel = 30;

    /// <summary>金价基准：3000 万梦幻币。</summary>
    public const decimal GoldPriceBaseCoinsWan = 3000m;

    private static readonly int[] LevelOneCosts = BuildLevelOneCosts();
    private static readonly int[] CumulativeQuantities = BuildCumulativeQuantities();

    /// <summary>1~10 级的消耗信息表。</summary>
    public static IReadOnlyList<DustLevelInfo> Levels { get; } = Enumerable
        .Range(1, MaxLevel)
        .Select(level => new DustLevelInfo(level, LevelOneCosts[level], CumulativeQuantities[level], StaminaFor(level)))
        .ToList();

    /// <summary>合成 1 个 level 级灵尘所需的一级灵尘数量。</summary>
    public static int LevelOneNeeded(int level) => LevelOneCosts[level];

    /// <summary>1 级到 level 级的数量累计。</summary>
    public static int TotalQuantity(int level) => CumulativeQuantities[level];

    /// <summary>合成 1 个 level 级灵尘消耗的体力（目标等级 × 30）。</summary>
    public static int StaminaFor(int level) => level * StaminaPerLevel;

    /// <summary>数量 × 一级灵尘单价（万）→ 成本（万）。单价为空时返回 null。</summary>
    public static decimal? CostWan(int quantity, decimal? unitPriceWan)
        => unitPriceWan is null ? null : quantity * unitPriceWan.Value;

    /// <summary>
    /// 万梦幻币金额 → 人民币。金价含义：3000 万梦幻币折合 rmbPer30M 元。
    /// 即 1 万梦幻币 = rmbPer30M / 3000 元。
    /// </summary>
    public static decimal? CoinsWanToRmb(decimal? coinsWan, decimal? rmbPer30M)
        => coinsWan is null || rmbPer30M is null
            ? null
            : coinsWan.Value * rmbPer30M.Value / GoldPriceBaseCoinsWan;

    private static int[] BuildLevelOneCosts()
    {
        var costs = new int[MaxLevel + 1];
        costs[1] = 1;
        if (MaxLevel >= 2)
            costs[2] = 2;

        for (var level = 3; level <= MaxLevel; level++)
            costs[level] = 2 * costs[level - 1] + costs[level - 2];

        return costs;
    }

    private static int[] BuildCumulativeQuantities()
    {
        var cumulative = new int[MaxLevel + 1];
        for (var level = 1; level <= MaxLevel; level++)
            cumulative[level] = cumulative[level - 1] + LevelOneCosts[level];

        return cumulative;
    }
}

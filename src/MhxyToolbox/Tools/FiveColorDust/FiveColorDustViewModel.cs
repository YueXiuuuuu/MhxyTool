using System.Collections.ObjectModel;
using System.Globalization;
using MhxyToolbox.Infrastructure;

namespace MhxyToolbox.Tools.FiveColorDust;

/// <summary>表格中的一行：某一级灵尘的消耗与成本。</summary>
public class DustLevelRow : ObservableObject
{
    private readonly DustLevelInfo _info;

    public DustLevelRow(DustLevelInfo info, bool isAlternate)
    {
        _info = info;
        IsAlternate = isAlternate;
    }

    public bool IsAlternate { get; }

    public string LevelText => $"{_info.Level} 级";
    public string LevelOneText => _info.LevelOneNeeded.ToString("#,##0");
    public string TotalQuantityText => _info.TotalQuantity.ToString("#,##0");
    public string StaminaText => _info.Stamina.ToString("#,##0");

    private string _unitCostWanText = Placeholder;
    public string UnitCostWanText { get => _unitCostWanText; private set => SetProperty(ref _unitCostWanText, value); }

    private string _totalCostWanText = Placeholder;
    public string TotalCostWanText { get => _totalCostWanText; private set => SetProperty(ref _totalCostWanText, value); }

    private string _unitCostRmbText = Placeholder;
    public string UnitCostRmbText { get => _unitCostRmbText; private set => SetProperty(ref _unitCostRmbText, value); }

    private string _totalCostRmbText = Placeholder;
    public string TotalCostRmbText { get => _totalCostRmbText; private set => SetProperty(ref _totalCostRmbText, value); }

    /// <summary>按当前单价与金价刷新本行成本列。</summary>
    public void Apply(decimal? unitPriceWan, decimal? rmbPer30M)
    {
        var unitCostWan = FiveColorDustCalculator.CostWan(_info.LevelOneNeeded, unitPriceWan);
        var totalCostWan = FiveColorDustCalculator.CostWan(_info.TotalQuantity, unitPriceWan);

        UnitCostWanText = FormatWan(unitCostWan);
        TotalCostWanText = FormatWan(totalCostWan);
        UnitCostRmbText = FormatRmb(FiveColorDustCalculator.CoinsWanToRmb(unitCostWan, rmbPer30M));
        TotalCostRmbText = FormatRmb(FiveColorDustCalculator.CoinsWanToRmb(totalCostWan, rmbPer30M));
    }

    private const string Placeholder = "—";

    /// <summary>万：整数不带小数，有小数时保留两位。</summary>
    internal static string FormatWan(decimal? value)
        => value is null ? Placeholder : value.Value.ToString("#,##0.##", CultureInfo.InvariantCulture);

    /// <summary>人民币：保留两位小数。</summary>
    internal static string FormatRmb(decimal? value)
        => value is null ? Placeholder : value.Value.ToString("#,##0.00", CultureInfo.InvariantCulture);
}

/// <summary>
/// 五色灵尘计算器。数量部分零输入即可算出；
/// 输入一级灵尘单价后显示梦幻币成本，输入金价后显示人民币成本。
/// </summary>
public class FiveColorDustViewModel : ObservableObject
{
    public FiveColorDustViewModel()
    {
        var rows = FiveColorDustCalculator.Levels
            .Select((info, index) => new DustLevelRow(info, index % 2 == 1))
            .ToList();

        Rows = new ObservableCollection<DustLevelRow>(rows);
        ResetCommand = new RelayCommand(_ => Reset());
        Recalculate();
    }

    public ObservableCollection<DustLevelRow> Rows { get; }

    public RelayCommand ResetCommand { get; }

    private string _unitPriceText = string.Empty;
    /// <summary>一级五色灵尘单价，单位：万。</summary>
    public string UnitPriceText
    {
        get => _unitPriceText;
        set { if (SetProperty(ref _unitPriceText, value)) Recalculate(); }
    }

    private string _goldPriceText = string.Empty;
    /// <summary>金价：3000 万梦幻币折合多少人民币（元）。</summary>
    public string GoldPriceText
    {
        get => _goldPriceText;
        set { if (SetProperty(ref _goldPriceText, value)) Recalculate(); }
    }

    private string _unitPriceHint = string.Empty;
    public string UnitPriceHint { get => _unitPriceHint; private set => SetProperty(ref _unitPriceHint, value); }

    private bool _hasUnitPriceError;
    public bool HasUnitPriceError { get => _hasUnitPriceError; private set => SetProperty(ref _hasUnitPriceError, value); }

    private string _goldPriceHint = string.Empty;
    public string GoldPriceHint { get => _goldPriceHint; private set => SetProperty(ref _goldPriceHint, value); }

    private bool _hasGoldPriceError;
    public bool HasGoldPriceError { get => _hasGoldPriceError; private set => SetProperty(ref _hasGoldPriceError, value); }

    // ---- 汇总（以最高等级 10 级为例）----

    private string _summaryHeader = string.Empty;
    public string SummaryHeader { get => _summaryHeader; private set => SetProperty(ref _summaryHeader, value); }

    private string _summaryUnitCost = string.Empty;
    public string SummaryUnitCost { get => _summaryUnitCost; private set => SetProperty(ref _summaryUnitCost, value); }

    private string _summaryTotalCost = string.Empty;
    public string SummaryTotalCost { get => _summaryTotalCost; private set => SetProperty(ref _summaryTotalCost, value); }

    private void Reset()
    {
        _unitPriceText = string.Empty;
        _goldPriceText = string.Empty;
        OnPropertyChanged(nameof(UnitPriceText));
        OnPropertyChanged(nameof(GoldPriceText));
        Recalculate();
    }

    private void Recalculate()
    {
        var unitPriceWan = ParseNumber(UnitPriceText, out var unitPriceInvalid);
        var rmbPer30M = ParseNumber(GoldPriceText, out var goldPriceInvalid);

        HasUnitPriceError = unitPriceInvalid;
        UnitPriceHint = unitPriceInvalid ? "请输入数字，例如 50（单位：万）" : string.Empty;

        HasGoldPriceError = goldPriceInvalid;
        GoldPriceHint = goldPriceInvalid ? "请输入数字，例如 30（单位：元）" : string.Empty;

        foreach (var row in Rows)
            row.Apply(unitPriceWan, rmbPer30M);

        UpdateSummary(unitPriceWan, rmbPer30M);
    }

    private void UpdateSummary(decimal? unitPriceWan, decimal? rmbPer30M)
    {
        var level = FiveColorDustCalculator.MaxLevel;
        var needed = FiveColorDustCalculator.LevelOneNeeded(level);
        var total = FiveColorDustCalculator.TotalQuantity(level);
        var stamina = FiveColorDustCalculator.StaminaFor(level);

        SummaryHeader = $"{level} 级五色灵尘：需一级灵尘 {needed.ToString("#,##0")} 个，"
                      + $"累计 {total.ToString("#,##0")} 个（含合成过程中的全部中间等级），"
                      + $"体力消耗 {stamina} 点";

        var unitCostWan = FiveColorDustCalculator.CostWan(needed, unitPriceWan);
        var totalCostWan = FiveColorDustCalculator.CostWan(total, unitPriceWan);

        SummaryUnitCost = DescribeCost(unitCostWan, rmbPer30M);
        SummaryTotalCost = DescribeCost(totalCostWan, rmbPer30M);
    }

    private static string DescribeCost(decimal? coinsWan, decimal? rmbPer30M)
    {
        if (coinsWan is null)
            return "—（输入一级灵尘单价后显示）";

        var wan = DustLevelRow.FormatWan(coinsWan);
        var rmb = FiveColorDustCalculator.CoinsWanToRmb(coinsWan, rmbPer30M);
        return rmb is null
            ? $"{wan} 万梦幻币"
            : $"{wan} 万梦幻币 ≈ {DustLevelRow.FormatRmb(rmb)} 元";
    }

    /// <summary>解析非负数字；空返回 null 且不算错误，非法输入标记 isInvalid。</summary>
    private static decimal? ParseNumber(string text, out bool isInvalid)
    {
        isInvalid = false;
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value >= 0)
            return value;

        isInvalid = true;
        return null;
    }
}

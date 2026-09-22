using System.Collections.ObjectModel;
using System.Globalization;
using MhxyToolbox.Infrastructure;

namespace MhxyToolbox.Tools.GemSynthesis;

/// <summary>表格中的一行：某一级宝石的合成消耗与成本。</summary>
public class GemLevelRow : ObservableObject
{
    private readonly GemLevelInfo _info;

    public GemLevelRow(GemLevelInfo info, bool isAlternate)
    {
        _info = info;
        IsAlternate = isAlternate;
    }

    public bool IsAlternate { get; }

    public string LevelText => $"{_info.Level} 级";
    public string ExtraMaterialText => _info.ExtraMaterialText;
    public string StaminaText => _info.Stamina.ToString("#,##0");
    public string EquivalentText => _info.EquivalentOnes.ToString("#,##0");

    private string _costWanText = Placeholder;
    public string CostWanText { get => _costWanText; private set => SetProperty(ref _costWanText, value); }

    private string _costRmbText = Placeholder;
    public string CostRmbText { get => _costRmbText; private set => SetProperty(ref _costRmbText, value); }

    private string _cumulativeCostWanText = Placeholder;
    public string CumulativeCostWanText { get => _cumulativeCostWanText; private set => SetProperty(ref _cumulativeCostWanText, value); }

    private string _cumulativeCostRmbText = Placeholder;
    public string CumulativeCostRmbText { get => _cumulativeCostRmbText; private set => SetProperty(ref _cumulativeCostRmbText, value); }

    private const string Placeholder = "—";

    /// <summary>按当前一级宝石单价与金价刷新本行成本列。</summary>
    public void Apply(decimal? unitPriceWan, decimal? rmbPer30M)
    {
        var costWan = GemSynthesisCalculator.CostWan(_info.EquivalentOnes, unitPriceWan);
        var cumulativeCostWan = GemSynthesisCalculator.CostWan(
            GemSynthesisCalculator.CumulativeEquivalentOnes(_info.Level), unitPriceWan);

        CostWanText = FormatWan(costWan);
        CostRmbText = FormatRmb(GemSynthesisCalculator.CoinsWanToRmb(costWan, rmbPer30M));
        CumulativeCostWanText = FormatWan(cumulativeCostWan);
        CumulativeCostRmbText = FormatRmb(GemSynthesisCalculator.CoinsWanToRmb(cumulativeCostWan, rmbPer30M));
    }

    /// <summary>万：整数不带小数，有小数时保留两位。</summary>
    internal static string FormatWan(decimal? value)
        => value is null ? Placeholder : value.Value.ToString("#,##0.##", CultureInfo.InvariantCulture);

    /// <summary>人民币：保留两位小数。</summary>
    internal static string FormatRmb(decimal? value)
        => value is null ? Placeholder : value.Value.ToString("#,##0.00", CultureInfo.InvariantCulture);
}

/// <summary>
/// 宝石合成计算器。数量部分零输入即可算出；
/// 输入一级宝石单价后显示各级合成成本，输入金价后显示人民币成本。
/// </summary>
public class GemSynthesisViewModel : ObservableObject
{
    public GemSynthesisViewModel()
    {
        Rows = new ObservableCollection<GemLevelRow>(
            GemSynthesisCalculator.Levels
                .Select((info, index) => new GemLevelRow(info, index % 2 == 1)));

        ResetCommand = new RelayCommand(_ => Reset());
        Recalculate();
    }

    public ObservableCollection<GemLevelRow> Rows { get; }

    public RelayCommand ResetCommand { get; }

    private string _unitPriceText = string.Empty;
    /// <summary>一级宝石单价，单位：万。</summary>
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

    // ---- 汇总（以最高等级 20 级为例）----

    private string _summaryHeader = string.Empty;
    public string SummaryHeader { get => _summaryHeader; private set => SetProperty(ref _summaryHeader, value); }

    private string _summaryCost = string.Empty;
    public string SummaryCost { get => _summaryCost; private set => SetProperty(ref _summaryCost, value); }

    private string _summaryCumulativeCost = string.Empty;
    public string SummaryCumulativeCost { get => _summaryCumulativeCost; private set => SetProperty(ref _summaryCumulativeCost, value); }

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
        UnitPriceHint = unitPriceInvalid ? "请输入数字，例如 8（单位：万）" : string.Empty;

        HasGoldPriceError = goldPriceInvalid;
        GoldPriceHint = goldPriceInvalid ? "请输入数字，例如 30（单位：元）" : string.Empty;

        foreach (var row in Rows)
            row.Apply(unitPriceWan, rmbPer30M);

        UpdateSummary(unitPriceWan, rmbPer30M);
    }

    private void UpdateSummary(decimal? unitPriceWan, decimal? rmbPer30M)
    {
        var level = GemSynthesisCalculator.MaxLevel;
        var equivalent = GemSynthesisCalculator.EquivalentOnes(level);
        var cumulative = GemSynthesisCalculator.CumulativeEquivalentOnes(level);
        var stamina = GemSynthesisCalculator.StaminaFor(level);

        SummaryHeader = $"{level} 级宝石：合成 1 颗等效 1 级宝石 {equivalent.ToString("#,##0")} 颗，"
                      + $"1~{level} 级各合成 1 颗累计 {cumulative.ToString("#,##0")} 颗，"
                      + $"体力消耗 {stamina} 点";

        var costWan = GemSynthesisCalculator.CostWan(equivalent, unitPriceWan);
        SummaryCost = costWan is null
            ? "合成成本：—（输入一级宝石单价后显示）"
            : DescribeCost("合成成本", costWan, rmbPer30M);

        var cumulativeCostWan = GemSynthesisCalculator.CostWan(cumulative, unitPriceWan);
        SummaryCumulativeCost = cumulativeCostWan is null
            ? "累计成本：—（输入一级宝石单价后显示）"
            : DescribeCost("累计成本", cumulativeCostWan, rmbPer30M);
    }

    private static string DescribeCost(string label, decimal? coinsWan, decimal? rmbPer30M)
    {
        var rmb = GemSynthesisCalculator.CoinsWanToRmb(coinsWan, rmbPer30M);
        return $"{label}：{GemLevelRow.FormatWan(coinsWan)} 万梦幻币"
             + (rmb is null ? "" : $" ≈ {GemLevelRow.FormatRmb(rmb)} 元");
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

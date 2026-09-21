using System.Windows;
using System.Windows.Controls;
using MhxyToolbox.Core.Navigation;

namespace MhxyToolbox.Tools.ExampleCalculator;

public partial class ExampleCalculatorView : ToolPageBase
{
    private string _operator = "+";

    public ExampleCalculatorView()
    {
        InitializeComponent();
    }

    private void OnOperatorChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string op })
            _operator = op;
    }

    private void OnCalculateClick(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(ValueA.Text, out var a) || !double.TryParse(ValueB.Text, out var b))
        {
            ResultText.Foreground = FindResource("ErrorBrush") as System.Windows.Media.Brush;
            ResultText.Text = "请输入有效的数字";
            return;
        }

        var result = _operator switch
        {
            "+" => a + b,
            "-" => a - b,
            "×" => a * b,
            "÷" => b == 0 ? double.NaN : a / b,
            _ => double.NaN,
        };

        if (double.IsNaN(result))
        {
            ResultText.Foreground = FindResource("ErrorBrush") as System.Windows.Media.Brush;
            ResultText.Text = "除数不能为 0";
            return;
        }

        ResultText.Foreground = FindResource("GoldBrightBrush") as System.Windows.Media.Brush;
        ResultText.Text = $"{a} {_operator} {b} = {result}";
    }
}

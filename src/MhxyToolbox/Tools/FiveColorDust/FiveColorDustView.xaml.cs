using MhxyToolbox.Core.Navigation;

namespace MhxyToolbox.Tools.FiveColorDust;

public partial class FiveColorDustView : ToolPageBase
{
    public FiveColorDustView()
    {
        InitializeComponent();
        DataContext = new FiveColorDustViewModel();
    }
}

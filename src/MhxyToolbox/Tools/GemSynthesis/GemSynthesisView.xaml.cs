using MhxyToolbox.Core.Navigation;

namespace MhxyToolbox.Tools.GemSynthesis;

public partial class GemSynthesisView : ToolPageBase
{
    public GemSynthesisView()
    {
        InitializeComponent();
        DataContext = new GemSynthesisViewModel();
    }
}

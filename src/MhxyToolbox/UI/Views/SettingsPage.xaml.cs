using System.Windows.Controls;
using MhxyToolbox.UI.ViewModels;

namespace MhxyToolbox.UI.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}

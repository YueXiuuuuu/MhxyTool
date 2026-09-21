using System.Windows.Controls;
using MhxyToolbox.Core.Navigation;

namespace MhxyToolbox.Tools.GameInfo;

public partial class GameInfoView : ToolPageBase
{
    private static readonly List<GameInfoEntry> AllEntries = GameInfoData.CreateDefault();

    public GameInfoView()
    {
        InitializeComponent();
        ApplyFilter(string.Empty);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchBox is not null)
            ApplyFilter(SearchBox.Text);
    }

    private void ApplyFilter(string keyword)
    {
        keyword = keyword.Trim();

        var items = string.IsNullOrEmpty(keyword)
            ? AllEntries
            : AllEntries.Where(x =>
                    x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.Type.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.Keywords.Any(k => k.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .ToList();

        EntryList.ItemsSource = items;
        if (items.Count > 0)
            EntryList.SelectedIndex = 0;
    }

    private void OnEntrySelected(object sender, SelectionChangedEventArgs e)
    {
        if (EntryList.SelectedItem is not GameInfoEntry entry)
            return;

        EntryTitle.Text = entry.Name;
        EntryType.Text = entry.Type;
        EntryDesc.Text = entry.Description;
    }
}

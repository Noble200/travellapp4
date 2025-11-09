using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views;

public partial class TravelPacksView : UserControl
{
    public TravelPacksView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
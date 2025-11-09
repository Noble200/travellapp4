using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views;

public partial class FlightTicketsView : UserControl
{
    public FlightTicketsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
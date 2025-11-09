using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views;

public partial class FoodPacksView : UserControl
{
    public FoodPacksView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
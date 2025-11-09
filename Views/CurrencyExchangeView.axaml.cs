using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views;

public partial class CurrencyExchangeView : UserControl
{
    public CurrencyExchangeView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
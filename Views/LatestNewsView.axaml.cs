using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views;

public partial class LatestNewsView : UserControl
{
    public LatestNewsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
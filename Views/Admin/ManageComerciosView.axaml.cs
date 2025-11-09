using Avalonia.Controls;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin;

/// <summary>
/// Vista para el módulo de Gestión de Comercios
/// Code-behind mínimo siguiendo patrón MVVM
/// </summary>
public partial class ManageComerciosView : UserControl
{
    public ManageComerciosView()
    {
        InitializeComponent();
        DataContext = new ManageComerciosViewModel();
    }
}
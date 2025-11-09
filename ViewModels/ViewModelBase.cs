using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// Clase base para todos los ViewModels de administración
/// Proporciona propiedades comunes
/// </summary>
public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _titulo = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private bool _mostrarError = false;
}
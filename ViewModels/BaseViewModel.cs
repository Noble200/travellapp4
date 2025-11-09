using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// Clase base para todos los ViewModels de los módulos
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _titulo = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;
}
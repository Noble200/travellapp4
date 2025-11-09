using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo que representa un Rol del sistema
/// </summary>
public partial class RolModel : ObservableObject
{
    /// <summary>
    /// ID del rol
    /// </summary>
    [ObservableProperty]
    private int _idRol;

    /// <summary>
    /// Nombre del rol (Administrador, Gerente, Empleado, etc.)
    /// </summary>
    [ObservableProperty]
    private string _nombreRol = string.Empty;

    /// <summary>
    /// Descripción del rol
    /// </summary>
    [ObservableProperty]
    private string _descripcion = string.Empty;

    /// <summary>
    /// Indica si el rol está activo
    /// </summary>
    [ObservableProperty]
    private bool _activo = true;

    /// <summary>
    /// Nivel de acceso (1-5, siendo 5 el más alto)
    /// </summary>
    [ObservableProperty]
    private int _nivelAcceso;

    /// <summary>
    /// Color asociado al rol para la UI
    /// </summary>
    public string ColorRol => NombreRol switch
    {
        "Administrador" => "#0b5394",
        "Gerente" => "#ffd966",
        "Empleado" => "#595959",
        _ => "#595959"
    };
}
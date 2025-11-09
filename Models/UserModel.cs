using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo de usuario del sistema
/// </summary>
public class UserModel : INotifyPropertyChanged
{
    private bool _activo;
    
    public int IdUsuario { get; set; }
    public string NumeroUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public bool EsFlotante { get; set; }
    
    public bool Activo
    {
        get => _activo;
        set
        {
            if (_activo != value)
            {
                _activo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EstadoTexto));
                OnPropertyChanged(nameof(EstadoColor));
                OnPropertyChanged(nameof(EstadoBotonTexto));
                OnPropertyChanged(nameof(EstadoBotonColor));
            }
        }
    }
    
    public DateTime? UltimoAcceso { get; set; }
    
    // ============================================
    // PROPIEDADES DE NAVEGACIÓN
    // ============================================
    
    public int? IdLocalPrincipal { get; set; }
    public string? NombreLocal { get; set; }
    public string? NombreComercio { get; set; }
    
    // ============================================
    // PROPIEDADES CALCULADAS PARA UI
    // ============================================
    
    public string NombreCompleto => $"{Nombre} {Apellidos}";
    
    public string TipoUsuario => EsFlotante ? "Flotante" : "Normal";
    
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
    
    public string EstadoColor => Activo ? "#28a745" : "#dc3545";
    
    public string UltimoAccesoTexto => UltimoAcceso.HasValue
        ? UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm")
        : "Nunca";
    
    // ============================================
    // PROPIEDADES ADICIONALES PARA MÓDULO DE GESTIÓN
    // ============================================
    
    public int IdComercio { get; set; }
    public int IdLocal { get; set; }
    public string CodigoLocal { get; set; } = string.Empty;
    
    public string EstadoBotonTexto => Activo ? "Desactivar" : "Activar";
    public string EstadoBotonColor => Activo ? "#dc3545" : "#28a745";
    
    // ============================================
    // INotifyPropertyChanged
    // ============================================
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
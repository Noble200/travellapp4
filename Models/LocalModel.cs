using System.Collections.Generic;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo auxiliar para gestionar locales en el formulario
/// ACTUALIZADO: Con todos los campos de la base de datos + campos según Datos_Allva.txt
/// </summary>
public class LocalFormModel
{
    public int IdLocal { get; set; }
    
    /// <summary>
    /// ID del comercio al que pertenece este local
    /// IMPORTANTE: Propiedad agregada para compatibilidad con ManageUsersViewModel
    /// </summary>
    public int IdComercio { get; set; }
    
    public string CodigoLocal { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    
    // ============================================
    // UBICACIÓN GEOGRÁFICA - NUEVOS CAMPOS
    // ============================================
    
    /// <summary>
    /// País donde se ubica el local
    /// </summary>
    public string Pais { get; set; } = string.Empty;
    
    /// <summary>
    /// Código postal del local
    /// </summary>
    public string CodigoPostal { get; set; } = string.Empty;
    
    // ============================================
    // DIRECCIÓN COMPLETA (SEGÚN BASE DE DATOS)
    // ============================================
    
    /// <summary>
    /// Tipo de vía (Calle, Avenida, Pasaje, etc)
    /// </summary>
    public string TipoVia { get; set; } = string.Empty;
    
    /// <summary>
    /// Dirección principal (calle, avenida, etc)
    /// </summary>
    public string Direccion { get; set; } = string.Empty;
    
    /// <summary>
    /// Número del local/establecimiento
    /// </summary>
    public string LocalNumero { get; set; } = string.Empty;
    
    /// <summary>
    /// Escalera (si aplica)
    /// </summary>
    public string? Escalera { get; set; }
    
    /// <summary>
    /// Piso (si aplica)
    /// </summary>
    public string? Piso { get; set; }
    
    // ============================================
    // DATOS DE CONTACTO Y GESTIÓN
    // ============================================
    
    /// <summary>
    /// Teléfono de contacto del local
    /// </summary>
    public string? Telefono { get; set; }
    
    /// <summary>
    /// Email de contacto del local
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Número máximo de usuarios permitidos en este local
    /// </summary>
    public int NumeroUsuariosMax { get; set; } = 10;
    
    /// <summary>
    /// Observaciones adicionales del local
    /// </summary>
    public string? Observaciones { get; set; }
    
    // ============================================
    // ESTADO Y FECHAS
    // ============================================
    
    public bool Activo { get; set; } = true;
    
    // ============================================
    // PERMISOS DE MÓDULOS POR LOCAL
    // ============================================
    
    public bool ModuloDivisas { get; set; } = false;
    public bool ModuloPackAlimentos { get; set; } = false;
    public bool ModuloBilletesAvion { get; set; } = false;
    public bool ModuloPackViajes { get; set; } = false;
    
    // ============================================
    // CONFIGURACIÓN DE COMISIONES - NUEVO CAMPO
    // ============================================
    
    /// <summary>
    /// Porcentaje de comisión de divisas específico para este local
    /// </summary>
    public decimal ComisionDivisas { get; set; } = 0;
    
    // ============================================
    // PROPIEDADES CALCULADAS
    // ============================================
    
    /// <summary>
    /// Dirección completa formateada para mostrar
    /// </summary>
    public string DireccionCompleta
    {
        get
        {
            var partes = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(TipoVia))
                partes.Add(TipoVia);
            
            if (!string.IsNullOrWhiteSpace(Direccion))
                partes.Add(Direccion);
            
            if (!string.IsNullOrWhiteSpace(LocalNumero))
                partes.Add($"Nº {LocalNumero}");
            
            if (!string.IsNullOrWhiteSpace(Escalera))
                partes.Add($"Esc. {Escalera}");
            
            if (!string.IsNullOrWhiteSpace(Piso))
                partes.Add($"Piso {Piso}");
            
            if (!string.IsNullOrWhiteSpace(CodigoPostal))
                partes.Add($"CP {CodigoPostal}");
                
            if (!string.IsNullOrWhiteSpace(Pais))
                partes.Add(Pais);
            
            return partes.Count > 0 
                ? string.Join(", ", partes) 
                : "Sin dirección";
        }
    }
    
    /// <summary>
    /// Resumen de permisos para mostrar en UI
    /// </summary>
    public string PermisosResumen
    {
        get
        {
            var permisos = new List<string>();
            if (ModuloDivisas) permisos.Add("Divisas");
            if (ModuloPackAlimentos) permisos.Add("Alimentos");
            if (ModuloBilletesAvion) permisos.Add("Billetes");
            if (ModuloPackViajes) permisos.Add("Viajes");
            
            return permisos.Count > 0 
                ? string.Join(", ", permisos) 
                : "Sin módulos";
        }
    }
}
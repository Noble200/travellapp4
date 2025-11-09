using System;

namespace Allva.Desktop.Models.Admin;

/// <summary>
/// Modelo de datos para Administradores Allva
/// Representa usuarios del Back Office que gestionan el sistema
/// NO se asignan a locales - Completamente separados de usuarios normales
/// </summary>
public class AdministradorAllvaModel
{
    // ============================================
    // IDENTIFICACIÓN PRINCIPAL
    // ============================================

    public int IdAdministrador { get; set; }

    // ============================================
    // DATOS PERSONALES
    // ============================================

    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellidos}";

    // ============================================
    // AUTENTICACIÓN
    // ============================================

    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // ============================================
    // DATOS DE CONTACTO
    // ============================================

    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }

    // ============================================
    // PERMISOS DEL PANEL DE ADMINISTRACIÓN
    // ============================================

    public bool AccesoGestionComercios { get; set; } = true;
    public bool AccesoGestionUsuariosLocales { get; set; } = true;
    public bool AccesoGestionUsuariosAllva { get; set; } = false;
    public bool AccesoAnalytics { get; set; } = false;
    public bool AccesoConfiguracionSistema { get; set; } = false;
    public bool AccesoFacturacionGlobal { get; set; } = false;
    public bool AccesoAuditoria { get; set; } = false;

    // ============================================
    // SEGURIDAD Y CONTROL
    // ============================================

    public bool Activo { get; set; } = true;
    public bool PrimerLogin { get; set; } = true;
    public int IntentosFallidos { get; set; } = 0;
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime? UltimoAcceso { get; set; }

    // ============================================
    // AUDITORÍA
    // ============================================

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaModificacion { get; set; } = DateTime.Now;
    public string? CreadoPor { get; set; }

    // ============================================
    // CONFIGURACIÓN
    // ============================================

    public string Idioma { get; set; } = "es";

    // ============================================
    // PROPIEDADES CALCULADAS PARA UI
    // ============================================

    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
    public string EstadoColor => Activo ? "#28a745" : "#dc3545";
    
    public string Iniciales
    {
        get
        {
            var inicialNombre = !string.IsNullOrEmpty(Nombre) ? Nombre[0].ToString().ToUpper() : "";
            var inicialApellido = !string.IsNullOrEmpty(Apellidos) ? Apellidos[0].ToString().ToUpper() : "";
            return inicialNombre + inicialApellido;
        }
    }

    public string TipoAdminColor => EsSuperAdministrador ? "#dc3545" : "#ffc107";
    public string EstadoBotonTexto => Activo ? "Desactivar" : "Activar";
    public string EstadoBotonColor => Activo ? "#dc3545" : "#28a745";
    public bool EsSuperAdministrador => AccesoGestionUsuariosAllva;
    public string TipoAdministrador => EsSuperAdministrador ? "Super Admin" : "Admin Limitado";
    public bool EstaBloqueado => BloqueadoHasta.HasValue && BloqueadoHasta.Value > DateTime.Now;

    // ⭐ NUEVA PROPIEDAD REQUERIDA
    public string UltimoAccesoTexto
    {
        get
        {
            if (UltimoAcceso == null)
                return "Nunca";

            var diferencia = DateTime.Now - UltimoAcceso.Value;

            if (diferencia.TotalMinutes < 1)
                return "Hace unos segundos";
            if (diferencia.TotalMinutes < 60)
                return $"Hace {(int)diferencia.TotalMinutes} min";
            if (diferencia.TotalHours < 24)
                return $"Hace {(int)diferencia.TotalHours} h";
            if (diferencia.TotalDays < 30)
                return $"Hace {(int)diferencia.TotalDays} días";
            if (diferencia.TotalDays < 365)
                return $"Hace {(int)(diferencia.TotalDays / 30)} meses";

            return UltimoAcceso.Value.ToString("dd/MM/yyyy");
        }
    }

    public int CantidadModulosAcceso
    {
        get
        {
            int count = 0;
            if (AccesoGestionComercios) count++;
            if (AccesoGestionUsuariosLocales) count++;
            if (AccesoGestionUsuariosAllva) count++;
            if (AccesoAnalytics) count++;
            if (AccesoConfiguracionSistema) count++;
            if (AccesoFacturacionGlobal) count++;
            if (AccesoAuditoria) count++;
            return count;
        }
    }

    public string ModulosConAcceso
    {
        get
        {
            var modulos = new System.Collections.Generic.List<string>();
            
            if (AccesoGestionComercios) modulos.Add("Comercios");
            if (AccesoGestionUsuariosLocales) modulos.Add("Usuarios");
            if (AccesoGestionUsuariosAllva) modulos.Add("Admins Allva");
            if (AccesoAnalytics) modulos.Add("Analytics");
            if (AccesoConfiguracionSistema) modulos.Add("Configuración");
            if (AccesoFacturacionGlobal) modulos.Add("Facturación");
            if (AccesoAuditoria) modulos.Add("Auditoría");

            return string.Join(", ", modulos);
        }
    }

    public string TiempoDesdeUltimoAcceso
    {
        get
        {
            if (!UltimoAcceso.HasValue)
                return "Nunca";

            var tiempo = DateTime.Now - UltimoAcceso.Value;

            if (tiempo.TotalMinutes < 60)
                return $"Hace {(int)tiempo.TotalMinutes} min";
            else if (tiempo.TotalHours < 24)
                return $"Hace {(int)tiempo.TotalHours} h";
            else
                return $"Hace {(int)tiempo.TotalDays} días";
        }
    }

    public bool TienePermiso(string nombreModulo)
    {
        return nombreModulo.ToLower() switch
        {
            "comercios" => AccesoGestionComercios,
            "usuarios" => AccesoGestionUsuariosLocales,
            "usuarios_allva" => AccesoGestionUsuariosAllva,
            "analytics" => AccesoAnalytics,
            "configuracion" => AccesoConfiguracionSistema,
            "facturacion" => AccesoFacturacionGlobal,
            "auditoria" => AccesoAuditoria,
            _ => false
        };
    }
}
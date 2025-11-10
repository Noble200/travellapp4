using System;
using System.Collections.Generic;
using System.Linq;

namespace Allva.Desktop.Models.Admin;

/// <summary>
/// Modelo de datos para Administradores Allva
/// Representa usuarios del Back Office que gestionan el sistema
/// NO se asignan a locales - Completamente separados de usuarios normales
/// ACTUALIZADO: Sistema de 4 niveles + módulos habilitados
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
    // ⭐ NUEVO: SISTEMA DE NIVELES DE ACCESO
    // ============================================

    /// <summary>
    /// Nivel de acceso del administrador (1-4)
    /// 1 = Visualizador, 2 = Admin Limitado, 3 = Admin Completo, 4 = Super Admin
    /// </summary>
    public int NivelAcceso { get; set; } = 1;

    /// <summary>
    /// Lista de módulos habilitados para este administrador
    /// Se almacena en la tabla admin_modulos_habilitados
    /// </summary>
    public List<string> ModulosHabilitados { get; set; } = new();

    // ============================================
    // PERMISOS DEL PANEL DE ADMINISTRACIÓN (LEGACY)
    // Se mantienen por compatibilidad con el sistema existente
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

    public string EstadoTexto => Activo ? "Activo" : "Bloqueado";
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

    // Botón de cambio de estado
    public string EstadoBotonTexto => Activo ? "Bloquear" : "Activar";
    public string EstadoBotonColor => Activo ? "#dc3545" : "#28a745";

    // ============================================
    // ⭐ PROPIEDADES BASADAS EN NIVELES
    // ============================================

    public bool EsSuperAdministrador => NivelAcceso == 4;
    
    public string NombreNivel => NivelAcceso switch
    {
        1 => "Nivel 1",
        2 => "Nivel 2",
        3 => "Nivel 3",
        4 => "Nivel 4",
        _ => "Sin nivel"
    };

    public string DescripcionNivel => NivelAcceso switch
    {
        1 => "Puede ver y editar pestañas de un módulo concreto (Balance, Operaciones o Informes).",
        2 => "Puede ver y editar módulos concretos, dar altas y editar comercios o usuarios del módulo asignado.",
        3 => "Puede ver y editar todos los módulos excepto crear usuarios de Allva.",
        4 => "Acceso total: puede crear, editar y aprobar nuevos usuarios Allva y gestionar niveles de acceso.",
        _ => "Sin descripción"
    };

    public string ColorNivel => NivelAcceso switch
    {
        1 => "#6c757d", // Gris
        2 => "#17a2b8", // Azul claro
        3 => "#ffc107", // Amarillo
        4 => "#dc3545", // Rojo
        _ => "#6c757d"
    };

    public string TipoAdministrador => NivelAcceso switch
    {
        1 => "Visualizador",
        2 => "Admin Limitado",
        3 => "Admin Completo",
        4 => "Super Admin",
        _ => "Sin nivel"
    };

    // ============================================
    // ⭐ MÓDULOS HABILITADOS (TEXTO PARA UI)
    // ============================================

    public string ModulosHabilitadosTexto
    {
        get
        {
            if (NivelAcceso >= 3)
            {
                return "Todos los módulos (por nivel de acceso)";
            }

            if (ModulosHabilitados == null || !ModulosHabilitados.Any())
            {
                return "Ningún módulo asignado";
            }

            var nombresLegibles = ModulosHabilitados.Select(codigo => codigo switch
            {
                "compra_divisa" => "Compra de Divisa",
                "packs_alimentos" => "Packs de Alimentos",
                "billetes_avion" => "Billetes de Avión",
                "pack_viajes" => "Pack de Viajes",
                _ => codigo
            });

            return string.Join(", ", nombresLegibles);
        }
    }

    // ============================================
    // PROPIEDADES DE TIEMPO
    // ============================================

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

    // ============================================
    // MÉTODOS LEGACY (SE MANTIENEN POR COMPATIBILIDAD)
    // ============================================

    public bool EstaBloqueado => BloqueadoHasta.HasValue && BloqueadoHasta.Value > DateTime.Now;

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
            var modulos = new List<string>();
            
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

    public bool TienePermiso(string nombreModulo)
    {
        // Sistema de niveles tiene prioridad
        if (NivelAcceso >= 3)
            return true; // Nivel 3 y 4 tienen acceso a todo

        // Verificar módulos específicos habilitados
        if (ModulosHabilitados != null && ModulosHabilitados.Any())
        {
            return nombreModulo.ToLower() switch
            {
                "compra_divisa" => ModulosHabilitados.Contains("compra_divisa"),
                "packs_alimentos" => ModulosHabilitados.Contains("packs_alimentos"),
                "billetes_avion" => ModulosHabilitados.Contains("billetes_avion"),
                "pack_viajes" => ModulosHabilitados.Contains("pack_viajes"),
                _ => false
            };
        }

        // Fallback a permisos legacy
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

    /// <summary>
    /// Verifica si el administrador puede gestionar otros administradores
    /// Solo nivel 4 (Super Admin) puede hacerlo
    /// </summary>
    public bool PuedeGestionarAdministradores => NivelAcceso == 4;

    /// <summary>
    /// Verifica si el administrador puede gestionar comercios y usuarios de locales
    /// Niveles 2, 3 y 4 pueden hacerlo
    /// </summary>
    public bool PuedeGestionarComerciosYUsuarios => NivelAcceso >= 2;

    /// <summary>
    /// Verifica si el administrador tiene acceso a analytics
    /// Niveles 3 y 4 tienen acceso
    /// </summary>
    public bool TieneAccesoAnalytics => NivelAcceso >= 3;
}

/// <summary>
/// Modelo para niveles de acceso (tabla: niveles_acceso)
/// </summary>
public class NivelAccesoModel
{
    public int IdNivel { get; set; }
    public string NombreNivel { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}
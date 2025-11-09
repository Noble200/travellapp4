using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Allva.Desktop.Views;
using Allva.Desktop.Views.Admin;
using Allva.Desktop.ViewModels;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Services;

/// <summary>
/// Servicio de navegación para cambiar entre vistas
/// ACTUALIZADO: Incluye navegación al panel de administración del sistema
/// </summary>
public class NavigationService
{
    public event EventHandler<object>? NavigationRequested;

    /// <summary>
    /// Navega a una vista específica
    /// </summary>
    public void NavigateTo(string viewName, object? parameter = null)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null) return;

            UserControl? newView = viewName.ToLower() switch
            {
                "login" => CreateLoginView(),
                "maindashboard" or "dashboard" => CreateMainDashboardView(parameter),
                "admindashboard" or "admin" => CreateAdminDashboardView(parameter),
                _ => null
            };

            if (newView != null)
            {
                mainWindow.Content = newView;
                mainWindow.Title = $"Allva System - {GetViewTitle(viewName)}";
                NavigationRequested?.Invoke(this, newView);
            }
        }
    }

    /// <summary>
    /// Navega al dashboard principal después de un login exitoso de usuario normal
    /// </summary>
    public void NavigateToDashboard(LoginSuccessData? loginData = null)
    {
        NavigateTo("maindashboard", loginData);
    }

    /// <summary>
    /// ⭐ NUEVO: Navega al panel de administración del sistema
    /// Solo para usuarios con rol "Administrador_Sistema"
    /// </summary>
    public void NavigateToAdminDashboard(LoginSuccessData? loginData = null)
    {
        NavigateTo("admindashboard", loginData);
    }

    /// <summary>
    /// Navega al TestPanel (mantener compatibilidad con código existente)
    /// AHORA redirige al MainDashboard
    /// </summary>
    public void NavigateToTestPanel(LoginSuccessData loginData)
    {
        NavigateToDashboard(loginData);
    }

    /// <summary>
    /// Vuelve al login
    /// </summary>
    public void NavigateToLogin()
    {
        NavigateTo("login");
    }

    // ============================================
    // MÉTODOS PRIVADOS PARA CREAR VISTAS
    // ============================================

    private UserControl CreateLoginView()
    {
        var view = new LoginView();
        view.DataContext = new LoginViewModel();
        return view;
    }

    private UserControl CreateMainDashboardView(object? parameter)
    {
        var view = new MainDashboardView();
        
        if (parameter is LoginSuccessData loginData)
        {
            view.DataContext = new MainDashboardViewModel(loginData.UserName, loginData.LocalCode);
        }
        else
        {
            view.DataContext = new MainDashboardViewModel();
        }
        
        return view;
    }

    /// <summary>
    /// ⭐ NUEVO: Crea vista del panel de administración del sistema
    /// </summary>
    private UserControl CreateAdminDashboardView(object? parameter)
    {
        var view = new AdminDashboardView();
        
        if (parameter is LoginSuccessData loginData)
        {
            view.DataContext = new AdminDashboardViewModel(loginData);
        }
        else
        {
            view.DataContext = new AdminDashboardViewModel();
        }
        
        return view;
    }

    /// <summary>
    /// Obtiene el título legible de la vista
    /// </summary>
    private string GetViewTitle(string viewName)
    {
        return viewName.ToLower() switch
        {
            "login" => "Login",
            "maindashboard" or "dashboard" => "Panel Principal",
            "admindashboard" or "admin" => "Panel de Administración",
            _ => "Allva System"
        };
    }
}

/// <summary>
/// Datos del login exitoso
/// ACTUALIZADO: Incluye información de rol y tipo de administrador
/// </summary>
public class LoginSuccessData
{
    public string UserName { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public string LocalCode { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// ⭐ NUEVO: Indica si el usuario es administrador del sistema
    /// </summary>
    public bool IsSystemAdmin { get; set; } = false;
    
    /// <summary>
    /// ⭐ NUEVO: Nombre del rol del usuario
    /// </summary>
    public string RoleName { get; set; } = string.Empty;
    
    /// <summary>
    /// ⭐ NUEVO: Permisos específicos del administrador
    /// </summary>
    public PermisosAdministrador? Permisos { get; set; }
}

/// <summary>
/// Permisos granulares para administradores Allva
/// Define a qué módulos del AdminDashboard puede acceder cada administrador
/// </summary>
public class PermisosAdministrador
{
    /// <summary>
    /// Acceso al módulo "Gestión de Comercios"
    /// </summary>
    public bool AccesoGestionComercios { get; set; } = true;

    /// <summary>
    /// Acceso al módulo "Gestión de Usuarios" (usuarios de locales)
    /// </summary>
    public bool AccesoGestionUsuariosLocales { get; set; } = true;

    /// <summary>
    /// Acceso al módulo "Usuarios Allva" (gestionar otros administradores)
    /// Solo super administradores tienen este permiso
    /// </summary>
    public bool AccesoGestionUsuariosAllva { get; set; } = false;

    /// <summary>
    /// Acceso a Analytics e Informes (futuro)
    /// </summary>
    public bool AccesoAnalytics { get; set; } = false;

    /// <summary>
    /// Acceso a Configuración del Sistema (futuro)
    /// </summary>
    public bool AccesoConfiguracionSistema { get; set; } = false;

    /// <summary>
    /// Acceso a Facturación Global (futuro)
    /// </summary>
    public bool AccesoFacturacionGlobal { get; set; } = false;

    /// <summary>
    /// Acceso a Auditoría (futuro)
    /// </summary>
    public bool AccesoAuditoria { get; set; } = false;

    /// <summary>
    /// Verifica si tiene permiso para un módulo específico
    /// </summary>
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

    /// <summary>
    /// Indica si es un Super Administrador (tiene permiso para gestionar otros admins)
    /// </summary>
    public bool EsSuperAdministrador => AccesoGestionUsuariosAllva;
}
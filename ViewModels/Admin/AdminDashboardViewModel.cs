using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Services;
using Allva.Desktop.Views.Admin;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel para el Panel de Administración
/// Exclusivo para administradores del sistema
/// MÓDULOS: Gestión de Comercios, Gestión de Usuarios y Usuarios Allva
/// </summary>
public partial class AdminDashboardViewModel : ObservableObject
{
    private readonly NavigationService? _navigationService;
    private Allva.Desktop.Services.PermisosAdministrador? _permisos;
    
    // ============================================
    // PROPIEDADES OBSERVABLES
    // ============================================

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _adminName = "Administrador";

    [ObservableProperty]
    private string _selectedModule = "comercios";

    /// <summary>
    /// Título del módulo seleccionado en mayúsculas para mostrar en UI
    /// </summary>
    public string SelectedModuleTitle => SelectedModule switch
    {
        "comercios" => "GESTIÓN DE COMERCIOS",
        "usuarios" => "GESTIÓN DE USUARIOS",
        "usuarios_allva" => "USUARIOS ALLVA",
        _ => "PANEL DE ADMINISTRACIÓN"
    };

    // ============================================
    // PROPIEDADES PARA VISIBILIDAD DE MÓDULOS
    // ============================================

    /// <summary>
    /// Controla si se muestra el módulo "Gestión de Comercios"
    /// </summary>
    public bool MostrarGestionComercios => _permisos?.AccesoGestionComercios ?? true;

    /// <summary>
    /// Controla si se muestra el módulo "Gestión de Usuarios" (de locales)
    /// </summary>
    public bool MostrarGestionUsuarios => _permisos?.AccesoGestionUsuariosLocales ?? true;

    /// <summary>
    /// Controla si se muestra el módulo "Usuarios Allva" (administradores)
    /// Solo super administradores tienen acceso
    /// </summary>
    public bool MostrarUsuariosAllva => _permisos?.AccesoGestionUsuariosAllva ?? false;

    // ============================================
    // CONSTRUCTORES
    // ============================================

    public AdminDashboardViewModel()
    {
        // Cargar vista inicial: Gestión de Comercios
        NavigateToModule("comercios");
    }

    /// <summary>
    /// Constructor con nombre del administrador (compatibilidad)
    /// </summary>
    public AdminDashboardViewModel(string adminName)
    {
        AdminName = adminName;
        NavigateToModule("comercios");
    }
    
    /// <summary>
    /// Constructor con datos completos de login (RECOMENDADO)
    /// </summary>
    public AdminDashboardViewModel(LoginSuccessData loginData)
    {
        AdminName = loginData.UserName;
        _permisos = loginData.Permisos;
        NavigateToModule("comercios");
    }
    
    /// <summary>
    /// Constructor con servicio de navegación
    /// </summary>
    public AdminDashboardViewModel(string adminName, NavigationService navigationService)
    {
        AdminName = adminName;
        _navigationService = navigationService;
        NavigateToModule("comercios");
    }

    // ============================================
    // COMANDOS
    // ============================================

    /// <summary>
    /// Navega a un módulo específico
    /// </summary>
    [RelayCommand]
    private void NavigateToModule(string? moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            return;
            
        var module = moduleName.ToLower();
        SelectedModule = module;
        
        // Actualizar la vista según el módulo seleccionado
        CurrentView = module switch
        {
            "comercios" => new ManageComerciosView(),
            "usuarios" => new ManageUsersView(),
            "usuarios_allva" => new ManageAdministradoresAllvaView(),
            _ => CurrentView
        };
        
        // Notificar cambio en el título
        OnPropertyChanged(nameof(SelectedModuleTitle));
    }

    /// <summary>
    /// Cierra sesión y vuelve al login
    /// </summary>
    [RelayCommand]
    private void Logout()
    {
        // Si hay servicio de navegación, usarlo
        if (_navigationService != null)
        {
            _navigationService.NavigateTo("Login");
        }
        else
        {
            // Implementación alternativa si no hay servicio de navegación
            // Puedes lanzar un evento o usar otro mecanismo
            System.Diagnostics.Debug.WriteLine("Logout solicitado - implementar navegación");
        }
    }
}
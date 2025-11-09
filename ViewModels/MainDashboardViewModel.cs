using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Allva.Desktop.Views;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el Dashboard principal con menú de navegación
/// </summary>
public partial class MainDashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _userName = "Usuario";

    [ObservableProperty]
    private string _localCode = "CENTRAL";

    [ObservableProperty]
    private string _selectedModule = "dashboard";

    public MainDashboardViewModel()
    {
        // Cargar vista inicial (Últimas Noticias)
        NavigateToModule("dashboard");
    }

    /// <summary>
    /// Constructor con datos de usuario
    /// </summary>
    public MainDashboardViewModel(string userName, string localCode)
    {
        UserName = userName;
        LocalCode = localCode;
        NavigateToModule("dashboard");
    }

    /// <summary>
    /// Navega a un módulo específico
    /// </summary>
    public void NavigateToModule(string moduleName)
    {
        SelectedModule = moduleName;

        CurrentView = moduleName.ToLower() switch
        {
            "dashboard" or "ultimasnoticias" => CreateLatestNewsView(),
            "divisas" => CreateCurrencyExchangeView(),
            "alimentos" => CreateFoodPacksView(),
            "billetes" => CreateFlightTicketsView(),
            "viajes" => CreateTravelPacksView(),
            _ => CreateLatestNewsView()
        };
    }

    /// <summary>
    /// Cierra sesión y vuelve al login
    /// </summary>
    public void Logout()
    {
        var navigationService = new NavigationService();
        navigationService.NavigateToLogin();
    }

    // ============================================
    // MÉTODOS PRIVADOS PARA CREAR VISTAS
    // ============================================

    private UserControl CreateLatestNewsView()
    {
        var view = new LatestNewsView();
        view.DataContext = new LatestNewsViewModel();
        return view;
    }

    private UserControl CreateCurrencyExchangeView()
    {
        var view = new CurrencyExchangeView();
        view.DataContext = new CurrencyExchangeViewModel();
        return view;
    }

    private UserControl CreateFoodPacksView()
    {
        var view = new FoodPacksView();
        view.DataContext = new FoodPacksViewModel();
        return view;
    }

    private UserControl CreateFlightTicketsView()
    {
        var view = new FlightTicketsView();
        view.DataContext = new FlightTicketsViewModel();
        return view;
    }

    private UserControl CreateTravelPacksView()
    {
        var view = new TravelPacksView();
        view.DataContext = new TravelPacksViewModel();
        return view;
    }
}
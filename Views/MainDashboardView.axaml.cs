using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views;

public partial class MainDashboardView : UserControl
{
    public MainDashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NavigateToDashboard(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainDashboardViewModel vm)
        {
            vm.NavigateToModule("dashboard");
            UpdateButtonStyles("dashboard");
        }
    }

    private void NavigateToDivisas(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainDashboardViewModel vm)
        {
            vm.NavigateToModule("divisas");
            UpdateButtonStyles("divisas");
        }
    }

    private void NavigateToAlimentos(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainDashboardViewModel vm)
        {
            vm.NavigateToModule("alimentos");
            UpdateButtonStyles("alimentos");
        }
    }

    private void NavigateToBilletes(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainDashboardViewModel vm)
        {
            vm.NavigateToModule("billetes");
            UpdateButtonStyles("billetes");
        }
    }

    private void NavigateToViajes(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainDashboardViewModel vm)
        {
            vm.NavigateToModule("viajes");
            UpdateButtonStyles("viajes");
        }
    }

    private void CerrarSesion(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainDashboardViewModel vm)
        {
            vm.Logout();
        }
    }

    private void UpdateButtonStyles(string selectedModule)
    {
        // Resetear todos los botones
        var btnDashboard = this.FindControl<Button>("BtnDashboard");
        var btnDivisas = this.FindControl<Button>("BtnDivisas");
        var btnAlimentos = this.FindControl<Button>("BtnAlimentos");
        var btnBilletes = this.FindControl<Button>("BtnBilletes");
        var btnViajes = this.FindControl<Button>("BtnViajes");

        if (btnDashboard != null) btnDashboard.Background = Avalonia.Media.Brushes.Transparent;
        if (btnDivisas != null) btnDivisas.Background = Avalonia.Media.Brushes.Transparent;
        if (btnAlimentos != null) btnAlimentos.Background = Avalonia.Media.Brushes.Transparent;
        if (btnBilletes != null) btnBilletes.Background = Avalonia.Media.Brushes.Transparent;
        if (btnViajes != null) btnViajes.Background = Avalonia.Media.Brushes.Transparent;

        // Marcar el seleccionado
        var selectedBrush = Avalonia.Media.Brush.Parse("#FFC600");
        
        switch (selectedModule.ToLower())
        {
            case "dashboard":
            case "ultimasnoticias":
                if (btnDashboard != null) btnDashboard.Background = selectedBrush;
                break;
            case "divisas":
                if (btnDivisas != null) btnDivisas.Background = selectedBrush;
                break;
            case "alimentos":
                if (btnAlimentos != null) btnAlimentos.Background = selectedBrush;
                break;
            case "billetes":
                if (btnBilletes != null) btnBilletes.Background = selectedBrush;
                break;
            case "viajes":
                if (btnViajes != null) btnViajes.Background = selectedBrush;
                break;
        }
    }
}
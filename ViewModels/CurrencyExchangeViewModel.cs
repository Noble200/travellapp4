using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el módulo de Compra de Divisas
/// </summary>
public partial class CurrencyExchangeViewModel : BaseViewModel
{
    public CurrencyExchangeViewModel()
    {
        Titulo = "Compra de Divisas";
        CargarDivisas();
    }

    [ObservableProperty]
    private ObservableCollection<DivisaItem> _divisas = new();

    [ObservableProperty]
    private decimal _montoOrigen = 0;

    [ObservableProperty]
    private decimal _montoDestino = 0;

    [ObservableProperty]
    private string _divisaOrigen = "EUR";

    [ObservableProperty]
    private string _divisaDestino = "USD";

    private void CargarDivisas()
    {
        // Datos de ejemplo
        Divisas = new ObservableCollection<DivisaItem>
        {
            new DivisaItem { Codigo = "EUR", Nombre = "Euro", TasaCompra = 1.00m, TasaVenta = 1.00m },
            new DivisaItem { Codigo = "USD", Nombre = "Dólar USA", TasaCompra = 1.08m, TasaVenta = 1.10m },
            new DivisaItem { Codigo = "GBP", Nombre = "Libra Esterlina", TasaCompra = 0.86m, TasaVenta = 0.88m },
            new DivisaItem { Codigo = "JPY", Nombre = "Yen Japonés", TasaCompra = 158.50m, TasaVenta = 160.00m },
            new DivisaItem { Codigo = "CHF", Nombre = "Franco Suizo", TasaCompra = 0.94m, TasaVenta = 0.96m }
        };
    }
}

/// <summary>
/// Modelo para una divisa
/// </summary>
public class DivisaItem
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal TasaCompra { get; set; }
    public decimal TasaVenta { get; set; }
}
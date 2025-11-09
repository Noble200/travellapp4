using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el módulo de Billetes de Avión
/// </summary>
public partial class FlightTicketsViewModel : BaseViewModel
{
    public FlightTicketsViewModel()
    {
        Titulo = "Billetes de Avión";
        CargarVuelos();
    }

    [ObservableProperty]
    private ObservableCollection<VueloItem> _vuelos = new();

    [ObservableProperty]
    private string _origen = string.Empty;

    [ObservableProperty]
    private string _destino = string.Empty;

    [ObservableProperty]
    private DateTime _fechaSalida = DateTime.Today;

    [ObservableProperty]
    private DateTime _fechaRetorno = DateTime.Today.AddDays(7);

    private void CargarVuelos()
    {
        // Datos de ejemplo
        Vuelos = new ObservableCollection<VueloItem>
        {
            new VueloItem 
            { 
                Origen = "Madrid (MAD)", 
                Destino = "París (CDG)",
                FechaSalida = DateTime.Today.AddDays(15),
                PrecioEconomica = 120.00m,
                PrecioBusiness = 450.00m,
                Disponible = true
            },
            new VueloItem 
            { 
                Origen = "Barcelona (BCN)", 
                Destino = "Roma (FCO)",
                FechaSalida = DateTime.Today.AddDays(20),
                PrecioEconomica = 95.00m,
                PrecioBusiness = 380.00m,
                Disponible = true
            },
            new VueloItem 
            { 
                Origen = "Madrid (MAD)", 
                Destino = "Londres (LHR)",
                FechaSalida = DateTime.Today.AddDays(10),
                PrecioEconomica = 85.00m,
                PrecioBusiness = 320.00m,
                Disponible = true
            }
        };
    }
}

/// <summary>
/// Modelo para un vuelo
/// </summary>
public class VueloItem
{
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateTime FechaSalida { get; set; }
    public decimal PrecioEconomica { get; set; }
    public decimal PrecioBusiness { get; set; }
    public bool Disponible { get; set; }
}
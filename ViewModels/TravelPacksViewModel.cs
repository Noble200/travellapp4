using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el módulo de Packs de Viajes
/// </summary>
public partial class TravelPacksViewModel : BaseViewModel
{
    public TravelPacksViewModel()
    {
        Titulo = "Packs de Viajes";
        CargarPacks();
    }

    [ObservableProperty]
    private ObservableCollection<PackViajeItem> _packs = new();

    private void CargarPacks()
    {
        // Datos de ejemplo
        Packs = new ObservableCollection<PackViajeItem>
        {
            new PackViajeItem 
            { 
                Destino = "Tokio, Japón", 
                Duracion = "7 días / 6 noches",
                Precio = 1899.00m,
                Incluye = "Vuelos + Hotel 4★ + Desayuno",
                ImagenUrl = "https://via.placeholder.com/300x200/003566/FFFFFF?text=Tokyo",
                Disponible = true
            },
            new PackViajeItem 
            { 
                Destino = "París, Francia", 
                Duracion = "5 días / 4 noches",
                Precio = 1250.00m,
                Incluye = "Vuelos + Hotel 4★ + Tour Eiffel",
                ImagenUrl = "https://via.placeholder.com/300x200/003566/FFFFFF?text=Paris",
                Disponible = true
            },
            new PackViajeItem 
            { 
                Destino = "Roma, Italia", 
                Duracion = "6 días / 5 noches",
                Precio = 1450.00m,
                Incluye = "Vuelos + Hotel 4★ + Tour Coliseo",
                ImagenUrl = "https://via.placeholder.com/300x200/003566/FFFFFF?text=Rome",
                Disponible = true
            },
            new PackViajeItem 
            { 
                Destino = "Caribe - Punta Cana", 
                Duracion = "10 días / 9 noches",
                Precio = 2150.00m,
                Incluye = "Vuelos + Resort 5★ Todo Incluido",
                ImagenUrl = "https://via.placeholder.com/300x200/003566/FFFFFF?text=Caribbean",
                Disponible = true
            }
        };
    }
}

/// <summary>
/// Modelo para un pack de viaje
/// </summary>
public class PackViajeItem
{
    public string Destino { get; set; } = string.Empty;
    public string Duracion { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string Incluye { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public bool Disponible { get; set; }
}
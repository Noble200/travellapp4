using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el módulo de Pack de Alimentos
/// </summary>
public partial class FoodPacksViewModel : BaseViewModel
{
    public FoodPacksViewModel()
    {
        Titulo = "Pack de Alimentos";
        CargarPacks();
    }

    [ObservableProperty]
    private ObservableCollection<PackAlimentoItem> _packs = new();

    private void CargarPacks()
    {
        // Datos de ejemplo
        Packs = new ObservableCollection<PackAlimentoItem>
        {
            new PackAlimentoItem 
            { 
                Nombre = "Pack Básico", 
                Descripcion = "Productos esenciales para la familia",
                Precio = 45.00m,
                Disponible = true
            },
            new PackAlimentoItem 
            { 
                Nombre = "Pack Premium", 
                Descripcion = "Selección gourmet de alimentos",
                Precio = 89.00m,
                Disponible = true
            },
            new PackAlimentoItem 
            { 
                Nombre = "Pack Vegetariano", 
                Descripcion = "Productos vegetales orgánicos",
                Precio = 67.00m,
                Disponible = true
            },
            new PackAlimentoItem 
            { 
                Nombre = "Pack Familiar", 
                Descripcion = "Para familias numerosas",
                Precio = 125.00m,
                Disponible = true
            }
        };
    }
}

/// <summary>
/// Modelo para un pack de alimentos
/// </summary>
public class PackAlimentoItem
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public bool Disponible { get; set; }
}
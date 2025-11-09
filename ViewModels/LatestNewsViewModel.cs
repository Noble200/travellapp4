using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el módulo de Últimas Noticias (Dashboard principal)
/// </summary>
public partial class LatestNewsViewModel : BaseViewModel
{
    public LatestNewsViewModel()
    {
        Titulo = "Últimas Noticias";
        CargarNoticias();
    }

    [ObservableProperty]
    private ObservableCollection<NoticiaItem> _noticias = new();

    private void CargarNoticias()
    {
        // Datos de ejemplo para el dashboard
        Noticias = new ObservableCollection<NoticiaItem>
        {
            new NoticiaItem
            {
                Titulo = "NUEVO DESTINO DISPONIBLE EN PACKS DE VIAJES:",
                Subtitulo = "DESTINO TOKIO",
                ImagenUrl = "https://via.placeholder.com/600x200/003566/FFFFFF?text=Tokyo+Temple",
                Tipo = "Packs de Viajes"
            },
            new NoticiaItem
            {
                Titulo = "BILLETES DE AVIÓN EN DESCUENTO:",
                Subtitulo = "AHORA DESDE 1100€",
                ImagenUrl = "https://via.placeholder.com/600x200/003566/FFFFFF?text=Tropical+Beach",
                Tipo = "Billetes de Avión"
            },
            new NoticiaItem
            {
                Titulo = "NUEVOS PACKS DE ALIMENTOS DISPONIBLES",
                Subtitulo = "VARIEDAD PREMIUM",
                ImagenUrl = "https://via.placeholder.com/600x200/003566/FFFFFF?text=Food+Packs",
                Tipo = "Pack Alimentos"
            }
        };
    }
}

/// <summary>
/// Modelo para una noticia individual
/// </summary>
public class NoticiaItem
{
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}
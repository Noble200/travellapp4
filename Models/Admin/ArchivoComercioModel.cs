using System;
using System.Linq;

namespace Allva.Desktop.Models.Admin;

/// <summary>
/// Modelo para archivos asociados a comercios
/// Representa documentos, imágenes y otros archivos adjuntos
/// </summary>
public class ArchivoComercioModel
{
    // ============================================
    // PROPIEDADES BÁSICAS
    // ============================================
    
    public int IdArchivo { get; set; }
    public int IdComercio { get; set; }
    
    /// <summary>
    /// Nombre único del archivo en el servidor
    /// </summary>
    public string NombreArchivo { get; set; } = string.Empty;
    
    /// <summary>
    /// Ruta completa del archivo en el servidor
    /// </summary>
    public string RutaArchivo { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo de archivo (PDF, Documento Word, Imagen JPEG, etc)
    /// </summary>
    public string TipoArchivo { get; set; } = string.Empty;
    
    /// <summary>
    /// Tamaño del archivo en KB (kilobytes)
    /// </summary>
    public int? TamanoKb { get; set; }
    
    /// <summary>
    /// Descripción opcional del archivo
    /// </summary>
    public string? Descripcion { get; set; }
    
    /// <summary>
    /// Fecha y hora de subida del archivo
    /// </summary>
    public DateTime? FechaSubida { get; set; }
    
    /// <summary>
    /// ID del usuario que subió el archivo
    /// </summary>
    public int? SubidoPor { get; set; }
    
    /// <summary>
    /// Indica si el archivo está activo (no eliminado)
    /// </summary>
    public bool? Activo { get; set; }
    
    // ============================================
    // PROPIEDADES CALCULADAS PARA UI
    // ============================================
    
    /// <summary>
    /// Nombre original del archivo (extraído del nombre_archivo)
    /// </summary>
    public string NombreOriginal
    {
        get
        {
            if (string.IsNullOrEmpty(NombreArchivo))
                return string.Empty;
            
            // Si el nombre contiene UUID, intentar extraer el nombre original
            // Formato esperado: "comercio_fecha_uuid.ext"
            var partes = NombreArchivo.Split('_');
            if (partes.Length >= 3)
            {
                // Tomar desde la tercera parte en adelante y reconstruir
                var nombreConExt = string.Join("_", partes.Skip(2));
                return nombreConExt;
            }
            
            return NombreArchivo;
        }
    }
    
    /// <summary>
    /// Tamaño en bytes calculado desde KB
    /// </summary>
    public long? TamanoBytes => TamanoKb.HasValue ? TamanoKb.Value * 1024L : null;
    
    /// <summary>
    /// Tamaño formateado para mostrar en UI (KB, MB)
    /// </summary>
    public string TamanoFormateado
    {
        get
        {
            if (!TamanoKb.HasValue) return "N/A";
            
            var kb = TamanoKb.Value;
            
            if (kb < 1024)
                return $"{kb} KB";
            else if (kb < 1024 * 1024)
                return $"{kb / 1024.0:F2} MB";
            else
                return $"{kb / (1024.0 * 1024.0):F2} GB";
        }
    }
    
    /// <summary>
    /// Extensión del archivo extraída del nombre
    /// </summary>
    public string Extension
    {
        get
        {
            if (string.IsNullOrEmpty(NombreArchivo))
                return string.Empty;
            
            var puntoIndex = NombreArchivo.LastIndexOf('.');
            return puntoIndex >= 0 ? NombreArchivo.Substring(puntoIndex) : string.Empty;
        }
    }
    
    /// <summary>
    /// Icono emoji según el tipo de archivo
    /// </summary>
    public string IconoArchivo
    {
        get
        {
            var ext = Extension.ToLower();
            
            return ext switch
            {
                ".pdf" => "📄",
                ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "🖼️",
                ".txt" => "📝",
                ".doc" or ".docx" => "📃",
                ".xls" or ".xlsx" => "📊",
                ".zip" or ".rar" => "📦",
                _ => "📎"
            };
        }
    }
    
    /// <summary>
    /// Fecha formateada para mostrar en UI
    /// </summary>
    public string FechaFormateada => FechaSubida?.ToString("dd/MM/yyyy HH:mm") ?? "Sin fecha";
    
    /// <summary>
    /// Usuario que subió (como string para compatibilidad)
    /// </summary>
    public string SubidoPorTexto => SubidoPor.HasValue ? $"Usuario ID: {SubidoPor.Value}" : "Desconocido";
    
    /// <summary>
    /// Información completa del archivo para tooltip
    /// </summary>
    public string InformacionCompleta => 
        $"{NombreOriginal}\n" +
        $"Tamaño: {TamanoFormateado}\n" +
        $"Tipo: {TipoArchivo}\n" +
        $"Subido: {FechaFormateada}\n" +
        $"Por: {SubidoPorTexto}";
}
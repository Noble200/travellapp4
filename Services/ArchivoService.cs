using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Allva.Desktop.Models.Admin;

namespace Allva.Desktop.Services;

/// <summary>
/// Servicio para gestión de archivos de comercios
/// NUEVA VERSIÓN: Archivos guardados en la tabla comercios como BYTEA
/// Los archivos se almacenan en arrays dentro de la tabla comercios
/// </summary>
public class ArchivoService
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    /// <summary>
    /// Obtiene todos los archivos de un comercio desde los arrays en la BD
    /// </summary>
    public async Task<List<ArchivoComercioModel>> ObtenerArchivosPorComercio(int idComercio)
    {
        var archivos = new List<ArchivoComercioModel>();
        
        try
        {
            Console.WriteLine($"📥 ObtenerArchivosPorComercio - ID Comercio: {idComercio}");
            
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var query = @"SELECT archivos_contenido, archivos_nombres, archivos_tipos, archivos_tamanos
                          FROM comercios 
                          WHERE id_comercio = @IdComercio";
            
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdComercio", idComercio);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                // Leer los arrays de la BD
                var contenidos = reader.IsDBNull(0) ? null : (byte[][])reader.GetValue(0);
                var nombres = reader.IsDBNull(1) ? null : (string[])reader.GetValue(1);
                var tipos = reader.IsDBNull(2) ? null : (string[])reader.GetValue(2);
                var tamanos = reader.IsDBNull(3) ? null : (int[])reader.GetValue(3);
                
                // Convertir arrays a lista de objetos
                if (nombres != null && nombres.Length > 0)
                {
                    for (int i = 0; i < nombres.Length; i++)
                    {
                        archivos.Add(new ArchivoComercioModel
                        {
                            IdArchivo = i, // Usar el índice como ID
                            IdComercio = idComercio,
                            NombreArchivo = nombres[i],
                            TipoArchivo = tipos?[i] ?? "Archivo",
                            TamanoKb = tamanos?[i],
                            Activo = true
                        });
                    }
                }
            }
            
            Console.WriteLine($"📦 Archivos encontrados: {archivos.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error obteniendo archivos: {ex.Message}");
            Console.WriteLine($"   StackTrace: {ex.StackTrace}");
        }
        
        return archivos;
    }
    
    /// <summary>
    /// Sube un archivo y lo agrega a los arrays del comercio en la BD
    /// </summary>
    public async Task<int> SubirArchivo(int idComercio, string rutaArchivoLocal, 
                                         string? descripcion, int? idUsuario)
    {
        try
        {
            Console.WriteLine($"📤 SubirArchivo - Inicio");
            Console.WriteLine($"   ID Comercio: {idComercio}");
            Console.WriteLine($"   Archivo: {rutaArchivoLocal}");
            
            // Validar que el archivo existe
            if (!File.Exists(rutaArchivoLocal))
            {
                throw new FileNotFoundException("El archivo no existe", rutaArchivoLocal);
            }
            
            // Leer el archivo como bytes
            var contenidoArchivo = await File.ReadAllBytesAsync(rutaArchivoLocal);
            var nombreArchivo = Path.GetFileName(rutaArchivoLocal);
            var extension = Path.GetExtension(rutaArchivoLocal);
            var tipoArchivo = ObtenerTipoArchivo(extension);
            var tamanoKb = (int)(contenidoArchivo.Length / 1024);
            if (tamanoKb == 0 && contenidoArchivo.Length > 0) tamanoKb = 1;
            
            Console.WriteLine($"   Nombre: {nombreArchivo}");
            Console.WriteLine($"   Tipo: {tipoArchivo}");
            Console.WriteLine($"   Tamaño: {tamanoKb} KB");
            
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            // Obtener arrays existentes
            var querySelect = @"SELECT archivos_contenido, archivos_nombres, archivos_tipos, archivos_tamanos
                               FROM comercios WHERE id_comercio = @IdComercio";
            
            List<byte[]> contenidos = new List<byte[]>();
            List<string> nombres = new List<string>();
            List<string> tipos = new List<string>();
            List<int> tamanos = new List<int>();
            
            using (var cmdSelect = new NpgsqlCommand(querySelect, connection))
            {
                cmdSelect.Parameters.AddWithValue("@IdComercio", idComercio);
                using var reader = await cmdSelect.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    // Cargar arrays existentes
                    if (!reader.IsDBNull(0))
                    {
                        var contenidosExistentes = (byte[][])reader.GetValue(0);
                        contenidos.AddRange(contenidosExistentes);
                    }
                    if (!reader.IsDBNull(1))
                    {
                        var nombresExistentes = (string[])reader.GetValue(1);
                        nombres.AddRange(nombresExistentes);
                    }
                    if (!reader.IsDBNull(2))
                    {
                        var tiposExistentes = (string[])reader.GetValue(2);
                        tipos.AddRange(tiposExistentes);
                    }
                    if (!reader.IsDBNull(3))
                    {
                        var tamanosExistentes = (int[])reader.GetValue(3);
                        tamanos.AddRange(tamanosExistentes);
                    }
                }
            }
            
            // Agregar el nuevo archivo
            contenidos.Add(contenidoArchivo);
            nombres.Add(nombreArchivo);
            tipos.Add(tipoArchivo);
            tamanos.Add(tamanoKb);
            
            // Actualizar en la BD
            var queryUpdate = @"UPDATE comercios SET 
                               archivos_contenido = @Contenidos,
                               archivos_nombres = @Nombres,
                               archivos_tipos = @Tipos,
                               archivos_tamanos = @Tamanos
                               WHERE id_comercio = @IdComercio";
            
            using var cmdUpdate = new NpgsqlCommand(queryUpdate, connection);
            cmdUpdate.Parameters.AddWithValue("@Contenidos", contenidos.ToArray());
            cmdUpdate.Parameters.AddWithValue("@Nombres", nombres.ToArray());
            cmdUpdate.Parameters.AddWithValue("@Tipos", tipos.ToArray());
            cmdUpdate.Parameters.AddWithValue("@Tamanos", tamanos.ToArray());
            cmdUpdate.Parameters.AddWithValue("@IdComercio", idComercio);
            
            await cmdUpdate.ExecuteNonQueryAsync();
            
            Console.WriteLine($"✅ Archivo guardado exitosamente en índice: {contenidos.Count - 1}");
            
            return contenidos.Count - 1; // Devolver el índice del archivo
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR en SubirArchivo: {ex.Message}");
            Console.WriteLine($"   StackTrace: {ex.StackTrace}");
            throw;
        }
    }
    
    /// <summary>
    /// Elimina un archivo del array (por índice)
    /// </summary>
    public async Task<bool> EliminarArchivo(int idComercio, int indiceArchivo)
    {
        try
        {
            Console.WriteLine($"🗑️ EliminarArchivo - ID Comercio: {idComercio}, Índice: {indiceArchivo}");
            
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            // Obtener arrays existentes
            var querySelect = @"SELECT archivos_contenido, archivos_nombres, archivos_tipos, archivos_tamanos
                               FROM comercios WHERE id_comercio = @IdComercio";
            
            List<byte[]> contenidos = new List<byte[]>();
            List<string> nombres = new List<string>();
            List<string> tipos = new List<string>();
            List<int> tamanos = new List<int>();
            
            using (var cmdSelect = new NpgsqlCommand(querySelect, connection))
            {
                cmdSelect.Parameters.AddWithValue("@IdComercio", idComercio);
                using var reader = await cmdSelect.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                    {
                        var contenidosExistentes = (byte[][])reader.GetValue(0);
                        contenidos.AddRange(contenidosExistentes);
                    }
                    if (!reader.IsDBNull(1))
                    {
                        var nombresExistentes = (string[])reader.GetValue(1);
                        nombres.AddRange(nombresExistentes);
                    }
                    if (!reader.IsDBNull(2))
                    {
                        var tiposExistentes = (string[])reader.GetValue(2);
                        tipos.AddRange(tiposExistentes);
                    }
                    if (!reader.IsDBNull(3))
                    {
                        var tamanosExistentes = (int[])reader.GetValue(3);
                        tamanos.AddRange(tamanosExistentes);
                    }
                }
            }
            
            // Validar índice
            if (indiceArchivo < 0 || indiceArchivo >= nombres.Count)
            {
                Console.WriteLine($"❌ Índice inválido: {indiceArchivo}");
                return false;
            }
            
            // Eliminar el archivo del índice especificado
            contenidos.RemoveAt(indiceArchivo);
            nombres.RemoveAt(indiceArchivo);
            tipos.RemoveAt(indiceArchivo);
            tamanos.RemoveAt(indiceArchivo);
            
            // Actualizar en la BD
            var queryUpdate = @"UPDATE comercios SET 
                               archivos_contenido = @Contenidos,
                               archivos_nombres = @Nombres,
                               archivos_tipos = @Tipos,
                               archivos_tamanos = @Tamanos
                               WHERE id_comercio = @IdComercio";
            
            using var cmdUpdate = new NpgsqlCommand(queryUpdate, connection);
            cmdUpdate.Parameters.AddWithValue("@Contenidos", contenidos.Count > 0 ? contenidos.ToArray() : DBNull.Value);
            cmdUpdate.Parameters.AddWithValue("@Nombres", nombres.Count > 0 ? nombres.ToArray() : DBNull.Value);
            cmdUpdate.Parameters.AddWithValue("@Tipos", tipos.Count > 0 ? tipos.ToArray() : DBNull.Value);
            cmdUpdate.Parameters.AddWithValue("@Tamanos", tamanos.Count > 0 ? tamanos.ToArray() : DBNull.Value);
            cmdUpdate.Parameters.AddWithValue("@IdComercio", idComercio);
            
            await cmdUpdate.ExecuteNonQueryAsync();
            
            Console.WriteLine($"✅ Archivo eliminado exitosamente");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error eliminando archivo: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Descarga un archivo (por índice) a una ubicación específica
    /// </summary>
    public async Task DescargarArchivo(int idComercio, int indiceArchivo, string rutaDestino)
    {
        try
        {
            Console.WriteLine($"⬇️ DescargarArchivo - ID Comercio: {idComercio}, Índice: {indiceArchivo}");
            Console.WriteLine($"   Destino: {rutaDestino}");
            
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var query = @"SELECT archivos_contenido, archivos_nombres
                         FROM comercios WHERE id_comercio = @IdComercio";
            
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdComercio", idComercio);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                {
                    var contenidos = (byte[][])reader.GetValue(0);
                    var nombres = (string[])reader.GetValue(1);
                    
                    if (indiceArchivo >= 0 && indiceArchivo < contenidos.Length)
                    {
                        // Crear directorio de destino si no existe
                        var directorioDestino = Path.GetDirectoryName(rutaDestino);
                        if (!string.IsNullOrEmpty(directorioDestino))
                            Directory.CreateDirectory(directorioDestino);
                        
                        // Guardar el archivo
                        await File.WriteAllBytesAsync(rutaDestino, contenidos[indiceArchivo]);
                        
                        Console.WriteLine($"✅ Archivo descargado: {nombres[indiceArchivo]}");
                        return;
                    }
                }
            }
            
            throw new FileNotFoundException("Archivo no encontrado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error descargando archivo: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Obtiene el contenido de un archivo como bytes
    /// </summary>
    public async Task<byte[]?> ObtenerContenidoArchivo(int idComercio, int indiceArchivo)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var query = "SELECT archivos_contenido FROM comercios WHERE id_comercio = @IdComercio";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdComercio", idComercio);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync() && !reader.IsDBNull(0))
            {
                var contenidos = (byte[][])reader.GetValue(0);
                if (indiceArchivo >= 0 && indiceArchivo < contenidos.Length)
                {
                    return contenidos[indiceArchivo];
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error obteniendo contenido de archivo: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Obtiene el tipo de archivo según la extensión
    /// </summary>
    private string ObtenerTipoArchivo(string extension)
    {
        return extension.ToLower() switch
        {
            ".pdf" => "PDF",
            ".png" => "Imagen PNG",
            ".jpg" => "Imagen JPEG",
            ".jpeg" => "Imagen JPEG",
            ".gif" => "Imagen GIF",
            ".bmp" => "Imagen BMP",
            ".txt" => "Texto",
            ".doc" => "Documento Word",
            ".docx" => "Documento Word",
            ".xls" => "Hoja de Cálculo",
            ".xlsx" => "Hoja de Cálculo",
            ".zip" => "Archivo ZIP",
            ".rar" => "Archivo RAR",
            _ => "Archivo"
        };
    }
    
    /// <summary>
    /// Elimina todos los archivos de un comercio
    /// </summary>
    public async Task<int> EliminarArchivosDeComercio(int idComercio)
    {
        try
        {
            Console.WriteLine($"🗑️ EliminarArchivosDeComercio - ID Comercio: {idComercio}");
            
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var query = @"UPDATE comercios SET 
                         archivos_contenido = NULL,
                         archivos_nombres = NULL,
                         archivos_tipos = NULL,
                         archivos_tamanos = NULL
                         WHERE id_comercio = @IdComercio";
            
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdComercio", idComercio);
            
            var resultado = await cmd.ExecuteNonQueryAsync();
            
            Console.WriteLine($"✅ Archivos eliminados del comercio");
            
            return resultado;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error eliminando archivos del comercio: {ex.Message}");
            return 0;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models.Admin;
using Allva.Desktop.Models;
using Allva.Desktop.Services;
using Npgsql;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel para la gestión de comercios en el panel de administración
/// VERSIÓN ACTUALIZADA CON FUNCIONALIDAD COMPLETA DE ARCHIVOS
/// Usa los modelos LocalSimpleModel y ComercioModel EXACTOS del proyecto
/// </summary>
public partial class ManageComerciosViewModel : ObservableObject
{
    // ============================================
    // CONFIGURACIÓN DE BASE DE DATOS
    // ============================================
    
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    // ============================================
    // PROPIEDADES OBSERVABLES - DATOS PRINCIPALES
    // ============================================

    [ObservableProperty]
    private ObservableCollection<ComercioModel> _comercios = new();

    [ObservableProperty]
    private ObservableCollection<ComercioModel> _comerciosFiltrados = new();

    [ObservableProperty]
    private ComercioModel? _comercioSeleccionado;

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private bool _mostrarMensajeExito;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    // ============================================
    // PROPIEDADES PARA PANEL DERECHO
    // ============================================

    [ObservableProperty]
    private bool _mostrarPanelDerecho = false;

    [ObservableProperty]
    private string _tituloPanelDerecho = "Detalles del Comercio";

    [ObservableProperty]
    private object? _contenidoPanelDerecho;

    [ObservableProperty]
    private bool _esModoCreacion = false;

    // ============================================
    // PROPIEDADES PARA FORMULARIO
    // ============================================

    [ObservableProperty]
    private bool _mostrarFormulario;

    [ObservableProperty]
    private bool _modoEdicion;

    [ObservableProperty]
    private string _tituloFormulario = "Crear Comercio";

    // Campos del formulario de comercio (según ComercioModel REAL)
    [ObservableProperty]
    private string _formNombreComercio = string.Empty;

    [ObservableProperty]
    private string _formNombreSrl = string.Empty;

    [ObservableProperty]
    private string _formDireccionCentral = string.Empty;

    [ObservableProperty]
    private string _formNumeroContacto = string.Empty;

    [ObservableProperty]
    private string _formMailContacto = string.Empty;

    [ObservableProperty]
    private string _formPais = string.Empty;

    [ObservableProperty]
    private string _formObservaciones = string.Empty;

    [ObservableProperty]
    private decimal _formPorcentajeComisionDivisas = 0;

    [ObservableProperty]
    private bool _formActivo = true;

    // Locales del comercio (usando LocalFormModel REAL que ya existe)
    [ObservableProperty]
    private ObservableCollection<LocalFormModel> _localesComercio = new();

    // ============================================
    // PROPIEDADES PARA FILTROS
    // ============================================

    [ObservableProperty]
    private string _filtroBusqueda = string.Empty;

    [ObservableProperty]
    private string _filtroPais = "Todos";

    [ObservableProperty]
    private string _filtroEstado = "Todos";

    [ObservableProperty]
    private ObservableCollection<string> _paisesDisponibles = new();

    // ============================================
    // PROPIEDADES PARA ARCHIVOS
    // ============================================

    [ObservableProperty]
    private ObservableCollection<ArchivoComercioModel> _archivosComercioSeleccionado = new();

    /// <summary>
    /// Lista de rutas de archivos que se subirán al crear/editar el comercio
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _archivosParaSubir = new();

    // ============================================
    // SERVICIOS
    // ============================================

    private readonly ArchivoService _archivoService = new();

    // ============================================
    // PROPIEDADES CALCULADAS
    // ============================================

    public int TotalComercios => Comercios.Count;
    public int ComerciosActivos => Comercios.Count(c => c.Activo);
    public int ComerciosInactivos => Comercios.Count(c => !c.Activo);
    public int TotalLocales => Comercios.Sum(c => c.CantidadLocales);

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public ManageComerciosViewModel()
    {
        // Cargar datos desde la base de datos
        _ = CargarDatosDesdeBaseDatos();
    }

    // ============================================
    // MÉTODOS DE BASE DE DATOS - CARGAR
    // ============================================

    private async Task CargarDatosDesdeBaseDatos()
    {
        Cargando = true;
        
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var comercios = await CargarComercios(connection);
            
            Comercios.Clear();
            foreach (var comercio in comercios)
            {
                // Cargar locales del comercio CON PERMISOS
                comercio.Locales = await CargarLocalesDelComercio(connection, comercio.IdComercio);
                
                // Contar usuarios
                comercio.TotalUsuarios = await ContarUsuariosDelComercio(connection, comercio.IdComercio);
                
                Comercios.Add(comercio);
            }

            // Actualizar contadores
            OnPropertyChanged(nameof(TotalComercios));
            OnPropertyChanged(nameof(ComerciosActivos));
            OnPropertyChanged(nameof(ComerciosInactivos));
            OnPropertyChanged(nameof(TotalLocales));
            
            // Inicializar filtros
            await InicializarFiltros();
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al cargar datos: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task<List<ComercioModel>> CargarComercios(NpgsqlConnection connection)
    {
        var comercios = new List<ComercioModel>();
        
        var query = @"SELECT id_comercio, nombre_comercio, nombre_srl, direccion_central,
                             numero_contacto, mail_contacto, pais, observaciones,
                             porcentaje_comision_divisas, activo, fecha_registro,
                             fecha_ultima_modificacion
                      FROM comercios 
                      ORDER BY nombre_comercio";
        
        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            comercios.Add(new ComercioModel
            {
                IdComercio = reader.GetInt32(0),
                NombreComercio = reader.GetString(1),
                NombreSrl = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                DireccionCentral = reader.GetString(3),
                NumeroContacto = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                MailContacto = reader.GetString(5),
                Pais = reader.GetString(6),
                Observaciones = reader.IsDBNull(7) ? null : reader.GetString(7),
                PorcentajeComisionDivisas = reader.GetDecimal(8),
                Activo = reader.GetBoolean(9),
                FechaRegistro = reader.GetDateTime(10),
                FechaUltimaModificacion = reader.GetDateTime(11)
            });
        }
        
        return comercios;
    }

    private async Task<List<LocalSimpleModel>> CargarLocalesDelComercio(NpgsqlConnection connection, int idComercio)
    {
        var locales = new List<LocalSimpleModel>();
        
        // Usar los campos REALES de LocalSimpleModel
        var query = @"SELECT id_local, codigo_local, nombre_local, direccion, local_numero,
                             escalera, piso, telefono, email, observaciones,
                             numero_usuarios_max, activo,
                             modulo_divisas, modulo_pack_alimentos, 
                             modulo_billetes_avion, modulo_pack_viajes
                      FROM locales 
                      WHERE id_comercio = @IdComercio
                      ORDER BY nombre_local";
        
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@IdComercio", idComercio);
        
        using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            locales.Add(new LocalSimpleModel
            {
                IdLocal = reader.GetInt32(0),
                CodigoLocal = reader.GetString(1),
                NombreLocal = reader.GetString(2),
                Direccion = reader.GetString(3),
                LocalNumero = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Escalera = reader.IsDBNull(5) ? null : reader.GetString(5),
                Piso = reader.IsDBNull(6) ? null : reader.GetString(6),
                Telefono = reader.IsDBNull(7) ? null : reader.GetString(7),
                Email = reader.IsDBNull(8) ? null : reader.GetString(8),
                Observaciones = reader.IsDBNull(9) ? null : reader.GetString(9),
                NumeroUsuariosMax = reader.GetInt32(10),
                Activo = reader.GetBoolean(11),
                ModuloDivisas = reader.GetBoolean(12),
                ModuloPackAlimentos = reader.GetBoolean(13),
                ModuloBilletesAvion = reader.GetBoolean(14),
                ModuloPackViajes = reader.GetBoolean(15)
            });
        }
        
        return locales;
    }

    private async Task<int> ContarUsuariosDelComercio(NpgsqlConnection connection, int idComercio)
    {
        var query = @"SELECT COUNT(*) 
                      FROM usuarios u
                      INNER JOIN locales l ON u.id_local = l.id_local
                      WHERE l.id_comercio = @IdComercio";
        
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@IdComercio", idComercio);
        
        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    // ============================================
    // COMANDOS - PANEL DERECHO
    // ============================================

    [RelayCommand]
    private void MostrarFormularioComercio()
    {
        LimpiarFormulario();
        EsModoCreacion = true;
        ModoEdicion = false;
        TituloFormulario = "Crear Nuevo Comercio";
        TituloPanelDerecho = "Crear Nuevo Comercio";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
    }

    [RelayCommand]
    private void EditarComercio(ComercioModel comercio)
    {
        ComercioSeleccionado = comercio;
        CargarDatosEnFormulario(comercio);
        EsModoCreacion = false;
        ModoEdicion = true;
        TituloFormulario = "Editar Comercio";
        TituloPanelDerecho = $"Editar: {comercio.NombreComercio}";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
    }

    [RelayCommand]
    private async Task VerDetallesComercio(ComercioModel comercio)
    {
        ComercioSeleccionado = comercio;
        TituloPanelDerecho = $"Detalles de {comercio.NombreComercio}";
        MostrarFormulario = false;
        MostrarPanelDerecho = true;
        
        // Cargar archivos del comercio
        await CargarArchivosComercio(comercio.IdComercio);
    }

    [RelayCommand]
    private void CerrarPanelDerecho()
    {
        MostrarPanelDerecho = false;
        MostrarFormulario = false;
        ContenidoPanelDerecho = null;
        ComercioSeleccionado = null;
        LimpiarFormulario();
    }

    // ============================================
    // COMANDOS - ACCIONES CRUD
    // ============================================

    [RelayCommand]
    private async Task GuardarComercio()
    {
        // Validar formulario
        if (!ValidarFormulario(out string mensajeError))
        {
            MensajeExito = $"⚠️ {mensajeError}";
            MostrarMensajeExito = true;
            await Task.Delay(4000);
            MostrarMensajeExito = false;
            return;
        }

        Cargando = true;

        try
        {
            if (ModoEdicion && ComercioSeleccionado != null)
            {
                await ActualizarComercio();
                MensajeExito = "✓ Comercio actualizado correctamente";
            }
            else
            {
                await CrearNuevoComercio();
                MensajeExito = "✓ Comercio creado correctamente";
            }

            // Recargar datos
            await CargarDatosDesdeBaseDatos();

            // Cerrar panel
            CerrarPanelDerecho();

            // Mostrar mensaje de éxito
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"❌ Error al guardar: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(5000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task CrearNuevoComercio()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            // 1. Insertar comercio principal (con TODOS los campos del modelo real)
            var queryComercio = @"
                INSERT INTO comercios (
                    nombre_comercio, nombre_srl, direccion_central, 
                    numero_contacto, mail_contacto, pais, observaciones,
                    porcentaje_comision_divisas, activo, fecha_registro, fecha_ultima_modificacion
                )
                VALUES (
                    @NombreComercio, @NombreSrl, @Direccion, 
                    @Telefono, @Email, @Pais, @Observaciones,
                    @Comision, @Activo, @FechaRegistro, @FechaModificacion
                )
                RETURNING id_comercio";
            
            using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
            cmdComercio.Parameters.AddWithValue("@NombreComercio", FormNombreComercio);
            cmdComercio.Parameters.AddWithValue("@NombreSrl", FormNombreSrl ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Direccion", FormDireccionCentral ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Telefono", FormNumeroContacto ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Email", FormMailContacto);
            cmdComercio.Parameters.AddWithValue("@Pais", FormPais ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Observaciones", 
                string.IsNullOrWhiteSpace(FormObservaciones) ? DBNull.Value : FormObservaciones);
            cmdComercio.Parameters.AddWithValue("@Comision", FormPorcentajeComisionDivisas);
            cmdComercio.Parameters.AddWithValue("@Activo", FormActivo);
            cmdComercio.Parameters.AddWithValue("@FechaRegistro", DateTime.Now);
            cmdComercio.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
            
            var idComercio = Convert.ToInt32(await cmdComercio.ExecuteScalarAsync());
            
            // 2. Insertar locales asociados (con TODOS los campos del modelo real)
            foreach (var local in LocalesComercio)
            {
                var queryLocal = @"
                    INSERT INTO locales (
                        id_comercio, codigo_local, nombre_local, 
                        pais, codigo_postal, tipo_via, direccion, local_numero,
                        escalera, piso, telefono, email, numero_usuarios_max,
                        observaciones, activo, modulo_divisas, modulo_pack_alimentos,
                        modulo_billetes_avion, modulo_pack_viajes, comision_divisas
                    )
                    VALUES (
                        @IdComercio, @CodigoLocal, @NombreLocal,
                        @Pais, @CodigoPostal, @TipoVia, @Direccion, @LocalNumero,
                        @Escalera, @Piso, @Telefono, @Email, @NumeroUsuariosMax,
                        @Observaciones, @Activo, @ModuloDivisas, @ModuloPackAlimentos,
                        @ModuloBilletesAvion, @ModuloPackViajes, @ComisionDivisas
                    )";
                
                using var cmdLocal = new NpgsqlCommand(queryLocal, connection, transaction);
                cmdLocal.Parameters.AddWithValue("@IdComercio", idComercio);
                cmdLocal.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
                cmdLocal.Parameters.AddWithValue("@NombreLocal", local.NombreLocal);
                cmdLocal.Parameters.AddWithValue("@Pais", local.Pais ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@CodigoPostal", local.CodigoPostal ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@TipoVia", local.TipoVia ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@Direccion", local.Direccion ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@Direccion", local.Direccion ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@LocalNumero", local.LocalNumero ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@Escalera", 
                    string.IsNullOrWhiteSpace(local.Escalera) ? DBNull.Value : local.Escalera);
                cmdLocal.Parameters.AddWithValue("@Piso", 
                    string.IsNullOrWhiteSpace(local.Piso) ? DBNull.Value : local.Piso);
                cmdLocal.Parameters.AddWithValue("@Telefono", 
                    string.IsNullOrWhiteSpace(local.Telefono) ? DBNull.Value : local.Telefono);
                cmdLocal.Parameters.AddWithValue("@Email", 
                    string.IsNullOrWhiteSpace(local.Email) ? DBNull.Value : local.Email);
                cmdLocal.Parameters.AddWithValue("@NumeroUsuariosMax", local.NumeroUsuariosMax);
                cmdLocal.Parameters.AddWithValue("@Observaciones", 
                    string.IsNullOrWhiteSpace(local.Observaciones) ? DBNull.Value : local.Observaciones);
                cmdLocal.Parameters.AddWithValue("@Activo", local.Activo);
                cmdLocal.Parameters.AddWithValue("@ModuloDivisas", local.ModuloDivisas);
                cmdLocal.Parameters.AddWithValue("@ModuloPackAlimentos", local.ModuloPackAlimentos);
                cmdLocal.Parameters.AddWithValue("@ModuloBilletesAvion", local.ModuloBilletesAvion);
                cmdLocal.Parameters.AddWithValue("@ModuloPackViajes", local.ModuloPackViajes);
                cmdLocal.Parameters.AddWithValue("@ComisionDivisas", local.ComisionDivisas);
                
                await cmdLocal.ExecuteNonQueryAsync();
            }
            
            // 3. Subir archivos si hay
            if (ArchivosParaSubir.Any())
            {
                foreach (var rutaArchivo in ArchivosParaSubir)
                {
                    try
                    {
                        await _archivoService.SubirArchivo(idComercio, rutaArchivo, null, null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error subiendo archivo {rutaArchivo}: {ex.Message}");
                        // Continuar con los demás archivos aunque uno falle
                    }
                }
            }
            
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ActualizarComercio()
    {
        if (ComercioSeleccionado == null) return;
        
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            // 1. Actualizar comercio principal
            var queryComercio = @"
                UPDATE comercios SET
                    nombre_comercio = @NombreComercio,
                    nombre_srl = @NombreSrl,
                    direccion_central = @Direccion,
                    numero_contacto = @Telefono,
                    mail_contacto = @Email,
                    pais = @Pais,
                    observaciones = @Observaciones,
                    porcentaje_comision_divisas = @Comision,
                    activo = @Activo,
                    fecha_ultima_modificacion = @FechaModificacion
                WHERE id_comercio = @IdComercio";
            
            using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
            cmdComercio.Parameters.AddWithValue("@NombreComercio", FormNombreComercio);
            cmdComercio.Parameters.AddWithValue("@NombreSrl", FormNombreSrl ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Direccion", FormDireccionCentral ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Telefono", FormNumeroContacto ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Email", FormMailContacto);
            cmdComercio.Parameters.AddWithValue("@Pais", FormPais ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Observaciones", 
                string.IsNullOrWhiteSpace(FormObservaciones) ? DBNull.Value : FormObservaciones);
            cmdComercio.Parameters.AddWithValue("@Comision", FormPorcentajeComisionDivisas);
            cmdComercio.Parameters.AddWithValue("@Activo", FormActivo);
            cmdComercio.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
            cmdComercio.Parameters.AddWithValue("@IdComercio", ComercioSeleccionado.IdComercio);
            
            await cmdComercio.ExecuteNonQueryAsync();
            
            // 2. Eliminar locales existentes y reinsertar
            var queryDeleteLocales = "DELETE FROM locales WHERE id_comercio = @IdComercio";
            using var cmdDelete = new NpgsqlCommand(queryDeleteLocales, connection, transaction);
            cmdDelete.Parameters.AddWithValue("@IdComercio", ComercioSeleccionado.IdComercio);
            await cmdDelete.ExecuteNonQueryAsync();
            
            // 3. Insertar locales actualizados
            foreach (var local in LocalesComercio)
            {
                var queryLocal = @"
                    INSERT INTO locales (
                        id_comercio, codigo_local, nombre_local, direccion, local_numero,
                        escalera, piso, telefono, email, numero_usuarios_max,
                        observaciones, activo, modulo_divisas, modulo_pack_alimentos,
                        modulo_billetes_avion, modulo_pack_viajes
                    )
                    VALUES (
                        @IdComercio, @CodigoLocal, @NombreLocal, @Direccion, @LocalNumero,
                        @Escalera, @Piso, @Telefono, @Email, @NumeroUsuariosMax,
                        @Observaciones, @Activo, @ModuloDivisas, @ModuloPackAlimentos,
                        @ModuloBilletesAvion, @ModuloPackViajes
                    )";
                
                using var cmdLocal = new NpgsqlCommand(queryLocal, connection, transaction);
                cmdLocal.Parameters.AddWithValue("@IdComercio", ComercioSeleccionado.IdComercio);
                cmdLocal.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
                cmdLocal.Parameters.AddWithValue("@NombreLocal", local.NombreLocal);
                cmdLocal.Parameters.AddWithValue("@Direccion", local.Direccion ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@LocalNumero", local.LocalNumero ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@Escalera", 
                    string.IsNullOrWhiteSpace(local.Escalera) ? DBNull.Value : local.Escalera);
                cmdLocal.Parameters.AddWithValue("@Piso", 
                    string.IsNullOrWhiteSpace(local.Piso) ? DBNull.Value : local.Piso);
                cmdLocal.Parameters.AddWithValue("@Telefono", 
                    string.IsNullOrWhiteSpace(local.Telefono) ? DBNull.Value : local.Telefono);
                cmdLocal.Parameters.AddWithValue("@Email", 
                    string.IsNullOrWhiteSpace(local.Email) ? DBNull.Value : local.Email);
                cmdLocal.Parameters.AddWithValue("@NumeroUsuariosMax", local.NumeroUsuariosMax);
                cmdLocal.Parameters.AddWithValue("@Observaciones", 
                    string.IsNullOrWhiteSpace(local.Observaciones) ? DBNull.Value : local.Observaciones);
                cmdLocal.Parameters.AddWithValue("@Activo", local.Activo);
                cmdLocal.Parameters.AddWithValue("@ModuloDivisas", local.ModuloDivisas);
                cmdLocal.Parameters.AddWithValue("@ModuloPackAlimentos", local.ModuloPackAlimentos);
                cmdLocal.Parameters.AddWithValue("@ModuloBilletesAvion", local.ModuloBilletesAvion);
                cmdLocal.Parameters.AddWithValue("@ModuloPackViajes", local.ModuloPackViajes);
                
                await cmdLocal.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
        // 4. AHORA SÍ, SUBIR ARCHIVOS FUERA DE LA TRANSACCIÓN
        if (ArchivosParaSubir.Any())
        {
            foreach (var rutaArchivo in ArchivosParaSubir)
            {
                try
                {
                    await _archivoService.SubirArchivo(ComercioSeleccionado.IdComercio, rutaArchivo, null, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error subiendo archivo {rutaArchivo}: {ex.Message}");
                    // Continuar con los demás archivos aunque uno falle
                }
            }
        }
    }

    [RelayCommand]
    private async Task EliminarComercio(ComercioModel comercio)
    {
        // TODO: Implementar confirmación con modal
        
        Cargando = true;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Eliminar archivos primero
            await _archivoService.EliminarArchivosDeComercio(comercio.IdComercio);

            // Eliminar comercio (CASCADE eliminará locales y relaciones)
            var query = "DELETE FROM comercios WHERE id_comercio = @Id";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", comercio.IdComercio);
            await cmd.ExecuteNonQueryAsync();

            // Recargar datos
            await CargarDatosDesdeBaseDatos();

            MensajeExito = "✓ Comercio eliminado correctamente";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"❌ Error al eliminar: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(5000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task CambiarEstadoComercio(ComercioModel comercio)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var nuevoEstado = !comercio.Activo;
            var query = @"UPDATE comercios 
                         SET activo = @Activo, fecha_ultima_modificacion = @Fecha
                         WHERE id_comercio = @Id";
            
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Activo", nuevoEstado);
            cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
            cmd.Parameters.AddWithValue("@Id", comercio.IdComercio);
            
            await cmd.ExecuteNonQueryAsync();
            
            // Actualizar en la lista
            comercio.Activo = nuevoEstado;
            
            OnPropertyChanged(nameof(ComerciosActivos));
            OnPropertyChanged(nameof(ComerciosInactivos));
            
            MensajeExito = $"✓ Estado actualizado: {(nuevoEstado ? "Activo" : "Inactivo")}";
            MostrarMensajeExito = true;
            await Task.Delay(2000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"❌ Error al cambiar estado: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    // ============================================
    // COMANDOS - FILTROS
    // ============================================

    [RelayCommand]
    private void AplicarFiltros()
    {
        var filtrados = Comercios.AsEnumerable();
        
        // Filtro por búsqueda
        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            filtrados = filtrados.Where(c => 
                c.NombreComercio.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                c.NombreSrl.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                c.Locales.Any(l => l.NombreLocal.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                                   l.CodigoLocal.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase))
            );
        }
        
        // Filtro por país
        if (!string.IsNullOrEmpty(FiltroPais) && FiltroPais != "Todos")
        {
            filtrados = filtrados.Where(c => c.Pais == FiltroPais);
        }
        
        // Filtro por estado
        if (!string.IsNullOrEmpty(FiltroEstado) && FiltroEstado != "Todos")
        {
            var activo = FiltroEstado == "Activo";
            filtrados = filtrados.Where(c => c.Activo == activo);
        }
        
        ComerciosFiltrados.Clear();
        foreach (var comercio in filtrados.OrderBy(c => c.NombreComercio))
        {
            ComerciosFiltrados.Add(comercio);
        }
    }

    // ============================================
    // COMANDOS - LOCALES
    // ============================================

    [RelayCommand]
    private void AgregarLocal()
    {
        var nuevoLocal = new LocalFormModel
        {
            CodigoLocal = GenerarCodigoLocal(),
            NombreLocal = $"Local {LocalesComercio.Count + 1}",
            Direccion = string.Empty,
            LocalNumero = string.Empty,
            NumeroUsuariosMax = 10,
            Activo = true,
            ModuloDivisas = false,
            ModuloPackAlimentos = false,
            ModuloBilletesAvion = false,
            ModuloPackViajes = false
        };
        
        LocalesComercio.Add(nuevoLocal);
    }

    [RelayCommand]
    private void EliminarLocal(LocalFormModel local)
    {
        if (local == null) return;
        LocalesComercio.Remove(local);
    }

    // ============================================
    // COMANDOS - ARCHIVOS
    // ============================================

    [RelayCommand]
    private async Task SubirArchivo()
    {
        try
        {
            // Obtener la ventana principal
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var storage = topLevel.StorageProvider;
            
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar archivos",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Todos los archivos") { Patterns = new[] { "*.*" } },
                    new FilePickerFileType("Documentos") { Patterns = new[] { "*.pdf", "*.doc", "*.docx", "*.txt" } },
                    new FilePickerFileType("Imágenes") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } }
                }
            });

            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    var rutaArchivo = file.Path.LocalPath;
                    if (!ArchivosParaSubir.Contains(rutaArchivo))
                    {
                        ArchivosParaSubir.Add(rutaArchivo);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MensajeExito = $"❌ Error al seleccionar archivos: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    [RelayCommand]
    private void EliminarArchivoSubido(string rutaArchivo)
    {
        if (string.IsNullOrEmpty(rutaArchivo)) return;
        ArchivosParaSubir.Remove(rutaArchivo);
    }

    [RelayCommand]
    private async Task DescargarArchivo(ArchivoComercioModel archivo)
    {
        if (archivo == null || ComercioSeleccionado == null) return;
        
        try
        {
            // Obtener la ventana principal
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var storage = topLevel.StorageProvider;
            
            // Abrir diálogo para guardar archivo
            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar archivo",
                SuggestedFileName = archivo.NombreArchivo,
                DefaultExtension = Path.GetExtension(archivo.NombreArchivo),
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Todos los archivos") { Patterns = new[] { "*.*" } }
                }
            });

            if (file != null)
            {
                var rutaDestino = file.Path.LocalPath;
                
                // Descargar el archivo
                await _archivoService.DescargarArchivo(
                    ComercioSeleccionado.IdComercio, 
                    archivo.IdArchivo,
                    rutaDestino
                );
                
                MensajeExito = $"✓ Archivo guardado: {archivo.NombreArchivo}";
                MostrarMensajeExito = true;
                await Task.Delay(3000);
                MostrarMensajeExito = false;
            }
        }
        catch (Exception ex)
        {
            MensajeExito = $"❌ Error al guardar: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(5000);
            MostrarMensajeExito = false;
        }
    }
    // ============================================
    // MÉTODOS AUXILIARES - FORMULARIO
    // ============================================

    private void LimpiarFormulario()
    {
        FormNombreComercio = string.Empty;
        FormNombreSrl = string.Empty;
        FormDireccionCentral = string.Empty;
        FormNumeroContacto = string.Empty;
        FormMailContacto = string.Empty;
        FormPais = string.Empty;
        FormObservaciones = string.Empty;
        FormPorcentajeComisionDivisas = 0;
        FormActivo = true;
        LocalesComercio.Clear();
        ArchivosParaSubir.Clear();
    }

    private void CargarDatosEnFormulario(ComercioModel comercio)
    {
        FormNombreComercio = comercio.NombreComercio;
        FormNombreSrl = comercio.NombreSrl;
        FormDireccionCentral = comercio.DireccionCentral;
        FormNumeroContacto = comercio.NumeroContacto;
        FormMailContacto = comercio.MailContacto;
        FormPais = comercio.Pais;
        FormObservaciones = comercio.Observaciones ?? string.Empty;
        FormPorcentajeComisionDivisas = comercio.PorcentajeComisionDivisas;
        FormActivo = comercio.Activo;

        LocalesComercio.Clear();
        foreach (var local in comercio.Locales)
        {
            LocalesComercio.Add(new LocalFormModel
            {
                IdLocal = local.IdLocal,
                IdComercio = comercio.IdComercio,
                CodigoLocal = local.CodigoLocal,
                NombreLocal = local.NombreLocal,
                Direccion = local.Direccion,
                LocalNumero = local.LocalNumero,
                Escalera = local.Escalera,
                Piso = local.Piso,
                Telefono = local.Telefono,
                Email = local.Email,
                NumeroUsuariosMax = local.NumeroUsuariosMax,
                Observaciones = local.Observaciones,
                Activo = local.Activo,
                ModuloDivisas = local.ModuloDivisas,
                ModuloPackAlimentos = local.ModuloPackAlimentos,
                ModuloBilletesAvion = local.ModuloBilletesAvion,
                ModuloPackViajes = local.ModuloPackViajes
            });
        }
        
        // Limpiar archivos para subir (los existentes se cargan en VerDetalles)
        ArchivosParaSubir.Clear();
    }

    private string GenerarCodigoLocal()
    {
        // Extraer 3 letras del nombre del comercio
        var letras = string.IsNullOrEmpty(FormNombreComercio) 
            ? "LOC"
            : new string(FormNombreComercio
                .Where(char.IsLetter)
                .Take(3)
                .ToArray())
                .ToUpper()
                .PadRight(3, 'X');
        
        // Generar 4 dígitos aleatorios únicos
        var random = new Random();
        string digitos;
        int intentos = 0;
        const int maxIntentos = 100;
        
        do
        {
            digitos = random.Next(1000, 9999).ToString();
            intentos++;
            
            if (intentos >= maxIntentos)
            {
                // Si después de 100 intentos no encontramos un código único,
                // usamos un timestamp
                digitos = DateTime.Now.ToString("HHmmss").Substring(2);
                break;
            }
        } 
        while (Comercios.Any(c => c.Locales.Any(l => l.CodigoLocal.EndsWith(digitos))));
        
        return $"{letras}{digitos}";
    }

    private bool ValidarFormulario(out string mensajeError)
    {
        mensajeError = string.Empty;
        
        // Validar nombre comercial
        if (string.IsNullOrWhiteSpace(FormNombreComercio))
        {
            mensajeError = "El nombre del comercio es obligatorio";
            return false;
        }
        
        // Validar email
        if (string.IsNullOrWhiteSpace(FormMailContacto))
        {
            mensajeError = "El email de contacto es obligatorio";
            return false;
        }
        
        // Validar formato de email
        if (!FormMailContacto.Contains("@") || !FormMailContacto.Contains("."))
        {
            mensajeError = "El formato del email no es válido";
            return false;
        }
        
        // Validar que tenga al menos un local
        if (!LocalesComercio.Any())
        {
            mensajeError = "Debe agregar al menos un local";
            return false;
        }
        
        // Validar locales
        foreach (var local in LocalesComercio)
        {
            if (string.IsNullOrWhiteSpace(local.NombreLocal))
            {
                mensajeError = "Todos los locales deben tener un nombre";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.Direccion))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener una dirección";
                return false;
            }
        }
        
        return true;
    }

    // ============================================
    // MÉTODOS AUXILIARES - ARCHIVOS
    // ============================================

    private async Task CargarArchivosComercio(int idComercio)
    {
        try
        {
            ArchivosComercioSeleccionado.Clear();
            
            var archivos = await _archivoService.ObtenerArchivosPorComercio(idComercio);
            
            foreach (var archivo in archivos)
            {
                ArchivosComercioSeleccionado.Add(archivo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar archivos: {ex.Message}");
        }
    }

    // ============================================
    // MÉTODOS AUXILIARES - FILTROS
    // ============================================

    private void CargarPaisesDisponibles()
    {
        PaisesDisponibles.Clear();
        PaisesDisponibles.Add("Todos");
        
        var paises = Comercios.Select(c => c.Pais).Distinct().OrderBy(p => p);
        foreach (var pais in paises)
        {
            if (!string.IsNullOrEmpty(pais))
            {
                PaisesDisponibles.Add(pais);
            }
        }
    }

    private async Task InicializarFiltros()
    {
        // Esperar un momento para que termine de cargar todo
        await Task.Delay(100);
        
        CargarPaisesDisponibles();
        
        // Inicializar con todos los comercios
        ComerciosFiltrados.Clear();
        foreach (var comercio in Comercios.OrderBy(c => c.NombreComercio))
        {
            ComerciosFiltrados.Add(comercio);
        }
    }
}
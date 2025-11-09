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
/// VERSIÓN CORREGIDA CON:
/// - "Flotante" → "Flooter" en todo el código
/// - Botón Activar/Desactivar para LOCALES (no comercios)
/// - Carga de usuarios por local implementada
/// - Corrección de NullReferenceException al ver detalles
/// - Campos obligatorios: Pais, CodigoPostal, TipoVia, Direccion, LocalNumero
/// - Eliminado ComisionDivisas de locales
/// - Filtros mejorados: por tipo de búsqueda, país y cantidad de locales
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

    // Título del botón guardar que cambia según el modo
    public string TituloBotonGuardar => EsModoCreacion ? "CREAR COMERCIO" : "GUARDAR CAMBIOS";

    // ============================================
    // PROPIEDADES PARA FORMULARIO
    // ============================================

    [ObservableProperty]
    private bool _mostrarFormulario;

    [ObservableProperty]
    private bool _modoEdicion;

    [ObservableProperty]
    private string _tituloFormulario = "Crear Comercio";

    // Campos del formulario de comercio (mantener todos para compatibilidad con BD)
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

    // Locales del comercio
    [ObservableProperty]
    private ObservableCollection<LocalFormModel> _localesComercio = new();

    // ============================================
    // PROPIEDADES PARA FILTROS (MEJORADAS Y COMPLETAS)
    // ============================================

    [ObservableProperty]
    private string _filtroBusqueda = string.Empty;

    [ObservableProperty]
    private string _filtroTipoBusqueda = "Todos";

    [ObservableProperty]
    private string _filtroPais = string.Empty;

    // Filtro por módulo
    [ObservableProperty]
    private string _filtroModulo = "Todos";

    [ObservableProperty]
    private ObservableCollection<string> _modulosDisponibles = new()
    {
        "Todos",
        "Compra divisa",
        "Packs de alimentos",
        "Billetes de avión",
        "Packs de viajes"
    };

    [ObservableProperty]
    private ObservableCollection<string> _paisesDisponibles = new();

    // ============================================
    // PROPIEDADES PARA ARCHIVOS
    // ============================================

    [ObservableProperty]
    private ObservableCollection<ArchivoComercioModel> _archivosComercioSeleccionado = new();

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
        _ = CargarDatosDesdeBaseDatos();
        _ = InicializarTablaCorrelativos();
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
                comercio.Locales = await CargarLocalesDelComercio(connection, comercio.IdComercio);
                
                // ✅ CRÍTICO: Cargar usuarios para cada local
                foreach (var local in comercio.Locales)
                {
                    local.Usuarios = await CargarUsuariosDelLocal(connection, local.IdLocal);
                }
                
                comercio.TotalUsuarios = await ContarUsuariosDelComercio(connection, comercio.IdComercio);
                Comercios.Add(comercio);
            }

            OnPropertyChanged(nameof(TotalComercios));
            OnPropertyChanged(nameof(ComerciosActivos));
            OnPropertyChanged(nameof(ComerciosInactivos));
            OnPropertyChanged(nameof(TotalLocales));
            
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
        
        // ✅ QUERY CORREGIDO: Incluye pais, codigo_postal, tipo_via
        var query = @"SELECT id_local, codigo_local, nombre_local,
                             pais, codigo_postal, tipo_via,
                             direccion, local_numero, escalera, piso, 
                             telefono, email, observaciones,
                             activo, modulo_divisas, modulo_pack_alimentos, 
                             modulo_billetes_avion, modulo_pack_viajes
                      FROM locales 
                      WHERE id_comercio = @IdComercio
                      ORDER BY codigo_local";
        
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
                
                // ✅ NUEVOS CAMPOS (índices 3, 4, 5):
                Pais = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                CodigoPostal = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                TipoVia = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                
                // Resto de campos (índices actualizados):
                Direccion = reader.GetString(6),
                LocalNumero = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                Escalera = reader.IsDBNull(8) ? null : reader.GetString(8),
                Piso = reader.IsDBNull(9) ? null : reader.GetString(9),
                Telefono = reader.IsDBNull(10) ? null : reader.GetString(10),
                Email = reader.IsDBNull(11) ? null : reader.GetString(11),
                Observaciones = reader.IsDBNull(12) ? null : reader.GetString(12),
                Activo = reader.GetBoolean(13),
                ModuloDivisas = reader.GetBoolean(14),
                ModuloPackAlimentos = reader.GetBoolean(15),
                ModuloBilletesAvion = reader.GetBoolean(16),
                ModuloPackViajes = reader.GetBoolean(17),
                Usuarios = new List<UserSimpleModel>() // ✅ Inicializar lista vacía
            });
        }
        
        return locales;
    }

    /// <summary>
    /// ✅ CRÍTICO: Cargar usuarios de un local específico
    /// </summary>
    private async Task<List<UserSimpleModel>> CargarUsuariosDelLocal(NpgsqlConnection connection, int idLocal)
    {
        var usuarios = new List<UserSimpleModel>();
        
        var query = @"SELECT u.id_usuario, u.numero_usuario, u.nombre, u.apellidos, u.es_flooter
                      FROM usuarios u
                      WHERE u.id_local = @IdLocal OR (u.es_flooter = true AND EXISTS (
                          SELECT 1 FROM usuario_locales_flooter ul 
                          WHERE ul.id_usuario = u.id_usuario AND ul.id_local = @IdLocal
                      ))
                      ORDER BY u.nombre, u.apellidos";
        
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@IdLocal", idLocal);
        
        using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            usuarios.Add(new UserSimpleModel
            {
                IdUsuario = reader.GetInt32(0),
                NumeroUsuario = reader.GetString(1),
                NombreCompleto = $"{reader.GetString(2)} {reader.GetString(3)}",
                EsFlooter = reader.GetBoolean(4)
            });
        }
        
        return usuarios;
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
        OnPropertyChanged(nameof(TituloBotonGuardar));
        ModoEdicion = false;
        TituloFormulario = "Crear Nuevo Comercio";
        TituloPanelDerecho = "Crear Nuevo Comercio";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
    }

    [RelayCommand]
    private async Task EditarComercio(ComercioModel comercio)
    {
        ComercioSeleccionado = comercio;
        CargarDatosEnFormulario(comercio);
        EsModoCreacion = false;
        OnPropertyChanged(nameof(TituloBotonGuardar));
        ModoEdicion = true;
        TituloFormulario = "Editar Comercio";
        TituloPanelDerecho = $"Editar: {comercio.NombreComercio}";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;

        await CargarArchivosComercio(comercio.IdComercio);
    }

    [RelayCommand]
    private async Task VerDetallesComercio(ComercioModel comercio)
    {
        try
        {
            Cargando = true;
            
            // ✅ CRÍTICO: Recargar el comercio con todos sus datos actualizados
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            // Recargar locales del comercio
            var localesActualizados = await CargarLocalesDelComercio(connection, comercio.IdComercio);
            
            // Cargar usuarios para cada local
            foreach (var local in localesActualizados)
            {
                local.Usuarios = await CargarUsuariosDelLocal(connection, local.IdLocal);
            }
            
            comercio.Locales = localesActualizados;
            
            ComercioSeleccionado = comercio;
            TituloPanelDerecho = $"Detalles: {comercio.NombreComercio}";
            MostrarFormulario = false;
            MostrarPanelDerecho = true;
            
            // Cargar archivos del comercio seleccionado
            await CargarArchivosComercio(comercio.IdComercio);
        }
        catch (Exception ex)
        {
            MensajeExito = $"❌ Error al cargar detalles: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private void CerrarPanelDerecho()
    {
        MostrarPanelDerecho = false;
        MostrarFormulario = false;
        ContenidoPanelDerecho = null;
        ComercioSeleccionado = null;
        ArchivosComercioSeleccionado.Clear();
        LimpiarFormulario();
    }

    [RelayCommand]
    private void CancelarFormulario()
    {
        CerrarPanelDerecho();
    }

    // ============================================
    // COMANDOS - ACCIONES CRUD
    // ============================================

    [RelayCommand]
    private async Task GuardarComercio()
    {
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

            await CargarDatosDesdeBaseDatos();
            CerrarPanelDerecho();

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
            // 1. Insertar comercio principal
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
            
            // 2. Insertar locales
            foreach (var local in LocalesComercio)
            {
                var queryLocal = @"
                    INSERT INTO locales (
                        id_comercio, codigo_local, nombre_local, direccion, local_numero,
                        escalera, piso, telefono, email, observaciones, numero_usuarios_max,
                        activo, modulo_divisas, modulo_pack_alimentos, 
                        modulo_billetes_avion, modulo_pack_viajes,
                        pais, codigo_postal, tipo_via
                    )
                    VALUES (
                        @IdComercio, @CodigoLocal, @NombreLocal, @Direccion, @LocalNumero,
                        @Escalera, @Piso, @Telefono, @Email, @Observaciones, @NumeroUsuariosMax,
                        @Activo, @ModuloDivisas, @ModuloPackAlimentos,
                        @ModuloBilletesAvion, @ModuloPackViajes,
                        @Pais, @CodigoPostal, @TipoVia
                    )";
                
                using var cmdLocal = new NpgsqlCommand(queryLocal, connection, transaction);
                cmdLocal.Parameters.AddWithValue("@IdComercio", idComercio);
                cmdLocal.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
                cmdLocal.Parameters.AddWithValue("@NombreLocal", local.NombreLocal);
                cmdLocal.Parameters.AddWithValue("@Direccion", local.Direccion);
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
                cmdLocal.Parameters.AddWithValue("@Pais", local.Pais ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@CodigoPostal", local.CodigoPostal ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@TipoVia", local.TipoVia ?? string.Empty);
                
                await cmdLocal.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
            
            // 3. Subir archivos
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
                    }
                }
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ActualizarComercio()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            // 1. Actualizar comercio
            var queryComercio = @"
                UPDATE comercios 
                SET nombre_comercio = @NombreComercio,
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
            cmdComercio.Parameters.AddWithValue("@IdComercio", ComercioSeleccionado!.IdComercio);
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
            
            await cmdComercio.ExecuteNonQueryAsync();
            
            // 2. Eliminar locales que ya no están
            var codigosActuales = LocalesComercio.Select(l => l.CodigoLocal).ToList();
            var queryEliminar = @"DELETE FROM locales 
                                  WHERE id_comercio = @IdComercio 
                                  AND codigo_local NOT IN (SELECT unnest(@Codigos))";
            
            using (var cmdEliminar = new NpgsqlCommand(queryEliminar, connection, transaction))
            {
                cmdEliminar.Parameters.AddWithValue("@IdComercio", ComercioSeleccionado.IdComercio);
                cmdEliminar.Parameters.AddWithValue("@Codigos", codigosActuales.ToArray());
                await cmdEliminar.ExecuteNonQueryAsync();
            }
            
            // 3. Actualizar/Insertar locales
            foreach (var local in LocalesComercio)
            {
                var queryUpsert = @"
                    INSERT INTO locales (
                        id_comercio, codigo_local, nombre_local, direccion, local_numero,
                        escalera, piso, telefono, email, observaciones, numero_usuarios_max,
                        activo, modulo_divisas, modulo_pack_alimentos, 
                        modulo_billetes_avion, modulo_pack_viajes,
                        pais, codigo_postal, tipo_via
                    )
                    VALUES (
                        @IdComercio, @CodigoLocal, @NombreLocal, @Direccion, @LocalNumero,
                        @Escalera, @Piso, @Telefono, @Email, @Observaciones, @NumeroUsuariosMax,
                        @Activo, @ModuloDivisas, @ModuloPackAlimentos,
                        @ModuloBilletesAvion, @ModuloPackViajes,
                        @Pais, @CodigoPostal, @TipoVia
                    )
                    ON CONFLICT (codigo_local) 
                    DO UPDATE SET
                        nombre_local = EXCLUDED.nombre_local,
                        direccion = EXCLUDED.direccion,
                        local_numero = EXCLUDED.local_numero,
                        escalera = EXCLUDED.escalera,
                        piso = EXCLUDED.piso,
                        telefono = EXCLUDED.telefono,
                        email = EXCLUDED.email,
                        observaciones = EXCLUDED.observaciones,
                        numero_usuarios_max = EXCLUDED.numero_usuarios_max,
                        activo = EXCLUDED.activo,
                        modulo_divisas = EXCLUDED.modulo_divisas,
                        modulo_pack_alimentos = EXCLUDED.modulo_pack_alimentos,
                        modulo_billetes_avion = EXCLUDED.modulo_billetes_avion,
                        modulo_pack_viajes = EXCLUDED.modulo_pack_viajes,
                        pais = EXCLUDED.pais,
                        codigo_postal = EXCLUDED.codigo_postal,
                        tipo_via = EXCLUDED.tipo_via";
                
                using var cmdLocal = new NpgsqlCommand(queryUpsert, connection, transaction);
                cmdLocal.Parameters.AddWithValue("@IdComercio", ComercioSeleccionado.IdComercio);
                cmdLocal.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
                cmdLocal.Parameters.AddWithValue("@NombreLocal", local.NombreLocal);
                cmdLocal.Parameters.AddWithValue("@Direccion", local.Direccion);
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
                cmdLocal.Parameters.AddWithValue("@Pais", local.Pais ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@CodigoPostal", local.CodigoPostal ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@TipoVia", local.TipoVia ?? string.Empty);
                
                await cmdLocal.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
            
            // 4. Subir nuevos archivos
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
                    }
                }
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [RelayCommand]
    private async Task EliminarComercio(ComercioModel comercio)
    {
        Cargando = true;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await _archivoService.EliminarArchivosDeComercio(comercio.IdComercio);

            var query = "DELETE FROM comercios WHERE id_comercio = @IdComercio";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdComercio", comercio.IdComercio);
            
            await cmd.ExecuteNonQueryAsync();

            await CargarDatosDesdeBaseDatos();

            MensajeExito = $"✓ Comercio {comercio.NombreComercio} eliminado correctamente";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"❌ Error al eliminar: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// ✅ CORREGIDO: Este comando ahora es para LOCALES, no comercios
    /// </summary>
    [RelayCommand]
    private async Task CambiarEstadoLocal(LocalFormModel local)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var nuevoEstado = !local.Activo;
            var query = "UPDATE locales SET activo = @Activo WHERE codigo_local = @CodigoLocal";
            
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Activo", nuevoEstado);
            cmd.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
            
            await cmd.ExecuteNonQueryAsync();

            local.Activo = nuevoEstado;
            
            // ✅ NUEVO: Actualizar también en la vista de detalles si existe
            if (ComercioSeleccionado != null)
            {
                var localEnDetalle = ComercioSeleccionado.Locales.FirstOrDefault(l => l.CodigoLocal == local.CodigoLocal);
                if (localEnDetalle != null)
                {
                    localEnDetalle.Activo = nuevoEstado;
                }
            }

            MensajeExito = $"✓ Local {local.NombreLocal} marcado como {(nuevoEstado ? "Activo" : "Inactivo")}";
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

    /// <summary>
    /// ✅ NUEVO: Comando para cambiar estado desde la vista de detalles
    /// </summary>
    [RelayCommand]
    private async Task CambiarEstadoLocalDetalle(LocalSimpleModel local)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var nuevoEstado = !local.Activo;
            var query = "UPDATE locales SET activo = @Activo WHERE codigo_local = @CodigoLocal";
            
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Activo", nuevoEstado);
            cmd.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
            
            await cmd.ExecuteNonQueryAsync();

            local.Activo = nuevoEstado;

            MensajeExito = $"✓ Local {local.NombreLocal} marcado como {(nuevoEstado ? "Activo" : "Inactivo")}";
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

    /// <summary>
    /// ✅ NUEVO: Toggle para expandir/contraer detalles del local
    /// </summary>
    [RelayCommand]
    private void ToggleLocalDetalles(LocalSimpleModel local)
    {
        if (local != null)
        {
            local.MostrarDetalles = !local.MostrarDetalles;
        }
    }

    // ============================================
    // COMANDOS - FILTROS (LÓGICA MEJORADA Y COMPLETA)
    // ============================================

    [RelayCommand]
    private void AplicarFiltros()
    {
        var filtrados = Comercios.AsEnumerable();
        
        // ===== FILTRO POR BÚSQUEDA GENERAL =====
        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            var busqueda = FiltroBusqueda.Trim();
            
            // Filtro según el tipo de búsqueda seleccionado
            switch (FiltroTipoBusqueda)
            {
                case "Por Comercio":
                    // Buscar solo en datos del comercio
                    filtrados = filtrados.Where(c =>
                        c.NombreComercio.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        c.MailContacto.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        c.NumeroContacto.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        c.DireccionCentral.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                    );
                    break;
                    
                case "Por Local":
                    // Buscar solo en datos de locales
                    filtrados = filtrados.Where(c =>
                        c.Locales.Any(l =>
                            (l.NombreLocal?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.Direccion?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.Email?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.Telefono?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.CodigoPostal?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.TipoVia?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false)
                        )
                    );
                    break;
                    
                case "Por Código":
                    // Buscar solo por código de local
                    filtrados = filtrados.Where(c =>
                        c.Locales.Any(l =>
                            l.CodigoLocal.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                        )
                    );
                    break;
                    
                default: // "Todos"
                    // Búsqueda completa en todos los campos
                    filtrados = filtrados.Where(c =>
                        // Datos del comercio
                        c.NombreComercio.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        c.MailContacto.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        (c.NumeroContacto?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.DireccionCentral?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        c.Pais.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        // Datos de locales
                        c.Locales.Any(l =>
                            l.CodigoLocal.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                            (l.NombreLocal?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.Direccion?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.Email?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.Telefono?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.CodigoPostal?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.TipoVia?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (l.Pais?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false)
                        )
                    );
                    break;
            }
        }
        
        // ===== FILTRO POR PAÍS (BÚSQUEDA DE TEXTO) =====
        if (!string.IsNullOrWhiteSpace(FiltroPais))
        {
            filtrados = filtrados.Where(c =>
                c.Pais.Contains(FiltroPais, StringComparison.OrdinalIgnoreCase) ||
                c.Locales.Any(l => l.Pais.Contains(FiltroPais, StringComparison.OrdinalIgnoreCase))
            );
        }
        
        // ===== FILTRO POR MÓDULO =====
        if (!string.IsNullOrEmpty(FiltroModulo) && FiltroModulo != "Todos")
        {
            filtrados = filtrados.Where(c => c.Locales.Any(l => 
                (FiltroModulo == "Compra divisa" && l.ModuloDivisas) ||
                (FiltroModulo == "Packs de alimentos" && l.ModuloPackAlimentos) ||
                (FiltroModulo == "Billetes de avión" && l.ModuloBilletesAvion) ||
                (FiltroModulo == "Packs de viajes" && l.ModuloPackViajes)
            ));
        }
        
        // ===== APLICAR RESULTADOS =====
        ComerciosFiltrados.Clear();
        foreach (var comercio in filtrados.OrderBy(c => c.NombreComercio))
        {
            ComerciosFiltrados.Add(comercio);
        }
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        FiltroBusqueda = string.Empty;
        FiltroTipoBusqueda = "Todos";
        FiltroModulo = "Todos";
        FiltroPais = string.Empty;
        
        ComerciosFiltrados.Clear();
        foreach (var comercio in Comercios.OrderBy(c => c.NombreComercio))
        {
            ComerciosFiltrados.Add(comercio);
        }
    }

    // ============================================
    // COMANDOS - LOCALES (MEJORADOS)
    // ============================================

    [RelayCommand]
    private async Task AgregarLocal()
    {
        var nuevoLocal = new LocalFormModel
        {
            CodigoLocal = await GenerarCodigoLocal(),
            NombreLocal = $"Local {LocalesComercio.Count + 1}",
            Direccion = string.Empty,
            LocalNumero = string.Empty,
            Activo = true,
            Pais = string.Empty,
            CodigoPostal = string.Empty,
            TipoVia = string.Empty,
            NumeroUsuariosMax = 10,
            ModuloDivisas = false,
            ModuloPackAlimentos = false,
            ModuloBilletesAvion = false,
            ModuloPackViajes = false
        };
        
        LocalesComercio.Add(nuevoLocal);
    }

    [RelayCommand]
    private void QuitarLocal(LocalFormModel local)
    {
        if (local == null) return;
        LocalesComercio.Remove(local);
    }

    // ============================================
    // COMANDOS - ARCHIVOS (MEJORADOS)
    // ============================================

    [RelayCommand]
    private async Task SeleccionarArchivos()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var storage = topLevel.StorageProvider;
            
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar archivos del comercio",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Documentos") 
                    { 
                        Patterns = new[] { "*.pdf", "*.doc", "*.docx", "*.txt" } 
                    },
                    new FilePickerFileType("Imágenes") 
                    { 
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } 
                    },
                    new FilePickerFileType("Todos") 
                    { 
                        Patterns = new[] { "*" } 
                    }
                }
            });

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file.Path.LocalPath);
                if (!ArchivosParaSubir.Contains(fileName))
                {
                    ArchivosParaSubir.Add(fileName);
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
    private void QuitarArchivo(string archivo)
    {
        ArchivosParaSubir.Remove(archivo);
    }

    [RelayCommand]
    private async Task DescargarArchivo(ArchivoComercioModel archivo)
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var storage = topLevel.StorageProvider;
            
            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar archivo",
                SuggestedFileName = archivo.NombreArchivo,
                FileTypeChoices = new[] { new FilePickerFileType("Todos") { Patterns = new[] { "*" } } }
            });

            if (file != null)
            {
                var rutaDestino = file.Path.LocalPath;
                await _archivoService.DescargarArchivo(
                    ComercioSeleccionado!.IdComercio, 
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
    // SISTEMA DE CÓDIGOS DE LOCAL
    // ============================================

    private async Task InicializarTablaCorrelativos()
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var queryCrearTabla = @"
                CREATE TABLE IF NOT EXISTS correlativo_locales (
                    id SERIAL PRIMARY KEY,
                    ultimo_correlativo INTEGER NOT NULL DEFAULT 0,
                    fecha_actualizacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";
            
            using (var cmd = new NpgsqlCommand(queryCrearTabla, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            var queryVerificar = "SELECT COUNT(*) FROM correlativo_locales";
            using (var cmd = new NpgsqlCommand(queryVerificar, connection))
            {
                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (count == 0)
                {
                    var queryInsertar = "INSERT INTO correlativo_locales (ultimo_correlativo) VALUES (0)";
                    using var cmdInsertar = new NpgsqlCommand(queryInsertar, connection);
                    await cmdInsertar.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inicializando tabla correlativos: {ex.Message}");
        }
    }

    private async Task<string> GenerarCodigoLocal()
    {
        if (string.IsNullOrEmpty(FormNombreComercio))
        {
            return "TEMP0001";
        }

        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            // 1. Obtener y actualizar correlativo
            var query = @"UPDATE correlativo_locales 
                         SET ultimo_correlativo = ultimo_correlativo + 1,
                             fecha_actualizacion = CURRENT_TIMESTAMP
                         RETURNING ultimo_correlativo";
            
            int nuevoCorrelativo;
            using (var cmd = new NpgsqlCommand(query, connection, transaction))
            {
                nuevoCorrelativo = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // 2. Generar prefijo de 4 letras únicas
            var prefijo = await GenerarPrefijoUnico(connection, transaction, FormNombreComercio);

            // 3. Formar código: LETRAS + NUMERO con padding de 4 dígitos
            var codigo = $"{prefijo}{nuevoCorrelativo:D4}";

            await transaction.CommitAsync();
            
            return codigo;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<string> GenerarPrefijoUnico(NpgsqlConnection connection, NpgsqlTransaction transaction, string nombreComercio)
    {
        // Limpiar nombre (solo letras)
        var letrasDisponibles = new string(nombreComercio
            .Where(char.IsLetter)
            .ToArray())
            .ToUpper();

        if (letrasDisponibles.Length < 4)
        {
            letrasDisponibles = letrasDisponibles.PadRight(4, 'X');
        }

        // Intentar generar prefijo único
        var random = new Random();
        string prefijo;
        int intentos = 0;
        const int maxIntentos = 100;

        do
        {
            // Tomar 4 letras aleatorias del nombre
            var indices = Enumerable.Range(0, letrasDisponibles.Length)
                .OrderBy(x => random.Next())
                .Take(4)
                .ToList();

            prefijo = new string(indices.Select(i => letrasDisponibles[i]).ToArray());

            // Verificar si ya existe
            var queryVerificar = @"SELECT COUNT(*) FROM locales WHERE codigo_local LIKE @Prefijo || '%'";
            using var cmd = new NpgsqlCommand(queryVerificar, connection, transaction);
            cmd.Parameters.AddWithValue("@Prefijo", prefijo);
            
            var existe = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
            
            if (!existe)
                break;

            intentos++;
            
        } while (intentos < maxIntentos);

        if (intentos >= maxIntentos)
        {
            // Fallback: usar hash del nombre
            var hash = Math.Abs(nombreComercio.GetHashCode());
            prefijo = $"C{hash % 1000:D3}";
        }

        return prefijo;
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
                
                // ✅ CARGAR desde el local, no vacíos:
                Pais = local.Pais ?? string.Empty,
                CodigoPostal = local.CodigoPostal ?? string.Empty,
                TipoVia = local.TipoVia ?? string.Empty,
                
                Direccion = local.Direccion,
                LocalNumero = local.LocalNumero,
                Escalera = local.Escalera,
                Piso = local.Piso,
                Telefono = local.Telefono,
                Email = local.Email,
                Observaciones = local.Observaciones,
                Activo = local.Activo,
                ModuloDivisas = local.ModuloDivisas,
                ModuloPackAlimentos = local.ModuloPackAlimentos,
                ModuloBilletesAvion = local.ModuloBilletesAvion,
                ModuloPackViajes = local.ModuloPackViajes,
                NumeroUsuariosMax = 10
            });
        }
        
        ArchivosParaSubir.Clear();
    }

    private bool ValidarFormulario(out string mensajeError)
    {
        mensajeError = string.Empty;
        
        if (string.IsNullOrWhiteSpace(FormNombreComercio))
        {
            mensajeError = "El nombre del comercio es requerido";
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(FormMailContacto))
        {
            mensajeError = "El email de contacto es requerido";
            return false;
        }
        
        if (!FormMailContacto.Contains("@"))
        {
            mensajeError = "El formato del email no es válido";
            return false;
        }
        
        if (!LocalesComercio.Any())
        {
            mensajeError = "Debe agregar al menos un local";
            return false;
        }
        
        foreach (var local in LocalesComercio)
        {
            if (string.IsNullOrWhiteSpace(local.NombreLocal))
            {
                mensajeError = "Todos los locales deben tener un nombre";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.Pais))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener un país";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.CodigoPostal))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener código postal";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.TipoVia))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener tipo de vía";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.Direccion))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener una dirección";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.LocalNumero))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener un número";
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

    private async Task InicializarFiltros()
    {
        await Task.Delay(100);
        
        ComerciosFiltrados.Clear();
        foreach (var comercio in Comercios.OrderBy(c => c.NombreComercio))
        {
            ComerciosFiltrados.Add(comercio);
        }
    }
}
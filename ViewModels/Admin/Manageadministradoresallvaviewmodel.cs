using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models.Admin;
using Npgsql;
using BCrypt.Net;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel COMPLETO para gestión de Administradores Allva
/// FASE 1: Sistema de 4 niveles de acceso + módulos habilitados
/// ACTUALIZADO: Filtros corregidos con índices, botón limpiar, sin iconos
/// </summary>
public partial class ManageAdministradoresAllvaViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    // ============================================
    // COLECCIONES PRINCIPALES
    // ============================================

    [ObservableProperty]
    private ObservableCollection<AdministradorAllvaModel> _administradores = new();

    [ObservableProperty]
    private ObservableCollection<AdministradorAllvaModel> _administradoresFiltrados = new();

    [ObservableProperty]
    private AdministradorAllvaModel? _administradorSeleccionado;

    [ObservableProperty]
    private ObservableCollection<NivelAccesoModel> _nivelesDisponibles = new();

    public List<ModuloDisponible> ModulosDisponibles { get; } = new()
    {
        new ModuloDisponible { Codigo = "compra_divisa", Nombre = "Compra de Divisa" },
        new ModuloDisponible { Codigo = "packs_alimentos", Nombre = "Packs de Alimentos" },
        new ModuloDisponible { Codigo = "billetes_avion", Nombre = "Billetes de Avión" },
        new ModuloDisponible { Codigo = "pack_viajes", Nombre = "Pack de Viajes" }
    };

    // ============================================
    // ESTADOS DE UI
    // ============================================

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private bool _mostrarMensajeExito;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    [ObservableProperty]
    private bool _mostrarPanelDerecho = false;

    [ObservableProperty]
    private string _tituloPanelDerecho = "Detalles del Administrador";

    [ObservableProperty]
    private bool _mostrarFormulario;

    [ObservableProperty]
    private bool _modoEdicion;

    [ObservableProperty]
    private string _tituloBotonGuardar = "Crear";

    // ============================================
    // CAMPOS DEL FORMULARIO - DATOS PERSONALES
    // ============================================

    [ObservableProperty]
    private string _formNombre = string.Empty;

    [ObservableProperty]
    private string _formApellidos = string.Empty;

    [ObservableProperty]
    private string _formNombreUsuario = string.Empty;

    [ObservableProperty]
    private string _formCorreo = string.Empty;

    [ObservableProperty]
    private string _formTelefono = string.Empty;

    [ObservableProperty]
    private string _formPassword = string.Empty;

    [ObservableProperty]
    private bool _formActivo = true;

    // ============================================
    // CAMPOS DEL FORMULARIO - SISTEMA DE NIVELES
    // ============================================

    [ObservableProperty]
    private int _formNivelAcceso = 1;

    public int FormNivelAccesoIndex
    {
        get => FormNivelAcceso - 1;
        set
        {
            if (FormNivelAcceso != value + 1)
            {
                FormNivelAcceso = value + 1;
            }
        }
    }

    [ObservableProperty]
    private string _formDescripcionNivel = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ModuloCheckbox> _formModulosSeleccionables = new();

    // ============================================
    // FILTROS DE BÚSQUEDA (CORREGIDOS CON ÍNDICES)
    // ============================================

    [ObservableProperty]
    private string _filtroBusqueda = string.Empty;

    [ObservableProperty]
    private int _filtroNivelSeleccionado = 0; // 0 = Todos, 1-4 = Niveles

    [ObservableProperty]
    private int _filtroEstadoSeleccionado = 0; // 0 = Todos, 1 = Activo, 2 = Inactivo

    // ============================================
    // ESTADÍSTICAS
    // ============================================

    public int TotalAdministradores => Administradores.Count;
    public int AdministradoresActivos => Administradores.Count(a => a.Activo);
    public int AdministradoresInactivos => Administradores.Count(a => !a.Activo);
    public int SuperAdministradores => Administradores.Count(a => a.NivelAcceso == 4);

    private AdministradorAllvaModel? _administradorEnEdicion;

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public ManageAdministradoresAllvaViewModel()
    {
        InicializarModulosSeleccionables();
        _ = CargarDatos();
    }

    // ============================================
    // CARGA DE DATOS
    // ============================================

    private async Task CargarDatos()
    {
        await CargarNivelesDisponibles();
        await CargarAdministradores();
    }

    private async Task CargarNivelesDisponibles()
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = "SELECT id_nivel, nombre_nivel, descripcion FROM niveles_acceso ORDER BY id_nivel";

            using var cmd = new NpgsqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            var niveles = new List<NivelAccesoModel>();

            while (await reader.ReadAsync())
            {
                niveles.Add(new NivelAccesoModel
                {
                    IdNivel = reader.GetInt32(0),
                    NombreNivel = reader.GetString(1),
                    Descripcion = reader.GetString(2)
                });
            }

            NivelesDisponibles.Clear();
            foreach (var nivel in niveles)
            {
                NivelesDisponibles.Add(nivel);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar niveles: {ex.Message}");
        }
    }

    private async Task CargarAdministradores()
    {
        Cargando = true;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    id_administrador, nombre, apellidos, nombre_usuario, correo, telefono,
                    nivel_acceso, activo, ultimo_acceso, fecha_creacion,
                    acceso_gestion_comercios, acceso_gestion_usuarios_locales, 
                    acceso_gestion_usuarios_allva, acceso_analytics
                FROM administradores_allva
                ORDER BY nombre, apellidos";

            using var cmd = new NpgsqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            var lista = new List<AdministradorAllvaModel>();

            while (await reader.ReadAsync())
            {
                var admin = new AdministradorAllvaModel
                {
                    IdAdministrador = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Apellidos = reader.GetString(2),
                    NombreUsuario = reader.GetString(3),
                    Correo = reader.GetString(4),
                    Telefono = reader.IsDBNull(5) ? "N/A" : reader.GetString(5),
                    NivelAcceso = reader.IsDBNull(6) ? 1 : reader.GetInt32(6),
                    Activo = reader.GetBoolean(7),
                    UltimoAcceso = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    FechaCreacion = reader.GetDateTime(9),
                    AccesoGestionComercios = reader.GetBoolean(10),
                    AccesoGestionUsuariosLocales = reader.GetBoolean(11),
                    AccesoGestionUsuariosAllva = reader.GetBoolean(12),
                    AccesoAnalytics = reader.GetBoolean(13)
                };

                lista.Add(admin);
            }

            reader.Close();

            // Cargar módulos habilitados
            foreach (var admin in lista)
            {
                admin.ModulosHabilitados = await CargarModulosHabilitados(admin.IdAdministrador, connection);
            }

            Administradores.Clear();
            foreach (var admin in lista)
            {
                Administradores.Add(admin);
            }

            AdministradoresFiltrados = new ObservableCollection<AdministradorAllvaModel>(lista);
            ActualizarEstadisticas();
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task<List<string>> CargarModulosHabilitados(int idAdmin, NpgsqlConnection connection)
    {
        var modulos = new List<string>();
        var query = "SELECT nombre_modulo FROM admin_modulos_habilitados WHERE id_administrador = @IdAdmin";
        
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@IdAdmin", idAdmin);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            modulos.Add(reader.GetString(0));
        }

        return modulos;
    }

    private void ActualizarEstadisticas()
    {
        OnPropertyChanged(nameof(TotalAdministradores));
        OnPropertyChanged(nameof(AdministradoresActivos));
        OnPropertyChanged(nameof(AdministradoresInactivos));
        OnPropertyChanged(nameof(SuperAdministradores));
    }

    // ============================================
    // COMANDOS
    // ============================================

    [RelayCommand]
    private void MostrarFormularioCrear()
    {
        LimpiarFormulario();
        ModoEdicion = false;
        TituloPanelDerecho = "Crear Nuevo Administrador";
        TituloBotonGuardar = "Crear";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
        ActualizarDescripcionNivel();
    }

    [RelayCommand]
    private async Task EditarAdministrador(AdministradorAllvaModel admin)
    {
        _administradorEnEdicion = admin;
        AdministradorSeleccionado = admin;

        FormNombre = admin.Nombre;
        FormApellidos = admin.Apellidos;
        FormNombreUsuario = admin.NombreUsuario;
        FormCorreo = admin.Correo;
        FormTelefono = admin.Telefono ?? string.Empty;
        FormActivo = admin.Activo;
        FormNivelAcceso = admin.NivelAcceso;
        FormPassword = string.Empty;

        foreach (var modulo in FormModulosSeleccionables)
        {
            modulo.Seleccionado = admin.ModulosHabilitados.Contains(modulo.Codigo);
        }

        ModoEdicion = true;
        TituloPanelDerecho = $"Editar: {admin.NombreCompleto}";
        TituloBotonGuardar = "Actualizar";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
        ActualizarDescripcionNivel();
        
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task VerDetallesAdministrador(AdministradorAllvaModel admin)
    {
        AdministradorSeleccionado = admin;
        TituloPanelDerecho = $"Detalles de {admin.NombreCompleto}";
        MostrarFormulario = false;
        MostrarPanelDerecho = true;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void CerrarPanelDerecho()
    {
        MostrarPanelDerecho = false;
        MostrarFormulario = false;
        AdministradorSeleccionado = null;
        LimpiarFormulario();
    }

    [RelayCommand]
    private async Task GuardarAdministrador()
    {
        if (!ValidarFormulario(out string mensajeError))
        {
            MensajeExito = $"Advertencia: {mensajeError}";
            MostrarMensajeExito = true;
            await Task.Delay(4000);
            MostrarMensajeExito = false;
            return;
        }

        Cargando = true;

        try
        {
            if (ModoEdicion && _administradorEnEdicion != null)
            {
                await ActualizarAdministrador();
                MensajeExito = "Administrador actualizado";
            }
            else
            {
                await CrearNuevoAdministrador();
                MensajeExito = "Administrador creado";
            }

            await CargarAdministradores();
            CerrarPanelDerecho();

            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(5000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task CrearNuevoAdministrador()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);

            var query = @"
                INSERT INTO administradores_allva (
                    nombre, apellidos, nombre_usuario, password_hash, correo, telefono,
                    nivel_acceso, activo, primer_login, creado_por, idioma, fecha_creacion,
                    acceso_gestion_comercios, acceso_gestion_usuarios_locales, 
                    acceso_gestion_usuarios_allva, acceso_analytics
                )
                VALUES (
                    @Nombre, @Apellidos, @NombreUsuario, @PasswordHash, @Correo, @Telefono,
                    @NivelAcceso, @Activo, true, 'SISTEMA', 'es', @FechaCreacion,
                    @AccesoGestionComercios, @AccesoGestionUsuariosLocales,
                    @AccesoGestionUsuariosAllva, @AccesoAnalytics
                )
                RETURNING id_administrador";

            using var cmd = new NpgsqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@Nombre", FormNombre);
            cmd.Parameters.AddWithValue("@Apellidos", FormApellidos);
            cmd.Parameters.AddWithValue("@NombreUsuario", FormNombreUsuario);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@Correo", FormCorreo);
            cmd.Parameters.AddWithValue("@Telefono", 
                string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono);
            cmd.Parameters.AddWithValue("@NivelAcceso", FormNivelAcceso);
            cmd.Parameters.AddWithValue("@Activo", FormActivo);
            cmd.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
            
            var (comercios, usuarios, admins, analytics) = CalcularPermisosLegacy(FormNivelAcceso);
            cmd.Parameters.AddWithValue("@AccesoGestionComercios", comercios);
            cmd.Parameters.AddWithValue("@AccesoGestionUsuariosLocales", usuarios);
            cmd.Parameters.AddWithValue("@AccesoGestionUsuariosAllva", admins);
            cmd.Parameters.AddWithValue("@AccesoAnalytics", analytics);

            var idAdmin = (int)(await cmd.ExecuteScalarAsync() ?? 0);

            if (FormNivelAcceso < 3)
            {
                await GuardarModulosHabilitados(idAdmin, connection, transaction);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ActualizarAdministrador()
    {
        if (_administradorEnEdicion == null) return;

        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var queryBase = @"
                UPDATE administradores_allva SET
                    nombre = @Nombre,
                    apellidos = @Apellidos,
                    correo = @Correo,
                    telefono = @Telefono,
                    nivel_acceso = @NivelAcceso,
                    activo = @Activo,
                    fecha_modificacion = @FechaModificacion,
                    acceso_gestion_comercios = @AccesoGestionComercios,
                    acceso_gestion_usuarios_locales = @AccesoGestionUsuariosLocales,
                    acceso_gestion_usuarios_allva = @AccesoGestionUsuariosAllva,
                    acceso_analytics = @AccesoAnalytics";

            var query = queryBase;
            if (!string.IsNullOrWhiteSpace(FormPassword))
            {
                query += ", password_hash = @PasswordHash";
            }

            query += " WHERE id_administrador = @IdAdministrador";

            using var cmd = new NpgsqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@Nombre", FormNombre);
            cmd.Parameters.AddWithValue("@Apellidos", FormApellidos);
            cmd.Parameters.AddWithValue("@Correo", FormCorreo);
            cmd.Parameters.AddWithValue("@Telefono", 
                string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono);
            cmd.Parameters.AddWithValue("@NivelAcceso", FormNivelAcceso);
            cmd.Parameters.AddWithValue("@Activo", FormActivo);
            cmd.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
            cmd.Parameters.AddWithValue("@IdAdministrador", _administradorEnEdicion.IdAdministrador);

            var (comercios, usuarios, admins, analytics) = CalcularPermisosLegacy(FormNivelAcceso);
            cmd.Parameters.AddWithValue("@AccesoGestionComercios", comercios);
            cmd.Parameters.AddWithValue("@AccesoGestionUsuariosLocales", usuarios);
            cmd.Parameters.AddWithValue("@AccesoGestionUsuariosAllva", admins);
            cmd.Parameters.AddWithValue("@AccesoAnalytics", analytics);

            if (!string.IsNullOrWhiteSpace(FormPassword))
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            }

            await cmd.ExecuteNonQueryAsync();

            if (FormNivelAcceso < 3)
            {
                var deleteQuery = "DELETE FROM admin_modulos_habilitados WHERE id_administrador = @IdAdmin";
                using var deleteCmd = new NpgsqlCommand(deleteQuery, connection, transaction);
                deleteCmd.Parameters.AddWithValue("@IdAdmin", _administradorEnEdicion.IdAdministrador);
                await deleteCmd.ExecuteNonQueryAsync();

                await GuardarModulosHabilitados(_administradorEnEdicion.IdAdministrador, connection, transaction);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task GuardarModulosHabilitados(int idAdmin, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var modulosSeleccionados = FormModulosSeleccionables.Where(m => m.Seleccionado).ToList();

        foreach (var modulo in modulosSeleccionados)
        {
            var query = @"
                INSERT INTO admin_modulos_habilitados (id_administrador, nombre_modulo, fecha_asignacion)
                VALUES (@IdAdmin, @NombreModulo, @Fecha)
                ON CONFLICT (id_administrador, nombre_modulo) DO NOTHING";

            using var cmd = new NpgsqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@IdAdmin", idAdmin);
            cmd.Parameters.AddWithValue("@NombreModulo", modulo.Codigo);
            cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    [RelayCommand]
    private async Task CambiarEstadoAdministrador(AdministradorAllvaModel admin)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var nuevoEstado = !admin.Activo;
            var query = @"UPDATE administradores_allva 
                         SET activo = @Activo, fecha_modificacion = @Fecha
                         WHERE id_administrador = @Id";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Activo", nuevoEstado);
            cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
            cmd.Parameters.AddWithValue("@Id", admin.IdAdministrador);

            await cmd.ExecuteNonQueryAsync();

            admin.Activo = nuevoEstado;
            ActualizarEstadisticas();

            MensajeExito = $"Estado cambiado a: {(nuevoEstado ? "Activo" : "Bloqueado")}";
            MostrarMensajeExito = true;
            await Task.Delay(2000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    [RelayCommand]
    private async Task EliminarAdministrador(AdministradorAllvaModel admin)
    {
        Cargando = true;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Eliminar módulos habilitados primero (clave foránea)
                var deleteModulosQuery = "DELETE FROM admin_modulos_habilitados WHERE id_administrador = @Id";
                using var deleteModulosCmd = new NpgsqlCommand(deleteModulosQuery, connection, transaction);
                deleteModulosCmd.Parameters.AddWithValue("@Id", admin.IdAdministrador);
                await deleteModulosCmd.ExecuteNonQueryAsync();

                // Eliminar administrador
                var deleteAdminQuery = "DELETE FROM administradores_allva WHERE id_administrador = @Id";
                using var deleteAdminCmd = new NpgsqlCommand(deleteAdminQuery, connection, transaction);
                deleteAdminCmd.Parameters.AddWithValue("@Id", admin.IdAdministrador);
                await deleteAdminCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                await CargarAdministradores();

                MensajeExito = "Administrador eliminado correctamente";
                MostrarMensajeExito = true;
                await Task.Delay(3000);
                MostrarMensajeExito = false;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al eliminar: {ex.Message}";
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
    private void AplicarFiltros()
    {
        try
        {
            var filtrados = Administradores.AsEnumerable();

            // Filtro por nombre (nombre completo, usuario o correo)
            if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
            {
                var busqueda = FiltroBusqueda.Trim();
                filtrados = filtrados.Where(a =>
                    (a.NombreCompleto?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.NombreUsuario?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.Correo?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Filtro por nivel de acceso
            if (FiltroNivelSeleccionado > 0) // Si no es "Todos"
            {
                var nivel = FiltroNivelSeleccionado; // 1, 2, 3 o 4
                filtrados = filtrados.Where(a => a.NivelAcceso == nivel);
            }

            // Filtro por estado
            if (FiltroEstadoSeleccionado == 1) // Activo
            {
                filtrados = filtrados.Where(a => a.Activo);
            }
            else if (FiltroEstadoSeleccionado == 2) // Inactivo
            {
                filtrados = filtrados.Where(a => !a.Activo);
            }
            // Si es 0 (Todos), no filtra

            AdministradoresFiltrados.Clear();
            foreach (var admin in filtrados.OrderBy(a => a.NombreCompleto))
            {
                AdministradoresFiltrados.Add(admin);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al aplicar filtros: {ex.Message}");
            
            // Recargar todos sin filtros
            AdministradoresFiltrados.Clear();
            foreach (var admin in Administradores.OrderBy(a => a.NombreCompleto))
            {
                AdministradoresFiltrados.Add(admin);
            }
        }
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        FiltroBusqueda = string.Empty;
        FiltroNivelSeleccionado = 0;
        FiltroEstadoSeleccionado = 0;
        
        // Recargar todos los administradores sin filtros
        AdministradoresFiltrados.Clear();
        foreach (var admin in Administradores.OrderBy(a => a.NombreCompleto))
        {
            AdministradoresFiltrados.Add(admin);
        }
    }

    private void InicializarModulosSeleccionables()
    {
        FormModulosSeleccionables.Clear();
        foreach (var modulo in ModulosDisponibles)
        {
            FormModulosSeleccionables.Add(new ModuloCheckbox
            {
                Codigo = modulo.Codigo,
                Nombre = modulo.Nombre,
                Seleccionado = false
            });
        }
    }

    private void LimpiarFormulario()
    {
        FormNombre = string.Empty;
        FormApellidos = string.Empty;
        FormNombreUsuario = string.Empty;
        FormCorreo = string.Empty;
        FormTelefono = string.Empty;
        FormPassword = string.Empty;
        FormActivo = true;
        FormNivelAcceso = 1;
        
        foreach (var modulo in FormModulosSeleccionables)
        {
            modulo.Seleccionado = false;
        }

        _administradorEnEdicion = null;
        ActualizarDescripcionNivel();
    }

    private string GenerarNombreUsuario()
    {
        var nombre = FormNombre.Trim().ToLower().Replace(" ", "_");
        var apellidos = FormApellidos.Trim().ToLower().Replace(" ", "_");
        var baseNombre = $"{nombre}_{apellidos}";

        var nombreUsuario = baseNombre;
        var contador = 2;

        while (Administradores.Any(a => a.NombreUsuario == nombreUsuario))
        {
            nombreUsuario = $"{baseNombre}_{contador}";
            contador++;
        }

        return nombreUsuario;
    }

    private void ActualizarDescripcionNivel()
    {
        FormDescripcionNivel = FormNivelAcceso switch
        {
            1 => "Puede ver y editar pestañas de un módulo concreto (Balance, Operaciones o Informes).",
            2 => "Puede ver y editar módulos concretos, dar altas y editar comercios o usuarios del módulo asignado.",
            3 => "Puede ver y editar todos los módulos excepto crear usuarios de Allva.",
            4 => "Acceso total: puede crear, editar y aprobar nuevos usuarios Allva y gestionar niveles de acceso.",
            _ => ""
        };

        if (FormNivelAcceso >= 3)
        {
            foreach (var modulo in FormModulosSeleccionables)
            {
                modulo.Seleccionado = true;
            }
        }
    }

    private (bool comercios, bool usuarios, bool admins, bool analytics) CalcularPermisosLegacy(int nivel)
    {
        return nivel switch
        {
            1 => (false, false, false, false),
            2 => (true, true, false, false),
            3 => (true, true, false, true),
            4 => (true, true, true, true),
            _ => (false, false, false, false)
        };
    }

    private bool ValidarFormulario(out string mensajeError)
    {
        mensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(FormNombre))
        {
            mensajeError = "El nombre es obligatorio";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormApellidos))
        {
            mensajeError = "Los apellidos son obligatorios";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormCorreo))
        {
            mensajeError = "El correo electrónico es obligatorio";
            return false;
        }

        if (!FormCorreo.Contains("@") || !FormCorreo.Contains("."))
        {
            mensajeError = "El formato del correo no es válido";
            return false;
        }

        if (!ModoEdicion && string.IsNullOrWhiteSpace(FormPassword))
        {
            mensajeError = "La contraseña es obligatoria";
            return false;
        }

        if (!ModoEdicion && FormPassword.Length < 8)
        {
            mensajeError = "La contraseña debe tener al menos 8 caracteres";
            return false;
        }

        if (FormNivelAcceso < 3 && !FormModulosSeleccionables.Any(m => m.Seleccionado))
        {
            mensajeError = "Debe seleccionar al menos un módulo";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormNombreUsuario))
        {
            FormNombreUsuario = GenerarNombreUsuario();
        }

        return true;
    }

    partial void OnFormNombreChanged(string value)
    {
        if (!ModoEdicion && !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(FormApellidos))
        {
            FormNombreUsuario = GenerarNombreUsuario();
        }
    }

    partial void OnFormApellidosChanged(string value)
    {
        if (!ModoEdicion && !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(FormNombre))
        {
            FormNombreUsuario = GenerarNombreUsuario();
        }
    }

    partial void OnFormNivelAccesoChanged(int value)
    {
        ActualizarDescripcionNivel();
        OnPropertyChanged(nameof(FormNivelAccesoIndex));
    }
}

public class ModuloDisponible
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public partial class ModuloCheckbox : ObservableObject
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _seleccionado;
}
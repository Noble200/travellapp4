using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Models.Admin;
using Npgsql;
using BCrypt.Net;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel para la gestión de usuarios normales y flotantes
/// VERSIÓN CORREGIDA - Sin observaciones, usando estructura real de BD
/// </summary>
public partial class ManageUsersViewModel : ObservableObject
{
    // ============================================
    // CONFIGURACIÓN DE BASE DE DATOS
    // ============================================
    
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    // ============================================
    // PROPIEDADES OBSERVABLES - DATOS PRINCIPALES
    // ============================================

    [ObservableProperty]
    private ObservableCollection<UserModel> _usuarios = new();

    [ObservableProperty]
    private ObservableCollection<UserModel> _usuariosFiltrados = new();

    [ObservableProperty]
    private UserModel? _usuarioSeleccionado;

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
    private string _tituloPanelDerecho = "Detalles del Usuario";

    [ObservableProperty]
    private bool _mostrarFormulario;

    [ObservableProperty]
    private bool _modoEdicion;

    [ObservableProperty]
    private string _tituloFormulario = "Crear Usuario";

    // ============================================
    // CAMPOS DEL FORMULARIO
    // ============================================

    [ObservableProperty]
    private string _formNombre = string.Empty;

    [ObservableProperty]
    private string _formApellidos = string.Empty;

    [ObservableProperty]
    private string _formNumeroUsuario = string.Empty;

    [ObservableProperty]
    private string _formCorreo = string.Empty;

    [ObservableProperty]
    private string _formTelefono = string.Empty;

    [ObservableProperty]
    private string _formPassword = string.Empty;

    [ObservableProperty]
    private bool _formEsFlotante;

    [ObservableProperty]
    private bool _formActivo = true;

    // ============================================
    // PROPIEDADES PARA FILTROS
    // ============================================

    [ObservableProperty]
    private string _filtroBusqueda = string.Empty;

    [ObservableProperty]
    private string _filtroLocal = "Todos";

    [ObservableProperty]
    private string _filtroComercio = "Todos";

    [ObservableProperty]
    private string _filtroEstado = "Todos";

    [ObservableProperty]
    private string _filtroTipoUsuario = "Todos";

    [ObservableProperty]
    private ObservableCollection<string> _localesDisponibles = new();

    [ObservableProperty]
    private ObservableCollection<string> _comerciosDisponibles = new();

    // ============================================
    // BÚSQUEDA Y ASIGNACIÓN DE LOCALES
    // ============================================

    [ObservableProperty]
    private string _busquedaLocal = string.Empty;

    [ObservableProperty]
    private bool _mostrarResultadosBusqueda;

    [ObservableProperty]
    private ObservableCollection<LocalFormModel> _resultadosBusquedaLocales = new();

    [ObservableProperty]
    private ObservableCollection<LocalFormModel> _localesAsignados = new();

    // ============================================
    // ESTADÍSTICAS
    // ============================================

    public int TotalUsuarios => Usuarios.Count;
    public int UsuariosActivos => Usuarios.Count(u => u.Activo);
    public int UsuariosInactivos => Usuarios.Count(u => !u.Activo);
    public int UsuariosFlotantes => Usuarios.Count(u => u.EsFlotante);

    // Usuario en edición
    private UserModel? _usuarioEnEdicion;

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public ManageUsersViewModel()
    {
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

            var usuarios = await CargarUsuarios(connection);

            Usuarios.Clear();
            foreach (var usuario in usuarios)
            {
                Usuarios.Add(usuario);
            }

            // Actualizar contadores
            OnPropertyChanged(nameof(TotalUsuarios));
            OnPropertyChanged(nameof(UsuariosActivos));
            OnPropertyChanged(nameof(UsuariosInactivos));
            OnPropertyChanged(nameof(UsuariosFlotantes));

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

    private async Task<List<UserModel>> CargarUsuarios(NpgsqlConnection connection)
    {
        var usuarios = new List<UserModel>();

        // Columnas reales: id_usuario, id_comercio, id_local, id_rol, nombre, apellidos, 
        // correo, telefono, numero_usuario, password_hash, es_flotante, idioma, activo,
        // primer_login, ultimo_acceso, intentos_fallidos, bloqueado_hasta, fecha_creacion, fecha_modificacion
        var query = @"SELECT u.id_usuario, u.numero_usuario, u.nombre, u.apellidos,
                             u.correo, u.telefono, u.es_flotante, u.activo, u.ultimo_acceso,
                             l.id_local, l.nombre_local, l.codigo_local,
                             c.id_comercio, c.nombre_comercio
                      FROM usuarios u
                      LEFT JOIN locales l ON u.id_local = l.id_local
                      LEFT JOIN comercios c ON l.id_comercio = c.id_comercio
                      ORDER BY u.nombre, u.apellidos";

        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            usuarios.Add(new UserModel
            {
                IdUsuario = reader.GetInt32(0),
                NumeroUsuario = reader.GetString(1),
                Nombre = reader.GetString(2),
                Apellidos = reader.GetString(3),
                Correo = reader.GetString(4),
                Telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
                EsFlotante = reader.GetBoolean(6),
                Activo = reader.GetBoolean(7),
                UltimoAcceso = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                IdLocal = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                NombreLocal = reader.IsDBNull(10) ? "Sin asignar" : reader.GetString(10),
                CodigoLocal = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                IdComercio = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
                NombreComercio = reader.IsDBNull(13) ? "Sin asignar" : reader.GetString(13)
            });
        }

        return usuarios;
    }

    // ============================================
    // COMANDOS - PANEL DERECHO
    // ============================================

    [RelayCommand]
    private void MostrarFormularioCrear()
    {
        LimpiarFormulario();
        ModoEdicion = false;
        TituloFormulario = "Crear Nuevo Usuario";
        TituloPanelDerecho = "Crear Nuevo Usuario";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
    }

    [RelayCommand]
    private async Task EditarUsuario(UserModel usuario)
    {
        _usuarioEnEdicion = usuario;
        UsuarioSeleccionado = usuario;

        FormNombre = usuario.Nombre;
        FormApellidos = usuario.Apellidos;
        FormNumeroUsuario = usuario.NumeroUsuario;
        FormCorreo = usuario.Correo;
        FormTelefono = usuario.Telefono ?? string.Empty;
        FormEsFlotante = usuario.EsFlotante;
        FormActivo = usuario.Activo;
        FormPassword = string.Empty;

        // Cargar locales asignados
        await CargarLocalesAsignadosUsuario(usuario.IdUsuario);

        ModoEdicion = true;
        TituloFormulario = "Editar Usuario";
        TituloPanelDerecho = $"Editar: {usuario.NombreCompleto}";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
    }

    [RelayCommand]
    private async Task VerDetallesUsuario(UserModel usuario)
    {
        UsuarioSeleccionado = usuario;
        TituloPanelDerecho = $"Detalles de {usuario.NombreCompleto}";
        MostrarFormulario = false;
        MostrarPanelDerecho = true;

        // Cargar locales asignados para vista de detalles
        await CargarLocalesAsignadosUsuario(usuario.IdUsuario);
    }

    [RelayCommand]
    private void CerrarPanelDerecho()
    {
        MostrarPanelDerecho = false;
        MostrarFormulario = false;
        UsuarioSeleccionado = null;
        LimpiarFormulario();
    }

    // ============================================
    // COMANDOS - ACCIONES CRUD
    // ============================================

    [RelayCommand]
    private async Task GuardarUsuario()
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
            if (ModoEdicion && _usuarioEnEdicion != null)
            {
                await ActualizarUsuario();
                MensajeExito = "✓ Usuario actualizado correctamente";
            }
            else
            {
                await CrearNuevoUsuario();
                MensajeExito = "✓ Usuario creado correctamente";
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

    private async Task CrearNuevoUsuario()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Hashear contraseña
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);

            // 2. Determinar id_local principal
            int? idLocalPrincipal = null;
            if (!FormEsFlotante && LocalesAsignados.Any())
            {
                idLocalPrincipal = LocalesAsignados.First().IdLocal;
            }

            // 3. Obtener id_comercio del primer local
            int? idComercio = null;
            if (LocalesAsignados.Any())
            {
                var queryComercio = "SELECT id_comercio FROM locales WHERE id_local = @IdLocal";
                using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
                cmdComercio.Parameters.AddWithValue("@IdLocal", LocalesAsignados.First().IdLocal);
                var result = await cmdComercio.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    idComercio = Convert.ToInt32(result);
                }
            }

            // 4. Insertar usuario principal
            // Campos: id_comercio, id_local, id_rol, nombre, apellidos, correo, telefono,
            // numero_usuario, password_hash, es_flotante, idioma, activo, primer_login,
            // fecha_creacion, fecha_modificacion
            var queryUsuario = @"
                INSERT INTO usuarios (
                    id_comercio, id_local, id_rol, nombre, apellidos, correo, telefono,
                    numero_usuario, password_hash, es_flotante, idioma, activo, primer_login,
                    fecha_creacion, fecha_modificacion
                )
                VALUES (
                    @IdComercio, @IdLocal, 2, @Nombre, @Apellidos, @Correo, @Telefono,
                    @NumeroUsuario, @PasswordHash, @EsFlotante, 'es', @Activo, true,
                    @FechaCreacion, @FechaModificacion
                )
                RETURNING id_usuario";

            using var cmdUsuario = new NpgsqlCommand(queryUsuario, connection, transaction);
            cmdUsuario.Parameters.AddWithValue("@IdComercio", idComercio.HasValue ? idComercio.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@IdLocal", idLocalPrincipal.HasValue ? idLocalPrincipal.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@Nombre", FormNombre);
            cmdUsuario.Parameters.AddWithValue("@Apellidos", FormApellidos);
            cmdUsuario.Parameters.AddWithValue("@Correo", FormCorreo);
            cmdUsuario.Parameters.AddWithValue("@Telefono",
                string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono);
            cmdUsuario.Parameters.AddWithValue("@NumeroUsuario", FormNumeroUsuario);
            cmdUsuario.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmdUsuario.Parameters.AddWithValue("@EsFlotante", FormEsFlotante);
            cmdUsuario.Parameters.AddWithValue("@Activo", FormActivo);
            cmdUsuario.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
            cmdUsuario.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);

            var idUsuario = Convert.ToInt32(await cmdUsuario.ExecuteScalarAsync());

            // 5. Insertar asignaciones de locales en usuario_locales (si existe esa tabla)
            foreach (var local in LocalesAsignados)
            {
                try
                {
                    var queryAsignacion = @"
                        INSERT INTO usuario_locales (id_usuario, id_local, es_principal)
                        VALUES (@IdUsuario, @IdLocal, @EsPrincipal)";

                    using var cmdAsignacion = new NpgsqlCommand(queryAsignacion, connection, transaction);
                    cmdAsignacion.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmdAsignacion.Parameters.AddWithValue("@IdLocal", local.IdLocal);
                    cmdAsignacion.Parameters.AddWithValue("@EsPrincipal", local == LocalesAsignados.First());

                    await cmdAsignacion.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Tabla usuario_locales puede no existir, continuar
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

    private async Task ActualizarUsuario()
    {
        if (_usuarioEnEdicion == null) return;

        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Determinar id_local principal
            int? idLocalPrincipal = null;
            if (!FormEsFlotante && LocalesAsignados.Any())
            {
                idLocalPrincipal = LocalesAsignados.First().IdLocal;
            }

            // 2. Obtener id_comercio del primer local
            int? idComercio = null;
            if (LocalesAsignados.Any())
            {
                var queryComercio = "SELECT id_comercio FROM locales WHERE id_local = @IdLocal";
                using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
                cmdComercio.Parameters.AddWithValue("@IdLocal", LocalesAsignados.First().IdLocal);
                var result = await cmdComercio.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    idComercio = Convert.ToInt32(result);
                }
            }

            // 3. Actualizar usuario principal
            var queryUsuario = @"
                UPDATE usuarios SET
                    id_comercio = @IdComercio,
                    id_local = @IdLocal,
                    numero_usuario = @NumeroUsuario,
                    nombre = @Nombre,
                    apellidos = @Apellidos,
                    correo = @Correo,
                    telefono = @Telefono,
                    es_flotante = @EsFlotante,
                    activo = @Activo,
                    fecha_modificacion = @FechaModificacion" +
                    (string.IsNullOrWhiteSpace(FormPassword) ? "" : ", password_hash = @PasswordHash") + @"
                WHERE id_usuario = @IdUsuario";

            using var cmdUsuario = new NpgsqlCommand(queryUsuario, connection, transaction);
            cmdUsuario.Parameters.AddWithValue("@IdComercio", idComercio.HasValue ? idComercio.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@IdLocal", idLocalPrincipal.HasValue ? idLocalPrincipal.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@NumeroUsuario", FormNumeroUsuario);
            cmdUsuario.Parameters.AddWithValue("@Nombre", FormNombre);
            cmdUsuario.Parameters.AddWithValue("@Apellidos", FormApellidos);
            cmdUsuario.Parameters.AddWithValue("@Correo", FormCorreo);
            cmdUsuario.Parameters.AddWithValue("@Telefono",
                string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono);
            cmdUsuario.Parameters.AddWithValue("@EsFlotante", FormEsFlotante);
            cmdUsuario.Parameters.AddWithValue("@Activo", FormActivo);
            cmdUsuario.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
            cmdUsuario.Parameters.AddWithValue("@IdUsuario", _usuarioEnEdicion.IdUsuario);

            // Si se proporcionó nueva contraseña, hashearla
            if (!string.IsNullOrWhiteSpace(FormPassword))
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);
                cmdUsuario.Parameters.AddWithValue("@PasswordHash", passwordHash);
            }

            await cmdUsuario.ExecuteNonQueryAsync();

            // 4. Actualizar asignaciones de locales (si existe la tabla)
            try
            {
                // Eliminar asignaciones existentes
                var queryDeleteAsignaciones = "DELETE FROM usuario_locales WHERE id_usuario = @IdUsuario";
                using var cmdDelete = new NpgsqlCommand(queryDeleteAsignaciones, connection, transaction);
                cmdDelete.Parameters.AddWithValue("@IdUsuario", _usuarioEnEdicion.IdUsuario);
                await cmdDelete.ExecuteNonQueryAsync();

                // Insertar nuevas asignaciones
                foreach (var local in LocalesAsignados)
                {
                    var queryAsignacion = @"
                        INSERT INTO usuario_locales (id_usuario, id_local, es_principal)
                        VALUES (@IdUsuario, @IdLocal, @EsPrincipal)";

                    using var cmdAsignacion = new NpgsqlCommand(queryAsignacion, connection, transaction);
                    cmdAsignacion.Parameters.AddWithValue("@IdUsuario", _usuarioEnEdicion.IdUsuario);
                    cmdAsignacion.Parameters.AddWithValue("@IdLocal", local.IdLocal);
                    cmdAsignacion.Parameters.AddWithValue("@EsPrincipal", local == LocalesAsignados.First());

                    await cmdAsignacion.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Tabla usuario_locales puede no existir
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [RelayCommand]
    private async Task EliminarUsuario(UserModel usuario)
    {
        // TODO: Implementar confirmación con modal

        Cargando = true;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            // Eliminar asignaciones de locales (si existe la tabla)
            try
            {
                var queryDeleteAsignaciones = "DELETE FROM usuario_locales WHERE id_usuario = @Id";
                using var cmdAsignaciones = new NpgsqlCommand(queryDeleteAsignaciones, connection, transaction);
                cmdAsignaciones.Parameters.AddWithValue("@Id", usuario.IdUsuario);
                await cmdAsignaciones.ExecuteNonQueryAsync();
            }
            catch
            {
                // Tabla puede no existir
            }

            // Eliminar usuario
            var query = "DELETE FROM usuarios WHERE id_usuario = @Id";
            using var cmd = new NpgsqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@Id", usuario.IdUsuario);
            await cmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            // Recargar datos
            await CargarDatosDesdeBaseDatos();

            MensajeExito = "✓ Usuario eliminado correctamente";
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
    private async Task CambiarEstadoUsuario(UserModel usuario)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var nuevoEstado = !usuario.Activo;
            var query = @"UPDATE usuarios 
                         SET activo = @Activo, fecha_modificacion = @Fecha
                         WHERE id_usuario = @Id";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Activo", nuevoEstado);
            cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
            cmd.Parameters.AddWithValue("@Id", usuario.IdUsuario);

            await cmd.ExecuteNonQueryAsync();

            // Actualizar en la lista
            usuario.Activo = nuevoEstado;

            OnPropertyChanged(nameof(UsuariosActivos));
            OnPropertyChanged(nameof(UsuariosInactivos));

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
        var filtrados = Usuarios.AsEnumerable();

        // Filtro por búsqueda
        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            filtrados = filtrados.Where(u =>
                u.NombreCompleto.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                u.NumeroUsuario.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                u.Correo.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase));
        }

        // Filtro por local
        if (!string.IsNullOrEmpty(FiltroLocal) && FiltroLocal != "Todos")
        {
            filtrados = filtrados.Where(u => u.NombreLocal == FiltroLocal);
        }

        // Filtro por comercio
        if (!string.IsNullOrEmpty(FiltroComercio) && FiltroComercio != "Todos")
        {
            filtrados = filtrados.Where(u => u.NombreComercio == FiltroComercio);
        }

        // Filtro por estado
        if (!string.IsNullOrEmpty(FiltroEstado) && FiltroEstado != "Todos")
        {
            var activo = FiltroEstado == "Activo";
            filtrados = filtrados.Where(u => u.Activo == activo);
        }

        // Filtro por tipo de usuario
        if (!string.IsNullOrEmpty(FiltroTipoUsuario) && FiltroTipoUsuario != "Todos")
        {
            var esFlotante = FiltroTipoUsuario == "Flotante";
            filtrados = filtrados.Where(u => u.EsFlotante == esFlotante);
        }

        UsuariosFiltrados.Clear();
        foreach (var usuario in filtrados.OrderBy(u => u.NombreCompleto))
        {
            UsuariosFiltrados.Add(usuario);
        }
    }

    // ============================================
    // BÚSQUEDA Y ASIGNACIÓN DE LOCALES
    // ============================================

    [RelayCommand]
    private async Task BuscarLocal()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BusquedaLocal))
            {
                MostrarResultadosBusqueda = false;
                return;
            }

            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"SELECT id_local, codigo_local, nombre_local, direccion
                         FROM locales
                         WHERE LOWER(codigo_local) LIKE LOWER(@Busqueda)
                            OR LOWER(nombre_local) LIKE LOWER(@Busqueda)
                         ORDER BY nombre_local
                         LIMIT 10";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Busqueda", $"%{BusquedaLocal}%");

            ResultadosBusquedaLocales.Clear();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ResultadosBusquedaLocales.Add(new LocalFormModel
                {
                    IdLocal = reader.GetInt32(0),
                    CodigoLocal = reader.GetString(1),
                    NombreLocal = reader.GetString(2),
                    Direccion = reader.GetString(3)
                });
            }

            MostrarResultadosBusqueda = ResultadosBusquedaLocales.Count > 0;
        }
        catch (Exception ex)
        {
            MensajeExito = $"❌ Error al buscar locales: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    [RelayCommand]
    private void SeleccionarLocal(LocalFormModel local)
    {
        if (!LocalesAsignados.Any(l => l.IdLocal == local.IdLocal))
        {
            LocalesAsignados.Add(local);
            
            // Si se asignan múltiples locales, marcar como flotante
            if (LocalesAsignados.Count > 1)
            {
                FormEsFlotante = true;
            }
        }

        BusquedaLocal = string.Empty;
        MostrarResultadosBusqueda = false;
        ResultadosBusquedaLocales.Clear();
    }

    [RelayCommand]
    private void QuitarLocalAsignado(LocalFormModel local)
    {
        LocalesAsignados.Remove(local);
        
        // Si queda solo un local, desmarcar flotante
        if (LocalesAsignados.Count <= 1)
        {
            FormEsFlotante = false;
        }
    }

    // ============================================
    // MÉTODOS AUXILIARES - FORMULARIO
    // ============================================

    private void LimpiarFormulario()
    {
        FormNombre = string.Empty;
        FormApellidos = string.Empty;
        FormNumeroUsuario = string.Empty;
        FormCorreo = string.Empty;
        FormTelefono = string.Empty;
        FormPassword = string.Empty;
        FormEsFlotante = false;
        FormActivo = true;

        LocalesAsignados.Clear();
        ResultadosBusquedaLocales.Clear();
        BusquedaLocal = string.Empty;
        MostrarResultadosBusqueda = false;

        _usuarioEnEdicion = null;
    }

    private string GenerarNumeroUsuario()
    {
        // Generar número de usuario: NOMBRE_APELLIDO en mayúsculas
        var nombre = FormNombre.Trim().ToUpper().Replace(" ", "");
        var apellido = FormApellidos.Trim().ToUpper().Replace(" ", "");
        var baseNumero = $"{nombre}_{apellido}";

        // Verificar si existe en la base de datos
        var numero = baseNumero;
        var contador = 2;

        while (Usuarios.Any(u => u.NumeroUsuario == numero))
        {
            numero = $"{baseNumero}_{contador:D2}";
            contador++;
        }

        return numero;
    }

    private bool ValidarFormulario(out string mensajeError)
    {
        mensajeError = string.Empty;

        // Validar nombre
        if (string.IsNullOrWhiteSpace(FormNombre))
        {
            mensajeError = "El nombre es obligatorio";
            return false;
        }

        // Validar apellidos
        if (string.IsNullOrWhiteSpace(FormApellidos))
        {
            mensajeError = "Los apellidos son obligatorios";
            return false;
        }

        // Validar email
        if (string.IsNullOrWhiteSpace(FormCorreo))
        {
            mensajeError = "El correo electrónico es obligatorio";
            return false;
        }

        // Validar formato de email
        if (!FormCorreo.Contains("@") || !FormCorreo.Contains("."))
        {
            mensajeError = "El formato del correo no es válido";
            return false;
        }

        // Validar contraseña (solo al crear)
        if (!ModoEdicion && string.IsNullOrWhiteSpace(FormPassword))
        {
            mensajeError = "La contraseña es obligatoria para nuevos usuarios";
            return false;
        }

        if (!ModoEdicion && FormPassword.Length < 6)
        {
            mensajeError = "La contraseña debe tener al menos 6 caracteres";
            return false;
        }

        // Validar que tenga al menos un local asignado
        if (!LocalesAsignados.Any())
        {
            mensajeError = "Debes asignar al menos un local al usuario";
            return false;
        }

        // Generar número de usuario automáticamente
        if (string.IsNullOrWhiteSpace(FormNumeroUsuario))
        {
            FormNumeroUsuario = GenerarNumeroUsuario();
        }

        return true;
    }

    // ============================================
    // MÉTODOS AUXILIARES - CARGA DE DATOS
    // ============================================

    private async Task CargarLocalesAsignadosUsuario(int idUsuario)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Intentar cargar desde usuario_locales
            var query = @"SELECT l.id_local, l.codigo_local, l.nombre_local, l.direccion
                         FROM locales l
                         INNER JOIN usuario_locales ul ON l.id_local = ul.id_local
                         WHERE ul.id_usuario = @IdUsuario
                         ORDER BY ul.es_principal DESC, l.nombre_local";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

            LocalesAsignados.Clear();

            try
            {
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    LocalesAsignados.Add(new LocalFormModel
                    {
                        IdLocal = reader.GetInt32(0),
                        CodigoLocal = reader.GetString(1),
                        NombreLocal = reader.GetString(2),
                        Direccion = reader.GetString(3)
                    });
                }
            }
            catch
            {
                // Si la tabla no existe, cargar solo el local principal del usuario
                if (UsuarioSeleccionado != null && UsuarioSeleccionado.IdLocal > 0)
                {
                    var queryLocal = @"SELECT id_local, codigo_local, nombre_local, direccion
                                      FROM locales WHERE id_local = @IdLocal";
                    using var cmdLocal = new NpgsqlCommand(queryLocal, connection);
                    cmdLocal.Parameters.AddWithValue("@IdLocal", UsuarioSeleccionado.IdLocal);
                    
                    using var readerLocal = await cmdLocal.ExecuteReaderAsync();
                    if (await readerLocal.ReadAsync())
                    {
                        LocalesAsignados.Add(new LocalFormModel
                        {
                            IdLocal = readerLocal.GetInt32(0),
                            CodigoLocal = readerLocal.GetString(1),
                            NombreLocal = readerLocal.GetString(2),
                            Direccion = readerLocal.GetString(3)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar locales asignados: {ex.Message}");
        }
    }

    private void CargarLocalesDisponibles()
    {
        LocalesDisponibles.Clear();
        LocalesDisponibles.Add("Todos");

        var locales = Usuarios.Select(u => u.NombreLocal).Distinct().OrderBy(l => l);
        foreach (var local in locales)
        {
            if (!string.IsNullOrEmpty(local) && local != "Sin asignar")
            {
                LocalesDisponibles.Add(local);
            }
        }
    }

    private void CargarComerciosDisponibles()
    {
        ComerciosDisponibles.Clear();
        ComerciosDisponibles.Add("Todos");

        var comercios = Usuarios.Select(u => u.NombreComercio).Distinct().OrderBy(c => c);
        foreach (var comercio in comercios)
        {
            if (!string.IsNullOrEmpty(comercio) && comercio != "Sin asignar")
            {
                ComerciosDisponibles.Add(comercio);
            }
        }
    }

    private async Task InicializarFiltros()
    {
        // Esperar un momento para que termine de cargar todo
        await Task.Delay(100);

        CargarLocalesDisponibles();
        CargarComerciosDisponibles();

        // Inicializar con todos los usuarios
        UsuariosFiltrados.Clear();
        foreach (var usuario in Usuarios.OrderBy(u => u.NombreCompleto))
        {
            UsuariosFiltrados.Add(usuario);
        }
    }

    // Observar cambios en FormEsFlotante para actualizar automáticamente
    partial void OnFormEsFlotanteChanged(bool value)
    {
        // Si desmarca flotante y tiene múltiples locales, avisar
        if (!value && LocalesAsignados.Count > 1)
        {
            // Mantener solo el primer local
            var primerLocal = LocalesAsignados.First();
            LocalesAsignados.Clear();
            LocalesAsignados.Add(primerLocal);
        }
    }
}
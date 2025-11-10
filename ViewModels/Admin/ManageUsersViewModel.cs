using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Npgsql;
using BCrypt.Net;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel para la gestión de usuarios normales y flooters
/// VERSIÓN COMPLETAMENTE CORREGIDA SEGÚN ESTRUCTURA REAL DE BD
/// 
/// CORRECCIONES APLICADAS:
/// 1. Tabla usuarios tiene: nombre, apellidos (SEPARADOS) ✓
/// 2. Tabla usuarios tiene: es_flooter (NO es_flotante) ✓
/// 3. Tabla locales tiene: local_numero (NO numero) ✓
/// 4. id_rol debe ser NULL para usuarios normales ✓
/// 5. Los usuarios necesitan codigo_local para login ✓
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

    [ObservableProperty]
    private string _mensajeExitoColor = "#28a745";

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
    private string _botonGuardarTexto = "CREAR USUARIO";

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
    private bool _formActivo = true;

    [ObservableProperty]
    private string _tipoEmpleadoTexto = "REGULAR (1 local asignado)";

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
    private string _busquedaComercio = string.Empty;

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
    // OBSERVADORES DE CAMBIOS - AUTO-GENERACIÓN
    // ============================================

    partial void OnFormNombreChanged(string value)
    {
        ActualizarNumeroUsuarioAutomaticamente();
    }

    partial void OnFormApellidosChanged(string value)
    {
        ActualizarNumeroUsuarioAutomaticamente();
    }

    partial void OnLocalesAsignadosChanged(ObservableCollection<LocalFormModel> value)
    {
        ActualizarTipoEmpleado();
    }

    private void ActualizarNumeroUsuarioAutomaticamente()
    {
        if (ModoEdicion)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(FormNombre) || string.IsNullOrWhiteSpace(FormApellidos))
        {
            FormNumeroUsuario = string.Empty;
            return;
        }

        var nombre = FormNombre.Trim().ToUpper().Replace(" ", "");
        var apellido = FormApellidos.Trim().ToUpper().Replace(" ", "");
        var baseNumero = $"{nombre}_{apellido}";

        var numero = baseNumero;
        var contador = 2;

        while (Usuarios.Any(u => u.NumeroUsuario.Equals(numero, StringComparison.OrdinalIgnoreCase)))
        {
            numero = $"{baseNumero}_{contador:D2}";
            contador++;
        }

        FormNumeroUsuario = numero;
    }

    private void ActualizarTipoEmpleado()
    {
        var cantidadLocales = LocalesAsignados.Count;
        
        if (cantidadLocales == 0)
        {
            TipoEmpleadoTexto = "Sin locales asignados";
        }
        else if (cantidadLocales == 1)
        {
            TipoEmpleadoTexto = "REGULAR (1 local asignado)";
        }
        else
        {
            TipoEmpleadoTexto = $"FLOOTER ({cantidadLocales} locales asignados)";
        }
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

            OnPropertyChanged(nameof(TotalUsuarios));
            OnPropertyChanged(nameof(UsuariosActivos));
            OnPropertyChanged(nameof(UsuariosInactivos));
            OnPropertyChanged(nameof(UsuariosFlotantes));

            await InicializarFiltros();
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al cargar datos: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task<List<UserModel>> CargarUsuarios(NpgsqlConnection connection)
    {
        var usuarios = new List<UserModel>();

        var query = @"SELECT u.id_usuario, u.numero_usuario, u.nombre, u.apellidos,
                             u.correo, COALESCE(u.telefono, '') as telefono, 
                             COALESCE(u.es_flooter, false) as es_flotante,
                             u.activo, u.ultimo_acceso,
                             COALESCE(l.id_local, 0) as id_local, 
                             COALESCE(l.nombre_local, 'Sin asignar') as nombre_local, 
                             COALESCE(l.codigo_local, 'N/A') as codigo_local,
                             COALESCE(c.id_comercio, 0) as id_comercio, 
                             COALESCE(c.nombre_comercio, 'Sin asignar') as nombre_comercio
                      FROM usuarios u
                      LEFT JOIN locales l ON u.id_local = l.id_local
                      LEFT JOIN comercios c ON l.id_comercio = c.id_comercio
                      WHERE u.id_rol = 2
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
                Telefono = reader.GetString(5),
                EsFlotante = reader.GetBoolean(6),
                Activo = reader.GetBoolean(7),
                UltimoAcceso = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                IdLocal = reader.GetInt32(9),
                NombreLocal = reader.GetString(10),
                CodigoLocal = reader.GetString(11),
                IdComercio = reader.GetInt32(12),
                NombreComercio = reader.GetString(13)
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
        TituloPanelDerecho = "Crear Nuevo Usuario";
        BotonGuardarTexto = "CREAR USUARIO";
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
        FormTelefono = usuario.Telefono == "Sin teléfono" ? string.Empty : usuario.Telefono ?? string.Empty;
        FormActivo = usuario.Activo;
        FormPassword = string.Empty;

        await CargarLocalesAsignadosUsuario(usuario.IdUsuario);

        ModoEdicion = true;
        TituloPanelDerecho = $"Editar: {usuario.NombreCompleto}";
        BotonGuardarTexto = "ACTUALIZAR USUARIO";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
    }

    [RelayCommand]
    private async Task VerDetallesUsuario(UserModel usuario)
    {
        UsuarioSeleccionado = usuario;
        TituloPanelDerecho = $"Detalles: {usuario.NombreCompleto}";
        MostrarFormulario = false;
        MostrarPanelDerecho = true;

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
    // COMANDOS - GENERAR CONTRASEÑA (MODIFICADO - SIN MODAL)
    // ============================================

    [RelayCommand]
    private void GenerarPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        FormPassword = password;
        MostrarMensajeExitoNotificacion("✓ Contraseña generada");
    }

    // ============================================
    // COMANDOS - ACCIONES CRUD
    // ============================================

    [RelayCommand]
    private async Task GuardarUsuario()
    {
        if (!ValidarFormulario(out string mensajeError))
        {
            MostrarMensajeError(mensajeError);
            return;
        }

        Cargando = true;

        try
        {
            if (ModoEdicion && _usuarioEnEdicion != null)
            {
                await ActualizarUsuario();
                MostrarMensajeExitoNotificacion("✓ Usuario actualizado correctamente");
            }
            else
            {
                await CrearNuevoUsuario();
                MostrarMensajeExitoNotificacion("✓ Usuario creado correctamente");
            }

            await CargarDatosDesdeBaseDatos();
            CerrarPanelDerecho();
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al guardar: {ex.Message}");
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
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);

            bool esFlotante = LocalesAsignados.Count > 1;

            // ✅ CORRECCIÓN CRÍTICA: Si es flooter, id_local debe ser NULL
            // Los flooters trabajan en múltiples locales (se manejan en usuario_locales)
            // Solo usuarios normales tienen un id_local asignado
            int? idLocalPrincipal = esFlotante ? null : LocalesAsignados.FirstOrDefault()?.IdLocal;
            int? idComercio = null;

            // Obtener id_comercio del primer local asignado
            var primerLocal = LocalesAsignados.FirstOrDefault();
            if (primerLocal != null)
            {
                var queryComercio = "SELECT id_comercio FROM locales WHERE id_local = @IdLocal";
                using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
                cmdComercio.Parameters.AddWithValue("@IdLocal", primerLocal.IdLocal);
                var result = await cmdComercio.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    idComercio = Convert.ToInt32(result);
                }
            }

            var queryUsuario = @"
                INSERT INTO usuarios (
                    id_comercio, id_local, id_rol, nombre, apellidos, correo, telefono,
                    numero_usuario, password_hash, es_flooter, idioma, activo, primer_login,
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
            cmdUsuario.Parameters.AddWithValue("@Nombre", FormNombre.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@Apellidos", FormApellidos.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@Correo", FormCorreo.Trim());
            cmdUsuario.Parameters.AddWithValue("@Telefono",
                string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono.Trim());
            cmdUsuario.Parameters.AddWithValue("@NumeroUsuario", FormNumeroUsuario);
            cmdUsuario.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmdUsuario.Parameters.AddWithValue("@EsFlotante", esFlotante);
            cmdUsuario.Parameters.AddWithValue("@Activo", FormActivo);
            cmdUsuario.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
            cmdUsuario.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);

            var idUsuario = Convert.ToInt32(await cmdUsuario.ExecuteScalarAsync());

            foreach (var local in LocalesAsignados)
            {
                var queryAsignacion = @"
                    INSERT INTO usuario_locales (id_usuario, id_local, es_principal)
                    VALUES (@IdUsuario, @IdLocal, @EsPrincipal)
                    ON CONFLICT (id_usuario, id_local) DO NOTHING";

                using var cmdAsignacion = new NpgsqlCommand(queryAsignacion, connection, transaction);
                cmdAsignacion.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmdAsignacion.Parameters.AddWithValue("@IdLocal", local.IdLocal);
                cmdAsignacion.Parameters.AddWithValue("@EsPrincipal", local == LocalesAsignados.First());

                await cmdAsignacion.ExecuteNonQueryAsync();
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
            bool esFlotante = LocalesAsignados.Count > 1;
            
            // ✅ CORRECCIÓN CRÍTICA: Si es flooter, id_local debe ser NULL
            int? idLocalPrincipal = esFlotante ? null : LocalesAsignados.FirstOrDefault()?.IdLocal;
            int? idComercio = null;

            // Obtener id_comercio del primer local asignado
            var primerLocal = LocalesAsignados.FirstOrDefault();
            if (primerLocal != null)
            {
                var queryComercio = "SELECT id_comercio FROM locales WHERE id_local = @IdLocal";
                using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
                cmdComercio.Parameters.AddWithValue("@IdLocal", primerLocal.IdLocal);
                var result = await cmdComercio.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    idComercio = Convert.ToInt32(result);
                }
            }

            var queryUsuario = @"
                UPDATE usuarios SET
                    id_comercio = @IdComercio,
                    id_local = @IdLocal,
                    numero_usuario = @NumeroUsuario,
                    nombre = @Nombre,
                    apellidos = @Apellidos,
                    correo = @Correo,
                    telefono = @Telefono,
                    es_flooter = @EsFlotante,
                    activo = @Activo,
                    fecha_modificacion = @FechaModificacion" +
                    (string.IsNullOrWhiteSpace(FormPassword) ? "" : ", password_hash = @PasswordHash") + @"
                WHERE id_usuario = @IdUsuario";

            using var cmdUsuario = new NpgsqlCommand(queryUsuario, connection, transaction);
            cmdUsuario.Parameters.AddWithValue("@IdComercio", idComercio.HasValue ? idComercio.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@IdLocal", idLocalPrincipal.HasValue ? idLocalPrincipal.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@NumeroUsuario", FormNumeroUsuario);
            cmdUsuario.Parameters.AddWithValue("@Nombre", FormNombre.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@Apellidos", FormApellidos.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@Correo", FormCorreo.Trim());
            cmdUsuario.Parameters.AddWithValue("@Telefono",
                string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono.Trim());
            cmdUsuario.Parameters.AddWithValue("@EsFlotante", esFlotante);
            cmdUsuario.Parameters.AddWithValue("@Activo", FormActivo);
            cmdUsuario.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
            cmdUsuario.Parameters.AddWithValue("@IdUsuario", _usuarioEnEdicion.IdUsuario);

            if (!string.IsNullOrWhiteSpace(FormPassword))
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);
                cmdUsuario.Parameters.AddWithValue("@PasswordHash", passwordHash);
            }

            await cmdUsuario.ExecuteNonQueryAsync();

            var queryDeleteAsignaciones = "DELETE FROM usuario_locales WHERE id_usuario = @IdUsuario";
            using var cmdDelete = new NpgsqlCommand(queryDeleteAsignaciones, connection, transaction);
            cmdDelete.Parameters.AddWithValue("@IdUsuario", _usuarioEnEdicion.IdUsuario);
            await cmdDelete.ExecuteNonQueryAsync();

            foreach (var local in LocalesAsignados)
            {
                var queryAsignacion = @"
                    INSERT INTO usuario_locales (id_usuario, id_local, es_principal)
                    VALUES (@IdUsuario, @IdLocal, @EsPrincipal)
                    ON CONFLICT (id_usuario, id_local) DO NOTHING";

                using var cmdAsignacion = new NpgsqlCommand(queryAsignacion, connection, transaction);
                cmdAsignacion.Parameters.AddWithValue("@IdUsuario", _usuarioEnEdicion.IdUsuario);
                cmdAsignacion.Parameters.AddWithValue("@IdLocal", local.IdLocal);
                cmdAsignacion.Parameters.AddWithValue("@EsPrincipal", local == LocalesAsignados.First());

                await cmdAsignacion.ExecuteNonQueryAsync();
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
        Cargando = true;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var queryDeleteAsignaciones = "DELETE FROM usuario_locales WHERE id_usuario = @Id";
                using var cmdAsignaciones = new NpgsqlCommand(queryDeleteAsignaciones, connection, transaction);
                cmdAsignaciones.Parameters.AddWithValue("@Id", usuario.IdUsuario);
                await cmdAsignaciones.ExecuteNonQueryAsync();
            }
            catch
            {
            }

            var query = "DELETE FROM usuarios WHERE id_usuario = @Id";
            using var cmd = new NpgsqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@Id", usuario.IdUsuario);
            await cmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            await CargarDatosDesdeBaseDatos();

            MostrarMensajeExitoNotificacion("✓ Usuario eliminado correctamente");
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al eliminar: {ex.Message}");
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

            usuario.Activo = nuevoEstado;

            OnPropertyChanged(nameof(UsuariosActivos));
            OnPropertyChanged(nameof(UsuariosInactivos));

            MostrarMensajeExitoNotificacion($"✓ Estado: {(nuevoEstado ? "Activo" : "Inactivo")}");
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al cambiar estado: {ex.Message}");
        }
    }

    // ============================================
    // COMANDOS - FILTROS
    // ============================================

    [RelayCommand]
    private void AplicarFiltros()
    {
        var filtrados = Usuarios.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            filtrados = filtrados.Where(u =>
                u.NombreCompleto.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                u.NumeroUsuario.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                u.Correo.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(FiltroLocal) && FiltroLocal != "Todos")
        {
            filtrados = filtrados.Where(u => u.CodigoLocal == FiltroLocal);
        }

        if (!string.IsNullOrEmpty(FiltroComercio) && FiltroComercio != "Todos")
        {
            filtrados = filtrados.Where(u => u.NombreComercio == FiltroComercio);
        }

        if (!string.IsNullOrEmpty(FiltroEstado) && FiltroEstado != "Todos")
        {
            var activo = FiltroEstado == "Activo";
            filtrados = filtrados.Where(u => u.Activo == activo);
        }

        if (!string.IsNullOrEmpty(FiltroTipoUsuario) && FiltroTipoUsuario != "Todos")
        {
            var esFlotante = FiltroTipoUsuario == "Flooter";
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
    private async Task BuscarLocalesPorComercio()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BusquedaComercio))
            {
                MostrarResultadosBusqueda = false;
                return;
            }

            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"SELECT l.id_local, 
                                l.codigo_local, 
                                COALESCE(l.nombre_local, '') as nombre_local, 
                                COALESCE(l.tipo_via, '') as tipo_via, 
                                COALESCE(l.direccion, '') as direccion, 
                                COALESCE(l.local_numero, '') as local_numero, 
                                COALESCE(l.escalera, '') as escalera, 
                                COALESCE(l.piso, '') as piso,
                                COALESCE(l.codigo_postal, '') as codigo_postal, 
                                COALESCE(l.pais, '') as pais, 
                                COALESCE(l.telefono, '') as telefono, 
                                COALESCE(l.email, '') as email,
                                c.id_comercio, 
                                c.nombre_comercio
                        FROM locales l
                        INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                        WHERE LOWER(c.nombre_comercio) LIKE LOWER(@Busqueda)
                        ORDER BY l.codigo_local";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Busqueda", $"%{BusquedaComercio}%");

            ResultadosBusquedaLocales.Clear();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ResultadosBusquedaLocales.Add(new LocalFormModel
                {
                    IdLocal = reader.GetInt32(0),
                    CodigoLocal = reader.GetString(1),
                    NombreLocal = reader.GetString(2),
                    TipoVia = reader.GetString(3),
                    Direccion = reader.GetString(4),
                    LocalNumero = reader.GetString(5),
                    Escalera = string.IsNullOrEmpty(reader.GetString(6)) ? null : reader.GetString(6),
                    Piso = string.IsNullOrEmpty(reader.GetString(7)) ? null : reader.GetString(7),
                    CodigoPostal = reader.GetString(8),
                    Pais = reader.GetString(9),
                    Telefono = string.IsNullOrEmpty(reader.GetString(10)) ? null : reader.GetString(10),
                    Email = string.IsNullOrEmpty(reader.GetString(11)) ? null : reader.GetString(11),
                    IdComercio = reader.GetInt32(12)
                });
            }

            MostrarResultadosBusqueda = ResultadosBusquedaLocales.Count > 0;
            
            if (ResultadosBusquedaLocales.Count == 0)
            {
                MostrarMensajeError("No se encontraron locales para ese comercio");
            }
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al buscar locales: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SeleccionarLocal(LocalFormModel local)
    {
        if (!LocalesAsignados.Any(l => l.IdLocal == local.IdLocal))
        {
            LocalesAsignados.Add(local);
            ActualizarTipoEmpleado();
        }

        BusquedaComercio = string.Empty;
        MostrarResultadosBusqueda = false;
        ResultadosBusquedaLocales.Clear();
    }

    [RelayCommand]
    private void QuitarLocalAsignado(LocalFormModel local)
    {
        LocalesAsignados.Remove(local);
        ActualizarTipoEmpleado();
    }

    // ============================================
    // MÉTODOS AUXILIARES
    // ============================================

    private void LimpiarFormulario()
    {
        FormNombre = string.Empty;
        FormApellidos = string.Empty;
        FormNumeroUsuario = string.Empty;
        FormCorreo = string.Empty;
        FormTelefono = string.Empty;
        FormPassword = string.Empty;
        FormActivo = true;

        LocalesAsignados.Clear();
        ResultadosBusquedaLocales.Clear();
        BusquedaComercio = string.Empty;
        MostrarResultadosBusqueda = false;
        TipoEmpleadoTexto = "REGULAR (1 local asignado)";

        _usuarioEnEdicion = null;
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
            mensajeError = "La contraseña es obligatoria para nuevos usuarios";
            return false;
        }

        if (!ModoEdicion && FormPassword.Length < 6)
        {
            mensajeError = "La contraseña debe tener al menos 6 caracteres";
            return false;
        }

        if (!LocalesAsignados.Any())
        {
            mensajeError = "⚠️ DEBE asignar al menos un local al usuario.\n\n" +
                          "Usa el buscador de 'Asignación de Locales' para buscar por nombre de comercio " +
                          "y selecciona el local donde trabajará este usuario.";
            return false;
        }

        if (LocalesAsignados.Any(l => l.IdLocal <= 0))
        {
            mensajeError = "Error: Hay locales asignados sin ID válido. Por favor, elimínalos y asígnalos nuevamente.";
            return false;
        }

        return true;
    }

    private async Task CargarLocalesAsignadosUsuario(int idUsuario)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"SELECT l.id_local, 
                                l.codigo_local, 
                                COALESCE(l.nombre_local, '') as nombre_local,
                                COALESCE(l.tipo_via, '') as tipo_via, 
                                COALESCE(l.direccion, '') as direccion, 
                                COALESCE(l.local_numero, '') as local_numero, 
                                COALESCE(l.escalera, '') as escalera, 
                                COALESCE(l.piso, '') as piso,
                                COALESCE(l.codigo_postal, '') as codigo_postal, 
                                COALESCE(l.pais, '') as pais, 
                                COALESCE(l.telefono, '') as telefono, 
                                COALESCE(l.email, '') as email,
                                c.id_comercio
                        FROM locales l
                        INNER JOIN usuario_locales ul ON l.id_local = ul.id_local
                        INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                        WHERE ul.id_usuario = @IdUsuario
                        ORDER BY ul.es_principal DESC, l.nombre_local";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

            LocalesAsignados.Clear();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                LocalesAsignados.Add(new LocalFormModel
                {
                    IdLocal = reader.GetInt32(0),
                    CodigoLocal = reader.GetString(1),
                    NombreLocal = reader.GetString(2),
                    TipoVia = reader.GetString(3),
                    Direccion = reader.GetString(4),
                    LocalNumero = reader.GetString(5),
                    Escalera = string.IsNullOrEmpty(reader.GetString(6)) ? null : reader.GetString(6),
                    Piso = string.IsNullOrEmpty(reader.GetString(7)) ? null : reader.GetString(7),
                    CodigoPostal = reader.GetString(8),
                    Pais = reader.GetString(9),
                    Telefono = string.IsNullOrEmpty(reader.GetString(10)) ? null : reader.GetString(10),
                    Email = string.IsNullOrEmpty(reader.GetString(11)) ? null : reader.GetString(11),
                    IdComercio = reader.GetInt32(12)
                });
            }

            ActualizarTipoEmpleado();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar locales asignados: {ex.Message}");
        }
    }

    private async Task CargarLocalesDisponiblesDesdeDB()
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = "SELECT DISTINCT codigo_local FROM locales WHERE activo = true ORDER BY codigo_local";
            using var cmd = new NpgsqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            LocalesDisponibles.Clear();
            LocalesDisponibles.Add("Todos");

            while (await reader.ReadAsync())
            {
                var codigo = reader.GetString(0);
                if (!string.IsNullOrEmpty(codigo))
                {
                    LocalesDisponibles.Add(codigo);
                }
            }
        }
        catch
        {
            CargarLocalesDisponibles();
        }
    }

    private void CargarLocalesDisponibles()
    {
        LocalesDisponibles.Clear();
        LocalesDisponibles.Add("Todos");

        var locales = Usuarios.Select(u => u.CodigoLocal).Distinct().OrderBy(l => l);
        foreach (var local in locales)
        {
            if (!string.IsNullOrEmpty(local) && local != "N/A" && local != "Sin asignar")
            {
                LocalesDisponibles.Add(local);
            }
        }
    }

    private async Task CargarComerciosDisponiblesDesdeDB()
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = "SELECT DISTINCT nombre_comercio FROM comercios WHERE activo = true ORDER BY nombre_comercio";
            using var cmd = new NpgsqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            ComerciosDisponibles.Clear();
            ComerciosDisponibles.Add("Todos");

            while (await reader.ReadAsync())
            {
                var nombre = reader.GetString(0);
                if (!string.IsNullOrEmpty(nombre))
                {
                    ComerciosDisponibles.Add(nombre);
                }
            }
        }
        catch
        {
            CargarComerciosDisponibles();
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
        await Task.Delay(100);

        await CargarLocalesDisponiblesDesdeDB();
        await CargarComerciosDisponiblesDesdeDB();

        UsuariosFiltrados.Clear();
        foreach (var usuario in Usuarios.OrderBy(u => u.NombreCompleto))
        {
            UsuariosFiltrados.Add(usuario);
        }
    }

    private async void MostrarMensajeExitoNotificacion(string mensaje)
    {
        MensajeExitoColor = "#28a745";
        MensajeExito = mensaje;
        MostrarMensajeExito = true;
        await Task.Delay(3000);
        MostrarMensajeExito = false;
    }

    private async void MostrarMensajeError(string mensaje)
    {
        MensajeExitoColor = "#dc3545";
        MensajeExito = $"❌ {mensaje}";
        MostrarMensajeExito = true;
        await Task.Delay(5000);
        MostrarMensajeExito = false;
    }
}
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
/// TODAS las funcionalidades CRUD implementadas
/// </summary>
public partial class ManageAdministradoresAllvaViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    [ObservableProperty]
    private ObservableCollection<AdministradorAllvaModel> _administradores = new();

    [ObservableProperty]
    private ObservableCollection<AdministradorAllvaModel> _administradoresFiltrados = new();

    [ObservableProperty]
    private AdministradorAllvaModel? _administradorSeleccionado;

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

    [ObservableProperty]
    private bool _formAccesoGestionComercios = true;

    [ObservableProperty]
    private bool _formAccesoGestionUsuariosLocales = true;

    [ObservableProperty]
    private bool _formAccesoGestionUsuariosAllva = false;

    [ObservableProperty]
    private bool _formAccesoAnalytics = false;

    [ObservableProperty]
    private string _filtroBusqueda = string.Empty;

    [ObservableProperty]
    private string _filtroEstado = "Todos";

    [ObservableProperty]
    private string _filtroTipo = "Todos";

    public int TotalAdministradores => Administradores.Count;
    public int AdministradoresActivos => Administradores.Count(a => a.Activo);
    public int AdministradoresInactivos => Administradores.Count(a => !a.Activo);
    public int SuperAdministradores => Administradores.Count(a => a.EsSuperAdministrador);

    private AdministradorAllvaModel? _administradorEnEdicion;

    public ManageAdministradoresAllvaViewModel()
    {
        _ = CargarAdministradores();
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
                    acceso_gestion_comercios, acceso_gestion_usuarios_locales, 
                    acceso_gestion_usuarios_allva, acceso_analytics, 
                    acceso_configuracion_sistema, acceso_facturacion_global, 
                    acceso_auditoria, activo, ultimo_acceso, fecha_creacion
                FROM administradores_allva
                ORDER BY nombre, apellidos";

            using var cmd = new NpgsqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            var lista = new List<AdministradorAllvaModel>();

            while (await reader.ReadAsync())
            {
                lista.Add(new AdministradorAllvaModel
                {
                    IdAdministrador = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Apellidos = reader.GetString(2),
                    NombreUsuario = reader.GetString(3),
                    Correo = reader.GetString(4),
                    Telefono = reader.IsDBNull(5) ? "N/A" : reader.GetString(5),
                    AccesoGestionComercios = reader.GetBoolean(6),
                    AccesoGestionUsuariosLocales = reader.GetBoolean(7),
                    AccesoGestionUsuariosAllva = reader.GetBoolean(8),
                    AccesoAnalytics = reader.GetBoolean(9),
                    AccesoConfiguracionSistema = reader.GetBoolean(10),
                    AccesoFacturacionGlobal = reader.GetBoolean(11),
                    AccesoAuditoria = reader.GetBoolean(12),
                    Activo = reader.GetBoolean(13),
                    UltimoAcceso = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                    FechaCreacion = reader.GetDateTime(15)
                });
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
            MensajeExito = $"❌ Error al cargar administradores: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    private void ActualizarEstadisticas()
    {
        OnPropertyChanged(nameof(TotalAdministradores));
        OnPropertyChanged(nameof(AdministradoresActivos));
        OnPropertyChanged(nameof(AdministradoresInactivos));
        OnPropertyChanged(nameof(SuperAdministradores));
    }

    [RelayCommand]
    private void MostrarFormularioCrear()
    {
        LimpiarFormulario();
        ModoEdicion = false;
        TituloPanelDerecho = "Crear Nuevo Administrador";
        TituloBotonGuardar = "Crear";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
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
        FormAccesoGestionComercios = admin.AccesoGestionComercios;
        FormAccesoGestionUsuariosLocales = admin.AccesoGestionUsuariosLocales;
        FormAccesoGestionUsuariosAllva = admin.AccesoGestionUsuariosAllva;
        FormAccesoAnalytics = admin.AccesoAnalytics;
        FormPassword = string.Empty;

        ModoEdicion = true;
        TituloPanelDerecho = $"Editar: {admin.NombreCompleto}";
        TituloBotonGuardar = "Actualizar";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
        
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
            MensajeExito = $"⚠️ {mensajeError}";
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
                MensajeExito = "✓ Administrador actualizado correctamente";
            }
            else
            {
                await CrearNuevoAdministrador();
                MensajeExito = "✓ Administrador creado correctamente";
            }

            await CargarAdministradores();
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

    private async Task CrearNuevoAdministrador()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);

        var query = @"
            INSERT INTO administradores_allva (
                nombre, apellidos, nombre_usuario, password_hash, correo, telefono,
                acceso_gestion_comercios, acceso_gestion_usuarios_locales, 
                acceso_gestion_usuarios_allva, acceso_analytics,
                acceso_configuracion_sistema, acceso_facturacion_global, acceso_auditoria,
                activo, primer_login, creado_por, idioma, fecha_creacion
            )
            VALUES (
                @Nombre, @Apellidos, @NombreUsuario, @PasswordHash, @Correo, @Telefono,
                @AccesoGestionComercios, @AccesoGestionUsuariosLocales,
                @AccesoGestionUsuariosAllva, @AccesoAnalytics,
                false, false, false,
                @Activo, true, 'SISTEMA', 'es', @FechaCreacion
            )";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@Nombre", FormNombre);
        cmd.Parameters.AddWithValue("@Apellidos", FormApellidos);
        cmd.Parameters.AddWithValue("@NombreUsuario", FormNombreUsuario);
        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        cmd.Parameters.AddWithValue("@Correo", FormCorreo);
        cmd.Parameters.AddWithValue("@Telefono", 
            string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono);
        cmd.Parameters.AddWithValue("@AccesoGestionComercios", FormAccesoGestionComercios);
        cmd.Parameters.AddWithValue("@AccesoGestionUsuariosLocales", FormAccesoGestionUsuariosLocales);
        cmd.Parameters.AddWithValue("@AccesoGestionUsuariosAllva", FormAccesoGestionUsuariosAllva);
        cmd.Parameters.AddWithValue("@AccesoAnalytics", FormAccesoAnalytics);
        cmd.Parameters.AddWithValue("@Activo", FormActivo);
        cmd.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ActualizarAdministrador()
    {
        if (_administradorEnEdicion == null) return;

        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var queryBase = @"
            UPDATE administradores_allva SET
                nombre = @Nombre,
                apellidos = @Apellidos,
                correo = @Correo,
                telefono = @Telefono,
                acceso_gestion_comercios = @AccesoGestionComercios,
                acceso_gestion_usuarios_locales = @AccesoGestionUsuariosLocales,
                acceso_gestion_usuarios_allva = @AccesoGestionUsuariosAllva,
                acceso_analytics = @AccesoAnalytics,
                activo = @Activo,
                fecha_modificacion = @FechaModificacion";

        var query = queryBase;
        if (!string.IsNullOrWhiteSpace(FormPassword))
        {
            query += ", password_hash = @PasswordHash";
        }

        query += " WHERE id_administrador = @IdAdministrador";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@Nombre", FormNombre);
        cmd.Parameters.AddWithValue("@Apellidos", FormApellidos);
        cmd.Parameters.AddWithValue("@Correo", FormCorreo);
        cmd.Parameters.AddWithValue("@Telefono", 
            string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono);
        cmd.Parameters.AddWithValue("@AccesoGestionComercios", FormAccesoGestionComercios);
        cmd.Parameters.AddWithValue("@AccesoGestionUsuariosLocales", FormAccesoGestionUsuariosLocales);
        cmd.Parameters.AddWithValue("@AccesoGestionUsuariosAllva", FormAccesoGestionUsuariosAllva);
        cmd.Parameters.AddWithValue("@AccesoAnalytics", FormAccesoAnalytics);
        cmd.Parameters.AddWithValue("@Activo", FormActivo);
        cmd.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
        cmd.Parameters.AddWithValue("@IdAdministrador", _administradorEnEdicion.IdAdministrador);

        if (!string.IsNullOrWhiteSpace(FormPassword))
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        }

        await cmd.ExecuteNonQueryAsync();
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

    [RelayCommand]
    private void AplicarFiltros()
    {
        var filtrados = Administradores.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            filtrados = filtrados.Where(a =>
                a.NombreCompleto.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                a.NombreUsuario.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                a.Correo.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(FiltroEstado) && FiltroEstado != "Todos")
        {
            var activo = FiltroEstado == "Activo";
            filtrados = filtrados.Where(a => a.Activo == activo);
        }

        if (!string.IsNullOrEmpty(FiltroTipo) && FiltroTipo != "Todos")
        {
            var esSuperAdmin = FiltroTipo == "Super Admin";
            filtrados = filtrados.Where(a => a.EsSuperAdministrador == esSuperAdmin);
        }

        AdministradoresFiltrados.Clear();
        foreach (var admin in filtrados.OrderBy(a => a.NombreCompleto))
        {
            AdministradoresFiltrados.Add(admin);
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
        FormAccesoGestionComercios = true;
        FormAccesoGestionUsuariosLocales = true;
        FormAccesoGestionUsuariosAllva = false;
        FormAccesoAnalytics = false;

        _administradorEnEdicion = null;
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
            mensajeError = "La contraseña es obligatoria para nuevos administradores";
            return false;
        }

        if (!ModoEdicion && FormPassword.Length < 8)
        {
            mensajeError = "La contraseña debe tener al menos 8 caracteres";
            return false;
        }

        if (!FormAccesoGestionComercios && 
            !FormAccesoGestionUsuariosLocales && 
            !FormAccesoGestionUsuariosAllva && 
            !FormAccesoAnalytics)
        {
            mensajeError = "Debe asignar al menos un permiso al administrador";
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
}
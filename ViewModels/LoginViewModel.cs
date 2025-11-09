using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Services;
using Npgsql;
using BCrypt.Net;

namespace Allva.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de inicio de sesión
    /// VERSIÓN ACTUALIZADA: Autenticación contra dos tablas separadas:
    /// - administradores_allva: Administradores del sistema (Back Office)
    /// - usuarios: Usuarios normales de locales (Front Office)
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        // ============================================
        // CONFIGURACIÓN DE BASE DE DATOS
        // ============================================
        
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        // ============================================
        // LOCALIZACIÓN
        // ============================================

        public LocalizationService Localization => LocalizationService.Instance;

        // ============================================
        // PROPIEDADES OBSERVABLES
        // ============================================

        [ObservableProperty]
        private string _numeroUsuario = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _codigoLocal = string.Empty;

        [ObservableProperty]
        private bool _recordarSesion = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _mostrarError = false;

        [ObservableProperty]
        private string _mensajeError = string.Empty;

        // ============================================
        // CONTROL DE SEGURIDAD
        // ============================================

        [ObservableProperty]
        private int _intentosFallidos = 0;

        [ObservableProperty]
        private bool _usuarioBloqueado = false;

        [ObservableProperty]
        private DateTime? _tiempoBloqueo;

        private const int MaxIntentosFallidos = 5;
        private const int MinutosBloqueoPorIntentosFallidos = 15;

        // ============================================
        // COMANDOS
        // ============================================

        [RelayCommand]
        private void CambiarIdioma()
        {
            // Alternar entre español e inglés
            var currentLang = LocalizationService.Instance.CurrentLanguageCode;
            var newLang = currentLang == "ES" ? "en" : "es";
            LocalizationService.Instance.SetLanguage(newLang);
        }

        [RelayCommand]
        private void RecuperarPassword()
        {
            // TODO: Implementar recuperación de contraseña
            MostrarMensajeError("Funcionalidad de recuperación de contraseña próximamente.");
        }

        [RelayCommand(CanExecute = nameof(PuedeIniciarSesion))]
        private async Task Login()
        {
            try
            {
                IsLoading = true;
                MostrarError = false;
                MensajeError = string.Empty;

                // Validar campos básicos
                if (!ValidarCampos())
                {
                    return;
                }

                // ============================================
                // PASO 1: INTENTAR AUTENTICACIÓN COMO ADMINISTRADOR ALLVA
                // Los admins NO necesitan código de local
                // ============================================
                
                var adminLoginResult = await IntentarLoginAdministradorAllva();
                
                if (adminLoginResult.Exitoso)
                {
                    // Login exitoso como Administrador Allva
                    await ProcesarLoginExitoso(adminLoginResult);
                    return;
                }

                // ============================================
                // PASO 2: INTENTAR AUTENTICACIÓN COMO USUARIO NORMAL
                // Los usuarios normales SÍ necesitan código de local
                // ============================================
                
                // Validar que tenga código de local para usuarios normales
                if (string.IsNullOrWhiteSpace(CodigoLocal))
                {
                    MostrarMensajeError("El código de local es requerido para usuarios de locales.");
                    return;
                }
                
                var usuarioLoginResult = await IntentarLoginUsuarioNormal();
                
                if (usuarioLoginResult.Exitoso)
                {
                    // Login exitoso como Usuario Normal
                    await ProcesarLoginExitoso(usuarioLoginResult);
                    return;
                }

                // ============================================
                // PASO 3: CREDENCIALES INCORRECTAS
                // ============================================
                
                IntentosFallidos++;
                
                if (IntentosFallidos >= MaxIntentosFallidos)
                {
                    UsuarioBloqueado = true;
                    TiempoBloqueo = DateTime.Now.AddMinutes(MinutosBloqueoPorIntentosFallidos);
                    MostrarMensajeError($"Cuenta bloqueada por {MinutosBloqueoPorIntentosFallidos} minutos.");
                }
                else
                {
                    int intentosRestantes = MaxIntentosFallidos - IntentosFallidos;
                    MostrarMensajeError($"Credenciales incorrectas. {intentosRestantes} intentos restantes.");
                }
            }
            catch (Exception ex)
            {
                MostrarMensajeError($"Error de conexión: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ============================================
        // MÉTODOS DE AUTENTICACIÓN
        // ============================================

        /// <summary>
        /// Intenta autenticar como Administrador Allva
        /// Retorna resultado con datos de sesión si es exitoso
        /// </summary>
        private async Task<LoginResult> IntentarLoginAdministradorAllva()
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                // Query: Buscar administrador por nombre_usuario
                var query = @"
                    SELECT 
                        id_administrador, nombre, apellidos, nombre_usuario, password_hash,
                        correo, telefono, activo, bloqueado_hasta, intentos_fallidos,
                        acceso_gestion_comercios, acceso_gestion_usuarios_locales, 
                        acceso_gestion_usuarios_allva, acceso_analytics, 
                        acceso_configuracion_sistema, acceso_facturacion_global, 
                        acceso_auditoria, idioma
                    FROM administradores_allva
                    WHERE nombre_usuario = @NombreUsuario AND activo = true";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NombreUsuario", NumeroUsuario.ToLower().Trim());

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    // No se encontró administrador con ese nombre
                    return new LoginResult { Exitoso = false };
                }

                // Administrador encontrado - verificar contraseña
                var idAdministrador = reader.GetInt32(0);
                var nombre = reader.GetString(1);
                var apellidos = reader.GetString(2);
                var nombreUsuario = reader.GetString(3);
                var passwordHash = reader.GetString(4);
                var correo = reader.GetString(5);
                var telefono = reader.IsDBNull(6) ? null : reader.GetString(6);
                var activo = reader.GetBoolean(7);
                var bloqueadoHasta = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8);
                var intentosFallidos = reader.GetInt32(9);

                // Permisos
                var accesoGestionComercios = reader.GetBoolean(10);
                var accesoGestionUsuariosLocales = reader.GetBoolean(11);
                var accesoGestionUsuariosAllva = reader.GetBoolean(12);
                var accesoAnalytics = reader.GetBoolean(13);
                var accesoConfiguracionSistema = reader.GetBoolean(14);
                var accesoFacturacionGlobal = reader.GetBoolean(15);
                var accesoAuditoria = reader.GetBoolean(16);
                var idioma = reader.GetString(17);

                reader.Close();

                // Verificar si está bloqueado
                if (bloqueadoHasta.HasValue && bloqueadoHasta.Value > DateTime.Now)
                {
                    var minutosRestantes = (int)(bloqueadoHasta.Value - DateTime.Now).TotalMinutes;
                    MostrarMensajeError($"Cuenta bloqueada. Intente en {minutosRestantes} minutos.");
                    return new LoginResult { Exitoso = false };
                }

                // Verificar contraseña
                bool passwordValido = BCrypt.Net.BCrypt.Verify(Password, passwordHash);

                if (!passwordValido)
                {
                    // Contraseña incorrecta - incrementar intentos fallidos
                    await IncrementarIntentosFallidosAdmin(connection, idAdministrador, intentosFallidos);
                    return new LoginResult { Exitoso = false };
                }

                // ✅ LOGIN EXITOSO - Actualizar último acceso y resetear intentos
                await ActualizarUltimoAccesoAdmin(connection, idAdministrador);

                // Crear datos de sesión
                return new LoginResult
                {
                    Exitoso = true,
                    EsAdministradorAllva = true,
                    IdUsuario = idAdministrador,
                    NombreCompleto = $"{nombre} {apellidos}",
                    NombreUsuario = nombreUsuario,
                    Correo = correo,
                    Telefono = telefono,
                    Idioma = idioma,
                    
                    // Permisos
                    AccesoGestionComercios = accesoGestionComercios,
                    AccesoGestionUsuariosLocales = accesoGestionUsuariosLocales,
                    AccesoGestionUsuariosAllva = accesoGestionUsuariosAllva,
                    AccesoAnalytics = accesoAnalytics,
                    AccesoConfiguracionSistema = accesoConfiguracionSistema,
                    AccesoFacturacionGlobal = accesoFacturacionGlobal,
                    AccesoAuditoria = accesoAuditoria
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en autenticación admin: {ex.Message}");
                return new LoginResult { Exitoso = false };
            }
        }

        /// <summary>
        /// Intenta autenticar como Usuario Normal (de local)
        /// Retorna resultado con datos de sesión si es exitoso
        /// </summary>
        private async Task<LoginResult> IntentarLoginUsuarioNormal()
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                // Query: Buscar usuario por numero_usuario y verificar que tenga acceso al local
                var query = @"
                    SELECT 
                        u.id_usuario, u.nombre, u.apellidos, u.numero_usuario, 
                        u.password_hash, u.correo, u.telefono, u.activo, 
                        u.bloqueado_hasta, u.intentos_fallidos, u.es_flotante,
                        u.id_local, u.id_comercio, u.idioma,
                        l.codigo_local, l.nombre_local,
                        c.nombre_comercio
                    FROM usuarios u
                    LEFT JOIN locales l ON u.id_local = l.id_local
                    LEFT JOIN comercios c ON u.id_comercio = c.id_comercio
                    WHERE u.numero_usuario = @NumeroUsuario 
                      AND u.activo = true";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NumeroUsuario", NumeroUsuario.Trim());

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    // No se encontró usuario
                    return new LoginResult { Exitoso = false };
                }

                // Usuario encontrado
                var idUsuario = reader.GetInt32(0);
                var nombre = reader.GetString(1);
                var apellidos = reader.GetString(2);
                var numeroUsuario = reader.GetString(3);
                var passwordHash = reader.GetString(4);
                var correo = reader.GetString(5);
                var telefono = reader.IsDBNull(6) ? null : reader.GetString(6);
                var activo = reader.GetBoolean(7);
                var bloqueadoHasta = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8);
                var intentosFallidos = reader.GetInt32(9);
                var esFlotante = reader.GetBoolean(10);
                var idLocal = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11);
                var idComercio = reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12);
                var idioma = reader.IsDBNull(13) ? "es" : reader.GetString(13);
                var codigoLocal = reader.IsDBNull(14) ? null : reader.GetString(14);
                var nombreLocal = reader.IsDBNull(15) ? null : reader.GetString(15);
                var nombreComercio = reader.IsDBNull(16) ? null : reader.GetString(16);

                reader.Close();

                // Verificar si está bloqueado
                if (bloqueadoHasta.HasValue && bloqueadoHasta.Value > DateTime.Now)
                {
                    var minutosRestantes = (int)(bloqueadoHasta.Value - DateTime.Now).TotalMinutes;
                    MostrarMensajeError($"Cuenta bloqueada. Intente en {minutosRestantes} minutos.");
                    return new LoginResult { Exitoso = false };
                }

                // Verificar acceso al local
                // TODO: Implementar verificación de permisos de local cuando se agregue tabla usuario_locales
                
                // Verificar contraseña
                bool passwordValido = BCrypt.Net.BCrypt.Verify(Password, passwordHash);

                if (!passwordValido)
                {
                    // Contraseña incorrecta
                    await IncrementarIntentosFallidosUsuario(connection, idUsuario, intentosFallidos);
                    return new LoginResult { Exitoso = false };
                }

                // ✅ LOGIN EXITOSO
                await ActualizarUltimoAccesoUsuario(connection, idUsuario);

                return new LoginResult
                {
                    Exitoso = true,
                    EsAdministradorAllva = false,
                    IdUsuario = idUsuario,
                    NombreCompleto = $"{nombre} {apellidos}",
                    NombreUsuario = numeroUsuario,
                    Correo = correo,
                    Telefono = telefono,
                    Idioma = idioma,
                    CodigoLocal = CodigoLocal.ToUpper(),
                    NombreLocal = nombreLocal ?? "Local",
                    NombreComercio = nombreComercio ?? "Comercio",
                    EsFlotante = esFlotante
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en autenticación usuario: {ex.Message}");
                return new LoginResult { Exitoso = false };
            }
        }

        // ============================================
        // MÉTODOS DE ACTUALIZACIÓN DE BASE DE DATOS
        // ============================================

        private async Task IncrementarIntentosFallidosAdmin(NpgsqlConnection connection, int idAdmin, int intentosActuales)
        {
            var nuevosIntentos = intentosActuales + 1;
            DateTime? bloqueadoHasta = null;

            if (nuevosIntentos >= MaxIntentosFallidos)
            {
                bloqueadoHasta = DateTime.Now.AddMinutes(MinutosBloqueoPorIntentosFallidos);
            }

            var query = @"UPDATE administradores_allva 
                         SET intentos_fallidos = @Intentos, 
                             bloqueado_hasta = @Bloqueado
                         WHERE id_administrador = @Id";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Intentos", nuevosIntentos);
            cmd.Parameters.AddWithValue("@Bloqueado", bloqueadoHasta.HasValue ? bloqueadoHasta.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", idAdmin);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task ActualizarUltimoAccesoAdmin(NpgsqlConnection connection, int idAdmin)
        {
            var query = @"UPDATE administradores_allva 
                         SET ultimo_acceso = @Fecha, 
                             intentos_fallidos = 0, 
                             bloqueado_hasta = NULL
                         WHERE id_administrador = @Id";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
            cmd.Parameters.AddWithValue("@Id", idAdmin);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task IncrementarIntentosFallidosUsuario(NpgsqlConnection connection, int idUsuario, int intentosActuales)
        {
            var nuevosIntentos = intentosActuales + 1;
            DateTime? bloqueadoHasta = null;

            if (nuevosIntentos >= MaxIntentosFallidos)
            {
                bloqueadoHasta = DateTime.Now.AddMinutes(MinutosBloqueoPorIntentosFallidos);
            }

            var query = @"UPDATE usuarios 
                         SET intentos_fallidos = @Intentos, 
                             bloqueado_hasta = @Bloqueado
                         WHERE id_usuario = @Id";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Intentos", nuevosIntentos);
            cmd.Parameters.AddWithValue("@Bloqueado", bloqueadoHasta.HasValue ? bloqueadoHasta.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", idUsuario);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task ActualizarUltimoAccesoUsuario(NpgsqlConnection connection, int idUsuario)
        {
            var query = @"UPDATE usuarios 
                         SET ultimo_acceso = @Fecha, 
                             intentos_fallidos = 0, 
                             bloqueado_hasta = NULL
                         WHERE id_usuario = @Id";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
            cmd.Parameters.AddWithValue("@Id", idUsuario);
            await cmd.ExecuteNonQueryAsync();
        }

        // ============================================
        // PROCESAMIENTO DE LOGIN EXITOSO
        // ============================================

        private async Task ProcesarLoginExitoso(LoginResult resultado)
        {
            // Resetear intentos fallidos
            IntentosFallidos = 0;
            UsuarioBloqueado = false;

            // Guardar datos si se seleccionó "recordar"
            if (RecordarSesion)
            {
                GuardarDatosLocales();
            }
            else
            {
                LimpiarDatosGuardados();
            }

            // Preparar datos de navegación
            var loginData = new LoginSuccessData
            {
                UserName = resultado.NombreCompleto,
                UserNumber = resultado.NombreUsuario,
                LocalCode = resultado.CodigoLocal ?? "SYSTEM",
                Token = $"token-{Guid.NewGuid()}",
                IsSystemAdmin = resultado.EsAdministradorAllva,
                UserType = resultado.EsAdministradorAllva ? "ADMIN_ALLVA" : "USUARIO_LOCAL",
                RoleName = resultado.EsAdministradorAllva ? "Administrador_Allva" : "Usuario_Local",
                
                // Guardar permisos de administrador
                Permisos = new PermisosAdministrador
                {
                    AccesoGestionComercios = resultado.AccesoGestionComercios,
                    AccesoGestionUsuariosLocales = resultado.AccesoGestionUsuariosLocales,
                    AccesoGestionUsuariosAllva = resultado.AccesoGestionUsuariosAllva,
                    AccesoAnalytics = resultado.AccesoAnalytics,
                    AccesoConfiguracionSistema = resultado.AccesoConfiguracionSistema,
                    AccesoFacturacionGlobal = resultado.AccesoFacturacionGlobal,
                    AccesoAuditoria = resultado.AccesoAuditoria
                }
            };

            // Navegar al dashboard correspondiente
            var navigationService = new NavigationService();

            if (resultado.EsAdministradorAllva)
            {
                // Redirigir al Panel de Administración (Back Office)
                navigationService.NavigateToAdminDashboard(loginData);
            }
            else
            {
                // Redirigir al Panel Principal (Front Office)
                navigationService.NavigateTo("MainDashboard", loginData);
            }
        }

        // ============================================
        // VALIDACIONES
        // ============================================

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NumeroUsuario))
            {
                MostrarMensajeError("El usuario es requerido.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MostrarMensajeError("La contraseña es requerida.");
                return false;
            }

            if (Password.Length < 6)
            {
                MostrarMensajeError("La contraseña debe tener al menos 6 caracteres.");
                return false;
            }

            return true;
        }

        private bool PuedeIniciarSesion()
        {
            return !string.IsNullOrWhiteSpace(NumeroUsuario) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !IsLoading &&
                   !UsuarioBloqueado;
        }

        private void MostrarMensajeError(string mensaje)
        {
            MensajeError = mensaje;
            MostrarError = true;
        }

        // ============================================
        // MÉTODOS DE PERSISTENCIA LOCAL
        // ============================================

        private void CargarDatosGuardados()
        {
            // TODO: Implementar
        }

        private void GuardarDatosLocales()
        {
            // TODO: Implementar
        }

        private void LimpiarDatosGuardados()
        {
            // TODO: Implementar
        }

        // ============================================
        // MÉTODOS AUXILIARES
        // ============================================

        partial void OnNumeroUsuarioChanged(string value)
        {
            LoginCommand.NotifyCanExecuteChanged();
        }

        partial void OnPasswordChanged(string value)
        {
            LoginCommand.NotifyCanExecuteChanged();
        }

        partial void OnCodigoLocalChanged(string value)
        {
            LoginCommand.NotifyCanExecuteChanged();
        }
    }

    // ============================================
    // CLASES AUXILIARES
    // ============================================

    /// <summary>
    /// Resultado de un intento de login
    /// </summary>
    public class LoginResult
    {
        public bool Exitoso { get; set; }
        public bool EsAdministradorAllva { get; set; }
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Idioma { get; set; } = "es";
        public string? CodigoLocal { get; set; }
        public string? NombreLocal { get; set; }
        public string? NombreComercio { get; set; }
        public bool EsFlotante { get; set; }
        
        // Permisos de administrador
        public bool AccesoGestionComercios { get; set; }
        public bool AccesoGestionUsuariosLocales { get; set; }
        public bool AccesoGestionUsuariosAllva { get; set; }
        public bool AccesoAnalytics { get; set; }
        public bool AccesoConfiguracionSistema { get; set; }
        public bool AccesoFacturacionGlobal { get; set; }
        public bool AccesoAuditoria { get; set; }
    }
}
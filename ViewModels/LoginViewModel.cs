using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Services;
using Npgsql;
using BCrypt.Net;

namespace Allva.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de inicio de sesión
    /// Soporta login de usuarios normales Y administradores Allva
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
        private bool _recordarCredenciales;
        
        [ObservableProperty]
        private bool _recordarSesion;

        [ObservableProperty]
        private bool _mostrarPassword;

        [ObservableProperty]
        private bool _cargando;
        
        // Alias para compatibilidad con LoginView.axaml
        public bool IsLoading => Cargando;
        
        // Notificar cambios en IsLoading cuando cambia Cargando
        partial void OnCargandoChanged(bool value)
        {
            OnPropertyChanged(nameof(IsLoading));
        }

        [ObservableProperty]
        private string _mensajeError = string.Empty;

        [ObservableProperty]
        private bool _mostrarMensajeError;

        // ============================================
        // COMANDOS
        // ============================================

        /// <summary>
        /// Comando para iniciar sesión
        /// </summary>
        [RelayCommand]
        private async Task IniciarSesion()
        {
            // Limpiar mensajes anteriores
            MensajeError = string.Empty;
            MostrarMensajeError = false;

            // Validar campos básicos
            if (!ValidarCampos())
                return;

            Cargando = true;

            try
            {
                // PASO 1: Intentar autenticar como Administrador Allva PRIMERO
                // Los nombres de usuario de admins NO son numéricos (ej: jose_noble, maria_gonzalez)
                bool esNumerico = int.TryParse(NumeroUsuario, out _);
                
                if (!esNumerico)
                {
                    // Es un nombre de usuario texto → buscar en administradores_allva
                    var resultadoAdmin = await AutenticarAdministradorAllva(NumeroUsuario, Password);
                    
                    if (resultadoAdmin.Exitoso)
                    {
                        // Login exitoso como admin
                        var loginData = new LoginSuccessData
                        {
                            UserName = resultadoAdmin.NombreCompleto,
                            UserNumber = NumeroUsuario,
                            LocalCode = "SYSTEM",
                            Token = $"token-{Guid.NewGuid()}",
                            IsSystemAdmin = true,
                            UserType = "ADMIN_ALLVA",
                            RoleName = "Administrador_Allva",
                            
                            Permisos = new PermisosAdministrador
                            {
                                AccesoGestionComercios = resultadoAdmin.AccesoGestionComercios,
                                AccesoGestionUsuariosLocales = resultadoAdmin.AccesoGestionUsuariosLocales,
                                AccesoGestionUsuariosAllva = resultadoAdmin.AccesoGestionUsuariosAllva,
                                AccesoAnalytics = resultadoAdmin.AccesoAnalytics,
                                AccesoConfiguracionSistema = resultadoAdmin.AccesoConfiguracionSistema,
                                AccesoFacturacionGlobal = resultadoAdmin.AccesoFacturacionGlobal,
                                AccesoAuditoria = resultadoAdmin.AccesoAuditoria
                            }
                        };

                        var navigationService = new NavigationService();
                        navigationService.NavigateToAdminDashboard(loginData);
                        return;
                    }
                    else
                    {
                        // No se encontró como admin
                        MostrarError(resultadoAdmin.MensajeError);
                        return;
                    }
                }
                
                // PASO 2: Usuario numérico (0001, 1001, etc) → buscar como usuario normal
                // Requiere código de local
                if (string.IsNullOrWhiteSpace(CodigoLocal))
                {
                    MostrarError("El código de local es requerido para usuarios normales");
                    return;
                }
                
                var resultadoNormal = await AutenticarUsuarioNormal(NumeroUsuario, Password, CodigoLocal);
                
                if (resultadoNormal.Exitoso)
                {
                    // Login exitoso como usuario normal
                    var loginData = new LoginSuccessData
                    {
                        UserName = resultadoNormal.NombreCompleto,
                        UserNumber = NumeroUsuario,
                        LocalCode = CodigoLocal,
                        Token = $"token-{Guid.NewGuid()}",
                        IsSystemAdmin = false,
                        UserType = "USUARIO_LOCAL",
                        RoleName = "Usuario_Local"
                    };

                    var navigationService = new NavigationService();
                    navigationService.NavigateTo("MainDashboard", loginData);
                }
                else
                {
                    MostrarError(resultadoNormal.MensajeError);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error de conexión: {ex.Message}");
            }
            finally
            {
                Cargando = false;
            }
        }
        
        /// <summary>
        /// Alias para LoginCommand (compatibilidad con LoginView.axaml)
        /// </summary>
        public IAsyncRelayCommand LoginCommand => IniciarSesionCommand;

        /// <summary>
        /// Comando para mostrar/ocultar contraseña
        /// </summary>
        [RelayCommand]
        private void ToggleMostrarPassword()
        {
            MostrarPassword = !MostrarPassword;
        }
        
        /// <summary>
        /// Comando para cambiar idioma (stub - no implementado aún)
        /// </summary>
        [RelayCommand]
        private void CambiarIdioma(string idioma)
        {
            // TODO: Implementar cambio de idioma
        }
        
        /// <summary>
        /// Comando para recuperar contraseña (stub - no implementado aún)
        /// </summary>
        [RelayCommand]
        private async Task RecuperarPassword()
        {
            // TODO: Implementar recuperación de contraseña
            await Task.CompletedTask;
        }

        // ============================================
        // MÉTODOS DE AUTENTICACIÓN
        // ============================================

        /// <summary>
        /// Autentica administradores de Allva (jose_noble, maria_gonzalez, etc)
        /// Busca en tabla administradores_allva
        /// </summary>
        private async Task<ResultadoAutenticacion> AutenticarAdministradorAllva(string nombreUsuario, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        nombre_usuario,
                        nombre,
                        apellidos,
                        password_hash,
                        activo,
                        acceso_gestion_comercios,
                        acceso_gestion_usuarios_locales,
                        acceso_gestion_usuarios_allva,
                        acceso_analytics,
                        acceso_configuracion_sistema,
                        acceso_facturacion_global,
                        acceso_auditoria
                    FROM administradores_allva
                    WHERE nombre_usuario = @NombreUsuario";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario administrador no encontrado"
                    };
                }

                // Verificar si está activo (índice 4)
                if (!reader.GetBoolean(4))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario administrador inactivo"
                    };
                }

                // Verificar contraseña con BCrypt (índice 3)
                var passwordHash = reader.GetString(3);
                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Contraseña incorrecta"
                    };
                }

                // Concatenar nombre completo (índices 1 y 2)
                var nombre = reader.GetString(1);
                var apellidos = reader.GetString(2);
                var nombreCompleto = $"{nombre} {apellidos}";

                // Login exitoso
                return new ResultadoAutenticacion
                {
                    Exitoso = true,
                    NombreCompleto = nombreCompleto,
                    EsAdministradorAllva = true,
                    AccesoGestionComercios = reader.GetBoolean(5),
                    AccesoGestionUsuariosLocales = reader.GetBoolean(6),
                    AccesoGestionUsuariosAllva = reader.GetBoolean(7),
                    AccesoAnalytics = reader.GetBoolean(8),
                    AccesoConfiguracionSistema = reader.GetBoolean(9),
                    AccesoFacturacionGlobal = reader.GetBoolean(10),
                    AccesoAuditoria = reader.GetBoolean(11)
                };
            }
            catch (Exception ex)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"Error al autenticar administrador: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Autentica usuarios normales (1001, 1002, 9999, etc)
        /// Busca en tabla usuarios
        /// </summary>
        private async Task<ResultadoAutenticacion> AutenticarUsuarioNormal(string numeroUsuario, string password, string codigoLocal)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        u.numero_usuario,
                        u.nombre_completo,
                        u.password_hash,
                        u.activo,
                        u.id_local,
                        l.codigo_local,
                        l.nombre_local,
                        l.activo AS local_activo
                    FROM usuarios u
                    INNER JOIN locales l ON u.id_local = l.id_local
                    WHERE u.numero_usuario = @NumeroUsuario
                    AND l.codigo_local = @CodigoLocal";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NumeroUsuario", numeroUsuario);
                cmd.Parameters.AddWithValue("@CodigoLocal", codigoLocal);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario no encontrado o código de local incorrecto"
                    };
                }

                // Verificar si el usuario está activo
                if (!reader.GetBoolean(3))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario inactivo"
                    };
                }

                // Verificar si el local está activo
                if (!reader.GetBoolean(7))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Local inactivo"
                    };
                }

                // Verificar contraseña con BCrypt
                var passwordHash = reader.GetString(2);
                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Contraseña incorrecta"
                    };
                }

                // Login exitoso
                return new ResultadoAutenticacion
                {
                    Exitoso = true,
                    NombreCompleto = reader.GetString(1),
                    EsAdministradorAllva = false
                };
            }
            catch (Exception ex)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"Error al autenticar usuario: {ex.Message}"
                };
            }
        }

        // ============================================
        // VALIDACIONES
        // ============================================

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NumeroUsuario))
            {
                MostrarError("El número de usuario es requerido");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MostrarError("La contraseña es requerida");
                return false;
            }

            if (Password.Length < 6)
            {
                MostrarError("La contraseña debe tener al menos 6 caracteres");
                return false;
            }

            // Solo requerir código de local si es usuario numérico (no admin)
            bool esNumerico = int.TryParse(NumeroUsuario, out _);
            if (esNumerico && string.IsNullOrWhiteSpace(CodigoLocal))
            {
                MostrarError("El código de local es requerido para usuarios normales");
                return false;
            }

            return true;
        }

        private void MostrarError(string mensaje)
        {
            MensajeError = mensaje;
            MostrarMensajeError = true;

            // Ocultar mensaje después de 5 segundos
            Task.Delay(5000).ContinueWith(_ =>
            {
                MostrarMensajeError = false;
            });
        }
    }

    // ============================================
    // CLASES DE SOPORTE
    // ============================================

    /// <summary>
    /// Resultado de la autenticación
    /// </summary>
    public class ResultadoAutenticacion
    {
        public bool Exitoso { get; set; }
        public string MensajeError { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public bool EsAdministradorAllva { get; set; }
        
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
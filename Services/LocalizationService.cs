using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Allva.Desktop.Services
{
    /// <summary>
    /// Servicio de localización para manejo de múltiples idiomas
    /// Soporta Español e Inglés
    /// </summary>
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        private string _currentLanguage = "es"; // Por defecto español

        public event PropertyChangedEventHandler? PropertyChanged;

        // ============================================
        // DICCIONARIOS DE TRADUCCIONES
        // ============================================

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["es"] = new Dictionary<string, string>
            {
                // Login
                ["Login_Title"] = "Iniciar Sesión",
                ["Login_User"] = "Usuario",
                ["Login_UserPlaceholder"] = "Ingrese su número de usuario",
                ["Login_Office"] = "Código oficina",
                ["Login_OfficePlaceholder"] = "CENTRAL",
                ["Login_Password"] = "Contraseña",
                ["Login_PasswordPlaceholder"] = "Ingrese su contraseña",
                ["Login_Remember"] = "Recordar en este dispositivo",
                ["Login_Button"] = "INICIAR SESIÓN",
                ["Login_Loading"] = "Iniciando sesión...",
                ["Login_ForgotPassword"] = "¿Has olvidado tu contraseña?",
                ["Login_SecureConnection"] = "🔐 Conexión segura",
                ["Login_SecureInfo"] = "Sus datos están protegidos con encriptación SSL",
                ["Login_Version"] = "Versión",
                ["Login_Support"] = "Soporte técnico",
                ["Login_Copyright"] = "© 2024 Allva System. Todos los derechos reservados.",
                
                // Errores
                ["Error_UserRequired"] = "El número de usuario es requerido.",
                ["Error_PasswordRequired"] = "La contraseña es requerida.",
                ["Error_PasswordMinLength"] = "La contraseña debe tener al menos 8 caracteres.",
                ["Error_OfficeRequired"] = "El código de local es requerido.",
                ["Error_UserNotFound"] = "Usuario no encontrado. Verifique sus datos.",
                ["Error_PasswordIncorrect"] = "Contraseña incorrecta. Le quedan {0} intentos antes de que su cuenta sea bloqueada temporalmente.",
                ["Error_UserBlocked"] = "Su cuenta ha sido bloqueada temporalmente por múltiples intentos fallidos. Podrá intentar nuevamente en {0} minutos.",
                ["Error_UserBlockedRemaining"] = "Su cuenta está bloqueada. Podrá intentar nuevamente en {0} minutos.",
                ["Error_OfficeInvalid"] = "El código de local no existe o está inactivo.",
                ["Error_NoPermission"] = "No tiene permiso para acceder a este local. Contacte a su administrador.",
                ["Error_CompanyInactive"] = "El comercio está deshabilitado. Contacte a soporte.",
                ["Error_DeviceUnauthorized"] = "Este dispositivo no está autorizado. Se ha notificado al administrador. Por favor, espere la autorización o use un dispositivo previamente autorizado.",
                ["Error_Connection"] = "Error de conexión. Verifique su conexión a internet.",
                ["Error_Generic"] = "Error al iniciar sesión. Intente nuevamente.",
                
                // Recuperar contraseña
                ["Recovery_Title"] = "Recuperar contraseña",
                ["Recovery_UserRequired"] = "Campo requerido",
                ["Recovery_UserRequiredMessage"] = "Por favor ingrese su número de usuario para recuperar la contraseña.",
                ["Recovery_Confirm"] = "Se enviará un enlace de recuperación al correo registrado para el usuario '{0}'. ¿Desea continuar?",
                ["Recovery_Success"] = "Solicitud enviada",
                ["Recovery_SuccessMessage"] = "Se ha enviado un correo con las instrucciones para recuperar su contraseña. El enlace expira en 1 hora.",
                ["Recovery_Error"] = "Error",
                ["Recovery_ErrorMessage"] = "No se pudo procesar la solicitud.",
                ["Recovery_ErrorGeneric"] = "Ocurrió un error al procesar su solicitud. Intente nuevamente.",
                
                // Primer login
                ["FirstLogin_Title"] = "Primer inicio de sesión",
                ["FirstLogin_Message"] = "Debe cambiar su contraseña antes de continuar.",
                
                // Diálogos
                ["Dialog_Yes"] = "Sí",
                ["Dialog_No"] = "No",
                ["Dialog_Ok"] = "Aceptar",
                ["Dialog_Cancel"] = "Cancelar",
                
                // Idiomas
                ["Language_Spanish"] = "ESP",
                ["Language_English"] = "ENG",
            },
            
            ["en"] = new Dictionary<string, string>
            {
                // Login
                ["Login_Title"] = "Sign In",
                ["Login_User"] = "User",
                ["Login_UserPlaceholder"] = "Enter your user number",
                ["Login_Office"] = "Office code",
                ["Login_OfficePlaceholder"] = "CENTRAL",
                ["Login_Password"] = "Password",
                ["Login_PasswordPlaceholder"] = "Enter your password",
                ["Login_Remember"] = "Remember on this device",
                ["Login_Button"] = "SIGN IN",
                ["Login_Loading"] = "Signing in...",
                ["Login_ForgotPassword"] = "Forgot your password?",
                ["Login_SecureConnection"] = "🔐 Secure connection",
                ["Login_SecureInfo"] = "Your data is protected with SSL encryption",
                ["Login_Version"] = "Version",
                ["Login_Support"] = "Technical support",
                ["Login_Copyright"] = "© 2024 Allva System. All rights reserved.",
                
                // Errors
                ["Error_UserRequired"] = "User number is required.",
                ["Error_PasswordRequired"] = "Password is required.",
                ["Error_PasswordMinLength"] = "Password must be at least 8 characters long.",
                ["Error_OfficeRequired"] = "Office code is required.",
                ["Error_UserNotFound"] = "User not found. Please verify your information.",
                ["Error_PasswordIncorrect"] = "Incorrect password. You have {0} attempts remaining before your account is temporarily locked.",
                ["Error_UserBlocked"] = "Your account has been temporarily locked due to multiple failed attempts. You may try again in {0} minutes.",
                ["Error_UserBlockedRemaining"] = "Your account is locked. You may try again in {0} minutes.",
                ["Error_OfficeInvalid"] = "Office code does not exist or is inactive.",
                ["Error_NoPermission"] = "You do not have permission to access this office. Contact your administrator.",
                ["Error_CompanyInactive"] = "The company is disabled. Contact support.",
                ["Error_DeviceUnauthorized"] = "This device is not authorized. The administrator has been notified. Please wait for authorization or use a previously authorized device.",
                ["Error_Connection"] = "Connection error. Check your internet connection.",
                ["Error_Generic"] = "Login error. Please try again.",
                
                // Password recovery
                ["Recovery_Title"] = "Password recovery",
                ["Recovery_UserRequired"] = "Required field",
                ["Recovery_UserRequiredMessage"] = "Please enter your user number to recover your password.",
                ["Recovery_Confirm"] = "A recovery link will be sent to the registered email for user '{0}'. Do you want to continue?",
                ["Recovery_Success"] = "Request sent",
                ["Recovery_SuccessMessage"] = "An email has been sent with instructions to recover your password. The link expires in 1 hour.",
                ["Recovery_Error"] = "Error",
                ["Recovery_ErrorMessage"] = "Could not process the request.",
                ["Recovery_ErrorGeneric"] = "An error occurred while processing your request. Please try again.",
                
                // First login
                ["FirstLogin_Title"] = "First login",
                ["FirstLogin_Message"] = "You must change your password before continuing.",
                
                // Dialogs
                ["Dialog_Yes"] = "Yes",
                ["Dialog_No"] = "No",
                ["Dialog_Ok"] = "Ok",
                ["Dialog_Cancel"] = "Cancel",
                
                // Languages
                ["Language_Spanish"] = "ESP",
                ["Language_English"] = "ENG",
            }
        };

        // ============================================
        // PROPIEDADES
        // ============================================

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    SaveLanguagePreference(value);
                    OnPropertyChanged();
                    NotifyAllTextsChanged();
                }
            }
        }

        public string CurrentLanguageFlag => CurrentLanguage switch
        {
            "es" => "🇪🇸",
            "en" => "🇬🇧",
            _ => "🌐"
        };

        public string CurrentLanguageCode => CurrentLanguage switch
        {
            "es" => "ESP",
            "en" => "ENG",
            _ => "ESP"
        };

        // ============================================
        // CONSTRUCTOR
        // ============================================

        private LocalizationService()
        {
            LoadLanguagePreference();
        }

        // ============================================
        // MÉTODOS PÚBLICOS
        // ============================================

        public string GetText(string key)
        {
            if (_translations.TryGetValue(_currentLanguage, out var languageDict))
            {
                if (languageDict.TryGetValue(key, out var text))
                {
                    return text;
                }
            }

            // Fallback a español si no encuentra la clave
            if (_translations.TryGetValue("es", out var fallbackDict))
            {
                if (fallbackDict.TryGetValue(key, out var fallbackText))
                {
                    return fallbackText;
                }
            }

            return $"[{key}]"; // Mostrar la clave si no encuentra traducción
        }

        public string GetText(string key, params object[] args)
        {
            var text = GetText(key);
            return string.Format(text, args);
        }

        public void ToggleLanguage()
        {
            CurrentLanguage = CurrentLanguage == "es" ? "en" : "es";
        }

        public void SetLanguage(string languageCode)
        {
            if (_translations.ContainsKey(languageCode))
            {
                CurrentLanguage = languageCode;
            }
        }

        // ============================================
        // PERSISTENCIA
        // ============================================

        private void SaveLanguagePreference(string language)
        {
            try
            {
                // Guardar en preferencias locales
                var prefsFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Allva",
                    "preferences.txt"
                );

                var dir = System.IO.Path.GetDirectoryName(prefsFile);
                if (dir != null && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                System.IO.File.WriteAllText(prefsFile, $"language={language}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving language preference: {ex.Message}");
            }
        }

        private void LoadLanguagePreference()
        {
            try
            {
                var prefsFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Allva",
                    "preferences.txt"
                );

                if (System.IO.File.Exists(prefsFile))
                {
                    var content = System.IO.File.ReadAllText(prefsFile);
                    if (content.StartsWith("language="))
                    {
                        var lang = content.Replace("language=", "").Trim();
                        if (_translations.ContainsKey(lang))
                        {
                            _currentLanguage = lang;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading language preference: {ex.Message}");
            }
        }

        // ============================================
        // INDEXER PARA ACCESO FÁCIL
        // ============================================

        public string this[string key] => GetText(key);

        // ============================================
        // INotifyPropertyChanged
        // ============================================

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyAllTextsChanged()
        {
            // Notificar que todos los textos han cambiado
            foreach (var key in _translations[_currentLanguage].Keys)
            {
                OnPropertyChanged($"[{key}]");
            }
            
            OnPropertyChanged(nameof(CurrentLanguageFlag));
            OnPropertyChanged(nameof(CurrentLanguageCode));
        }
    }
}
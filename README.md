# GraphSmtpBridge - Puente SMTP a Microsoft 365 (OAuth2)

Este servicio de Windows actúa como un relé SMTP local para aplicaciones legadas que no soportan autenticación OAuth2. Recibe correos vía SMTP estándar y los reenvía a Microsoft 365 utilizando la **Microsoft Graph API**.

## Características principales
- **Protocolo Moderno:** Envío a través de Graph API usando el flujo `client_credentials` (OAuth 2.1).
- **Servicio de Windows:** Se ejecuta en segundo plano como un servicio nativo de Windows.
- **Soporte de Adjuntos:** Implementado con `MimeKit` para procesar archivos adjuntos (PDF, imágenes, etc.).
- **Logging Persistente:** Sistema de logs configurable para depurar el tráfico y los errores de la API.
- **Seguridad:** Permite filtrar peticiones por subred IP.

## Requisitos
- **.NET 8.0 Runtime** (o SDK para compilar).
- Registro de aplicación en **Microsoft Entra ID (Azure AD)** con permisos `Mail.Send`.

## Configuración (`appsettings.json`)
Antes de instalar, edita los parámetros en `appsettings.json`:
- **TenantId**: ID del directorio de Azure.
- **ClientId**: ID de la aplicación registrada.
- **ClientSecret**: Secreto generado en Azure.
- **SenderEmail**: Buzón real de Office 365 que enviará los correos.
- **SmtpPort**: Puerto donde escuchará el servicio (por defecto 2525).
- **AllowedSubnet**: Rango de red local permitida (ignora el resto por seguridad).

## Instalación rápida (Admin)

1. **Compilar:**
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
   ```
2. **Crear Servicio:**
   ```powershell
   sc.exe create "GescofSmtpBridge" binpath= "C:\Ruta\Al\Proyecto\publish\GraphSmtpBridge.exe" start= auto
   ```
3. **Iniciar Servicio:**
   ```powershell
   sc.exe start "GescofSmtpBridge"
   ```

## Resolución de Problemas
El servicio genera logs en la ruta configurada (por defecto `C:\SmtpBridgeLogs\smtp_bridge.log`). Si los correos no llegan, revisa este archivo para ver el mensaje de error exacto devuelto por la API de Microsoft.

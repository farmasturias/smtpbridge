# C# Windows Service: Puente SMTP -> MS365

Este servicio actuará como un relé SMTP local que captura correos y los envía a Microsoft 365 mediante la Graph API.

## Requisitos de Entorno
* **.NET SDK 8.0** (Recomendado por ser LTS y moderno).
* Acceso a Internet para descargar paquetes NuGet y conectar con MS Graph.

## Proposed Changes

### [Proyecto C# (.NET 8)]

#### [NEW] [GraphSmtpBridge](file:///c:/Users/antonio.COF/antigravity/GraphSmtpBridge/)
Un proyecto de tipo "Worker Service" que incluye:
1. **`Worker.cs`**: Inicia el servidor SMTP escuchando en todas las interfaces (`0.0.0.0`) o una IP específica.
2. **`SmtpMessageHandler.cs`**: Interceptor que verifica la IP del cliente antes de procesar el correo (filtro de red local).
3. **`GraphClient.cs`**: Servicio de envío OAuth2.
4. **`appsettings.json`**:
   - `OAuth`: ClientId, TenantId, ClientSecret.
   - `Network`: Port (ej. 2525), AllowedSubnet (ej. 192.168.1.0/24).

## Verification Plan

### Automated Tests
1. Compilación: `dotnet build`.
2. Prueba interactiva: `dotnet run` (debe escuchar en el puerto configurado).
3. Prueba de carga del servicio: `sc create ...` y `sc start ...`.

### Manual Verification
1. Modificar el `ClientSecret` en `appsettings.json` y verificar que el servicio lo recarga (o reiniciarlo).
2. Enviar un correo desde la aplicación antigua al puerto SMTP local y verificar llegada.

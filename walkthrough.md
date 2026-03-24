# Walkthrough: Puente SMTP a Microsoft 365 (C# .NET 8)

He implementado un servicio de Windows en C# que actúa como un relé SMTP local para redirigir correos a Microsoft 365 usando OAuth2.

## Estructura del Proyecto
- [GraphSmtpBridge.csproj](file:///c:/Users/antonio.COF/antigravity/GraphSmtpBridge/GraphSmtpBridge.csproj): Configuración de .NET 8 y paquetes.
- [appsettings.json](file:///c:/Users/antonio.COF/antigravity/GraphSmtpBridge/appsettings.json): **Aquí se editan los secretos (OAuth) y el puerto de red.**
- [GraphClient.cs](file:///c:/Users/antonio.COF/antigravity/GraphSmtpBridge/GraphClient.cs): Lógica de autenticación con Azure y envío via Graph API.
- [SmtpMessageHandler.cs](file:///c:/Users/antonio.COF/antigravity/GraphSmtpBridge/SmtpMessageHandler.cs): Interceptor que recibe el correo SMTP.

## Pasos para Compilar e Instalar

### 1. Compilación
Abre una terminal en la carpeta del proyecto y ejecuta:
```bash
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```
Esto generará los ejecutables listos en la carpeta `publish`.

### 2. Actualización (Desinstalar Anterior)
Si ya tienes una versión instalada, primero debes detenerla y eliminarla desde una terminal con **permisos de Administrador**:
```powershell
sc.exe stop "GescofSmtpBridge"
sc.exe delete "GescofSmtpBridge"
```
*Espera un par de segundos asegurando que el proceso se cerró antes de copiar los nuevos archivos.*

### 3. Instalación como Servicio
Desde una terminal con **permisos de Administrador**:
```powershell
# Crear el servicio
sc.exe create "GescofSmtpBridge" binpath= "c:\Users\antonio.COF\antigravity\GraphSmtpBridge\publish\GraphSmtpBridge.exe" start= auto

# Iniciar el servicio
sc.exe start "GescofSmtpBridge"
```

### 4. Apertura de Firewall
Si el servicio está en otra máquina, debes abrir el puerto configurado (ej. 2525):
```powershell
New-NetFirewallRule -DisplayName "SMTP Bridge 2525" -Direction Inbound -LocalPort 2525 -Protocol TCP -Action Allow
```

## Pruebas
Configura tu aplicación original para apuntar a:
- **Host:** IP de la máquina donde instalaste el servicio.
- **Puerto:** `2525` (o el que definas en `appsettings.json`).
- **Autenticación:** Desactivada (Anonymous).

> [!IMPORTANT]
> Puedes actualizar el `ClientSecret` editando directamente el archivo `appsettings.json` en la carpeta de instalación y reiniciando el servicio (`sc.exe stop GescofSmtpBridge` y `sc.exe start GescofSmtpBridge`).

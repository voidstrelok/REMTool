# REMTool Administracion local

`RemTool.Admin.exe` inicia una instancia temporal en `127.0.0.1` y abre el navegador. No publica puertos de red ni modifica la API publica.

## Configuracion del operador

Copie `appsettings.example.json` a `%LOCALAPPDATA%\REMTool\adminsettings.json` y complete la conexion PostgreSQL y las carpetas locales. `PublicationsDirectory` contiene los consolidados y `WorkingDirectory` recibe archivos temporales.

## Desarrollo

Ejecute `dotnet run --project .\admin\RemTool.Admin.csproj` (o F5 en Visual Studio). Cada build compila `admin-ui` y lo copia a `admin/wwwroot` automaticamente, solo si cambio algo en `admin-ui`. Requiere Node.js. Para omitirlo: `dotnet build -p:SkipAdminUi=true`.

## Publicacion Windows

El proyecto `admin` instala y compila la UI React automaticamente durante `dotnet publish`:

```powershell
dotnet publish admin\RemTool.Admin.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\dist\REMTool.Admin
```

Entregue el contenido de `dist\REMTool.Admin` junto a este instructivo. La configuracion queda fuera de la instalacion, por lo que una actualizacion no sobrescribe credenciales ni rutas locales.

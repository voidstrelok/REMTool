using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RemTool.DTO;
using RemTool.Shared;

namespace RemTool.Services;

public sealed class ConstruirRemException : Exception
{
    public ConstruirRemException(string message) : base(message) { }
    public ConstruirRemException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class ConstruirRemService
{
    private readonly RemToolDataContext _db;
    private readonly IRemAnalyzer _analyzer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConstruirRemService> _logger;

    public ConstruirRemService(
        RemToolDataContext db,
        IRemAnalyzer analyzer,
        IConfiguration configuration,
        ILogger<ConstruirRemService> logger)
    {
        _db = db;
        _analyzer = analyzer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ParteConstruirRemDTO> ExtraerParteAsync(
        Stream archivo,
        string nombre,
        string? versionEsperada,
        string nombreArchivo,
        CancellationToken cancellationToken)
    {
        if (archivo is null || !archivo.CanRead)
            throw new ConstruirRemException("No se pudo leer el archivo enviado.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ConstruirRemException("El nombre identificador de la parte es requerido.");
        if (nombre.Trim().Length > 120)
            throw new ConstruirRemException("El nombre identificador no puede superar los 120 caracteres.");

        if (archivo.CanSeek)
            archivo.Position = 0;

        try
        {
            using var workbook = new ExcelPackage(archivo);
            var hojaNombre = workbook.Workbook.Worksheets["NOMBRE"]
                ?? throw new ConstruirRemException("No se encontró la hoja 'NOMBRE'.");

            var version = TextoCelda(hojaNombre, "A9").Trim();
            if (string.IsNullOrWhiteSpace(version))
                throw new ConstruirRemException("No se encontró la versión en la celda A9.");

            if (!string.IsNullOrWhiteSpace(versionEsperada)
                && !string.Equals(version.Trim(), versionEsperada.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ConstruirRemException(
                    $"La parte corresponde a la versión '{version}', pero el armado utiliza la versión '{versionEsperada}'.");
            }

            var versionRem = await _db.VersionRem
                .AsNoTracking()
                .Include(v => v.SerieRem)
                .FirstOrDefaultAsync(v => v.Nombre == version, cancellationToken);

            if (versionRem is null)
                throw new ConstruirRemException($"No se encontró la versión '{version}' en la base de datos.");

            var serie = versionRem.SerieRem?.Nombre ?? string.Empty;
            if (!string.Equals(serie, "A", StringComparison.OrdinalIgnoreCase))
                throw new ConstruirRemException($"La versión '{version}' no corresponde a la Serie A.");

            var ultimaVersion = await ObtenerUltimaVersionSerieAAsync(cancellationToken);
            if (!string.Equals(versionRem.Nombre, ultimaVersion.Nombre, StringComparison.OrdinalIgnoreCase))
            {
                throw new ConstruirRemException(
                    $"La parte utiliza la versión '{versionRem.Nombre}'. La última versión disponible de la Serie A es '{ultimaVersion.Nombre}'.");
            }

            var prestaciones = await _db.Prestacion
                .AsNoTracking()
                .Include(p => p.HojaRem)
                .Include(p => p.Seccion)
                    .ThenInclude(s => s!.VersionHoja)
                .Where(p => p.id_version == versionRem.Id)
                .OrderBy(p => p.Seccion != null && p.Seccion.VersionHoja != null ? p.Seccion.VersionHoja.Orden : int.MaxValue)
                .ThenBy(p => p.Seccion != null ? p.Seccion.Orden : int.MaxValue)
                .ThenBy(p => p.Orden)
                .ToListAsync(cancellationToken);

            var datos = new List<PrestacionDatosDTO>();
            var secciones = new Dictionary<string, SeccionConstruirRemDTO>(StringComparer.OrdinalIgnoreCase);
            foreach (var prestacion in prestaciones)
            {
                var hoja = workbook.Workbook.Worksheets[prestacion.HojaRem.Nombre];
                if (hoja is null || prestacion.Coordenada.Count == 0)
                    continue;

                var valores = prestacion.Coordenada
                    .Select(coordenada => TextoCelda(hoja, coordenada))
                    .Select(valor => string.IsNullOrWhiteSpace(valor) ? "0" : valor)
                    .ToList();

                if (!valores.Any(EsValorConDatos))
                    continue;

                datos.Add(new PrestacionDatosDTO
                {
                    Prestacion = prestacion.CodigoPrestacion,
                    Valores = valores
                });

                var seccion = prestacion.Seccion;
                var sectionCode = seccion?.Codigo ?? $"{prestacion.HojaRem.Nombre}:sin-seccion";
                if (!secciones.TryGetValue(sectionCode, out var sectionDto))
                {
                    sectionDto = new SeccionConstruirRemDTO
                    {
                        Codigo = sectionCode,
                        Nombre = seccion?.Nombre ?? prestacion.HojaRem.Titulo,
                        Hoja = prestacion.HojaRem.Titulo == ""
                            ? prestacion.HojaRem.Nombre
                            : prestacion.HojaRem.Titulo,
                        HojaCodigo = prestacion.HojaRem.Nombre,
                        HojaOrden = seccion?.VersionHoja?.Orden ?? int.MaxValue,
                        Orden = seccion?.Orden ?? prestacion.Orden
                    };
                    secciones[sectionCode] = sectionDto;
                }

                sectionDto.Prestaciones.Add(new PrestacionSeccionDTO
                {
                    Codigo = prestacion.CodigoPrestacion,
                    Nombre = prestacion.Nombre,
                    Orden = prestacion.Orden,
                    CantidadValores = valores.Count
                });
            }

            return new ParteConstruirRemDTO
            {
                Id = Guid.NewGuid().ToString("N"),
                Nombre = nombre.Trim(),
                NombreArchivo = nombreArchivo,
                FechaSubida = DateTime.UtcNow,
                Serie = serie,
                Version = version,
                Secciones = secciones.Values
                    .OrderBy(s => s.HojaOrden)
                    .ThenBy(s => s.Orden)
                    .ThenBy(s => s.Codigo)
                    .ToList(),
                Datos = datos
            };
        }
        catch (ConstruirRemException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extrayendo una parte de ConstruirREM.");
            throw new ConstruirRemException($"Error al procesar la parte: {ex.Message}", ex);
        }
    }

    public async Task<ConstruirRemRevisionDTO> RevisarAsync(
        ConstruirRemRequest request,
        CancellationToken cancellationToken)
    {
        var bytes = await ConstruirArchivoAsync(request, cancellationToken);
        await using var stream = new MemoryStream(bytes);

        var version = await ObtenerVersionSerieAAsync(request.Version, cancellationToken);
        var analisis = await _analyzer.AnalizarAsync(
            stream,
            new RemAnalysisExpectations
            {
                Serie = "A",
                Version = request.Version,
                Mes = request.Mes,
                Año = version.Fecha.Year,
                CodDeis = request.CodDeis
            },
            cancellationToken);

        return new ConstruirRemRevisionDTO
        {
            Analisis = analisis,
            Secciones = await ConstruirVistaSeccionesAsync(bytes, version.Id, cancellationToken)
        };
    }

    private async Task<List<SeccionConstruirRemVistaDTO>> ConstruirVistaSeccionesAsync(
        byte[] archivo,
        int versionId,
        CancellationToken cancellationToken)
    {
        var secciones = await _db.SeccionRem
            .AsNoTracking()
            .Include(s => s.VersionHoja)
                .ThenInclude(vh => vh!.HojaRem)
            .Include(s => s.Filas)
                .ThenInclude(fila => fila.Celdas)
            .Where(s => s.VersionHoja != null && s.VersionHoja.IdVersion == versionId)
            .OrderBy(s => s.VersionHoja!.Orden)
            .ThenBy(s => s.Orden)
            .ToListAsync(cancellationToken);

        using var package = new ExcelPackage(new MemoryStream(archivo));
        var result = new List<SeccionConstruirRemVistaDTO>(secciones.Count);
        foreach (var seccion in secciones)
        {
            var hojaRem = seccion.VersionHoja!.HojaRem;
            var hoja = package.Workbook.Worksheets[hojaRem.Nombre];
            if (hoja is null)
                continue;

            var valores = seccion.Filas
                .OrderBy(fila => fila.Orden)
                .SelectMany(fila => fila.Celdas.OrderBy(celda => celda.ColumnaOrigen))
                .Where(celda => !string.IsNullOrWhiteSpace(celda.CeldaBase))
                .GroupBy(celda => celda.CeldaBase, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => hoja.Cells[grupo.Key].Value?.ToString() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);

            result.Add(new SeccionConstruirRemVistaDTO
            {
                Id = seccion.Id,
                Hoja = hojaRem.Titulo == "" ? hojaRem.Nombre : hojaRem.Titulo,
                HojaCodigo = hojaRem.Nombre,
                HojaOrden = seccion.VersionHoja.Orden,
                Seccion = seccion.Nombre,
                Codigo = seccion.Codigo,
                Orden = seccion.Orden,
                Html = RenderizarSeccion(seccion),
                Valores = valores
            });
        }

        return result;
    }

    public async Task<byte[]> ExportarAsync(
        ConstruirRemRequest request,
        CancellationToken cancellationToken) =>
        await ConstruirArchivoAsync(request, cancellationToken);

    private async Task<byte[]> ConstruirArchivoAsync(
        ConstruirRemRequest request,
        CancellationToken cancellationToken)
    {
        ValidarRequest(request);
        var version = await ObtenerVersionSerieAAsync(request.Version, cancellationToken);
        var establecimiento = await _db.Establecimiento
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CodDeis == request.CodDeis, cancellationToken);

        if (establecimiento is null)
            throw new ConstruirRemException($"No se encontró el establecimiento con CodDEIS '{request.CodDeis}'.");

        var basePath = ObtenerRutaBase();
        if (string.IsNullOrWhiteSpace(basePath) || !File.Exists(basePath))
            throw new ConstruirRemException("La plantilla base de Serie A no está configurada o no existe.");

        using var package = new ExcelPackage(new FileInfo(basePath));
        var hojaNombre = package.Workbook.Worksheets["NOMBRE"]
            ?? throw new ConstruirRemException("La plantilla base no contiene la hoja 'NOMBRE'.");
        var versionBase = TextoCelda(hojaNombre, "A9");
        if (!string.Equals(versionBase.Trim(), request.Version.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConstruirRemException(
                $"La plantilla base corresponde a la versión '{versionBase}', pero el armado utiliza '{request.Version}'.");
        }

        CompletarEncabezado(hojaNombre, establecimiento, request.CodDeis, request.Mes);

        var prestaciones = await _db.Prestacion
            .AsNoTracking()
            .Include(p => p.HojaRem)
            .Where(p => p.id_version == version.Id)
            .ToDictionaryAsync(p => p.CodigoPrestacion, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var consolidados = Consolidar(request.Partes, prestaciones);
        foreach (var (codigo, valores) in consolidados)
        {
            var prestacion = prestaciones[codigo];
            var hoja = package.Workbook.Worksheets[prestacion.HojaRem.Nombre]
                ?? throw new ConstruirRemException($"No se encontró la hoja '{prestacion.HojaRem.Nombre}' para '{codigo}'.");

            for (var index = 0; index < valores.Count; index++)
            {
                var celda = hoja.Cells[prestacion.Coordenada[index]];
                if (!string.IsNullOrWhiteSpace(celda.Formula) || celda.Style.Locked)
                    continue;

                var baseValue = TryParseRemNumber(celda.Value?.ToString(), out var parsedBase)
                    ? parsedBase
                    : 0m;
                celda.Value = baseValue + valores[index];
            }
        }

        package.Workbook.Calculate();
        using var output = new MemoryStream();
        package.SaveAs(output);
        return output.ToArray();
    }

    private async Task<VersionRem> ObtenerVersionSerieAAsync(string versionNombre, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(versionNombre))
            throw new ConstruirRemException("La versión del armado es requerida.");

        var version = await _db.VersionRem
            .AsNoTracking()
            .Include(v => v.SerieRem)
            .FirstOrDefaultAsync(v => v.Nombre == versionNombre, cancellationToken);
        if (version is null)
            throw new ConstruirRemException($"No se encontró la versión '{versionNombre}' en la base de datos.");
        if (!string.Equals(version.SerieRem?.Nombre, "A", StringComparison.OrdinalIgnoreCase))
            throw new ConstruirRemException($"La versión '{versionNombre}' no corresponde a la Serie A.");

        var ultimaVersion = await ObtenerUltimaVersionSerieAAsync(cancellationToken);
        if (!string.Equals(version.Nombre, ultimaVersion.Nombre, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConstruirRemException(
                $"El armado utiliza la versión '{version.Nombre}', pero la última versión disponible de la Serie A es '{ultimaVersion.Nombre}'. Inicie un nuevo armado.");
        }

        return version;
    }

    public async Task<VersionRem> ObtenerUltimaVersionSerieAAsync(CancellationToken cancellationToken)
    {
        var version = await _db.VersionRem
            .AsNoTracking()
            .Include(v => v.SerieRem)
            .Where(v => v.SerieRem != null && v.SerieRem.Nombre == "A")
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return version ?? throw new ConstruirRemException("No hay versiones disponibles para la Serie A.");
    }

    private static Dictionary<string, List<decimal>> Consolidar(
        IEnumerable<ParteConstruirRemPayload> partes,
        IReadOnlyDictionary<string, Prestacion> prestaciones)
    {
        var result = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var parte in partes)
        {
            foreach (var dato in parte.Datos ?? [])
            {
                if (string.IsNullOrWhiteSpace(dato.Prestacion))
                    throw new ConstruirRemException("Una parte contiene una prestación sin código.");
                if (!prestaciones.TryGetValue(dato.Prestacion, out var prestacion))
                    throw new ConstruirRemException($"La prestación '{dato.Prestacion}' no existe en la versión seleccionada.");
                var incomingValues = dato.Valores ?? [];
                if (incomingValues.Count > prestacion.Coordenada.Count)
                {
                    throw new ConstruirRemException(
                        $"La prestación '{dato.Prestacion}' contiene más valores que la plantilla base.");
                }

                if (!result.TryGetValue(dato.Prestacion, out var values))
                {
                    values = Enumerable.Repeat(0m, prestacion.Coordenada.Count).ToList();
                    result[dato.Prestacion] = values;
                }

                for (var index = 0; index < incomingValues.Count; index++)
                {
                    if (!TryParseRemNumber(incomingValues[index], out var value))
                        throw new ConstruirRemException(
                            $"No se pudo interpretar el valor de '{dato.Prestacion}' en la columna {index + 1}.");
                    values[index] += value;
                }
            }
        }

        return result;
    }

    private static string RenderizarSeccion(SeccionRem seccion)
    {
        var html = new StringBuilder();
        html.Append("<table class=\"rem-seccion\">");

        foreach (var fila in seccion.Filas.OrderBy(fila => fila.Orden))
        {
            html.Append("<tr data-fila=\"")
                .Append(fila.FilaOrigen)
                .Append("\" class=\"fila-")
                .Append(WebUtility.HtmlEncode(fila.TipoFila.ToLowerInvariant()))
                .Append("\">");

            foreach (var celda in fila.Celdas.OrderBy(celda => celda.ColumnaOrigen))
            {
                html.Append("<td data-celda=\"")
                    .Append(WebUtility.HtmlEncode(celda.CeldaBase))
                    .Append("\"");
                if (celda.RowSpan > 1)
                    html.Append(" rowspan=\"").Append(celda.RowSpan).Append("\"");
                if (celda.ColSpan > 1)
                    html.Append(" colspan=\"").Append(celda.ColSpan).Append("\"");
                if (celda.EsTotal)
                    html.Append(" data-tipo=\"total\"");

                html.Append(" class=\"")
                    .Append(celda.EsTotal ? "total" : celda.EsEntradaPrestacion ? "prestacion" : celda.EsEditable ? "editable" : "estructura")
                    .Append("\">")
                    .Append(celda.EsTotal ? string.Empty : WebUtility.HtmlEncode(celda.Valor))
                    .Append("</td>");
            }

            html.Append("</tr>");
        }

        return html.Append("</table>").ToString();
    }

    private static void ValidarRequest(ConstruirRemRequest request)
    {
        if (request is null)
            throw new ConstruirRemException("La solicitud es requerida.");
        if (request.Mes is < 1 or > 12)
            throw new ConstruirRemException("El mes debe estar entre 1 y 12.");
        if (string.IsNullOrWhiteSpace(request.CodDeis)
            || request.CodDeis.Length != 6
            || !request.CodDeis.All(char.IsDigit))
            throw new ConstruirRemException("El CodDEIS debe contener exactamente 6 dígitos.");
        if (request.Partes is null || request.Partes.Count == 0)
            throw new ConstruirRemException("Debe incluir al menos una parte.");
    }

    private string? ObtenerRutaBase() =>
        Environment.GetEnvironmentVariable("Paths__BaseSA")
        ?? _configuration["Paths:BaseSA"];

    private static void CompletarEncabezado(ExcelWorksheet hoja, Establecimiento establecimiento, string codDeis, int mes)
    {
        hoja.Cells["B3"].Value = establecimiento.Nombre;
        for (var index = 0; index < 6; index++)
            hoja.Cells[3, 3 + index].Value = codDeis[index] - '0';

        var mesTexto = new DateTime(2000, mes, 1)
            .ToString("MMMM", CultureInfo.GetCultureInfo("es-CL"))
            .ToUpper(CultureInfo.GetCultureInfo("es-CL"));
        hoja.Cells["B6"].Value = mesTexto;
        hoja.Cells["C6"].Value = mes / 10;
        hoja.Cells["D6"].Value = mes % 10;
        if (!string.IsNullOrWhiteSpace(establecimiento.Director))
            hoja.Cells["B11"].Value = establecimiento.Director;
    }

    private static bool EsValorConDatos(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim() != "0";

    private static string TextoCelda(ExcelWorksheet hoja, string coordenada) =>
        hoja.Cells[coordenada].Value?.ToString()?.Trim() ?? string.Empty;

    private static bool TryParseRemNumber(string? raw, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = 0m;
            return true;
        }

        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("es-CL"), out value);
    }
}

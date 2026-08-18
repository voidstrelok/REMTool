using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NCalc;
using OfficeOpenXml;
using RemTool.Shared;
using RemTool.Util;

namespace RemTool.Services;

public sealed class RemAnalysisExpectations
{
    public string? Serie { get; init; }
    public string? Version { get; init; }
    public bool SoloValidarSerieYVersion { get; init; }
    public int? Mes { get; init; }
    public int? Año { get; init; }
    public string? CodDeis { get; init; }
}

public sealed class RemAnalysisException : Exception
{
    public RemAnalysisException(string message) : base(message) { }
    public RemAnalysisException(string message, Exception innerException) : base(message, innerException) { }
}

public interface IRemAnalyzer
{
    Task<AnalisisRemDTO> AnalizarAsync(
        Stream archivo,
        RemAnalysisExpectations? expectativas = null,
        CancellationToken cancellationToken = default);
}

public sealed class RemAnalyzerService : IRemAnalyzer
{
    private readonly RemToolDataContext _db;
    private readonly ILogger<RemAnalyzerService> _logger;

    public RemAnalyzerService(RemToolDataContext db, ILogger<RemAnalyzerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AnalisisRemDTO> AnalizarAsync(
        Stream archivo,
        RemAnalysisExpectations? expectativas = null,
        CancellationToken cancellationToken = default)
    {
        if (archivo is null || !archivo.CanRead)
            throw new RemAnalysisException("No se pudo leer el archivo enviado.");

        if (archivo.CanSeek)
            archivo.Position = 0;

        try
        {
            var soloValidarSerieYVersion = expectativas?.SoloValidarSerieYVersion == true;
            using var workbook = new ExcelPackage(archivo);
            var hojaNombre = workbook.Workbook.Worksheets["NOMBRE"];
            if (hojaNombre is null)
                throw new RemAnalysisException("No se encontró la hoja 'NOMBRE'.");

            var codDeis = ConcatenarCeldas(hojaNombre, "C3", "D3", "E3", "F3", "G3", "H3");
            var mesRaw = ConcatenarCeldas(hojaNombre, "C6", "D6");
            var versionArchivo = TextoCelda(hojaNombre, "A9");
            var serieEncabezado = ExtraerSerie(TextoCelda(hojaNombre, "B17"));

            var mesValido = int.TryParse(mesRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mes)
                && mes is >= 1 and <= 12;
            if (!mesValido && !soloValidarSerieYVersion)
            {
                throw new RemAnalysisException($"No se pudo leer un mes válido desde la planilla ('{mesRaw}').");
            }

            if (string.IsNullOrWhiteSpace(versionArchivo))
                throw new RemAnalysisException("No se encontró la versión en la celda A9.");

            Establecimiento? establecimiento = null;
            if (!soloValidarSerieYVersion)
            {
                establecimiento = await _db.Establecimiento
                    .Include(e => e.Sector)
                    .FirstOrDefaultAsync(e => e.CodDeis == codDeis, cancellationToken);
            }

            if (establecimiento is null && !soloValidarSerieYVersion)
                throw new RemAnalysisException($"No se encontró el establecimiento con CodDEIS '{codDeis}'.");

            var versionRem = await _db.VersionRem
                .Include(v => v.SerieRem)
                .FirstOrDefaultAsync(v => v.Nombre == versionArchivo, cancellationToken);

            if (versionRem is null)
                throw new RemAnalysisException($"No se encontró la versión '{versionArchivo}' en la base de datos.");

            var serieVersion = versionRem.SerieRem?.Nombre ?? serieEncabezado;
            var serie = string.IsNullOrWhiteSpace(serieVersion) ? serieEncabezado : serieVersion;
            var año = versionRem.Fecha.Year;

            var result = new AnalisisRemDTO
            {
                CodDeis = codDeis,
                NombreEstablecimiento = establecimiento?.Nombre ?? string.Empty,
                IdSector = establecimiento?.id_sector ?? 0,
                NombreSector = establecimiento?.Sector?.Nombre ?? string.Empty,
                Serie = serie,
                Version = versionArchivo,
                Mes = mes,
                Año = año
            };

            if (!string.IsNullOrWhiteSpace(serieEncabezado)
                && !string.Equals(serieEncabezado, serie, StringComparison.OrdinalIgnoreCase))
            {
                AgregarInconsistencia(result, "serieArchivo", serie, serieEncabezado,
                    $"La serie del encabezado ({serieEncabezado}) no coincide con la serie de la versión ({serie}).");
            }

            CompararExpectativa(result, "serie", expectativas?.Serie, serie,
                value => string.Equals(value, serie, StringComparison.OrdinalIgnoreCase),
                value => $"La planilla corresponde a la serie '{value}', pero se seleccionó '{expectativas?.Serie}'.");
            CompararExpectativa(result, "version", expectativas?.Version, versionArchivo,
                value => string.Equals(value?.Trim(), versionArchivo, StringComparison.OrdinalIgnoreCase),
                value => $"La planilla corresponde a la versión '{value}', pero se seleccionó '{expectativas?.Version}'.");
            if (!soloValidarSerieYVersion)
            {
                CompararExpectativa(result, "mes", expectativas?.Mes?.ToString(CultureInfo.InvariantCulture), mes.ToString(CultureInfo.InvariantCulture),
                    value => int.TryParse(value, out var esperado) && esperado == mes,
                    value => $"La planilla corresponde al mes {mes}, pero se seleccionó el mes {value}.");
                CompararExpectativa(result, "año", expectativas?.Año?.ToString(CultureInfo.InvariantCulture), año.ToString(CultureInfo.InvariantCulture),
                    value => int.TryParse(value, out var esperado) && esperado == año,
                    value => $"La versión de la planilla corresponde al año {año}, pero se seleccionó el año {value}.");
                CompararExpectativa(result, "establecimiento", expectativas?.CodDeis, codDeis,
                    value => string.Equals(value?.Trim(), codDeis, StringComparison.OrdinalIgnoreCase),
                    value => $"La planilla corresponde al establecimiento {codDeis}, pero se seleccionó {value}.");
            }

            var reglas = await _db.Regla
                .Include(r => r.VersionREM)
                .Include(r => r.TipoRegla)
                .Where(r => r.VersionREM.Nombre == versionArchivo)
                .ToListAsync(cancellationToken);

            foreach (var regla in reglas)
            {
                try
                {
                    var expr = Regex.Replace(regla.Expresion, Utils.RegexHoja,
                        match => Utils.ParseaHojas(match.Value, workbook));
                    expr = expr.Replace("[", string.Empty).Replace("]", string.Empty);

                    var evaluado = new Expression(expr).Evaluate();
                    if (evaluado is bool resultado && !resultado)
                    {
                        AgregarResultadoRegla(result, regla);
                    }
                    else if (evaluado is not bool && !Convert.ToBoolean(evaluado, CultureInfo.InvariantCulture))
                    {
                        AgregarResultadoRegla(result, regla);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo evaluar la regla {ReglaId} para la versión {Version}.", regla.Id, versionArchivo);
                    result.Advertencias.Add($"No se pudo evaluar la regla {regla.Id}: {ex.Message}");
                }
            }

            var prestaciones = await _db.Prestacion
                .Include(p => p.HojaRem)
                .Where(p => p.VersionRem.Nombre == versionArchivo && p.IsEnabled)
                .ToListAsync(cancellationToken);

            var hojasAvisadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prestacion in prestaciones)
            {
                var hojaExcel = workbook.Workbook.Worksheets[prestacion.HojaRem.Nombre];
                if (hojaExcel is null)
                {
                    if (hojasAvisadas.Add(prestacion.HojaRem.Nombre))
                        result.Advertencias.Add($"No se encontró la hoja '{prestacion.HojaRem.Nombre}' requerida para extraer datos.");
                    continue;
                }

                var valores = new List<string>();
                var tieneDatos = false;
                foreach (var coordenada in prestacion.Coordenada ?? [])
                {
                    var valor = hojaExcel.Cells[coordenada].Value?.ToString() ?? "0";
                    valores.Add(valor);
                    if (!string.IsNullOrWhiteSpace(valor) && valor != "0")
                        tieneDatos = true;
                }

                if (tieneDatos)
                {
                    result.Datos.Add(new PrestacionDatosDTO
                    {
                        Prestacion = prestacion.CodigoPrestacion,
                        Valores = valores
                    });
                }
            }

            return result;
        }
        catch (RemAnalysisException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando planilla REM.");
            throw new RemAnalysisException($"Error al procesar el archivo: {ex.Message}", ex);
        }
    }

    private static void AgregarResultadoRegla(AnalisisRemDTO result, RemTool.Shared.Regla regla)
    {
        if (string.Equals(regla.TipoRegla?.Nombre, "Advertencia", StringComparison.OrdinalIgnoreCase))
            result.Advertencias.Add(regla.Mensaje);
        else
            result.Errores.Add(regla.Mensaje);
    }

    private static void AgregarInconsistencia(
        AnalisisRemDTO result,
        string campo,
        string esperado,
        string encontrado,
        string mensaje)
    {
        result.IncluidoEnResumen = false;
        result.Advertencias.Add(mensaje);
        result.Validaciones.Add(new ValidacionRemDTO
        {
            Campo = campo,
            Esperado = esperado,
            Encontrado = encontrado,
            Mensaje = mensaje
        });
    }

    private static void CompararExpectativa(
        AnalisisRemDTO result,
        string campo,
        string? esperado,
        string encontrado,
        Func<string?, bool> coincide,
        Func<string?, string> mensaje)
    {
        if (string.IsNullOrWhiteSpace(esperado) || coincide(esperado))
            return;

        AgregarInconsistencia(result, campo, esperado!, encontrado, mensaje(esperado));
    }

    private static string ConcatenarCeldas(ExcelWorksheet hoja, params string[] coordenadas) =>
        string.Concat(coordenadas.Select(coordenada => TextoCelda(hoja, coordenada))).Trim();

    private static string TextoCelda(ExcelWorksheet hoja, string coordenada) =>
        hoja.Cells[coordenada].Value?.ToString()?.Trim() ?? string.Empty;

    private static string ExtraerSerie(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        // Formato actual de NOMBRE!B17:
        //   "SERIE A", "SERIE P", "SERIE D"
        //   "SERIE BM\nEstablecimientos Municipales"
        // \s+ también contempla el salto de línea de la celda BM.
        var serieActual = Regex.Match(
            raw,
            @"\bSERIE\s+([A-Za-z]+)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (serieActual.Success)
            return serieActual.Groups[1].Value.ToUpperInvariant();

        // Compatibilidad con planillas antiguas que usaban "REM A".
        var serieAntigua = Regex.Match(
            raw,
            @"\bREM\s+([A-Za-z]+)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return serieAntigua.Success
            ? serieAntigua.Groups[1].Value.ToUpperInvariant()
            : string.Empty;
    }
}

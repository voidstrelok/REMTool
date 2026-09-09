using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTool.Controllers;

[ApiController]
[Route("api/consultarREM")]
public sealed class ConsultaREMController : ControllerBase
{
    private readonly RemToolDataContext _db;

    public ConsultaREMController(RemToolDataContext db)
    {
        _db = db;
    }

    [HttpGet("opciones")]
    public async Task<IActionResult> Opciones([FromQuery] long serieId, CancellationToken cancellationToken)
    {
        if (serieId <= 0)
            return BadRequest("La serie es requerida.");

        var serie = await _db.SerieRem
            .AsNoTracking()
            .Where(s => s.Id == serieId)
            .Select(s => new { s.Id, s.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (serie is null)
            return NotFound("La serie no existe.");

        var secciones = await _db.SeccionRem
            .AsNoTracking()
            .Where(s => s.VersionHoja != null && s.VersionHoja.VersionRem.id_serie == serieId)
            .Select(s => new SeccionOptionDto
            {
                Id = s.Id,
                HojaId = s.VersionHoja!.IdHoja,
                Hoja = s.VersionHoja.HojaRem.Titulo == ""
                    ? s.VersionHoja.HojaRem.Nombre
                    : s.VersionHoja.HojaRem.Titulo,
                HojaCodigo = s.VersionHoja.HojaRem.Nombre,
                Nombre = s.Nombre,
                Codigo = s.Codigo,
                Orden = s.Orden,
                Version = s.VersionHoja.VersionRem.Nombre,
                VersionFecha = s.VersionHoja.VersionRem.Fecha
            })
            .OrderBy(s => s.Hoja)
            .ThenByDescending(s => s.VersionFecha)
            .ThenBy(s => s.Orden)
            .ToListAsync(cancellationToken);

        var hojas = secciones
            .GroupBy(s => new { s.HojaId, s.Hoja, s.HojaCodigo })
            .Select(group => new HojaOptionDto
            {
                Id = group.Key.HojaId,
                Codigo = group.Key.HojaCodigo,
                Nombre = group.Key.Hoja
            })
            .OrderBy(h => h.Codigo)
            .ThenBy(h => h.Nombre)
            .ToList();

        var periodos = await _db.Reporte
            .AsNoTracking()
            .Where(reporte => reporte.Registros.Any(registro =>
                registro.Prestacion.VersionRem.id_serie == serieId))
            .Select(reporte => new
            {
                Anio = reporte.Año,
                reporte.Mes
            })
            .Distinct()
            .OrderByDescending(periodo => periodo.Anio)
            .ThenBy(periodo => periodo.Mes)
            .ToListAsync(cancellationToken);

        var anios = periodos
            .GroupBy(periodo => periodo.Anio)
            .Select(grupo => new AnioConsultaRemDto
            {
                Anio = grupo.Key,
                Meses = grupo.Select(periodo => periodo.Mes).ToList()
            })
            .OrderByDescending(anio => anio.Anio)
            .ToList();

        return Ok(new OpcionesConsultaRemDto
        {
            Serie = serie,
            Hojas = hojas,
            Secciones = secciones,
            Anios = anios
        });
    }

    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] long serieId,
        [FromQuery] int anio,
        [FromQuery] int[] meses,
        [FromQuery] long[] establecimientoIds,
        [FromQuery] long? sectorId,
        [FromQuery] int? hojaId,
        [FromQuery] long? seccionId,
        CancellationToken cancellationToken)
    {
        if (serieId <= 0)
            return BadRequest("La serie es requerida.");
        if (anio <= 0)
            return BadRequest("El año es requerido.");
        if (meses is null || meses.Length == 0)
            return BadRequest("Debe seleccionar al menos un mes.");
        if (meses.Any(mes => mes is < 1 or > 12))
            return BadRequest("Los meses deben estar entre 1 y 12.");
        if (establecimientoIds is null || establecimientoIds.Length == 0)
            return BadRequest("Debe seleccionar al menos un establecimiento.");
        if (establecimientoIds.Any(id => id <= 0))
            return BadRequest("Los establecimientos seleccionados no son válidos.");
        if (sectorId is <= 0)
            sectorId = null;

        var serie = await _db.SerieRem
            .AsNoTracking()
            .Where(s => s.Id == serieId)
            .Select(s => new { s.Id, s.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (serie is null)
            return NotFound("La serie no existe.");

        var seccionesQuery = _db.SeccionRem
            .AsNoTracking()
            .Where(s => s.VersionHoja != null && s.VersionHoja.VersionRem.id_serie == serieId);

        if (hojaId.HasValue)
            seccionesQuery = seccionesQuery.Where(s => s.VersionHoja!.IdHoja == hojaId.Value);
        if (seccionId.HasValue)
            seccionesQuery = seccionesQuery.Where(s => s.Id == seccionId.Value);

        var secciones = await seccionesQuery
            .Include(s => s.VersionHoja!)
                .ThenInclude(vh => vh.VersionRem)
            .Include(s => s.VersionHoja!)
                .ThenInclude(vh => vh.HojaRem)
            .Include(s => s.Filas)
                .ThenInclude(fila => fila.Celdas)
            .Include(s => s.Prestacions)
                .ThenInclude(prestacion => prestacion.Coordenadas)
            .AsSplitQuery()
            .OrderBy(s => s.VersionHoja!.IdHoja)
            .ThenByDescending(s => s.VersionHoja!.VersionRem.Fecha)
            .ThenBy(s => s.Orden)
            .ToListAsync(cancellationToken);

        var reporteQuery = _db.Reporte
            .AsNoTracking()
            .Where(reporte => reporte.Año == anio)
            .Where(reporte => meses.Contains(reporte.Mes))
            .Where(reporte => establecimientoIds.Contains(reporte.id_establecimiento));

        if (sectorId.HasValue)
            reporteQuery = reporteQuery.Where(reporte => reporte.Establecimiento.id_sector == sectorId.Value);

        reporteQuery = reporteQuery.Where(reporte => reporte.Registros
            .Any(registro => registro.Prestacion.VersionRem.id_serie == serieId));

        var reportes = await reporteQuery
            .OrderByDescending(reporte => reporte.Id)
            .Select(reporte => new
            {
                reporte.Id,
                reporte.Mes,
                reporte.id_establecimiento,
                Establecimiento = reporte.Establecimiento.Nombre
            })
            .ToListAsync(cancellationToken);

        // Si existen varias cargas para el mismo establecimiento y mes,
        // sólo se considera la más reciente, igual que en la consulta individual.
        var reportesSeleccionados = reportes
            .GroupBy(reporte => new { reporte.id_establecimiento, reporte.Mes })
            .Select(group => group.OrderByDescending(reporte => reporte.Id).First())
            .ToList();
        var reporteIds = reportesSeleccionados.Select(reporte => reporte.Id).ToArray();

        var registros = reportesSeleccionados.Count == 0
            ? []
            : await _db.Registro
                .AsNoTracking()
                .Where(registro => reporteIds.Contains(registro.id_reporte)
                    && registro.Prestacion.VersionRem.id_serie == serieId)
                .Select(registro => new RegistroConsultaDto
                {
                    IdPrestacion = registro.id_prestacion,
                    Valores = registro.Valor
                })
                .ToListAsync(cancellationToken);

        var registrosPorPrestacion = ConsolidarRegistros(registros);

        var tablas = secciones
            .Select(seccion => ConstruirTabla(seccion, registrosPorPrestacion, reportesSeleccionados.Count > 0))
            .ToList();

        return Ok(new ConsultaRemResponseDto
        {
            Serie = serie,
            Anio = anio,
            Meses = meses.Distinct().OrderBy(mes => mes).ToList(),
            Establecimientos = reportesSeleccionados
                .Select(reporte => reporte.Establecimiento)
                .Distinct()
                .OrderBy(nombre => nombre)
                .ToList(),
            ReportesEncontrados = reportesSeleccionados.Count,
            ReporteEncontrado = reportesSeleccionados.Count > 0,
            Tablas = tablas
        });
    }

    private static Dictionary<int, RegistroConsultaDto> ConsolidarRegistros(
        IEnumerable<RegistroConsultaDto> registros)
    {
        return registros
            .GroupBy(registro => registro.IdPrestacion)
            .ToDictionary(
                group => group.Key,
                group => new RegistroConsultaDto
                {
                    IdPrestacion = group.Key,
                    Valores = Enumerable.Range(0, group.Max(registro => registro.Valores.Count))
                        .Select(indice => group.Sum(registro => registro.Valores.ElementAtOrDefault(indice)))
                        .ToList()
                });
    }

    private static TablaConsultaDto ConstruirTabla(
        SeccionRem seccion,
        IReadOnlyDictionary<int, RegistroConsultaDto> registros,
        bool tieneReporte)
    {
        var valoresNumericos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (tieneReporte)
        {
            foreach (var prestacion in seccion.Prestacions)
            {
                registros.TryGetValue(prestacion.Id, out var registro);
                var coordenadas = prestacion.Coordenadas
                    .OrderBy(coordenada => coordenada.Orden)
                    .ToList();

                if (coordenadas.Count > 0)
                {
                    for (var index = 0; index < coordenadas.Count; index++)
                    {
                        var valor = registro?.Valores.ElementAtOrDefault(index) ?? 0;
                        valoresNumericos[coordenadas[index].CeldaBase] = valor;
                    }
                }
                else
                {
                    for (var index = 0; index < prestacion.Coordenada.Count; index++)
                    {
                        var valor = registro?.Valores.ElementAtOrDefault(index) ?? 0;
                        valoresNumericos[prestacion.Coordenada[index]] = valor;
                    }
                }
            }

            CalcularTotales(seccion, valoresNumericos);
        }

        // Se renderiza desde las entidades para que el HTML siempre refleje
        // los metadatos actuales de las celdas, incluyendo los totales. El
        // HTML persistido se conserva como histórico, pero no debe reintroducir
        // los valores cacheados de la planilla base.
        var html = RenderizarSeccion(seccion);

        return new TablaConsultaDto
        {
            SeccionId = seccion.Id,
            HojaId = seccion.VersionHoja?.IdHoja ?? (int)seccion.IdHojaRem,
            Hoja = seccion.VersionHoja?.HojaRem.Titulo == ""
                ? seccion.VersionHoja.HojaRem.Nombre
                : seccion.VersionHoja?.HojaRem.Titulo ?? string.Empty,
            HojaCodigo = seccion.VersionHoja?.HojaRem.Nombre ?? string.Empty,
            Seccion = seccion.Nombre,
            Codigo = seccion.Codigo,
            Version = seccion.VersionHoja?.VersionRem.Nombre ?? string.Empty,
            Html = html,
            Valores = valoresNumericos.ToDictionary(
                item => item.Key,
                item => item.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                StringComparer.OrdinalIgnoreCase)
        };
    }

    private static void CalcularTotales(
        SeccionRem seccion,
        IDictionary<string, int> valores)
    {
        var celdas = seccion.Filas
            .SelectMany(fila => fila.Celdas)
            .GroupBy(celda => celda.CeldaBase, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.First(),
                StringComparer.OrdinalIgnoreCase);
        var estados = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        foreach (var celda in celdas.Values.Where(celda => celda.EsTotal))
            EvaluarTotal(celda.CeldaBase, celdas, valores, estados);
    }

    private static int EvaluarTotal(
        string direccion,
        IReadOnlyDictionary<string, CeldaSeccionRem> celdas,
        IDictionary<string, int> valores,
        IDictionary<string, byte> estados)
    {
        if (valores.TryGetValue(direccion, out var valorExistente))
            return valorExistente;

        if (!celdas.TryGetValue(direccion, out var celda)
            || !celda.EsTotal
            || !celda.OperacionTotal.Equals("SUMA", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (estados.TryGetValue(direccion, out var estado))
        {
            // Una referencia circular no debe bloquear toda la consulta.
            if (estado == 1)
                return 0;
            if (estado == 2 && valores.TryGetValue(direccion, out var valorCalculado))
                return valorCalculado;
        }

        estados[direccion] = 1;
        long total = 0;
        foreach (var dependencia in celda.DependenciasTotal
                     .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            total += EvaluarTotal(dependencia, celdas, valores, estados);
        }

        var resultado = total > int.MaxValue
            ? int.MaxValue
            : total < int.MinValue
                ? int.MinValue
                : (int)total;
        valores[direccion] = resultado;
        estados[direccion] = 2;
        return resultado;
    }

    private static string RenderizarSeccion(SeccionRem seccion)
    {
        var html = new System.Text.StringBuilder();
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

    private sealed class RegistroConsultaDto
    {
        public int IdPrestacion { get; init; }
        public List<int> Valores { get; init; } = [];
    }

    public sealed class OpcionesConsultaRemDto
    {
        public object Serie { get; init; } = null!;
        public List<HojaOptionDto> Hojas { get; init; } = [];
        public List<SeccionOptionDto> Secciones { get; init; } = [];
        public List<AnioConsultaRemDto> Anios { get; init; } = [];
    }

    public sealed class AnioConsultaRemDto
    {
        public int Anio { get; init; }
        public List<int> Meses { get; init; } = [];
    }

    public sealed class HojaOptionDto
    {
        public int Id { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
    }

    public sealed class SeccionOptionDto
    {
        public long Id { get; init; }
        public int HojaId { get; init; }
        public string Hoja { get; init; } = string.Empty;
        public string HojaCodigo { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public string Codigo { get; init; } = string.Empty;
        public int Orden { get; init; }
        public string Version { get; init; } = string.Empty;
        public DateOnly VersionFecha { get; init; }
    }

    public sealed class ConsultaRemResponseDto
    {
        public object Serie { get; init; } = null!;
        public int Anio { get; init; }
        public List<int> Meses { get; init; } = [];
        public List<string> Establecimientos { get; init; } = [];
        public int ReportesEncontrados { get; init; }
        public bool ReporteEncontrado { get; init; }
        public List<TablaConsultaDto> Tablas { get; init; } = [];
    }

    public sealed class TablaConsultaDto
    {
        public long SeccionId { get; init; }
        public int HojaId { get; init; }
        public string Hoja { get; init; } = string.Empty;
        public string HojaCodigo { get; init; } = string.Empty;
        public string Seccion { get; init; } = string.Empty;
        public string Codigo { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public string Html { get; init; } = string.Empty;
        public Dictionary<string, string> Valores { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

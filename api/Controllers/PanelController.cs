using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCalc;
using RemTool.Services;
using RemTool.Shared;

namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class PanelController : ControllerBase
    {
        private readonly RemToolDataContext _db;
        private readonly IRemAnalyzer _analyzer;

        public PanelController(RemToolDataContext context, IRemAnalyzer analyzer)
        {
            _db = context;
            _analyzer = analyzer;
        }

        [HttpGet("GetSeries/")]
        public async Task<IActionResult> GetSeries(CancellationToken cancellationToken)
        {
            var series = await _db.SerieRem
                .OrderBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Nombre })
                .ToListAsync(cancellationToken);
            return Ok(series);
        }

        [HttpPost("AnalizarREM/")]
        public async Task<IActionResult> AnalizarREM(
            [FromForm] IFormFile? archivo,
            [FromForm] string? serieEsperada,
            [FromForm] string? versionEsperada,
            [FromForm] bool esComplementaria,
            [FromForm] int? mesEsperado,
            [FromForm(Name = "añoEsperado")] int? añoEsperado,
            [FromForm] string? codDeisEsperado,
            CancellationToken cancellationToken)
        {
            if (archivo is null || archivo.Length == 0)
                return BadRequest("No se ha enviado ningún archivo.");

            if (!string.Equals(Path.GetExtension(archivo.FileName), ".xlsm", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Sólo se permiten archivos con extensión .xlsm.");

            await using var stream = archivo.OpenReadStream();
            try
            {
                var resultado = await _analyzer.AnalizarAsync(
                    stream,
                    new RemAnalysisExpectations
                    {
                        Serie = serieEsperada,
                        Version = versionEsperada,
                        SoloValidarSerieYVersion = esComplementaria,
                        Mes = mesEsperado,
                        Año = añoEsperado,
                        CodDeis = codDeisEsperado
                    },
                    cancellationToken);

                return Ok(resultado);
            }
            catch (RemAnalysisException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetPuntosResumen/{serieNombre}")]
        public async Task<IActionResult> GetPuntosResumen(string serieNombre, CancellationToken cancellationToken)
        {
            var serie = await _db.SerieRem
                .Include(s => s.PuntosResumen)
                .FirstOrDefaultAsync(s => s.Nombre == serieNombre, cancellationToken);

            if (serie is null)
                return NotFound($"Serie '{serieNombre}' no encontrada.");

            var result = serie.PuntosResumen
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .Select(p => new { p.Id, p.Nombre, p.Categoria, p.Expresion })
                .ToList();

            return Ok(result);
        }

        [HttpPost("ComputarResumen/")]
        public async Task<IActionResult> ComputarResumen(
            [FromBody] ComputarResumenRequest? request,
            CancellationToken cancellationToken)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.SerieNombre))
                return BadRequest("SerieNombre es requerido.");

            if (request.Mes is < 1 or > 12)
                return BadRequest("Mes debe estar entre 1 y 12.");

            var serie = await _db.SerieRem
                .Include(s => s.PuntosResumen)
                .FirstOrDefaultAsync(s => s.Nombre == request.SerieNombre, cancellationToken);

            if (serie is null)
                return NotFound($"Serie '{request.SerieNombre}' no encontrada.");

            var entries = request.Entries ?? [];
            var entriesConsideradas = entries.Where(e => e.IncluidoEnResumen).ToList();
            var response = new ComputarResumenResponseDTO
            {
                Serie = serie.Nombre,
                Mes = request.Mes,
                Año = request.Año,
                Cobertura = new ResumenCoberturaDTO
                {
                    PlanillasRecibidas = entries.Count,
                    PlanillasConsideradas = entriesConsideradas.Count,
                    PlanillasExcluidas = entries.Count(e => !e.IncluidoEnResumen),
                    PlanillasConErrores = entries.Count(e => e.TieneErrores),
                    PlanillasConAdvertencias = entries.Count(e => e.TieneAdvertencias)
                }
            };

            var tokenRegex = new Regex(@"\[([^\]]+)\]\[(\d+)\]");
            foreach (var punto in serie.PuntosResumen.OrderBy(p => p.Categoria).ThenBy(p => p.Nombre))
            {
                decimal total = 0m;
                foreach (var entry in entriesConsideradas)
                {
                    var expression = tokenRegex.Replace(punto.Expresion, match =>
                    {
                        var prestacion = match.Groups[1].Value;
                        if (!int.TryParse(match.Groups[2].Value, out var index) || index < 1)
                            return "0";

                        var dato = entry.Datos?.FirstOrDefault(d => d.Prestacion == prestacion);
                        if (dato?.Valores is null || dato.Valores.Count < index)
                            return "0";

                        return ParseDecimal(dato.Valores[index - 1])
                            .ToString(CultureInfo.InvariantCulture);
                    });

                    try
                    {
                        var evaluated = new Expression(expression).Evaluate();
                        total += Convert.ToDecimal(evaluated, CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        response.ErroresCalculo.Add(
                            $"No se pudo calcular el punto resumen '{punto.Nombre}' para {entry.CodDeis}: {ex.Message}");
                    }
                }

                response.Puntos.Add(new PuntoResumenResultadoDTO
                {
                    Nombre = punto.Nombre,
                    Categoria = punto.Categoria,
                    Valor = total,
                    PlanillasConsideradas = entriesConsideradas.Count
                });
            }

            return Ok(response);
        }

        private static decimal ParseDecimal(string? raw)
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant))
                return invariant;

            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("es-CL"), out var local)
                ? local
                : 0m;
        }
    }
}

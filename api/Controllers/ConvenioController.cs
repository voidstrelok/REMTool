using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.Enum;

namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class ConvenioController : ControllerBase
    {
        private readonly VoidDataContext db;
        private List<long> EstablecimientosExcluidos = new List<long> { (long)EnumEstablecimiento.ClinicaDentalMovilMontePatria, (long)EnumEstablecimiento.SARMontePatria, (long)EnumEstablecimiento.SURElPalqui };

        public ConvenioController(VoidDataContext context)
        {
            db = context;
        }

        public class ConvenioResumenDTO
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public int Año { get; set; }
            public float Avance { get; set; }
            public int IndicadorCount { get; set; }
        }

        public class IndicadorResumenDTO
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public int Año { get; set; }
            public long Numerador { get; set; }
            public long Denominador { get; set; }
            public float Meta { get; set; }
            public float Aporte { get; set; }
            public float Actual { get; set; }
            public bool Mensual { get; set; }
            public bool IsTasa { get; set; }
            public bool IsColaborativo { get; set; }
        }

        public class ConvenioDetalleDTO
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public int Año { get; set; }
            public List<IndicadorResumenDTO> Indicadores { get; set; } = new();
        }

        [HttpGet("getConvenios/{ano:int}")]
        public async Task<IActionResult> GetConvenios(int ano)
        {
            var convenios = await db.Convenio
                .Include(c => c.IndicadorConvenios)
                    .ThenInclude(ic => ic.Indicador)
                        .ThenInclude(i => i.ResultadoIndicadors)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var octSepOrden = convenios
                .SelectMany(c => c.IndicadorConvenios)
                .Select(ic => ic.Indicador)
                .Where(i => i.Año == ano && i.EsPeriodoOctubreSep)
                .Select(i => i.Orden)
                .Distinct()
                .ToList();

            var prevDenMap = new Dictionary<int, decimal>();
            if (octSepOrden.Count > 0)
            {
                var prevData = await db.Indicador
                    .Where(i => i.Año == ano - 1 && octSepOrden.Contains(i.Orden))
                    .Select(i => new { i.Orden, Den = i.ResultadoIndicadors.Where(r => r.Mes >= 10).Sum(r => r.Denominador) })
                    .ToListAsync();
                prevDenMap = prevData.ToDictionary(x => x.Orden, x => decimal.Round(x.Den));
            }

            var result = new List<ConvenioResumenDTO>();
            foreach (var convenio in convenios)
            {
                var indicadores = convenio.IndicadorConvenios
                    .Select(ic => ic.Indicador)
                    .Where(i => i.Año == ano)
                    .ToList();

                if (!indicadores.Any())
                {
                    result.Add(new ConvenioResumenDTO
                    {
                        Id = convenio.Id,
                        Nombre = convenio.Nombre,
                        Año = ano,
                        Avance = 0,
                        IndicadorCount = 0
                    });
                    continue;
                }

                float avanceTotal = 0f;
                foreach (var ind in indicadores)
                {
                    var num = decimal.Round(ind.ResultadoIndicadors.Sum(r => r.Numerador));
                    decimal den = ind.EsPeriodoOctubreSep
                        ? decimal.Round(ind.ResultadoIndicadors.Where(r => r.Mes < 10).Sum(r => r.Denominador))
                          + prevDenMap.GetValueOrDefault(ind.Orden, 0m)
                        : ind.IsColaborativo && ind.ResultadoIndicadors.Any()
                            ? decimal.Round(ind.ResultadoIndicadors.Max(r => r.Denominador))
                            : decimal.Round(ind.ResultadoIndicadors.Sum(r => r.Denominador));

                    decimal displayDen = ind.IsDenFijo ? decimal.Round(den * (decimal)ind.Meta) : den;
                    float displayMeta = ind.IsDenFijo ? 1.0f : ind.Meta;

                    var actual = displayDen != 0 ? MathF.Round((float)(num / displayDen), 3) : 0f;
                    float avance = displayMeta > 0 ? Math.Min(actual / displayMeta, 1f) : 0f;
                    avanceTotal += ind.Peso * avance;
                }

                result.Add(new ConvenioResumenDTO
                {
                    Id = convenio.Id,
                    Nombre = convenio.Nombre,
                    Año = ano,
                    Avance = avanceTotal,
                    IndicadorCount = indicadores.Count
                });
            }

            return Ok(result);
        }

        [HttpGet("getConvenioIndicadores/{convenioId:int}/{ano:int}")]
        public async Task<IActionResult> GetConvenioIndicadores(int convenioId, int ano)
        {
            var convenio = await db.Convenio
                .Where(c => c.Id == convenioId)
                .Include(c => c.IndicadorConvenios)
                    .ThenInclude(ic => ic.Indicador)
                        .ThenInclude(i => i.ResultadoIndicadors)
                .FirstOrDefaultAsync();

            if (convenio == null)
                return NotFound();

            var indicadores = convenio.IndicadorConvenios
                .Select(ic => ic.Indicador)
                .Where(i => i.Año == ano)
                .OrderBy(i => i.Orden)
                .ToList();

            var octSepOrden = indicadores
                .Where(i => i.EsPeriodoOctubreSep)
                .Select(i => i.Orden)
                .Distinct()
                .ToList();

            var prevDenMap = new Dictionary<int, decimal>();
            if (octSepOrden.Count > 0)
            {
                var prevData = await db.Indicador
                    .Where(i => i.Año == ano - 1 && octSepOrden.Contains(i.Orden))
                    .Select(i => new { i.Orden, Den = i.ResultadoIndicadors.Where(r => r.Mes >= 10).Sum(r => r.Denominador) })
                    .ToListAsync();
                prevDenMap = prevData.ToDictionary(x => x.Orden, x => decimal.Round(x.Den));
            }

            var resumen = new List<IndicadorResumenDTO>();
            foreach (var ind in indicadores)
            {
                var num = decimal.Round(ind.ResultadoIndicadors.Sum(r => r.Numerador));
                decimal den = ind.EsPeriodoOctubreSep
                    ? decimal.Round(ind.ResultadoIndicadors.Where(r => r.Mes < 10).Sum(r => r.Denominador))
                      + prevDenMap.GetValueOrDefault(ind.Orden, 0m)
                    : ind.IsColaborativo && ind.ResultadoIndicadors.Any()
                        ? decimal.Round(ind.ResultadoIndicadors.Max(r => r.Denominador))
                        : decimal.Round(ind.ResultadoIndicadors.Sum(r => r.Denominador));

                decimal displayDen = ind.IsDenFijo ? decimal.Round(den * (decimal)ind.Meta) : den;
                float displayMeta = ind.IsDenFijo ? 1.0f : ind.Meta;

                var actual = displayDen != 0 ? MathF.Round((float)(num / displayDen), 3) : 0f;
                float avance = displayMeta > 0 ? Math.Min(actual / displayMeta, 1f) : 0f;

                resumen.Add(new IndicadorResumenDTO
                {
                    Id = ind.Id,
                    Nombre = ind.Nombre,
                    Año = ind.Año,
                    Meta = displayMeta,
                    Numerador = (long)num,
                    Denominador = (long)displayDen,
                    Actual = actual,
                    Aporte = ind.Peso * avance,
                    Mensual = ind.Mensual,
                    IsTasa = ind.IsTasa,
                    IsColaborativo = ind.IsColaborativo
                });
            }

            return Ok(new ConvenioDetalleDTO
            {
                Id = convenio.Id,
                Nombre = convenio.Nombre,
                Año = ano,
                Indicadores = resumen
            });
        }
    }
}

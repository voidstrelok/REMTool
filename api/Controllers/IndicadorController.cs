

using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using RemTool.Shared.Enum;
using System.Text.Json;
using static Antlr4.Runtime.Atn.SemanticContext;

namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class IndicadorController : ControllerBase
    {

        private readonly ILogger<IndicadorController> _logger;
        private RemToolDataContext db;
        private List<long> EstablecimientosExcluidos = new List<long>{(long)EnumEstablecimiento.ClinicaDentalMovilMontePatria, (long)EnumEstablecimiento.SARMontePatria,(long)EnumEstablecimiento.SURElPalqui };

        public IndicadorController(ILogger<IndicadorController> logger, RemToolDataContext context)
        {
            _logger = logger;
            db = context;
        }

        [HttpGet("getIndicadores/")]
        public async Task<IActionResult> getIndicadores()
        {
            var indicadores = await db.ResultadoIndicador
                .Include(r => r.Establecimiento)
                .Include(r=>r.Indicador)
                .OrderBy(i => i.id_indicador).ToListAsync();
            return Ok(indicadores);
        }
        [HttpGet("getIndicador/{id_indicador:int}")]
        public async Task<IActionResult> getIndicador(int id_indicador,
            [FromQuery] long? sectorId = null,
            [FromQuery] long? establecimientoId = null)
        {
            // Proyección a DTO para poder ordenar los establecimientos por sector
            var indicadorDto = await db.Indicador
                .Where(i => i.Id == id_indicador)
                .Include(i=>i.ResultadoIndicadors.Where(e => !EstablecimientosExcluidos.Contains(e.id_establecimiento)))
                .ThenInclude(r=>r.Establecimiento)
                .ThenInclude(e=>e.Sector)
                .Select(i => new IndicadorDetalleDTO
                {
                    Id = i.Id,
                    Nombre = i.Nombre,
                    Meta = i.Meta,
                    Año = i.Año,
                    IsDenFijo = i.IsDenFijo,
                    IsTasa = i.IsTasa,
                    Mensual = i.Mensual,
                    EsPeriodoOctubreSep = i.EsPeriodoOctubreSep,
                    Tipoindicador = i.Tipoindicador ?? 0,
                    Orden = i.Orden,
                    IsColaborativo = i.IsColaborativo,
                    Resultados = i.ResultadoIndicadors
                        .Where(r =>
                            !EstablecimientosExcluidos.Contains(r.id_establecimiento) &&
                            (establecimientoId == null || establecimientoId == 0 || r.id_establecimiento == establecimientoId) &&
                            (sectorId == null || sectorId == 0 || (r.Establecimiento != null && r.Establecimiento.Sector != null && r.Establecimiento.Sector.Id == sectorId)))
                        .Select(r => new ResultadoIndicadorDTO
                        {
                            Mes = r.Mes,
                            Numerador = r.Numerador,
                            Denominador = r.Denominador,
                            NumeradorP = r.NumeradorP,
                            DenominadorP = r.DenominadorP,
                            EstablecimientoNombre = r.Establecimiento != null ? r.Establecimiento.Nombre : string.Empty,
                            SectorId = r.Establecimiento != null && r.Establecimiento.Sector != null ? r.Establecimiento.Sector.Id : 0,
                            EstablecimientoId = r.id_establecimiento
                        })
                        .OrderBy(rr => rr.SectorId)
                        .ThenBy(rr => rr.EstablecimientoNombre)
                        .ToList()
                })
                .FirstOrDefaultAsync();


            if (indicadorDto == null)
                return NotFound();

            decimal numerador = indicadorDto.Resultados.Sum(f => f.Numerador);
            decimal denominador;

            if (indicadorDto.EsPeriodoOctubreSep)
            {
                denominador = decimal.Round(indicadorDto.Resultados
                    .Where(r => r.Mes < 10).Sum(f => f.Denominador));

                // Fetch prev-year Oct–Dec records with establishment info
                var prevRecords = await db.Indicador
                    .Where(i => i.Año == indicadorDto.Año - 1
                             && i.Tipoindicador == indicadorDto.Tipoindicador
                             && i.Orden == indicadorDto.Orden)
                    .SelectMany(i => i.ResultadoIndicadors)
                    .Where(r => r.Mes >= 10
                             && !EstablecimientosExcluidos.Contains(r.id_establecimiento)
                             && (establecimientoId == null || establecimientoId == 0 || r.id_establecimiento == establecimientoId)
                             && (sectorId == null || sectorId == 0 || (r.Establecimiento != null && r.Establecimiento.Sector != null && r.Establecimiento.Sector.Id == sectorId)))
                    .Select(r => new
                    {
                        r.id_establecimiento,
                        r.Mes,
                        r.Denominador,
                        EstablecimientoNombre = r.Establecimiento != null ? r.Establecimiento.Nombre : string.Empty,
                        SectorId = r.Establecimiento != null && r.Establecimiento.Sector != null ? r.Establecimiento.Sector.Id : 0
                    })
                    .ToListAsync();

                denominador += decimal.Round(prevRecords.Sum(r => r.Denominador));

                // Merge Oct(?Jan), Nov(?Feb), Dec(?Mar) denominators per establishment
                foreach (var prev in prevRecords)
                {
                    int targetMes = prev.Mes - 9;
                    var matching = indicadorDto.Resultados
                        .FirstOrDefault(r => r.EstablecimientoId == prev.id_establecimiento && r.Mes == targetMes);

                    if (matching != null)
                    {
                        matching.Denominador += prev.Denominador;
                    }
                    else
                    {
                        indicadorDto.Resultados.Add(new ResultadoIndicadorDTO
                        {
                            Mes = targetMes,
                            Numerador = 0,
                            Denominador = prev.Denominador,
                            NumeradorP = 0,
                            DenominadorP = 0,
                            EstablecimientoNombre = prev.EstablecimientoNombre,
                            SectorId = prev.SectorId,
                            EstablecimientoId = prev.id_establecimiento
                        });
                    }
                }

                indicadorDto.Resultados = indicadorDto.Resultados
                    .OrderBy(r => r.SectorId)
                    .ThenBy(r => r.EstablecimientoNombre)
                    .ThenBy(r => r.Mes)
                    .ToList();
            }
            else
            {
                denominador = decimal.Round(indicadorDto.Resultados.Sum(f => f.Denominador));
            }

            if (indicadorDto.IsDenFijo                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            )
            {
                indicadorDto.Resultados = indicadorDto.Resultados.Select(r => new ResultadoIndicadorDTO
                {
                    Mes = r.Mes,
                    Numerador = r.Numerador,
                    Denominador = r.Denominador * (decimal)indicadorDto.Meta,
                    NumeradorP = r.NumeradorP,
                    DenominadorP = r.DenominadorP * (decimal)indicadorDto.Meta,
                    EstablecimientoNombre = r.EstablecimientoNombre,
                    SectorId = r.SectorId,
                    EstablecimientoId = r.EstablecimientoId
                }).ToList();

                denominador = decimal.Round(denominador * (decimal)indicadorDto.Meta);
                indicadorDto.Meta = 1.0f;
            }

            // Para indicadores colaborativos el denominador es un valor único comunal
            // almacenado en cada fila. Usar Sum() lo inflaría por el nº de establecimientos;
            // tomamos el máximo como representante del denominador comunal.
            if (indicadorDto.IsColaborativo && indicadorDto.Resultados.Any())
            {
                denominador = decimal.Round(indicadorDto.Resultados.Max(r => r.Denominador));
            }

            // Guardar denominador comunal antes de zerear los registros por establecimiento
            indicadorDto.Denominador = denominador;

            // Si es colaborativo, el denominador comunal se muestra solo en el resumen;
            // los establecimientos solo aportan numerador.
            if (indicadorDto.IsColaborativo)
            {
                indicadorDto.Resultados = indicadorDto.Resultados.Select(r => new ResultadoIndicadorDTO
                {
                    Mes = r.Mes,
                    Numerador = r.Numerador,
                    Denominador = 0,
                    NumeradorP = r.NumeradorP,
                    DenominadorP = 0,
                    EstablecimientoNombre = r.EstablecimientoNombre,
                    SectorId = r.SectorId,
                    EstablecimientoId = r.EstablecimientoId
                }).ToList();
            }

            decimal avanceIndicador = denominador != 0 ? (numerador / denominador) / (decimal)indicadorDto.Meta : 0;
            indicadorDto.Avance = denominador != 0 ? MathF.Round((float)avanceIndicador, 3) : 0;

            return Ok(indicadorDto);
        }
        // DTOs
        public class IndicadorDetalleDTO
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public float Meta { get; set; }
            public float Avance { get; set; }
            /// <summary>Denominador comunal (solo relevante cuando IsColaborativo = true).</summary>
            public decimal Denominador { get; set; }
            public int Año { get; set; }
            public bool IsDenFijo { get; set; }
            public bool IsTasa { get; set; }
            public bool EsPeriodoOctubreSep { get; set; }
            public int Tipoindicador { get; set; }
            public int Orden { get; set; }
            public int DenFijo { get; set; }
            public bool Mensual { get; set; }
            public bool IsColaborativo { get; set; }
            public List<ResultadoIndicadorDTO> Resultados { get; set; } = new List<ResultadoIndicadorDTO>();
        }

        public class ResultadoIndicadorDTO
        {
            public int Mes { get; set; }
            public decimal Numerador { get; set; }
            public decimal Denominador { get; set; }
            public decimal NumeradorP { get; set; }
            public decimal DenominadorP { get; set; }
            public string EstablecimientoNombre { get; set; } = string.Empty;
            public long SectorId { get; set; }
            public long EstablecimientoId { get; set; }
        }
        // DTO para el resumen
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

        [HttpGet("getIndicadores/{ano:int}/{tipo:int}")]
        public async Task<IActionResult> getIndicadoresTipo(int ano, int tipo)
        {
            var indicadores = await db.Indicador
                .Where(i => i.Año == ano && i.Tipoindicador == tipo)
                .Include(i => i.ResultadoIndicadors.Where(e => !EstablecimientosExcluidos.Contains(e.id_establecimiento)))
                .OrderBy(i => i.Orden)
                .ToListAsync();

            // Pre-fetch denominadores oct–dic del año anterior para indicadores con período oct–sep
            var octSepOrden = indicadores
                .Where(i => i.EsPeriodoOctubreSep)
                .Select(i => i.Orden).Distinct().ToList();
            var prevDenMap = new Dictionary<int, decimal>();
            if (octSepOrden.Count > 0)
            {
                var prevData = await db.Indicador
                    .Where(i => i.Año == ano - 1 && i.Tipoindicador == tipo && octSepOrden.Contains(i.Orden))
                    .Select(i => new { i.Orden, Den = i.ResultadoIndicadors.Where(r => r.Mes >= 10 && !EstablecimientosExcluidos.Contains(r.id_establecimiento)).Sum(r => r.Denominador + r.DenominadorP) })
                    .ToListAsync();
                prevDenMap = prevData.ToDictionary(x => x.Orden, x => decimal.Round(x.Den));
            }

            var resumen = new List<IndicadorResumenDTO>();
            foreach (var i in indicadores)
            {
                var num = decimal.Round(i.ResultadoIndicadors.Sum(r => r.Numerador + r.NumeradorP));
                decimal den = i.EsPeriodoOctubreSep
                    ? decimal.Round(i.ResultadoIndicadors.Where(r => r.Mes < 10).Sum(r => r.Denominador + r.DenominadorP))
                      + prevDenMap.GetValueOrDefault(i.Orden, 0m)
                    : i.IsColaborativo && i.ResultadoIndicadors.Any()
                        ? decimal.Round(i.ResultadoIndicadors.Max(r => r.Denominador + r.DenominadorP))
                        : decimal.Round(i.ResultadoIndicadors.Sum(r => r.Denominador + r.DenominadorP));

                decimal displayDen = i.IsDenFijo ? decimal.Round(den * (decimal)i.Meta) : den;
                float displayMeta  = i.IsDenFijo ? 1.0f : i.Meta;

                var actual = displayDen != 0 ? MathF.Round((float)(num / displayDen), 3) : 0;
                float avance = (actual / displayMeta) >= 1 ? 1 : (actual / displayMeta);

                resumen.Add(new IndicadorResumenDTO
                {
                    Id = i.Id,
                    Nombre = i.Nombre,
                    Año = i.Año,
                    Meta = displayMeta,
                    Numerador = (long)num,
                    Denominador = (long)displayDen,
                    Actual = actual,
                    Aporte = i.Peso * avance,
                    Mensual = i.Mensual,
                    IsTasa = i.IsTasa,
                    IsColaborativo = i.IsColaborativo
                });
            }

            // Ajuste de peso por meta 8 (exclusivo de metas sanitarias)
            if (tipo == 2 && resumen.Count > 0)
                resumen[0].Aporte += (float)0.0625;

            return Ok(resumen);
        }

                        [HttpGet("getIndicadores/{ano:int}/{tipo:int}/filtrado")]
                        public async Task<IActionResult> getIndicadoresFiltrado(
                            int ano, int tipo,
                            [FromQuery] long? sectorId = null,
                            [FromQuery] long? establecimientoId = null)
                        {
                            var indicadores = await db.Indicador
                                .Where(i => i.Año == ano && i.Tipoindicador == tipo)
                                .Include(i => i.ResultadoIndicadors.Where(e => !EstablecimientosExcluidos.Contains(e.id_establecimiento)))
                                    .ThenInclude(r => r.Establecimiento)
                                        .ThenInclude(e => e.Sector)
                                .OrderBy(i => i.Orden)
                                .ToListAsync();

                            // Pre-fetch denominadores oct–dic del año anterior para indicadores con período oct–sep
                            var octSepOrden = indicadores
                                .Where(i => i.EsPeriodoOctubreSep)
                                .Select(i => i.Orden).Distinct().ToList();
                            var prevDenMap = new Dictionary<int, decimal>();
                            if (octSepOrden.Count > 0)
                            {
                                var prevIndicadores = await db.Indicador
                                    .Where(i => i.Año == ano - 1 && i.Tipoindicador == tipo && octSepOrden.Contains(i.Orden))
                                    .Include(i => i.ResultadoIndicadors.Where(r => !EstablecimientosExcluidos.Contains(r.id_establecimiento)))
                                        .ThenInclude(r => r.Establecimiento)
                                            .ThenInclude(e => e.Sector)
                                    .ToListAsync();

                                foreach (var prev in prevIndicadores)
                                {
                                    var prevSrc = prev.ResultadoIndicadors.Where(r => r.Mes >= 10).AsEnumerable();
                                    if (establecimientoId.HasValue && establecimientoId.Value != 0)
                                        prevSrc = prevSrc.Where(r => r.id_establecimiento == establecimientoId.Value);
                                    else if (sectorId.HasValue && sectorId.Value != 0)
                                        prevSrc = prevSrc.Where(r => r.Establecimiento?.Sector?.Id == sectorId.Value);
                                    prevDenMap[prev.Orden] = decimal.Round(prevSrc.Sum(r => r.Denominador + r.DenominadorP));
                                }
                            }

                            var resumen = new List<IndicadorResumenDTO>();
                            foreach (var i in indicadores)
                            {
                                var resultados = i.ResultadoIndicadors.AsEnumerable();

                                if (sectorId.HasValue && sectorId.Value != 0)
                                    resultados = resultados.Where(r => r.Establecimiento?.Sector?.Id == sectorId.Value);

                                if (establecimientoId.HasValue && establecimientoId.Value != 0)
                                    resultados = resultados.Where(r => r.id_establecimiento == establecimientoId.Value);

                                var filteredList = resultados.ToList();

                                var num = decimal.Round(filteredList.Sum(r => r.Numerador + r.NumeradorP));
                                decimal den = i.EsPeriodoOctubreSep
                                    ? decimal.Round(filteredList.Where(r => r.Mes < 10).Sum(r => r.Denominador + r.DenominadorP))
                                      + prevDenMap.GetValueOrDefault(i.Orden, 0m)
                                    : i.IsColaborativo && filteredList.Any()
                                        ? decimal.Round(filteredList.Max(r => r.Denominador + r.DenominadorP))
                                        : decimal.Round(filteredList.Sum(r => r.Denominador + r.DenominadorP));

                                decimal displayDen = i.IsDenFijo ? decimal.Round(den * (decimal)i.Meta) : den;
                                float displayMeta  = i.IsDenFijo ? 1.0f : i.Meta;

                                var actual = displayDen != 0 ? MathF.Round((float)(num / displayDen), 3) : 0;
                                float avance = (actual / displayMeta) >= 1 ? 1 : (actual / displayMeta);

                                resumen.Add(new IndicadorResumenDTO
                                {
                                    Id = i.Id,
                                    Nombre = i.Nombre,
                                    Año = i.Año,
                                    Meta = displayMeta,
                                    Numerador = (long)num,
                                    Denominador = (long)displayDen,
                                    Actual = actual,
                                    Aporte = i.Peso * avance,
                                    Mensual = i.Mensual,
                                    IsTasa = i.IsTasa,
                                    IsColaborativo = i.IsColaborativo
                                });
                            }

                            if (tipo == 2 && resumen.Count > 0)
                                resumen[0].Aporte += (float)0.0625;

                            return Ok(resumen);
                        }
                    }
                }
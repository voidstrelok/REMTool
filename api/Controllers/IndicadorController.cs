using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.Services;
using RemTool.Shared;

namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class IndicadorController : ControllerBase
    {
        private readonly RemToolDataContext _db;
        private readonly IIndicadorService _indicadorService;

        public IndicadorController(RemToolDataContext context, IIndicadorService indicadorService)
        {
            _db = context;
            _indicadorService = indicadorService;
        }

        private static IEnumerable<ResultadoIndicador> FiltrarResultados(
            Indicador indicador,
            ResultadoFiltroEstablecimiento filtro,
            long? sectorId = null,
            long? establecimientoId = null)
        {
            var resultados = indicador.ResultadoIndicadors
                .Where(r => filtro.Incluye(r.id_establecimiento));

            if (sectorId.HasValue && sectorId.Value != 0)
                resultados = resultados.Where(r => r.Establecimiento?.id_sector == sectorId.Value);

            if (establecimientoId.HasValue && establecimientoId.Value != 0)
                resultados = resultados.Where(r => r.id_establecimiento == establecimientoId.Value);

            return resultados;
        }

        private async Task<List<Indicador>> CargarIndicadoresAsync(int ano, int tipo)
        {
            return await _db.Indicador
                .Where(i => i.Año == ano && i.Tipoindicador == tipo)
                .Include(i => i.ResultadoIndicadors)
                    .ThenInclude(r => r.Establecimiento)
                        .ThenInclude(e => e.Sector)
                .OrderBy(i => i.Orden)
                .ToListAsync();
        }

        private async Task<List<Indicador>> CargarIndicadoresAnterioresAsync(int ano, int tipo, IEnumerable<int> ordenes)
        {
            var ordenesLista = ordenes.Distinct().ToList();
            if (ordenesLista.Count == 0)
                return [];

            return await _db.Indicador
                .Where(i => i.Año == ano - 1
                         && i.Tipoindicador == tipo
                         && ordenesLista.Contains(i.Orden))
                .Include(i => i.ResultadoIndicadors)
                    .ThenInclude(r => r.Establecimiento)
                        .ThenInclude(e => e.Sector)
                .ToListAsync();
        }

        private static decimal ObtenerDenominadorAnterior(
            Indicador indicador,
            IEnumerable<Indicador> indicadoresAnteriores,
            ResultadoFiltroEstablecimiento filtro,
            long? sectorId,
            long? establecimientoId)
        {
            if (!indicador.EsPeriodoOctubreSep)
                return 0m;

            var resultados = indicadoresAnteriores
                .Where(i => i.Orden == indicador.Orden)
                .SelectMany(i => FiltrarResultados(i, filtro, sectorId, establecimientoId))
                .Where(r => r.Mes >= 10);

            if (indicador.IsColaborativo && resultados.Any())
                return decimal.Round(resultados.Max(r => IndicadorService.ObtenerDenominadorFila(indicador, r)));

            return decimal.Round(resultados.Sum(r => IndicadorService.ObtenerDenominadorFila(indicador, r)));
        }

        private async Task<List<IndicadorResumenDTO>> ObtenerResumenAsync(
            int ano,
            int tipo,
            long? sectorId = null,
            long? establecimientoId = null)
        {
            var indicadores = await CargarIndicadoresAsync(ano, tipo);
            var filtros = await _indicadorService.GetFiltrosAsync(indicadores.Select(i => i.Id));
            var indicadoresAnteriores = await CargarIndicadoresAnterioresAsync(
                ano, tipo, indicadores.Where(i => i.EsPeriodoOctubreSep).Select(i => i.Orden));

            var resumen = new List<IndicadorResumenDTO>();
            foreach (var indicador in indicadores)
            {
                var filtro = filtros[indicador.Id];
                var resultados = FiltrarResultados(indicador, filtro, sectorId, establecimientoId).ToList();
                var numerador = decimal.Round(resultados.Sum(r => r.Numerador + r.NumeradorP));
                var denominador = indicador.EsPeriodoOctubreSep
                    ? decimal.Round(resultados.Where(r => r.Mes < 10)
                        .Sum(r => IndicadorService.ObtenerDenominadorFila(indicador, r)))
                        + ObtenerDenominadorAnterior(indicador, indicadoresAnteriores, filtro, sectorId, establecimientoId)
                    : indicador.IsColaborativo && resultados.Any()
                        ? decimal.Round(resultados.Max(r => IndicadorService.ObtenerDenominadorFila(indicador, r)))
                        : decimal.Round(resultados.Sum(r => IndicadorService.ObtenerDenominadorFila(indicador, r)));

                var displayDenominador = indicador.IsDenFijo
                    ? decimal.Round(denominador * (decimal)indicador.Meta)
                    : denominador;
                var displayMeta = indicador.IsDenFijo ? 1.0f : indicador.Meta;
                var actual = displayDenominador != 0
                    ? MathF.Round((float)(numerador / displayDenominador), 3)
                    : 0f;
                var avance = displayMeta > 0 ? Math.Min(actual / displayMeta, 1f) : 0f;

                resumen.Add(new IndicadorResumenDTO
                {
                    Id = indicador.Id,
                    Nombre = indicador.Nombre,
                    Año = indicador.Año,
                    Meta = displayMeta,
                    Numerador = (long)numerador,
                    Denominador = (long)displayDenominador,
                    Actual = actual,
                    Aporte = indicador.Peso * avance,
                    Mensual = indicador.Mensual,
                    IsTasa = indicador.IsTasa,
                    IsColaborativo = indicador.IsColaborativo
                });
            }

            if (tipo == 2 && resumen.Count > 0)
                resumen[0].Aporte += 0.0625f;

            return resumen;
        }

        [HttpGet("getIndicadores/")]
        public async Task<IActionResult> GetIndicadores()
        {
            var resultados = await _db.ResultadoIndicador
                .Include(r => r.Establecimiento)
                .Include(r => r.Indicador)
                .OrderBy(r => r.id_indicador)
                .ToListAsync();
            var filtros = await _indicadorService.GetFiltrosAsync(resultados.Select(r => r.id_indicador));

            return Ok(resultados.Where(r => filtros[r.id_indicador].Incluye(r.id_establecimiento)));
        }

        [HttpGet("getIndicador/{id_indicador:int}")]
        public async Task<IActionResult> GetIndicador(
            int id_indicador,
            [FromQuery] long? sectorId = null,
            [FromQuery] long? establecimientoId = null)
        {
            var indicador = await _db.Indicador
                .Where(i => i.Id == id_indicador)
                .Include(i => i.ResultadoIndicadors)
                    .ThenInclude(r => r.Establecimiento)
                        .ThenInclude(e => e.Sector)
                .FirstOrDefaultAsync();

            if (indicador == null)
                return NotFound();

            var filtro = await _indicadorService.GetFiltroAsync(indicador.Id);
            var resultados = FiltrarResultados(indicador, filtro, sectorId, establecimientoId)
                .OrderBy(r => r.Establecimiento?.Sector?.Id)
                .ThenBy(r => r.Establecimiento?.Nombre)
                .ThenBy(r => r.Mes)
                .ToList();

            var indicadorDto = new IndicadorDetalleDTO
            {
                Id = indicador.Id,
                Nombre = indicador.Nombre,
                Meta = indicador.Meta,
                Año = indicador.Año,
                IsDenFijo = indicador.IsDenFijo,
                IsTasa = indicador.IsTasa,
                Mensual = indicador.Mensual,
                EsPeriodoOctubreSep = indicador.EsPeriodoOctubreSep,
                Tipoindicador = indicador.Tipoindicador ?? 0,
                Orden = indicador.Orden,
                IsColaborativo = indicador.IsColaborativo,
                Resultados = resultados.Select(r => new ResultadoIndicadorDTO
                {
                    Mes = r.Mes,
                    Numerador = r.Numerador,
                    Denominador = r.Denominador,
                    NumeradorP = r.NumeradorP,
                    DenominadorP = r.DenominadorP,
                    EstablecimientoNombre = r.Establecimiento?.Nombre ?? string.Empty,
                    SectorId = r.Establecimiento?.Sector?.Id ?? 0,
                    EstablecimientoId = r.id_establecimiento
                }).ToList()
            };

            var indicadoresAnteriores = await CargarIndicadoresAnterioresAsync(
                indicador.Año,
                indicador.Tipoindicador ?? 0,
                [indicador.Orden]);
            var denominadorAnterior = ObtenerDenominadorAnterior(
                indicador, indicadoresAnteriores, filtro, sectorId, establecimientoId);

            decimal numerador = indicadorDto.Resultados.Sum(r => r.Numerador + r.NumeradorP);
            decimal denominador = indicador.EsPeriodoOctubreSep
                ? decimal.Round(indicadorDto.Resultados.Where(r => r.Mes < 10)
                    .Sum(r => indicador.IsDenFijo || indicador.IsColaborativo
                        ? r.Denominador
                        : r.Denominador + r.DenominadorP)) + denominadorAnterior
                : indicador.IsColaborativo && indicadorDto.Resultados.Any()
                    ? decimal.Round(indicadorDto.Resultados.Max(r => r.Denominador))
                    : decimal.Round(indicadorDto.Resultados.Sum(r =>
                        indicador.IsDenFijo ? r.Denominador : r.Denominador + r.DenominadorP));

            if (indicador.IsDenFijo)
            {
                indicadorDto.Resultados = indicadorDto.Resultados.Select(r => new ResultadoIndicadorDTO
                {
                    Mes = r.Mes,
                    Numerador = r.Numerador,
                    Denominador = r.Denominador * (decimal)indicador.Meta,
                    NumeradorP = r.NumeradorP,
                    DenominadorP = indicador.IsDenFijo ? 0m : r.DenominadorP * (decimal)indicador.Meta,
                    EstablecimientoNombre = r.EstablecimientoNombre,
                    SectorId = r.SectorId,
                    EstablecimientoId = r.EstablecimientoId
                }).ToList();
                denominador = decimal.Round(denominador * (decimal)indicador.Meta);
                indicadorDto.Meta = 1.0f;
            }

            if (indicador.IsColaborativo && indicadorDto.Resultados.Any())
                denominador = decimal.Round(indicadorDto.Resultados.Max(r => r.Denominador));

            indicadorDto.Denominador = denominador;
            if (indicador.IsColaborativo)
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

            var avance = denominador != 0
                ? (numerador / denominador) / (decimal)indicadorDto.Meta
                : 0m;
            indicadorDto.Avance = denominador != 0 ? MathF.Round((float)avance, 3) : 0;
            return Ok(indicadorDto);
        }

        [HttpGet("getIndicadores/{ano:int}/{tipo:int}")]
        public async Task<IActionResult> GetIndicadoresTipo(int ano, int tipo)
            => Ok(await ObtenerResumenAsync(ano, tipo));

        [HttpGet("getIndicadores/{ano:int}/{tipo:int}/filtrado")]
        public async Task<IActionResult> GetIndicadoresFiltrado(
            int ano,
            int tipo,
            [FromQuery] long? sectorId = null,
            [FromQuery] long? establecimientoId = null)
            => Ok(await ObtenerResumenAsync(ano, tipo, sectorId, establecimientoId));

        public class IndicadorDetalleDTO
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public float Meta { get; set; }
            public float Avance { get; set; }
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
            public List<ResultadoIndicadorDTO> Resultados { get; set; } = [];
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
    }
}

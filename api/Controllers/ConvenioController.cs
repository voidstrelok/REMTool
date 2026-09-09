using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.Services;
using RemTool.Shared;

namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class ConvenioController : ControllerBase
    {
        private const int TipoConvenio = 1;
        private readonly RemToolDataContext db;
        private readonly IIndicadorService indicadorService;

        public ConvenioController(RemToolDataContext context, IIndicadorService indicadorService)
        {
            db = context;
            this.indicadorService = indicadorService;
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
            public float Avance { get; set; }
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

        private List<ResultadoIndicador> FiltrarResultados(
            Indicador indicador,
            ResultadoFiltroEstablecimiento filtro,
            long? sectorId,
            long? establecimientoId)
        {
            var resultados = indicador.ResultadoIndicadors
                .Where(r => filtro.Incluye(r.id_establecimiento));

            if (sectorId.HasValue && sectorId.Value != 0)
            {
                resultados = resultados.Where(r => r.Establecimiento?.id_sector == sectorId.Value);
            }

            if (establecimientoId.HasValue && establecimientoId.Value != 0)
            {
                resultados = resultados.Where(r => r.id_establecimiento == establecimientoId.Value);
            }

            return resultados.ToList();
        }

        private (decimal numerador, decimal denominador, float meta, float actual, float avance) CalcularIndicador(
            Indicador indicador,
            IEnumerable<ResultadoIndicador> resultados,
            decimal denominadorAnterior)
        {
            var numerador = indicadorService.CalcularNumerador(resultados);
            var denominador = indicadorService.CalcularDenominador(
                indicador,
                resultados,
                mesCorte: 12,
                denPrevAnoOctDic: denominadorAnterior);
            var (displayDen, displayMeta) = indicadorService.AjustarDenFijo(
                denominador,
                indicador.Meta,
                indicador.IsDenFijo);
            var actual = indicadorService.CalcularActual(numerador, displayDen);
            var avance = indicadorService.CalcularAvance(actual, displayMeta);

            return (numerador, displayDen, displayMeta, actual, avance);
        }

        private async Task<Dictionary<int, decimal>> ObtenerDenominadoresAnterioresAsync(
            IEnumerable<Indicador> indicadores,
            int ano,
            long? sectorId,
            long? establecimientoId)
        {
            var ordenes = indicadores
                .Where(i => i.EsPeriodoOctubreSep)
                .Select(i => i.Orden)
                .Distinct()
                .ToList();

            if (ordenes.Count == 0)
                return new Dictionary<int, decimal>();

            var indicadoresLista = indicadores.ToList();
            var filtros = await indicadorService.GetFiltrosAsync(indicadoresLista.Select(i => i.Id));
            var anteriores = await db.Indicador
                .Where(i => i.Año == ano - 1
                         && i.Tipoindicador == TipoConvenio
                         && ordenes.Contains(i.Orden))
                .Include(i => i.ResultadoIndicadors)
                    .ThenInclude(r => r.Establecimiento)
                .ToListAsync();

            var resultado = new Dictionary<int, decimal>();
            foreach (var indicador in indicadoresLista.Where(i => i.EsPeriodoOctubreSep))
            {
                var datos = anteriores
                    .Where(i => i.Orden == indicador.Orden)
                    .SelectMany(i => FiltrarResultados(i, filtros[indicador.Id], sectorId, establecimientoId))
                    .Where(r => r.Mes >= 10)
                    .ToList();
                resultado[indicador.Orden] = indicador.IsColaborativo && datos.Count > 0
                    ? decimal.Round(datos.Max(r => IndicadorService.ObtenerDenominadorFila(indicador, r)))
                    : decimal.Round(datos.Sum(r => IndicadorService.ObtenerDenominadorFila(indicador, r)));
            }

            return resultado;
        }

        [HttpGet("getConvenios/{ano:int}")]
        public async Task<IActionResult> GetConvenios(
            int ano,
            [FromQuery] long? sectorId = null,
            [FromQuery] long? establecimientoId = null)
        {
            var convenios = await db.Convenio
                .Include(c => c.IndicadorConvenios)
                    .ThenInclude(ic => ic.Indicador)
                        .ThenInclude(i => i.ResultadoIndicadors)
                            .ThenInclude(r => r.Establecimiento)
                                .ThenInclude(e => e.Sector)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var indicadoresAno = convenios
                .SelectMany(c => c.IndicadorConvenios)
                .Select(ic => ic.Indicador)
                .Where(i => i.Año == ano)
                .GroupBy(i => i.Id)
                .Select(g => g.First())
                .ToList();
            var denominadoresAnteriores = await ObtenerDenominadoresAnterioresAsync(
                indicadoresAno,
                ano,
                sectorId,
                establecimientoId);

            var result = new List<ConvenioResumenDTO>();
            foreach (var convenio in convenios)
            {
                var indicadores = convenio.IndicadorConvenios
                    .Select(ic => ic.Indicador)
                    .Where(i => i.Año == ano)
                    .GroupBy(i => i.Id)
                    .Select(g => g.First())
                    .ToList();

                var sumaAvances = 0f;
                foreach (var indicador in indicadores)
                {
                    var filtro = await indicadorService.GetFiltroAsync(indicador.Id);
                    var resultados = FiltrarResultados(indicador, filtro, sectorId, establecimientoId);
                    var calculo = CalcularIndicador(
                        indicador,
                        resultados,
                        denominadoresAnteriores.GetValueOrDefault(indicador.Orden));
                    sumaAvances += calculo.avance;
                }

                result.Add(new ConvenioResumenDTO
                {
                    Id = convenio.Id,
                    Nombre = convenio.Nombre,
                    Año = ano,
                    Avance = indicadores.Count > 0 ? sumaAvances / indicadores.Count : 0f,
                    IndicadorCount = indicadores.Count
                });
            }

            return Ok(result);
        }

        [HttpGet("getConvenioIndicadores/{convenioId:int}/{ano:int}")]
        public async Task<IActionResult> GetConvenioIndicadores(
            int convenioId,
            int ano,
            [FromQuery] long? sectorId = null,
            [FromQuery] long? establecimientoId = null)
        {
            var convenio = await db.Convenio
                .Where(c => c.Id == convenioId)
                .Include(c => c.IndicadorConvenios)
                    .ThenInclude(ic => ic.Indicador)
                        .ThenInclude(i => i.ResultadoIndicadors)
                            .ThenInclude(r => r.Establecimiento)
                                .ThenInclude(e => e.Sector)
                .FirstOrDefaultAsync();

            if (convenio == null)
                return NotFound();

            var indicadores = convenio.IndicadorConvenios
                .Select(ic => ic.Indicador)
                .Where(i => i.Año == ano)
                .GroupBy(i => i.Id)
                .Select(g => g.First())
                .OrderBy(i => i.Orden)
                .ToList();
            var denominadoresAnteriores = await ObtenerDenominadoresAnterioresAsync(
                indicadores,
                ano,
                sectorId,
                establecimientoId);

            var resumen = new List<IndicadorResumenDTO>();
            foreach (var indicador in indicadores)
            {
                var filtro = await indicadorService.GetFiltroAsync(indicador.Id);
                var resultados = FiltrarResultados(indicador, filtro, sectorId, establecimientoId);
                var calculo = CalcularIndicador(
                    indicador,
                    resultados,
                    denominadoresAnteriores.GetValueOrDefault(indicador.Orden));

                resumen.Add(new IndicadorResumenDTO
                {
                    Id = indicador.Id,
                    Nombre = indicador.Nombre,
                    Año = indicador.Año,
                    Meta = calculo.meta,
                    Numerador = (long)calculo.numerador,
                    Denominador = (long)calculo.denominador,
                    Avance = calculo.avance,
                    Actual = calculo.actual,
                    Aporte = indicador.Peso * calculo.avance,
                    Mensual = indicador.Mensual,
                    IsTasa = indicador.IsTasa,
                    IsColaborativo = indicador.IsColaborativo
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

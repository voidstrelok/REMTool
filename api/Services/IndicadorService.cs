using RemTool.Shared;
using Microsoft.EntityFrameworkCore;
using RemTool.Shared.Enum;

namespace RemTool.Services
{
    public class ResultadoFiltroEstablecimiento
    {
        public bool EsWhitelist { get; }
        public IReadOnlyList<long> Ids { get; }

        public ResultadoFiltroEstablecimiento(bool esWhitelist, IReadOnlyList<long> ids)
        {
            EsWhitelist = esWhitelist;
            Ids = ids;
        }
    }

    public interface IIndicadorService
    {
        /// <summary>Exclusiones globales (id_indicador IS NULL). Respaldado por BD, cacheado por request.</summary>
        IReadOnlyList<long> EstablecimientosExcluidos { get; }

        /// <summary>
        /// Devuelve el filtro efectivo para un indicador dado.
        /// Si existen reglas Incluir para ese indicador â?' whitelist.
        /// Si no â?' blacklist con exclusiones globales + por indicador.
        /// Pasar null para obtener solo el filtro global.
        /// </summary>
        Task<ResultadoFiltroEstablecimiento> GetFiltroAsync(int? indicadorId = null);
        Task<Dictionary<int, decimal>> GetPrevYearOctDecDenominatorsAsync(
            int ano,
            int? tipoIndicador,
            IEnumerable<int> ordenes,
            long? sectorId = null,
            long? establecimientoId = null,
            bool includeP = true);
        decimal CalcularNumerador(IEnumerable<ResultadoIndicador> resultados, bool includeP = true);
        decimal CalcularDenominador(
            Indicador indicador,
            IEnumerable<ResultadoIndicador> resultados,
            int mesCorte,
            decimal denPrevAnoOctDic = 0m,
            bool includeP = true);
        (decimal displayDen, float displayMeta) AjustarDenFijo(decimal den, float meta, bool isDenFijo);
        float CalcularActual(decimal numerador, decimal denominador, int decimals = 3);
        float CalcularAvance(float actual, float meta);
    }

    public class IndicadorService : IIndicadorService
    {
        private readonly RemToolDataContext _db;
        private IReadOnlyList<long>? _cachedExcluidos;

        public IndicadorService(RemToolDataContext db)
        {
            _db = db;
        }

        public IReadOnlyList<long> EstablecimientosExcluidos
        {
            get
            {
                return _cachedExcluidos ??= _db.FiltroEstablecimiento
                    .Where(f => f.Tipo == TipoFiltroEstablecimiento.Excluir && f.id_indicador == null)
                    .Select(f => f.id_establecimiento)
                    .Distinct()
                    .ToList()
                    .AsReadOnly();
            }
        }

        public async Task<ResultadoFiltroEstablecimiento> GetFiltroAsync(int? indicadorId = null)
        {
            if (indicadorId.HasValue)
            {
                var whitelist = await _db.FiltroEstablecimiento
                    .Where(f => f.id_indicador == indicadorId && f.Tipo == TipoFiltroEstablecimiento.Incluir)
                    .Select(f => f.id_establecimiento)
                    .ToListAsync();

                if (whitelist.Count > 0)
                    return new ResultadoFiltroEstablecimiento(true, whitelist);
            }

            var exclusiones = await _db.FiltroEstablecimiento
                .Where(f => f.Tipo == TipoFiltroEstablecimiento.Excluir
                         && (f.id_indicador == null || f.id_indicador == indicadorId))
                .Select(f => f.id_establecimiento)
                .Distinct()
                .ToListAsync();

            return new ResultadoFiltroEstablecimiento(false, exclusiones);
        }

        public async Task<Dictionary<int, decimal>> GetPrevYearOctDecDenominatorsAsync(
            int ano,
            int? tipoIndicador,
            IEnumerable<int> ordenes,
            long? sectorId = null,
            long? establecimientoId = null,
            bool includeP = true)
        {
            var ordenesSet = ordenes.Distinct().ToList();
            if (ordenesSet.Count == 0)
            {
                return new Dictionary<int, decimal>();
            }

            var query = _db.Indicador
                .Where(i => i.Año == ano - 1 && ordenesSet.Contains(i.Orden));

            if (tipoIndicador.HasValue)
            {
                query = query.Where(i => i.Tipoindicador == tipoIndicador.Value);
            }

            var raw = await query
                .SelectMany(
                    i => i.ResultadoIndicadors,
                    (i, r) => new
                    {
                        i.Orden,
                        r.Mes,
                        r.id_establecimiento,
                        SectorId = r.Establecimiento != null ? (long?)r.Establecimiento.id_sector : null,
                        Denominador = r.Denominador,
                        DenominadorP = r.DenominadorP
                    })
                .Where(x => x.Mes >= 10 && !EstablecimientosExcluidos.Contains(x.id_establecimiento))
                .ToListAsync();

            var filtered = raw.AsEnumerable();

            if (establecimientoId.HasValue && establecimientoId.Value != 0)
            {
                filtered = filtered.Where(x => x.id_establecimiento == establecimientoId.Value);
            }
            else if (sectorId.HasValue && sectorId.Value != 0)
            {
                filtered = filtered.Where(x => x.SectorId == sectorId.Value);
            }

            return filtered
                .GroupBy(x => x.Orden)
                .ToDictionary(
                    g => g.Key,
                    g => decimal.Round(g.Sum(x => x.Denominador + (includeP ? x.DenominadorP : 0m))));
        }

        public decimal CalcularNumerador(IEnumerable<ResultadoIndicador> resultados, bool includeP = true)
        {
            return decimal.Round(resultados.Sum(r => r.Numerador + (includeP ? r.NumeradorP : 0m)));
        }

        public decimal CalcularDenominador(
            Indicador indicador,
            IEnumerable<ResultadoIndicador> resultados,
            int mesCorte,
            decimal denPrevAnoOctDic = 0m,
            bool includeP = true)
        {
            var hastaCorte = resultados.Where(r => r.Mes <= mesCorte).ToList();

            Func<ResultadoIndicador, decimal> denSelector = r => r.Denominador + (includeP ? r.DenominadorP : 0m);

            if (indicador.EsPeriodoOctubreSep)
            {
                return decimal.Round(hastaCorte.Where(r => r.Mes < 10).Sum(denSelector)) + denPrevAnoOctDic;
            }

            if (indicador.IsColaborativo && hastaCorte.Any())
            {
                return decimal.Round(hastaCorte.Max(denSelector));
            }

            return decimal.Round(hastaCorte.Sum(denSelector));
        }

        public (decimal displayDen, float displayMeta) AjustarDenFijo(decimal den, float meta, bool isDenFijo)
        {
            if (!isDenFijo)
            {
                return (den, meta);
            }

            return (decimal.Round(den * (decimal)meta), 1.0f);
        }

        public float CalcularActual(decimal numerador, decimal denominador, int decimals = 3)
        {
            return denominador != 0
                ? MathF.Round((float)(numerador / denominador), decimals)
                : 0f;
        }

        public float CalcularAvance(float actual, float meta)
        {
            return meta > 0 ? Math.Min(actual / meta, 1f) : 0f;
        }
    }
}

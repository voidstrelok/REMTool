using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTools
{
    public class RemDataProvider : IDataProvider
    {
        private const bool UsarPercapitaSsc = true;

        private readonly RemToolDataContext _db;
        public readonly HashSet<string> CodPrestaciones = new();
        private List<Registro> _registros = new();
        private Dictionary<string, List<Registro>> _registrosPorCodigo = new(StringComparer.OrdinalIgnoreCase);
        private List<Fonasa> _fonasa = new();
        private List<PercapitaSsc> _percapitaSsc = new();
        private int _pMonth;
        private bool _pEsFallbackAñoAnterior;
        public int PMonth => _pMonth;
        /// <summary>Mes donde se aplica el valor P al iterar: 1 si es fallback año anterior, si no _pMonth.</summary>
        public int PMesAplicacion => _pEsFallbackAñoAnterior ? 1 : _pMonth;

        public RemDataProvider(RemToolDataContext db) => _db = db;

        public void CollectPrestacion(string prestacion) => CodPrestaciones.Add(prestacion);

        public void CargarFonasa(int año)
        {
            _fonasa = new List<Fonasa>(); // Fonasa entity removed; UsarPercapitaSsc=true so this path is unused
        }

        public void CargarPercapitaSsc(int año)
        {
            _percapitaSsc = _db.PercapitaSsc
                .AsNoTracking()
                .Where(p => p.AñoCorte == año)
                .ToList();
        }

        // Si es false, no se carga el año anterior como fallback cuando aún no hay datos del año actual
        private const bool UsarFallbackAñoAnterior = true;

        public void CargarPrestaciones(int año, DateTime ahora)
        {
            int pYear, pMonth;
            _pEsFallbackAñoAnterior = false;

            if (ahora.Year != año)
            {
                pYear = año;
                pMonth = 12;
            }
            else if (ahora.Month >= 6)
            {
                pYear = año;
                pMonth = 6;
            }
            else if (UsarFallbackAñoAnterior)
            {
                pYear = año - 1;
                pMonth = 12;
                _pEsFallbackAñoAnterior = true;
            }
            else
            {
                // Sin fallback: no hay corte P disponible para el año actual
                _pMonth = 0;
                CargarRegistros(año, null, Array.Empty<(int Año, int Mes, string Serie)>());
                return;
            }

            _pMonth = pMonth;
            CargarRegistros(
                año,
                (pYear, pMonth, "P"),
                (año, 0, "A"),
                (año, 0, "BM"),
                (año, 0, "D"));
        }

        public decimal GetPrestacionValue(string prestacion, int columna, EvaluationContext ctx)
        {
            if (!_registrosPorCodigo.TryGetValue(prestacion, out var registros))
                return 0;

            var q = registros;

            if (ctx.EstablecimientoId.HasValue)
                q = q.Where(r => r.Reporte.id_establecimiento == ctx.EstablecimientoId).ToList();

            if (q.Count == 0) return 0;

            var seriesIncluidas = ctx.SeriesIncluidas;
            var qMensual = q.Where(r =>
                !r.Prestacion.VersionRem.SerieRem.Nombre.Equals("P", StringComparison.OrdinalIgnoreCase)
                && (seriesIncluidas is null
                    || seriesIncluidas.Contains(r.Prestacion.VersionRem.SerieRem.Nombre)))
                .Where(r => r.Reporte.Año == ctx.Año && r.Reporte.Mes == ctx.Mes)
                .ToList();
            var qP = q.Where(r =>
                r.Prestacion.VersionRem.SerieRem.Nombre.Equals("P", StringComparison.OrdinalIgnoreCase)
                && (seriesIncluidas is null || seriesIncluidas.Contains("P")))
                .ToList();

            decimal total = 0;

            total += qMensual.Sum(r => ValorEnColumna(r, columna));

            if (qP.Count > 0 && _pMonth > 0)
            {
                // Serie P: acumulado. Solo aporta en el mes de aplicación:
                // - Fallback año anterior (_pEsFallbackAñoAnterior): mes 1
                // - Corte corriente: mes exacto del corte (6 o 12)
                if (ctx.Mes == PMesAplicacion)
                {
                    var fP = qP.Where(r => r.Reporte.Mes == _pMonth).ToList();
                    total += fP.Sum(r => ValorEnColumna(r, columna));
                }
            }

            return total;
        }

        private void CargarRegistros(
            int año,
            (int Año, int Mes, string Serie)? corteP,
            params (int Año, int Mes, string Serie)[] series)
        {
            var reportesSeleccionados = new HashSet<int>();

            foreach (var grupo in series
                .Where(item => item.Serie is "A" or "BM" or "D")
                .GroupBy(item => item.Año))
            {
                foreach (var reporteId in ObtenerReportesSeleccionados(
                    grupo.Key,
                    null,
                    "A",
                    "BM",
                    "D"))
                {
                    reportesSeleccionados.Add(reporteId);
                }
            }

            if (corteP.HasValue)
            {
                foreach (var reporteId in ObtenerReportesSeleccionados(
                    corteP.Value.Año,
                    corteP.Value.Mes,
                    "P"))
                {
                    reportesSeleccionados.Add(reporteId);
                }
            }

            _registros = reportesSeleccionados.Count == 0
                ? new List<Registro>()
                : _db.Registro
                    .AsNoTracking()
                    .Include(r => r.Prestacion)
                        .ThenInclude(p => p.VersionRem)
                            .ThenInclude(v => v.SerieRem)
                    .Include(r => r.Prestacion)
                        .ThenInclude(p => p.Coordenadas)
                    .Include(r => r.Reporte)
                    .Where(r => reportesSeleccionados.Contains(r.id_reporte)
                        && CodPrestaciones.Contains(r.Prestacion.CodigoPrestacion))
                    .ToList();

            _registrosPorCodigo = _registros
                .GroupBy(registro => registro.Prestacion.CodigoPrestacion, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        }

        private List<int> ObtenerReportesSeleccionados(int año, int? mes, params string[] series)
        {
            var candidatos = _db.Registro
                .AsNoTracking()
                .Where(registro => registro.Reporte.Año == año
                    && (!mes.HasValue || registro.Reporte.Mes == mes.Value)
                    && series.Contains(registro.Prestacion.VersionRem.SerieRem.Nombre))
                .Select(registro => new ReporteSerieCandidato
                {
                    ReporteId = registro.id_reporte,
                    EstablecimientoId = registro.Reporte.id_establecimiento,
                    Mes = registro.Reporte.Mes,
                    Serie = registro.Prestacion.VersionRem.SerieRem.Nombre
                })
                .Distinct()
                .ToList();

            return candidatos
                .GroupBy(candidato => new
                {
                    candidato.EstablecimientoId,
                    candidato.Mes,
                    candidato.Serie
                })
                .Select(grupo => grupo.Max(candidato => candidato.ReporteId))
                .ToList();
        }

        private static decimal ValorEnColumna(Registro registro, int columna)
        {
            if (columna <= 0)
                return 0;

            // Las fórmulas históricas guardan el código lógico de columna
            // (COL01, COL02, ...), no la posición física que finalmente se
            // extrae en Registro. Una versión de la planilla puede omitir
            // columnas estructurales y desplazar esa posición física.
            var coordenada = registro.Prestacion.Coordenadas
                .FirstOrDefault(c => EsCodigoColumna(c.CodigoColumna, columna));
            var indice = coordenada is null ? columna - 1 : coordenada.Orden - 1;

            return indice >= 0 && indice < registro.Valor.Count
                ? registro.Valor[indice]
                : 0;
        }

        private static bool EsCodigoColumna(string? codigo, int columna)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            var valor = codigo.Trim();
            if (!valor.StartsWith("COL", StringComparison.OrdinalIgnoreCase))
                return false;

            return int.TryParse(valor[3..], out var numero) && numero == columna;
        }

        private sealed class ReporteSerieCandidato
        {
            public int ReporteId { get; init; }
            public long EstablecimientoId { get; init; }
            public int Mes { get; init; }
            public string Serie { get; init; } = string.Empty;
        }

        public decimal ResolveVariable(string variableName, Dictionary<string, object> filters, EvaluationContext ctx)
        {
            if (variableName != "FONASA" && variableName != "DM2" && variableName != "HTA" && variableName != "EPOC")
                throw new Exception($"Variable no soportada: {variableName}");

            // Las variables poblacionales (FONASA, DM2, HTA, EPOC) no pertenecen a Serie P
            if (ctx.SeriesIncluidas is not null
                && ctx.SeriesIncluidas.Contains("P")
                && ctx.SeriesIncluidas.Count == 1)
                return 0;

            if (UsarPercapitaSsc)
                return ResolveVariablePercapita(variableName, filters, ctx);
            else
                return ResolveVariableFonasa(variableName, filters, ctx);
        }

        private decimal ResolveVariableFonasa(string variableName, Dictionary<string, object> filters, EvaluationContext ctx)
        {
            var query = _fonasa.ToList();
            if (ctx.EstablecimientoId.HasValue)
                query = query.Where(x => x.id_establecimiento == ctx.EstablecimientoId).ToList();

            if (variableName == "DM2")
            {
                var sum = query.Count(q => q.Edad >= 15 && q.Edad <= 24) * 0.018;
                sum += query.Count(q => q.Edad >= 25 && q.Edad <= 44) * 0.063;
                sum += query.Count(q => q.Edad >= 45 && q.Edad <= 64) * 0.183;
                sum += query.Count(q => q.Edad >= 65) * 0.306;
                return (decimal)sum;
            }
            if (variableName == "HTA")
            {
                var sum = query.Count(q => q.Edad >= 15 && q.Edad <= 24) * 0.007;
                sum += query.Count(q => q.Edad >= 25 && q.Edad <= 44) * 0.106;
                sum += query.Count(q => q.Edad >= 45 && q.Edad <= 64) * 0.451;
                sum += query.Count(q => q.Edad >= 65) * 0.733;
                return (decimal)sum;
            }
            if (variableName == "EPOC")
            {
                var sum = query.Count(q => q.Edad >= 40) * 0.117;
                sum += query.Count(q => q.Edad >= 5) * 0.1;
                return (decimal)sum;
            }

            if (filters.TryGetValue("edad_min", out var min))
                query = query.Where(x => x.Edad >= int.Parse(min.ToString())).ToList();
            if (filters.TryGetValue("edad_max", out var max))
                query = query.Where(x => x.Edad <= int.Parse(max.ToString())).ToList();
            if (filters.TryGetValue("edad", out var edad))
                query = query.Where(x => x.Edad == int.Parse(edad.ToString())).ToList();
            if (filters.TryGetValue("sexo", out var sexo))
                query = query.Where(x => x.Genero == sexo.ToString()).ToList();

            return query.Count;
        }

        private decimal ResolveVariablePercapita(string variableName, Dictionary<string, object> filters, EvaluationContext ctx)
        {
            var query = _percapitaSsc.ToList();
            if (ctx.EstablecimientoId.HasValue)
                query = query.Where(x => x.id_establecimiento == ctx.EstablecimientoId).ToList();

            if (variableName == "DM2")
            {
                var sum = query.Where(q => q.Edad >= 15 && q.Edad <= 24).Sum(q => q.Inscritos) * 0.018;
                sum += query.Where(q => q.Edad >= 25 && q.Edad <= 44).Sum(q => q.Inscritos) * 0.063;
                sum += query.Where(q => q.Edad >= 45 && q.Edad <= 64).Sum(q => q.Inscritos) * 0.183;
                sum += query.Where(q => q.Edad >= 65).Sum(q => q.Inscritos) * 0.306;
                return (decimal)sum;
            }
            if (variableName == "HTA")
            {
                var sum = query.Where(q => q.Edad >= 15 && q.Edad <= 24).Sum(q => q.Inscritos) * 0.007;
                sum += query.Where(q => q.Edad >= 25 && q.Edad <= 44).Sum(q => q.Inscritos) * 0.106;
                sum += query.Where(q => q.Edad >= 45 && q.Edad <= 64).Sum(q => q.Inscritos) * 0.451;
                sum += query.Where(q => q.Edad >= 65).Sum(q => q.Inscritos) * 0.733;
                return (decimal)sum;
            }
            if (variableName == "EPOC")
            {
                var sum = query.Where(q => q.Edad >= 40).Sum(q => q.Inscritos) * 0.117;
                sum += query.Where(q => q.Edad >= 5).Sum(q => q.Inscritos) * 0.1;
                return (decimal)sum;
            }

            if (filters.TryGetValue("edad_min", out var min))
                query = query.Where(x => x.Edad >= int.Parse(min.ToString())).ToList();
            if (filters.TryGetValue("edad_max", out var max))
                query = query.Where(x => x.Edad <= int.Parse(max.ToString())).ToList();
            if (filters.TryGetValue("edad", out var edad))
                query = query.Where(x => x.Edad == int.Parse(edad.ToString())).ToList();
            if (filters.TryGetValue("sexo", out var sexo))
                query = query.Where(x => x.Sexo == sexo.ToString()).ToList();

            return query.Sum(x => x.Inscritos);
        }
    }

    // Stub: Fonasa entity was removed (UsarPercapitaSsc=true, so this code path is never active)
    internal sealed class Fonasa
    {
        public int id_establecimiento { get; set; }
        public int Edad { get; set; }
        public string Genero { get; set; } = "";
    }
}

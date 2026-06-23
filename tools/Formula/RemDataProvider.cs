using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTools
{
    public class RemDataProvider : IDataProvider
    {
        private const bool UsarPercapitaSsc = true;

        private readonly RemToolDataContext _db;
        public readonly HashSet<string> CodPrestaciones = new();
        private List<Registro> _registros;
        private List<Fonasa> _fonasa = new();
        private List<PercapitaSsc> _percapitaSsc;
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
            _percapitaSsc = _db.PercapitaSsc.Where(p => p.AñoCorte == año).Include(p => p.Establecimiento).ToList();
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
            else if (ahora.Month >= 7)
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
                _registros = _db.Registro
                    .Include(r => r.Prestacion).ThenInclude(p => p.VersionRem).ThenInclude(v => v.SerieRem)
                    .Include(r => r.Reporte)
                    .Where(r => CodPrestaciones.Contains(r.Prestacion.CodigoPrestacion)
                                && (r.Prestacion.VersionRem.SerieRem.Nombre == "A" || r.Prestacion.VersionRem.SerieRem.Nombre == "BM")
                                && r.Prestacion.VersionRem.Fecha.Year == año)
                    .ToList();
                return;
            }

            _pMonth = pMonth;
            _registros = _db.Registro
                .Include(r => r.Prestacion).ThenInclude(p => p.VersionRem).ThenInclude(v => v.SerieRem)
                .Include(r => r.Reporte)
                .Where(r => CodPrestaciones.Contains(r.Prestacion.CodigoPrestacion)
                            && (
                                (r.Prestacion.VersionRem.SerieRem.Nombre == "A" && r.Prestacion.VersionRem.Fecha.Year == año)
                                || (r.Prestacion.VersionRem.SerieRem.Nombre == "BM" && r.Prestacion.VersionRem.Fecha.Year == año)
                                || (r.Prestacion.VersionRem.SerieRem.Nombre == "P" && r.Prestacion.VersionRem.Fecha.Year == pYear && r.Reporte.Mes == pMonth)
                               ))
                .ToList();
        }

        public decimal GetPrestacionValue(string prestacion, int columna, EvaluationContext ctx)
        {
            var q = _registros.Where(r => r.Prestacion.CodigoPrestacion.Equals(prestacion)).ToList();

            if (ctx.EstablecimientoId.HasValue)
                q = q.Where(r => r.Reporte.id_establecimiento == ctx.EstablecimientoId).ToList();

            if (q.Count == 0) return 0;

            // Separar registros A, BM y P
            var qA = q.Where(r => r.Prestacion.VersionRem.SerieRem.Nombre == "A" || r.Prestacion.VersionRem.SerieRem.Nombre == "BM").ToList();
            var qP = q.Where(r => r.Prestacion.VersionRem.SerieRem.Nombre == "P").ToList();

            decimal total = 0;

            bool incluirA = ctx.SoloSerie == null || ctx.SoloSerie == "A" || ctx.SoloSerie == "BM";
            bool incluirP = ctx.SoloSerie == null || ctx.SoloSerie == "P";

            if (incluirA)
            {
                // Series A y BM: mensual, filtrar por mes y año exactos
                var fA = qA.Where(r =>
                    r.Reporte.Mes == ctx.Mes &&
                    r.Prestacion.VersionRem.Fecha.Year == ctx.Año
                ).ToList();
                total += (decimal)fA.Sum(r => r.Valor[columna - 1]);
            }

            if (incluirP && _pMonth > 0)
            {
                // Serie P: acumulado. Solo aporta en el mes de aplicación:
                // - Fallback año anterior (_pEsFallbackAñoAnterior): mes 1
                // - Corte corriente: mes exacto del corte (6 o 12)
                if (ctx.Mes == PMesAplicacion)
                {
                    var fP = qP.Where(r => r.Reporte.Mes == _pMonth).ToList();
                    total += (decimal)fP.Sum(r => r.Valor[columna - 1]);
                }
            }

            return total;
        }

        public decimal ResolveVariable(string variableName, Dictionary<string, object> filters, EvaluationContext ctx)
        {
            if (variableName != "FONASA" && variableName != "DM2" && variableName != "HTA" && variableName != "EPOC")
                throw new Exception($"Variable no soportada: {variableName}");

            // Las variables poblacionales (FONASA, DM2, HTA, EPOC) no pertenecen a Serie P
            if (ctx.SoloSerie == "P") return 0;

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

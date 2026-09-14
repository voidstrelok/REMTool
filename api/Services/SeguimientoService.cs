using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTool.Services;

public record EvaluacionSeguimiento
{
    public int MesCorte { get; init; }
    public int MesEvaluacion { get; init; }
    public int? UltimoMesDatos { get; init; }
    public bool Provisional { get; init; }
    public bool TieneDatos { get; init; }
    public bool TieneRegistroPeriodo { get; init; }
    public decimal Numerador { get; init; }
    public decimal Denominador { get; init; }
    public decimal Meta { get; init; }
    public decimal? Resultado { get; init; }
    public decimal? Cumplimiento { get; init; }
    public decimal Esperado { get; init; }
    public decimal? Brecha { get; init; }
    public string Estado { get; init; } = "SinDatos";
    public string Unidad { get; init; } = "%";
}

public record RegistroSeguimiento(int Mes, decimal Numerador, decimal Denominador,
    decimal NumeradorP, decimal DenominadorP, string EstablecimientoNombre,
    long SectorId, long EstablecimientoId);
public record EstablecimientoSeguimiento(long Id, string Nombre, string Sector,
    EvaluacionSeguimiento Evaluacion, List<RegistroSeguimiento> Meses);

public class IndicadorSeguimiento
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool EsDetalle { get; init; }
    public string Criterios => EvaluadorSeguimiento.Criterios;
    public int Id { get; init; }
    public string Nombre { get; init; } = "";
    public int Año { get; init; }
    public int Orden { get; init; }
    public int Tipoindicador { get; init; }
    public bool Mensual { get; init; }
    public bool IsTasa { get; init; }
    public bool IsColaborativo { get; init; }
    public bool IsDenFijo { get; init; }
    public bool EsPeriodoOctubreSep { get; init; }
    public string? Detalle { get; init; }
    public decimal Peso { get; init; }
    public decimal Ajuste { get; set; }
    public EvaluacionSeguimiento Evaluacion { get; init; } = new();
    // Legacy fields retain their original scale. New clients use Evaluacion.
    public decimal Numerador => Evaluacion.Numerador;
    public decimal Denominador => Evaluacion.Denominador;
    public decimal Meta => IsTasa ? Evaluacion.Meta : Evaluacion.Meta / 100m;
    public decimal Actual => (Evaluacion.Resultado ?? 0m) / (IsTasa ? 1m : 100m);
    public decimal Avance => EsDetalle ? (Evaluacion.Cumplimiento ?? 0m) / 100m : Math.Clamp((Evaluacion.Cumplimiento ?? 0m) / 100m, 0m, 1m);
    public decimal Aporte => Peso * Math.Clamp(Avance, 0m, 1m) + Ajuste;
    public int DenFijo => 0;
    public List<RegistroSeguimiento> Resultados { get; init; } = [];
    public List<EvaluacionSeguimiento> Evolucion { get; init; } = [];
    public List<EstablecimientoSeguimiento> Establecimientos { get; init; } = [];
    public List<string> Analisis { get; init; } = [];
}

public record ResumenSeguimiento(decimal Cumplimiento, string Metodo, int Total,
    int Cumplidas, int EnCurso, int EnRiesgo, int Criticos, int SinDatos, int SinDenominador, int SinMeta)
{
    public static ResumenSeguimiento Crear(IReadOnlyList<IndicadorSeguimiento> items, bool ponderado)
    {
        int Count(string estado) => items.Count(i => i.Evaluacion.Estado == estado);
        decimal valor = ponderado ? items.Sum(i => i.Aporte) * 100m
            : items.Count > 0 ? items.Average(i => Math.Clamp(i.Avance, 0m, 1m)) * 100m : 0m;
        return new(Math.Clamp(valor, 0m, 100m), ponderado
            ? "Suma ponderada del cumplimiento (máximo 100% por indicador), con los ajustes configurados."
            : "Promedio simple del cumplimiento (máximo 100% por indicador).",
            items.Count, Count("Cumplida"), Count("EnCurso"), Count("EnRiesgo"), Count("Critico"),
            Count("SinDatos"), Count("SinDenominador"), Count("SinMeta"));
    }
}

public record ConvenioSeguimiento(int Id, string Nombre, int Año, int MesCorte,
    List<IndicadorSeguimiento> Indicadores, ResumenSeguimiento Resumen)
{
    public string Criterios => EvaluadorSeguimiento.Criterios;
    public decimal Avance => Resumen.Cumplimiento / 100m;
    public int IndicadorCount => Indicadores.Count;
}

/// <summary>Pure evaluation shared by JSON and PDF. Percentages are returned in display units.</summary>
public static class EvaluadorSeguimiento
{
    public static List<IndicadorSeguimiento> Seleccionar(IReadOnlyList<IndicadorSeguimiento> items, string? buscar, string? estado, string? orden)
    {
        var cultura = CultureInfo.GetCultureInfo("es-CL").CompareInfo;
        var query = items.Where(i => (string.IsNullOrEmpty(buscar) || cultura.IndexOf(i.Nombre, buscar, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)
            && (string.IsNullOrEmpty(estado) || i.Evaluacion.Estado == estado));
        return orden == "brecha" ? query.OrderBy(i => i.IsTasa).ThenBy(i => i.Evaluacion.Brecha == null).ThenBy(i => i.Evaluacion.Brecha).ThenBy(i => i.Orden).ToList()
            : orden == "cumplimiento" ? query.OrderBy(i => i.Evaluacion.Cumplimiento == null).ThenBy(i => i.Evaluacion.Cumplimiento).ThenBy(i => i.Orden).ToList()
            : query.OrderBy(i => i.Orden).ThenBy(i => i.Id).ToList();
    }
    public const string Criterios = "Resultado y esperado usan la misma unidad; la brecha es resultado menos esperado (pp para porcentajes). " +
        "Referencia de seguimiento: esperado mensual = meta × mes / 12; semestral = 50% en el primer semestre y 100% en el segundo. " +
        "Cumplida: alcanza la meta; en curso: alcanza el esperado; en riesgo: alcanza al menos el 90% del esperado; crítico: menos del 90%. " +
        "Los semestrales se evalúan al último cierre de junio o diciembre; antes de junio la evaluación es provisional. " +
        "Sin datos, sin denominador o sin meta no se clasifican como riesgo: su cumplimiento aporta cero; se mantienen los ajustes fijos y no se redistribuyen los pesos.";

    public static int MesEvaluacion(bool mensual, int corte) => mensual ? corte : corte >= 12 ? 12 : corte >= 6 ? 6 : corte;

    public static EvaluacionSeguimiento Evaluar(Indicador indicador, IEnumerable<ResultadoIndicador> registros,
        int corte, IEnumerable<ResultadoIndicador>? anteriores = null)
    {
        if (corte is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(corte));
        var todos = registros.Where(r => r.Mes is >= 1 and <= 12).ToList();
        var mes = MesEvaluacion(indicador.Mensual, corte);
        var filas = todos.Where(r => r.Mes <= mes).ToList();
        decimal Den(IEnumerable<ResultadoIndicador> source)
        {
            var valores = source.Select(r => IndicadorService.ObtenerDenominadorFila(indicador, r)).ToList();
            return valores.Count == 0 ? 0m : indicador.IsColaborativo ? valores.Max() : valores.Sum();
        }
        var den = Den(filas.Where(r => !indicador.EsPeriodoOctubreSep || r.Mes < 10));
        if (indicador.EsPeriodoOctubreSep) den += Den((anteriores ?? []).Where(r => r.Mes >= 10));
        den = decimal.Round(den);
        var num = decimal.Round(filas.Sum(r => r.Numerador + r.NumeradorP));
        if (indicador.IsDenFijo) den = decimal.Round(den * (decimal)indicador.Meta);
        var escala = indicador.IsTasa ? 1m : 100m;
        var metaBase = indicador.IsDenFijo ? 1m : (decimal)indicador.Meta;
        var meta = metaBase * escala;
        // Keep full precision for status boundaries, round only in presentation.
        decimal? actual = filas.Count > 0 && den > 0 ? num / den * escala : null;
        var esperado = meta * (indicador.Mensual ? mes / 12m : mes <= 6 ? .5m : 1m);
        decimal? cumplimiento = actual.HasValue && meta > 0 ? actual.Value / meta * 100m : null;
        var estado = filas.Count == 0 ? "SinDatos" : den <= 0 ? "SinDenominador" : meta <= 0 ? "SinMeta"
            : actual >= meta ? "Cumplida" : actual >= esperado ? "EnCurso"
            : actual >= esperado * .9m ? "EnRiesgo" : "Critico";
        return new()
        {
            MesCorte = corte, MesEvaluacion = mes, UltimoMesDatos = todos.Where(r => r.Mes <= corte).Select(r => (int?)r.Mes).Max(),
            Provisional = !indicador.Mensual && corte < 6, TieneDatos = filas.Count > 0,
            TieneRegistroPeriodo = filas.Any(r => indicador.Mensual ? r.Mes == mes : r.Mes > (mes == 12 ? 6 : 0)),
            Numerador = num, Denominador = den, Meta = meta, Resultado = actual, Cumplimiento = cumplimiento,
            Esperado = esperado, Brecha = actual - esperado, Estado = estado, Unidad = indicador.IsTasa ? "tasa" : "%"
        };
    }

    public static IndicadorSeguimiento Crear(Indicador indicador, List<ResultadoIndicador> filas, int corte,
        List<ResultadoIndicador>? anteriores = null, bool detalle = false)
    {
        anteriores ??= [];
        var evaluacion = Evaluar(indicador, filas, corte, anteriores);
        RegistroSeguimiento Registro(ResultadoIndicador r) => new(r.Mes, r.Numerador,
            indicador.IsColaborativo ? 0m : indicador.IsDenFijo ? r.Denominador * (decimal)indicador.Meta : r.Denominador,
            r.NumeradorP, indicador.IsColaborativo || indicador.IsDenFijo ? 0m : r.DenominadorP,
            r.Establecimiento?.Nombre ?? "Establecimiento", r.Establecimiento?.id_sector ?? 0, r.id_establecimiento);
        var evolucion = detalle ? Enumerable.Range(1, evaluacion.MesEvaluacion)
            .Where(m => indicador.Mensual || m == 6 || m == 12 || evaluacion.Provisional && m == evaluacion.MesEvaluacion)
            .Select(m => Evaluar(indicador, filas, m, anteriores)).ToList() : [];
        var establecimientos = detalle ? filas.Where(r => r.Mes <= evaluacion.MesEvaluacion)
            .GroupBy(r => r.id_establecimiento).Select(g => new EstablecimientoSeguimiento(g.Key,
                g.First().Establecimiento?.Nombre ?? "Establecimiento", g.First().Establecimiento?.Sector?.Nombre ?? "—",
                Evaluar(indicador, g, corte, anteriores.Where(r => r.id_establecimiento == g.Key)),
                g.OrderBy(r => r.Mes).Select(Registro).ToList()))
            .OrderBy(e => e.Sector).ThenBy(e => e.Nombre).ToList() : [];
        var analisis = new List<string>();
        var cultura = CultureInfo.GetCultureInfo("es-CL");
        string Valor(decimal? v) => v?.ToString("N1", cultura) ?? "—";
        if (detalle)
        {
            analisis.Add(evaluacion.Resultado.HasValue
                ? $"Resultado acumulado: {Valor(evaluacion.Resultado)}{(indicador.IsTasa ? "" : "%")}; cumplimiento de la meta: {Valor(evaluacion.Cumplimiento)}%."
                : evaluacion.TieneDatos ? "Existen registros, pero falta un denominador válido para evaluar el resultado." : "No hay registros en el período evaluado.");
            var observados = evolucion.Where(e => e.Resultado.HasValue).ToList();
            if (observados.Count >= 2)
            {
                var cambio = observados[^1].Resultado - observados[^2].Resultado;
                analisis.Add($"Variación respecto de la evaluación anterior: {Valor(cambio)} {(indicador.IsTasa ? "unidades" : "pp")}. Los cambios de denominador también pueden modificar el resultado acumulado.");
            }
            if (observados.Count > 0)
            {
                var mejor = observados.MaxBy(e => e.Resultado)!;
                var menor = observados.MinBy(e => e.Resultado)!;
                analisis.Add($"Mayor resultado acumulado: {cultura.DateTimeFormat.GetMonthName(mejor.MesEvaluacion)} ({Valor(mejor.Resultado)}). Menor: {cultura.DateTimeFormat.GetMonthName(menor.MesEvaluacion)} ({Valor(menor.Resultado)}).");
            }
            if (indicador.IsColaborativo) analisis.Add("El denominador es comunal: los establecimientos aportan producción, no metas individuales. No se suman denominadores repetidos.");
            if (indicador.IsDenFijo) analisis.Add($"El denominador mostrado corresponde al objetivo calculado con la meta configurada; la meta efectiva es {Valor(evaluacion.Meta)}{(indicador.IsTasa ? "" : "%")}.");
            if (indicador.EsPeriodoOctubreSep) analisis.Add("El denominador incluye octubre-diciembre del año anterior y enero-septiembre del año consultado, hasta el corte evaluado.");
            if (filas.Any(r => r.Mes <= evaluacion.MesEvaluacion && (r.NumeradorP != 0 || r.DenominadorP != 0))) analisis.Add("Los totales incorporan la serie P aplicable. El desglose muestra sus aportes por separado.");
            if (evaluacion.Provisional) analisis.Add("Evaluación provisional: todavía no existe un cierre semestral en el período seleccionado.");
            if (!indicador.IsColaborativo)
            {
                var pendientes = establecimientos.Where(e => e.Evaluacion.Estado is "Critico" or "EnRiesgo").Select(e => e.Nombre).ToList();
                if (pendientes.Count > 0) analisis.Add($"Establecimientos que requieren atención al corte: {string.Join(", ", pendientes)}.");
            }
        }
        return new()
        {
            EsDetalle = detalle, Id = indicador.Id, Nombre = indicador.Nombre, Año = indicador.Año, Orden = indicador.Orden,
            Tipoindicador = indicador.Tipoindicador ?? 0, Mensual = indicador.Mensual, IsTasa = indicador.IsTasa,
            IsColaborativo = indicador.IsColaborativo, IsDenFijo = indicador.IsDenFijo,
            EsPeriodoOctubreSep = indicador.EsPeriodoOctubreSep, Detalle = indicador.Detalle, Peso = (decimal)indicador.Peso,
            Evaluacion = evaluacion, Evolucion = evolucion, Establecimientos = establecimientos, Analisis = analisis,
            Resultados = detalle ? filas.Where(r => r.Mes <= evaluacion.MesEvaluacion).Select(Registro).ToList() : []
        };
    }
}

public class SeguimientoService(RemToolDataContext db, IIndicadorService filtros)
{
    public static bool CorteValido(int? corte) => !corte.HasValue || corte is >= 1 and <= 12;
    private static List<ResultadoIndicador> Filtrar(IEnumerable<ResultadoIndicador> filas, ResultadoFiltroEstablecimiento filtro, long? sector, long? establecimiento) =>
        filas.Where(r => filtro.Incluye(r.id_establecimiento)
            && (!sector.HasValue || sector == 0 || r.Establecimiento?.id_sector == sector)
            && (!establecimiento.HasValue || establecimiento == 0 || r.id_establecimiento == establecimiento)).ToList();

    public async Task<List<IndicadorSeguimiento>> Indicadores(int año, int tipo, int? corte, long? sector, long? establecimiento,
        int? convenio = null, int? id = null, bool detalle = false)
    {
        var query = db.Indicador.AsNoTracking().Where(i => i.Año == año && (i.Tipoindicador ?? 0) == tipo);
        if (id.HasValue) query = query.Where(i => i.Id == id);
        if (convenio.HasValue && convenio != 0) query = query.Where(i => i.IndicadorConvenios.Any(c => c.id_convenio == convenio));
        var indicadores = await query.Include(i => i.ResultadoIndicadors).ThenInclude(r => r.Establecimiento).ThenInclude(e => e.Sector)
            .OrderBy(i => i.Orden).ThenBy(i => i.Id).ToListAsync();
        var reglas = await filtros.GetFiltrosAsync(indicadores.Select(i => i.Id));
        var filas = indicadores.ToDictionary(i => i.Id, i => Filtrar(i.ResultadoIndicadors, reglas[i.Id], sector, establecimiento));
        var ordenes = indicadores.Where(i => i.EsPeriodoOctubreSep).Select(i => i.Orden).ToList();
        var anteriores = ordenes.Count > 0 ? await db.Indicador.AsNoTracking()
            .Where(i => i.Año == año - 1 && i.Tipoindicador == tipo && ordenes.Contains(i.Orden))
            .Include(i => i.ResultadoIndicadors).ThenInclude(r => r.Establecimiento).ToListAsync() : [];
        // An explicit cut stays fixed across list, detail and export, even when some data arrive later.
        int mes = corte ?? filas.Values.SelectMany(r => r).Select(r => r.Mes).Where(m => m is >= 1 and <= 12).DefaultIfEmpty(1).Max();
        var resultado = indicadores.Select(i => EvaluadorSeguimiento.Crear(i, filas[i.Id], mes,
            Filtrar(anteriores.Where(p => p.Orden == i.Orden).SelectMany(p => p.ResultadoIndicadors), reglas[i.Id], sector, establecimiento), detalle)).ToList();
        if (tipo == 2 && resultado.Count > 0)
        {
            var primero = await db.Indicador.Where(i => i.Año == año && i.Tipoindicador == tipo).OrderBy(i => i.Orden).ThenBy(i => i.Id).Select(i => i.Id).FirstAsync();
            var item = resultado.FirstOrDefault(i => i.Id == primero);
            if (item != null) item.Ajuste = .0625m;
        }
        return resultado;
    }

    public async Task<IndicadorSeguimiento?> Indicador(int id, int? corte, long? sector, long? establecimiento)
    {
        var item = await db.Indicador.AsNoTracking().Where(i => i.Id == id).Select(i => new { i.Año, i.Tipoindicador }).FirstOrDefaultAsync();
        return item == null ? null : (await Indicadores(item.Año, item.Tipoindicador ?? 0, corte, sector, establecimiento, id: id, detalle: true)).FirstOrDefault();
    }

    public async Task<List<ConvenioSeguimiento>> Convenios(int año, int? corte, long? sector, long? establecimiento, int? id = null, bool detalle = false)
    {
        var convenios = await db.Convenio.AsNoTracking().Where(c => !id.HasValue || c.Id == id)
            .Include(c => c.IndicadorConvenios).OrderBy(c => c.Nombre).ToListAsync();
        var indicadores = await Indicadores(año, 1, corte, sector, establecimiento, convenio: id, detalle: detalle);
        var mes = indicadores.FirstOrDefault()?.Evaluacion.MesCorte ?? corte ?? 1;
        return convenios.Select(c =>
        {
            var ids = c.IndicadorConvenios.Select(i => i.id_indicador).ToHashSet();
            var items = indicadores.Where(i => ids.Contains(i.Id)).ToList();
            return new ConvenioSeguimiento(c.Id, c.Nombre, año, mes, items, ResumenSeguimiento.Crear(items, false));
        }).ToList();
    }
}

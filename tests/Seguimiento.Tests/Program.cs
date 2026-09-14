using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;
using RemTool.Controllers;
using RemTool.Services;
using RemTool.Shared;

var count = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; }
Indicador Indicator(bool mensual = true, bool tasa = false, bool fijo = false, bool comunal = false, bool octubre = false, float meta = 1f) =>
    new() { Id = 1, Año = 2026, Nombre = "Indicador de prueba", Mensual = mensual, IsTasa = tasa, IsDenFijo = fijo, IsColaborativo = comunal, EsPeriodoOctubreSep = octubre, Meta = meta, Peso = .5f };
ResultadoIndicador Row(int mes, decimal num, decimal den, long est = 1, decimal numP = 0, decimal denP = 0) =>
    new() { Mes = mes, Numerador = num, Denominador = den, NumeradorP = numP, DenominadorP = denP, id_establecimiento = est,
        Establecimiento = new() { Id = est, Nombre = "Establecimiento " + est, id_sector = est, Sector = new() { Id = est, Nombre = "Sector " + est } } };
var regular = Indicator(meta: .8f);
var sobre = EvaluadorSeguimiento.Crear(regular, [Row(6, 1929, 1000)], 6, detalle: true);
Check(sobre.Evaluacion.Resultado == 192.9m, "Conserva resultado superior al 100%");
Check(sobre.Evaluacion.Cumplimiento == 241.125m, "Cumplimiento distinto del resultado");
Check(sobre.Evaluacion.Estado == "Cumplida", "Sobrecumplimiento cumplido");
Check(sobre.Avance == 2.41125m, "Escala legacy del detalle sin recorte");
Check(ResumenSeguimiento.Crear([sobre], false).Cumplimiento == 100, "Promedio limita cada aporte, no los valores");
Check(ResumenSeguimiento.Crear([sobre], true).Cumplimiento == 50, "Ponderación conservada");
sobre.Ajuste = .0625m;
Check(ResumenSeguimiento.Crear([sobre], true).Cumplimiento == 56.25m, "Ajuste de metas preservado");
foreach (var (num, estado) in new[] { (100m, "Cumplida"), (50m, "EnCurso"), (45m, "EnRiesgo"), (44.99m, "Critico"), (0m, "Critico") })
    Check(EvaluadorSeguimiento.Evaluar(Indicator(), [Row(6, num * 100, 10000)], 6).Estado == estado, "Umbral " + estado);
Check(EvaluadorSeguimiento.Evaluar(regular, [], 7).Estado == "SinDatos", "Ausencia de datos neutral");
Check(EvaluadorSeguimiento.Evaluar(regular, [Row(6, 1, 0)], 7).Resultado == null, "Sin denominador no inventa resultado");
Check(EvaluadorSeguimiento.Evaluar(regular, [Row(6, 1, 0)], 7).Estado == "SinDenominador", "Sin denominador distinto de ausencia");
Check(EvaluadorSeguimiento.Evaluar(Indicator(meta: 0), [Row(6, 1, 100)], 7).Estado == "SinMeta", "Meta cero neutral");
Check(EvaluadorSeguimiento.Evaluar(Indicator(tasa: true, meta: 6.3f), [Row(6, 520, 100)], 6).Resultado == 5.2m, "Tasa no multiplica por 100");
var cero = EvaluadorSeguimiento.Crear(regular, [Row(6, 0, 100)], 7, detalle: true);
Check(cero.Establecimientos.Count == 1 && cero.Evaluacion.Resultado == 0, "Producción cero sigue visible");
var p = EvaluadorSeguimiento.Evaluar(regular, [Row(6, 10, 100, numP: 5, denP: 20)], 6);
Check(p.Numerador == 15 && p.Denominador == 120, "Serie P se incorpora una vez");
var fijo = EvaluadorSeguimiento.Evaluar(Indicator(fijo: true, meta: .5f), [Row(6, 10, 100, numP: 5, denP: 100)], 6);
Check(fijo.Denominador == 50 && fijo.Meta == 100 && fijo.Numerador == 15, "Objetivo fijo y serie P sin denominador duplicado");
var comunal = EvaluadorSeguimiento.Crear(Indicator(comunal: true), [Row(1, 10, 100), Row(2, 20, 100), Row(2, 30, 100, 2)], 2, detalle: true);
Check(comunal.Denominador == 100 && comunal.Numerador == 60, "Denominador comunal único");
Check(comunal.Resultados.All(r => r.Denominador == 0), "Contrato de aportes comunales");
var repetidos = new List<ResultadoIndicador> { Row(1, 10, 100, 1), Row(1, 20, 100, 2) };
repetidos.ForEach(r => r.Establecimiento.Nombre = "Mismo nombre");
Check(EvaluadorSeguimiento.Crear(regular, repetidos, 1, detalle: true).Establecimientos.Count == 2, "Agrupar por ID, no por nombre");
var oct = EvaluadorSeguimiento.Evaluar(Indicator(octubre: true), [Row(1, 10, 100), Row(10, 20, 999)], 12, [Row(10, 0, 20), Row(11, 0, 30)]);
Check(oct.Denominador == 150 && oct.Numerador == 30, "Octubre-septiembre excluye denominadores actuales oct-dic");
var octComunal = EvaluadorSeguimiento.Evaluar(Indicator(octubre: true, comunal: true), [Row(1, 10, 100), Row(2, 20, 100, 2)], 7, [Row(10, 0, 20), Row(11, 0, 20, 2)]);
Check(octComunal.Denominador == 120, "Octubre-septiembre comunal usa máximo en cada período");
var sem = Indicator(mensual: false);
foreach (var (corte, esperadoMes, provisional) in new[] { (3, 3, true), (6, 6, false), (7, 6, false), (12, 12, false) })
{
    var evaluation = EvaluadorSeguimiento.Evaluar(sem, [Row(3, 10, 100), Row(6, 20, 100), Row(7, 50, 100), Row(12, 60, 100)], corte);
    Check(evaluation.MesEvaluacion == esperadoMes && evaluation.Provisional == provisional, "Cierre semestral " + corte);
    Check(evaluation.Esperado == (corte < 12 ? 50 : 100), "Esperado semestral " + corte);
    if (corte == 7) Check(evaluation.Numerador == 30, "Julio no se incorpora al cierre de junio");
}
var serial = new JsonSerializerOptions(JsonSerializerDefaults.Web);
foreach (var source in new[] { regular, sem, Indicator(tasa: true), Indicator(fijo: true), Indicator(comunal: true), Indicator(octubre: true) })
{
    var rows = new List<ResultadoIndicador> { Row(3, 10, 100), Row(6, 20, 100), Row(7, 30, 100, 2) };
    var list = EvaluadorSeguimiento.Crear(source, rows, 7);
    var detail = EvaluadorSeguimiento.Crear(source, rows, 7, detalle: true);
    Check(JsonSerializer.Serialize(list.Evaluacion, serial) == JsonSerializer.Serialize(detail.Evaluacion, serial), "Paridad lista-detalle-PDF " + source.IsTasa);
    Check(detail.Evolucion.Last().Resultado == detail.Evaluacion.Resultado, "Última evolución coincide con resultado");
}
var late = EvaluadorSeguimiento.Evaluar(regular, [Row(3, 20, 100)], 9);
Check(late.MesCorte == 9 && late.UltimoMesDatos == 3 && late.Esperado == 60, "Corte explícito y antigüedad no se confunden");
Check(!SeguimientoService.CorteValido(0) && !SeguimientoService.CorteValido(13) && SeguimientoService.CorteValido(null), "Validación de cortes");
Check(await new IndicadorController(null!, null!, null!).GetIndicador(1, mesCorte: 0) is BadRequestObjectResult, "API detalle rechaza corte inválido");
Check(await new ConvenioController(null!).GetConvenios(2026, mesCorte: 13) is BadRequestObjectResult, "API convenio rechaza corte inválido");
Check(await new ReporteController(null!, null!).DetalleIndicador(1, mesCorte: -1) is BadRequestObjectResult, "PDF rechaza corte inválido");
var sinDatos = EvaluadorSeguimiento.Crear(regular, [], 6);
Check(ResumenSeguimiento.Crear([sobre, sinDatos], false).Cumplimiento == 50, "No redistribuye aportes de indicadores no evaluables");
Check(EvaluadorSeguimiento.Seleccionar([sobre, sinDatos], "PRUEBA", "SinDatos", "cumplimiento").Count == 1, "Filtro de respaldo equivalente a pantalla");
Check(SeguimientoPdf.Numero(56.25m) == "56,3", "Mismo redondeo de presentación que Intl en frontend");
Check(SeguimientoPdf.Brecha(null, false) == "—" && SeguimientoPdf.Brecha(2.3m, true) == "+2,3 u.", "Mismas unidades de brecha que pantalla");
Console.WriteLine($"OK: {count} comprobaciones de seguimiento.");

// Synthetic fixtures only. No application startup or database migrations are executed.
List<IndicadorSeguimiento> Fixtures(int tipo, int corte = 7, long? sector = null, long? est = null, bool detail = true)
{
    var nombres = new[] { "Cobertura de controles de salud integral", "Continuidad de atención y seguimiento de usuarios", "Incremento de atenciones de ronda (controles, consultas y EMP) realizadas en postas rurales de la comuna", "Evaluación semestral de controles preventivos", "Seguimiento sin registros disponibles", "Prestaciones con producción registrada en cero" };
    return Enumerable.Range(1, 6).Select(n =>
    {
        var indicator = Indicator(mensual: n != 4, tasa: n == 2, comunal: n == 3, meta: n == 2 ? 6.3f : .8f);
        indicator.Id = tipo * 100 + n; indicator.Tipoindicador = tipo; indicator.Orden = n; indicator.Nombre = nombres[n - 1]; indicator.Peso = 1f / 6;
        indicator.Detalle = "Datos sintéticos de validación. Este indicador permite revisar el seguimiento por establecimiento y período, incluyendo producción registrada en cero.";
        var rows = n == 5 ? new List<ResultadoIndicador>() : Enumerable.Range(1, 7).SelectMany(m => new[] {
            Row(m, n == 6 ? 0 : n == 2 ? 520 : n == 3 ? 190 : n == 1 ? 60 : 30, 100),
            Row(m, n == 6 ? 0 : n == 2 ? 340 : n == 3 ? 150 : 15, 100, 2)
        }).Where(r => (!sector.HasValue || r.Establecimiento.id_sector == sector) && (!est.HasValue || r.id_establecimiento == est)).ToList();
        var result = EvaluadorSeguimiento.Crear(indicator, rows, corte, detalle: detail);
        if (tipo == 2 && n == 1) result.Ajuste = .0625m;
        return result;
    }).ToList();
}
List<ConvenioSeguimiento> Convenios(List<IndicadorSeguimiento> items, int year, int corte) => [
    new(11, "Fortalecimiento de la atención primaria y continuidad de cuidados", year, corte, items.Take(3).ToList(), ResumenSeguimiento.Crear(items.Take(3).ToList(), false)),
    new(12, "Programa de seguimiento y prestaciones preventivas", year, corte, items.Skip(3).ToList(), ResumenSeguimiento.Crear(items.Skip(3).ToList(), false))];

QuestPDF.Settings.License = LicenseType.Community;
if (args.Contains("--pdf"))
{
    var output = Path.GetFullPath("tmp/seguimiento-qa"); Directory.CreateDirectory(output);
    var fixture = Fixtures(2);
    File.WriteAllBytes(Path.Combine(output, "indicadores.pdf"), SeguimientoPdf.Generar("Metas Sanitarias · Validación", "Datos sintéticos", 2026, 7, fixture, ResumenSeguimiento.Crear(fixture, true)));
    File.WriteAllBytes(Path.Combine(output, "detalle.pdf"), SeguimientoPdf.Generar(fixture[2].Nombre, "Datos sintéticos", 2026, 7, [fixture[2]], soloDetalle: true));
    var convenios = Convenios(Fixtures(1), 2026, 7);
    File.WriteAllBytes(Path.Combine(output, "convenios.pdf"), SeguimientoPdf.Generar("Convenios · Validación", "Datos sintéticos", 2026, 7, convenios.SelectMany(c => c.Indicadores).ToList(), convenios: convenios));
    File.WriteAllText(Path.Combine(output, "fixtures.json"), JsonSerializer.Serialize(fixture, serial));
    Console.WriteLine("PDF y JSON generados en " + output);
}

if (args.Contains("--serve"))
{
    var builder = WebApplication.CreateBuilder();
    builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
    var app = builder.Build(); app.UseCors();
    int Corte(HttpRequest r) => int.TryParse(r.Query["mesCorte"], out int m) && m is >= 1 and <= 12 ? m : 7;
    long? Id(HttpRequest r, string key) => long.TryParse(r.Query[key], out long id) && id > 0 ? id : null;
    List<IndicadorSeguimiento> Items(HttpRequest r, int tipo) => Fixtures(tipo, Corte(r), Id(r, "sectorId"), Id(r, "establecimientoId"));
    app.MapGet("/api/getSectores", () => new[] { new { id = 1, nombre = "Sector norte" }, new { id = 2, nombre = "Sector sur" } });
    var ests = new[] { new { id = 1, nombre = "Centro de salud norte", codDeis = "001" }, new { id = 2, nombre = "Posta rural sur", codDeis = "002" } };
    app.MapGet("/api/getEstablecimientos", () => ests);
    app.MapGet("/api/getEstablecimientos/{sector:int}", (int sector) => ests.Where(e => e.id == sector));
    app.MapGet("/api/getIndicadores/{year:int}/{tipo:int}", (HttpRequest r, int year, int tipo) => { var items = Items(r, tipo); return Results.Json(new { items, resumen = ResumenSeguimiento.Crear(items, tipo != 1), mesCorte = Corte(r), criterios = EvaluadorSeguimiento.Criterios }); });
    app.MapGet("/api/getIndicador/{id:int}", (HttpRequest r, int id) => { var item = Items(r, id / 100).FirstOrDefault(i => i.Id == id); return item == null ? Results.NotFound() : Results.Json(item); });
    app.MapGet("/api/getConvenios/{year:int}", (HttpRequest r, int year) => { var items = Convenios(Items(r, 1), year, Corte(r)); return Results.Json(new { items, cumplimiento = items.Average(c => c.Resumen.Cumplimiento), mesCorte = Corte(r), criterios = EvaluadorSeguimiento.Criterios }); });
    app.MapGet("/api/getConvenioIndicadores/{id:int}/{year:int}", (HttpRequest r, int id, int year) => Results.Json(Convenios(Items(r, 1), year, Corte(r)).First(c => c.Id == id)));
    app.MapGet("/api/detalleIndicador/{id:int}", (HttpRequest r, int id) => { var item = Items(r, id / 100).First(i => i.Id == id); return Results.File(SeguimientoPdf.Generar(item.Nombre, "Datos sintéticos", 2026, Corte(r), [item], soloDetalle: true), "application/pdf"); });
    app.MapGet("/api/informeMensual/{tipo:int}/{year:int}", (HttpRequest r, int tipo, int year) => { var all = Items(r, tipo); if (Id(r, "convenioId") is long convenio) all = Convenios(all, year, Corte(r)).First(c => c.Id == convenio).Indicadores; var items = EvaluadorSeguimiento.Seleccionar(all, r.Query["buscar"], r.Query["estado"], r.Query["orden"]); return Results.File(SeguimientoPdf.Generar("Respaldo", "Datos sintéticos", year, Corte(r), items, ResumenSeguimiento.Crear(all, tipo != 1)), "application/pdf"); });
    app.MapGet("/api/informeConvenios/{year:int}", (HttpRequest r, int year) => { var items = Items(r, 1); return Results.File(SeguimientoPdf.Generar("Convenios", "Datos sintéticos", year, Corte(r), items, convenios: Convenios(items, year, Corte(r))), "application/pdf"); });
    await app.RunAsync("http://localhost:62910");
}

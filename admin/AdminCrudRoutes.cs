using Microsoft.EntityFrameworkCore;
using RemTool.Application.Admin;
using RemTool.Shared;
using RemTools;

public static class AdminCrudRoutes
{
    public static void MapCrud(this RouteGroupBuilder api)
    {
        api.MapGet("/catalog", async (RemToolDataContext db, CancellationToken ct) => Results.Ok(new {
            series = await db.SerieRem.AsNoTracking().OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct),
            versions = await db.VersionRem.AsNoTracking().Include(x => x.SerieRem).OrderByDescending(x => x.Fecha).Select(x => new { x.Id, x.Nombre, x.Fecha, serie = x.SerieRem.Nombre }).ToListAsync(ct),
            ruleTypes = await db.TipoRegla.AsNoTracking().OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct),
            indicatorTypes = await db.TipoIndicador.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct),
            establishments = await db.Establecimiento.AsNoTracking().OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre, x.CodDeis }).ToListAsync(ct),
            indicators = await db.Indicador.AsNoTracking().OrderByDescending(x => x.Año).ThenBy(x => x.Orden).Select(x => new { x.Id, x.Nombre, x.Año }).ToListAsync(ct)
        }));
        api.MapGet("/rules", async (RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.Regla.AsNoTracking().Include(x => x.TipoRegla).Include(x => x.VersionREM).ThenInclude(x => x.SerieRem).OrderBy(x => x.Id).Select(x => new { x.Id, x.id_version, x.IdTipoRegla, x.Expresion, x.Mensaje, version = x.VersionREM.Nombre, serie = x.VersionREM.SerieRem.Nombre, tipo = x.TipoRegla.Nombre }).ToListAsync(ct)));
        api.MapPost("/rules", async (RuleCommand command, RemToolDataContext db, CancellationToken ct) => { ValidateRule(command); var x = new Regla { id_version = command.VersionId, IdTipoRegla = command.RuleTypeId, Expresion = command.Expresion.Trim(), Mensaje = command.Mensaje.Trim() }; db.Add(x); await db.SaveChangesAsync(ct); return Results.Ok(new { x.Id }); });
        api.MapPut("/rules/{id:int}", async (int id, RuleCommand command, RemToolDataContext db, CancellationToken ct) => { ValidateRule(command); var x = await db.Regla.FindAsync([id], ct) ?? throw new KeyNotFoundException("Regla no encontrada."); x.id_version = command.VersionId; x.IdTipoRegla = command.RuleTypeId; x.Expresion = command.Expresion.Trim(); x.Mensaje = command.Mensaje.Trim(); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapDelete("/rules/{id:int}", async (int id, RemToolDataContext db, CancellationToken ct) => { var x = await db.Regla.FindAsync([id], ct) ?? throw new KeyNotFoundException("Regla no encontrada."); db.Remove(x); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapGet("/summary-points", async (RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.PuntoResumen.AsNoTracking().Include(x => x.SerieRem).OrderBy(x => x.Categoria).ThenBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre, x.Categoria, x.Expresion, x.IdSerieRem, serie = x.SerieRem.Nombre }).ToListAsync(ct)));
        api.MapPost("/summary-points", async (SummaryPointCommand command, RemToolDataContext db, CancellationToken ct) => { Validate(command.Nombre, command.Expresion); var x = new PuntoResumen { Nombre = command.Nombre.Trim(), Categoria = command.Categoria.Trim(), Expresion = command.Expresion.Trim(), IdSerieRem = command.SeriesId }; db.Add(x); await db.SaveChangesAsync(ct); return Results.Ok(new { x.Id }); });
        api.MapPut("/summary-points/{id:int}", async (int id, SummaryPointCommand command, RemToolDataContext db, CancellationToken ct) => { Validate(command.Nombre, command.Expresion); var x = await db.PuntoResumen.FindAsync([id], ct) ?? throw new KeyNotFoundException("Punto no encontrado."); x.Nombre = command.Nombre.Trim(); x.Categoria = command.Categoria.Trim(); x.Expresion = command.Expresion.Trim(); x.IdSerieRem = command.SeriesId; await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapDelete("/summary-points/{id:int}", async (int id, RemToolDataContext db, CancellationToken ct) => { var x = await db.PuntoResumen.FindAsync([id], ct) ?? throw new KeyNotFoundException("Punto no encontrado."); db.Remove(x); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapGet("/reports", async (int? year, int? month, string? search, RemToolDataContext db, CancellationToken ct) => { var q = db.Reporte.AsNoTracking().Include(x => x.Establecimiento).Include(x => x.Comuna).AsQueryable(); if (year.HasValue) q = q.Where(x => x.Año == year); if (month.HasValue) q = q.Where(x => x.Mes == month); if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Establecimiento.Nombre.Contains(search)); return Results.Ok(await q.OrderByDescending(x => x.Año).ThenByDescending(x => x.Mes).Take(500).Select(x => new { x.Id, x.Año, x.Mes, establecimiento = x.Establecimiento.Nombre, comuna = x.Comuna.Nombre, records = x.Registros.Count }).ToListAsync(ct)); });
        api.MapGet("/reports/{id:int}/records", async (int id, RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.Registro.AsNoTracking().Where(x => x.id_reporte == id).Include(x => x.Prestacion).OrderBy(x => x.Prestacion.CodigoPrestacion).Select(x => new { x.Id, code = x.Prestacion.CodigoPrestacion, x.Valor }).ToListAsync(ct)));
        api.MapDelete("/reports/{id:int}", async (int id, RemToolDataContext db, CancellationToken ct) => { var x = await db.Reporte.FindAsync([id], ct) ?? throw new KeyNotFoundException("Reporte no encontrado."); await db.Registro.Where(r => r.id_reporte == id).ExecuteDeleteAsync(ct); db.Remove(x); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapGet("/convenios", async (RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.Convenio.AsNoTracking().Include(x => x.IndicadorConvenios).OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre, indicatorIds = x.IndicadorConvenios.Select(y => y.id_indicador) }).ToListAsync(ct)));
        api.MapPost("/convenios", async (ConvenioCommand command, RemToolDataContext db, CancellationToken ct) => Results.Ok(new { id = await SaveConvenio(null, command, db, ct) }));
        api.MapPut("/convenios/{id:int}", async (int id, ConvenioCommand command, RemToolDataContext db, CancellationToken ct) => { await SaveConvenio(id, command, db, ct); return Results.NoContent(); });
        api.MapDelete("/convenios/{id:int}", async (int id, RemToolDataContext db, CancellationToken ct) => { var x = await db.Convenio.FindAsync([id], ct) ?? throw new KeyNotFoundException("Convenio no encontrado."); db.Remove(x); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapGet("/percapita", async (RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.PercapitaSsc.AsNoTracking().GroupBy(x => x.AñoCorte).OrderByDescending(g => g.Key).Select(g => new { year = g.Key, rows = g.Count(), establishments = g.Select(x => x.id_establecimiento).Distinct().Count(), inscritos = g.Sum(x => x.Inscritos) }).ToListAsync(ct)));
        api.MapGet("/percapita/{year:int}", async (int year, RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.PercapitaSsc.AsNoTracking().Where(x => x.AñoCorte == year).GroupBy(x => new { x.id_establecimiento, x.Establecimiento.Nombre, x.Establecimiento.CodDeis }).OrderBy(g => g.Key.Nombre).Select(g => new { g.Key.Nombre, g.Key.CodDeis, f = g.Where(x => x.Sexo == "F").Sum(x => x.Inscritos), m = g.Where(x => x.Sexo == "M").Sum(x => x.Inscritos) }).ToListAsync(ct)));
        // Reemplaza completo el corte del año: borra y reinserta en una transacción.
        api.MapPut("/percapita/{year:int}", async (int year, IReadOnlyList<PercapitaRow> rows, RemToolDataContext db, CancellationToken ct) =>
        {
            if (year < 2000 || year > 2100) throw new ArgumentException("Año inválido.");
            if (rows.Count == 0) throw new ArgumentException("El archivo no contiene filas.");
            var ids = (await db.Establecimiento.AsNoTracking().Where(x => x.CodDeis != null).Select(x => new { x.Id, x.CodDeis }).ToListAsync(ct)).GroupBy(x => x.CodDeis!.Trim()).ToDictionary(g => g.Key, g => g.First().Id);
            var entities = rows.Select((r, i) =>
            {
                var line = i + 2; var sexo = r.Sexo.Trim().ToUpperInvariant();
                if (!ids.TryGetValue(r.CodDeis.Trim(), out var est)) throw new ArgumentException($"Fila {line}: código DEIS '{r.CodDeis}' no existe.");
                if (sexo is not ("F" or "M")) throw new ArgumentException($"Fila {line}: sexo debe ser F o M.");
                if (r.Edad < 0 || r.Edad > 130 || r.Inscritos < 0) throw new ArgumentException($"Fila {line}: edad o inscritos fuera de rango.");
                return new PercapitaSsc { id_establecimiento = est, Edad = r.Edad, Sexo = sexo, Inscritos = r.Inscritos, AñoCorte = year };
            }).ToList();
            var dup = entities.GroupBy(x => (x.id_establecimiento, x.Edad, x.Sexo)).FirstOrDefault(g => g.Count() > 1);
            if (dup is not null) throw new ArgumentException($"Fila duplicada: establecimiento {dup.Key.id_establecimiento}, edad {dup.Key.Edad}, sexo {dup.Key.Sexo}.");
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.PercapitaSsc.Where(x => x.AñoCorte == year).ExecuteDeleteAsync(ct);
            db.PercapitaSsc.AddRange(entities); await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new { rows = entities.Count });
        });
        api.MapDelete("/percapita/{year:int}", async (int year, RemToolDataContext db, CancellationToken ct) => { await db.PercapitaSsc.Where(x => x.AñoCorte == year).ExecuteDeleteAsync(ct); return Results.NoContent(); });
        api.MapGet("/rem-informes", async (RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.InformeRem.AsNoTracking().OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre, x.IdSerieRem, serie = x.SerieRem.Nombre, x.SeccionIds }).ToListAsync(ct)));
        api.MapGet("/rem-informes/secciones", async (long serieId, RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.SeccionRem.AsNoTracking().Where(s => s.VersionHoja != null && s.VersionHoja.VersionRem.id_serie == serieId).OrderBy(s => s.VersionHoja!.HojaRem.Nombre).ThenByDescending(s => s.VersionHoja!.VersionRem.Fecha).ThenBy(s => s.Orden).Select(s => new { s.Id, hoja = s.VersionHoja!.HojaRem.Nombre, s.Nombre, s.Codigo, version = s.VersionHoja.VersionRem.Nombre }).ToListAsync(ct)));
        api.MapPost("/rem-informes", async (InformeRemCommand command, RemToolDataContext db, CancellationToken ct) => Results.Ok(new { id = await SaveInformeRem(null, command, db, ct) }));
        api.MapPut("/rem-informes/{id:int}", async (int id, InformeRemCommand command, RemToolDataContext db, CancellationToken ct) => { await SaveInformeRem(id, command, db, ct); return Results.NoContent(); });
        api.MapDelete("/rem-informes/{id:int}", async (int id, RemToolDataContext db, CancellationToken ct) => { var x = await db.InformeRem.FindAsync([id], ct) ?? throw new KeyNotFoundException("Informe no encontrado."); db.Remove(x); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapGet("/filters",async (RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.FiltroEstablecimiento.AsNoTracking().Include(x => x.Establecimiento).Include(x => x.Indicador).OrderBy(x => x.Id).Select(x => new { x.Id, x.id_establecimiento, x.id_indicador, x.Tipo, establishment = x.Establecimiento.Nombre, indicator = x.Indicador == null ? null : x.Indicador.Nombre }).ToListAsync(ct)));
        api.MapPost("/filters", async (FilterCommand command, RemToolDataContext db, CancellationToken ct) => Results.Ok(new { id = await SaveFilter(null, command, db, ct) }));
        api.MapPut("/filters/{id:int}", async (int id, FilterCommand command, RemToolDataContext db, CancellationToken ct) => { await SaveFilter(id, command, db, ct); return Results.NoContent(); });
        api.MapDelete("/filters/{id:int}", async (int id, RemToolDataContext db, CancellationToken ct) => { var x = await db.FiltroEstablecimiento.FindAsync([id], ct) ?? throw new KeyNotFoundException("Filtro no encontrado."); db.Remove(x); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapGet("/indicators", async (int? year, RemToolDataContext db, CancellationToken ct) => { var q = db.Indicador.AsNoTracking().AsQueryable(); if (year.HasValue) q = q.Where(x => x.Año == year); return Results.Ok(await q.OrderByDescending(x => x.Año).ThenBy(x => x.Orden).Select(x => new { x.Id, x.Nombre, x.Formula, x.Año, x.IsDenFijo, x.IsTasa, x.FormulaDenFijo, x.Meta, x.Peso, x.Mensual, x.Orden, x.Detalle, x.Tipoindicador, x.EsPeriodoOctubreSep, x.IsColaborativo, convenioIds = x.IndicadorConvenios.Select(y => y.id_convenio) }).ToListAsync(ct)); });
        api.MapPost("/indicators", async (IndicatorCommand command, RemToolDataContext db, CancellationToken ct) => Results.Ok(new { id = await SaveIndicator(null, command, db, ct) }));
        api.MapPut("/indicators/{id:int}", async (int id, IndicatorCommand command, RemToolDataContext db, CancellationToken ct) => { await SaveIndicator(id, command, db, ct); return Results.NoContent(); });
        api.MapGet("/indicator-results", async (int indicatorId, long establishmentId, RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.ResultadoIndicador.AsNoTracking().Where(x => x.id_indicador == indicatorId && x.id_establecimiento == establishmentId).OrderBy(x => x.Mes).Select(x => new { x.Mes, x.Numerador, x.Denominador, x.NumeradorP, x.DenominadorP, x.EsManual }).ToListAsync(ct)));
        api.MapPut("/indicator-results", async (ManualResultCommand c, RemToolDataContext db, CancellationToken ct) =>
        {
            if (c.Mes is < 1 or > 12) throw new ArgumentException("El mes no es válido.");
            if (c.Numerador < 0 || c.Denominador < 0) throw new ArgumentException("Los valores no pueden ser negativos.");
            if (!await db.Indicador.AnyAsync(x => x.Id == c.IndicatorId, ct) || !await db.Establecimiento.AnyAsync(x => x.Id == c.EstablishmentId, ct)) throw new KeyNotFoundException("Indicador o establecimiento no encontrado.");
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var current = await db.ResultadoIndicador.Where(x => x.id_indicador == c.IndicatorId && x.id_establecimiento == c.EstablishmentId && x.Mes == c.Mes).ToListAsync(ct);
            // Una sola fila manual por mes; conserva los valores de serie P que ya tuviera.
            db.RemoveRange(current);
            db.Add(new ResultadoIndicador { id_indicador = c.IndicatorId, id_establecimiento = c.EstablishmentId, Mes = c.Mes, Numerador = c.Numerador, Denominador = c.Denominador, NumeradorP = current.Sum(x => x.NumeradorP), DenominadorP = current.Sum(x => x.DenominadorP), EsManual = true });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.NoContent();
        });
        // Quita la marca manual: el valor queda hasta el próximo cálculo, que lo reemplaza.
        api.MapDelete("/indicator-results", async (int indicatorId, long establishmentId, int mes, RemToolDataContext db, CancellationToken ct) => { await db.ResultadoIndicador.Where(x => x.id_indicador == indicatorId && x.id_establecimiento == establishmentId && x.Mes == mes).ExecuteUpdateAsync(x => x.SetProperty(r => r.EsManual, false), ct); return Results.NoContent(); });
        api.MapDelete("/indicators/{id:int}", async (int id, RemToolDataContext db, CancellationToken ct) =>
        {
            var x = await db.Indicador.FindAsync([id], ct) ?? throw new KeyNotFoundException("Indicador no encontrado.");
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.ResultadoIndicador.Where(r => r.id_indicador == id).ExecuteDeleteAsync(ct); // FK sin cascada
            db.Remove(x); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.NoContent();
        });
    }
    private static void ValidateRule(RuleCommand command)
    {
        Validate(command.Expresion, command.Mensaje);
        var result = ReglaExpressionService.Validate(command.Expresion);
        if (!result.IsValid) throw new ArgumentException(result.Message);
    }
    private static void Validate(string first, string second) { if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second)) throw new ArgumentException("Los campos obligatorios están vacíos."); }
    private static async Task<int> SaveConvenio(int? id, ConvenioCommand command, RemToolDataContext db, CancellationToken ct) { if (string.IsNullOrWhiteSpace(command.Nombre)) throw new ArgumentException("El nombre es obligatorio."); if (await db.Convenio.AnyAsync(x => x.Id != id && x.Nombre == command.Nombre.Trim(), ct)) throw new ArgumentException("Ya existe un convenio con ese nombre."); var x = id is null ? new Convenio() : await db.Convenio.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException("Convenio no encontrado."); x.Nombre = command.Nombre.Trim(); if (id is null) db.Add(x); await db.SaveChangesAsync(ct); var selected = command.IndicatorIds.Distinct().ToHashSet(); var current = await db.IndicadorConvenio.Where(y => y.id_convenio == x.Id).ToListAsync(ct); db.RemoveRange(current.Where(y => !selected.Contains(y.id_indicador))); foreach (var indicatorId in selected.Except(current.Select(y => y.id_indicador))) db.IndicadorConvenio.Add(new IndicadorConvenio { id_convenio = x.Id, id_indicador = indicatorId }); await db.SaveChangesAsync(ct); return x.Id; }
    private static async Task<int> SaveInformeRem(int? id, InformeRemCommand command, RemToolDataContext db, CancellationToken ct)
    {
        var nombre = command.Nombre?.Trim() ?? "";
        if (nombre.Length == 0) throw new ArgumentException("El nombre es obligatorio.");
        var ids = command.SeccionIds.Distinct().ToList();
        if (ids.Count == 0) throw new ArgumentException("Selecciona al menos una sección.");
        if (await db.SeccionRem.CountAsync(s => ids.Contains(s.Id) && s.VersionHoja != null && s.VersionHoja.VersionRem.id_serie == command.SeriesId, ct) != ids.Count) throw new ArgumentException("Hay secciones que no pertenecen a la serie seleccionada.");
        if (await db.InformeRem.AnyAsync(x => x.Id != id && x.IdSerieRem == command.SeriesId && x.Nombre == nombre, ct)) throw new ArgumentException("Ya existe un informe con ese nombre en la serie.");
        var x = id is null ? new InformeRem() : await db.InformeRem.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException("Informe no encontrado.");
        x.Nombre = nombre; x.IdSerieRem = command.SeriesId; x.SeccionIds = ids;
        if (id is null) db.Add(x);
        await db.SaveChangesAsync(ct);
        return x.Id;
    }
    private static async Task<int> SaveFilter(int? id, FilterCommand command, RemToolDataContext db, CancellationToken ct) { if (command.IndicatorId is null && command.Tipo == 1) throw new ArgumentException("No se permite una inclusión global."); if (await db.FiltroEstablecimiento.AnyAsync(x => x.Id != id && x.id_establecimiento == command.EstablishmentId && x.id_indicador == command.IndicatorId, ct)) throw new ArgumentException("Ya existe ese filtro."); var x = id is null ? new FiltroEstablecimiento() : await db.FiltroEstablecimiento.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException("Filtro no encontrado."); x.id_establecimiento = command.EstablishmentId; x.id_indicador = command.IndicatorId; x.Tipo = (RemTool.Shared.Enum.TipoFiltroEstablecimiento)command.Tipo; if (id is null) db.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
    private static async Task<int> SaveIndicator(int? id, IndicatorCommand c, RemToolDataContext db, CancellationToken ct)
    {
        Validate(c.Nombre, c.Formula);
        if (c.Year is < 2000 or > 2100) throw new ArgumentException("El año no es válido.");
        IndicatorCalculationService.ParseFormula(c.Formula); // rechaza fórmulas que el cálculo no podría evaluar
        var x = id is null ? new Indicador() : await db.Indicador.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException("Indicador no encontrado.");
        x.Nombre = c.Nombre.Trim(); x.Formula = c.Formula; x.Año = c.Year; x.IsDenFijo = c.IsDenFijo; x.IsTasa = c.IsTasa;
        x.FormulaDenFijo = string.IsNullOrWhiteSpace(c.FormulaDenFijo) ? null : c.FormulaDenFijo.Trim(); x.Meta = c.Meta; x.Peso = c.Peso; x.Mensual = c.Mensual; x.Orden = c.Orden;
        x.Detalle = string.IsNullOrWhiteSpace(c.Detalle) ? null : c.Detalle.Trim(); x.Tipoindicador = c.TipoIndicador; x.EsPeriodoOctubreSep = c.EsPeriodoOctubreSep; x.IsColaborativo = c.IsColaborativo;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (id is null) db.Add(x);
        await db.SaveChangesAsync(ct);
        if (c.ConvenioIds is not null)
        {
            var selected = c.ConvenioIds.ToHashSet();
            var current = await db.IndicadorConvenio.Where(y => y.id_indicador == x.Id).ToListAsync(ct);
            db.RemoveRange(current.Where(y => !selected.Contains(y.id_convenio)));
            foreach (var convenioId in selected.Except(current.Select(y => y.id_convenio))) db.IndicadorConvenio.Add(new IndicadorConvenio { id_indicador = x.Id, id_convenio = convenioId });
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return x.Id;
    }
}
public sealed record RuleCommand(int VersionId, int RuleTypeId, string Expresion, string Mensaje);
public sealed record SummaryPointCommand(string Nombre, string Categoria, string Expresion, long SeriesId);
public sealed record ConvenioCommand(string Nombre, IReadOnlyList<int> IndicatorIds);
public sealed record InformeRemCommand(string Nombre, long SeriesId, IReadOnlyList<long> SeccionIds);
public sealed record FilterCommand(long EstablishmentId, int? IndicatorId, int Tipo);
public sealed record IndicatorCommand(string Nombre, string Formula, int Year, bool IsDenFijo, bool IsTasa, string? FormulaDenFijo, float Meta, float Peso, bool Mensual, int Orden, string? Detalle, int? TipoIndicador, bool EsPeriodoOctubreSep, bool IsColaborativo, IReadOnlyList<int>? ConvenioIds = null);
public sealed record PercapitaRow(string CodDeis, int Edad, string Sexo, int Inscritos);
public sealed record ManualResultCommand(int IndicatorId, long EstablishmentId, int Mes, decimal Numerador, decimal Denominador);

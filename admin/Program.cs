using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RemTool.Application.Admin;
using RemTool.Shared;
using RemTools;
using RemTools.Consolidados;

var localConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "REMTool", "adminsettings.json");
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(localConfig, optional: true, reloadOnChange: false);
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

var connection = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException($"Configure ConnectionStrings:PostgreSQL in {localConfig}.");
builder.Services.AddDbContextFactory<RemToolDataContext>(options => options.UseNpgsql(connection));
// Minimal API handlers use a scoped context, while long-running application services
// create and dispose their own contexts through the factory.
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<RemToolDataContext>>().CreateDbContext());
builder.Services.Configure<LocalPathsOptions>(builder.Configuration.GetSection("Paths"));
builder.Services.AddSingleton(sp => new LocalPathPolicy(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LocalPathsOptions>>().Value));
builder.Services.AddScoped<InformativoService>();
builder.Services.AddSingleton<RemImportService>();
builder.Services.AddSingleton<IndicatorCalculationService>();
builder.Services.AddSingleton<RemIntegrityService>();
ExcelPackage.License.SetNonCommercialOrganization("DESAM Monte Patria");

var app = builder.Build();
app.UseExceptionHandler(errors => errors.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    context.Response.StatusCode = exception is KeyNotFoundException ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
    await context.Response.WriteAsJsonAsync(new { error = exception?.Message ?? "Error inesperado." });
}));
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/admin-api");
api.MapCrud();
api.MapGet("/status", (LocalPathPolicy paths) => Results.Ok(new { localOnly = true, publicationsDirectory = paths.PublicationsDirectory }));

api.MapPost("/rules/validate", (ExpressionRequest request) => Results.Ok(ReglaExpressionService.Validate(request.Expression ?? string.Empty)));
api.MapPost("/rem/import", async (HttpRequest request, RemImportService service, LocalPathPolicy paths, CancellationToken ct) =>
{
    var form = await request.ReadFormAsync(ct);
    if (!int.TryParse(form["year"], out var year)) throw new ArgumentException("El año es obligatorio.");
    var series = form["series"].ToString();
    var files = form.Files.GetFiles("files"); if (files.Count == 0) throw new ArgumentException("Debe adjuntar uno o más archivos REM.");
    var saved = new List<string>();
    try { foreach (var file in files) { if (!Path.GetExtension(file.FileName).Equals(".xlsm", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Sólo se admiten archivos .xlsm."); var path = paths.CreateWorkingFile(".xlsm"); await using var stream = File.Create(path); await file.CopyToAsync(stream, ct); saved.Add(path); } return Results.Ok(await service.ImportAsync(saved, series, year, ct)); }
    finally { foreach (var path in saved) if (File.Exists(path)) File.Delete(path); }
});
api.MapPost("/indicators/calculate/{year:int}", async (int year, IndicatorCalculationService service, CancellationToken ct) => Results.Ok(await service.CalculateAsync(year, ct)));
api.MapPost("/rem/integrity", async (HttpRequest request, RemIntegrityService service, LocalPathPolicy paths, CancellationToken ct) =>
{
    var files = (await request.ReadFormAsync(ct)).Files.GetFiles("files"); if (files.Count == 0) throw new ArgumentException("Debe adjuntar archivos REM."); var saved = new List<string>();
    try { foreach (var file in files) { var path = paths.CreateWorkingFile(Path.GetExtension(file.FileName)); await using var stream = File.Create(path); await file.CopyToAsync(stream, ct); saved.Add(path); } return Results.Ok(await service.CheckAsync(saved, ct)); }
    finally { foreach (var path in saved) if (File.Exists(path)) File.Delete(path); }
});

api.MapGet("/informativos", async (InformativoService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)));
api.MapPost("/informativos", async (InformativoCommand request, InformativoService service, CancellationToken ct) =>
    Results.Ok(await service.SaveAsync(null, request, ct)));
api.MapPut("/informativos/{id:int}", async (int id, InformativoCommand request, InformativoService service, CancellationToken ct) =>
    Results.Ok(await service.SaveAsync(id, request, ct)));
api.MapDelete("/informativos/{id:int}", async (int id, InformativoService service, CancellationToken ct) => { await service.DeleteAsync(id, ct); return Results.NoContent(); });

api.MapGet("/consolidados/versions", async (IDbContextFactory<RemToolDataContext> factory, CancellationToken ct) =>
{
    var reader = new ConsolidadoDataReader(() => factory.CreateDbContext());
    return Results.Ok(await reader.GetVersionsAsync(ct));
});
api.MapPost("/consolidados/generate", async (HttpRequest request, IDbContextFactory<RemToolDataContext> factory,
    LocalPathPolicy paths, CancellationToken ct) =>
{
    var form = await request.ReadFormAsync(ct);
    var template = form.Files.GetFile("template") ?? throw new ArgumentException("Debe adjuntar la plantilla Excel.");
    if (!Path.GetExtension(template.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("La plantilla debe ser un archivo .xlsx.");
    if (!int.TryParse(form["year"], out var year) || !int.TryParse(form["versionId"], out var versionId))
        throw new ArgumentException("Año o versión inválidos.");
    var working = paths.CreateWorkingFile(".xlsx");
    try
    {
        await using (var output = File.Create(working)) await template.CopyToAsync(output, ct);
        var reader = new ConsolidadoDataReader(() => factory.CreateDbContext());
        var generator = new ConsolidadoGenerationService(reader);
        var messages = new List<string>();
        var result = await generator.GenerateAsync(new GenerationRequest(year, versionId, working, paths.PublicationsDirectory),
            new Progress<string>(messages.Add), ct);
        return Results.Ok(new { result.FilePath, result.RowCount, result.Bytes, elapsedSeconds = result.Elapsed.TotalSeconds, result.Warnings, messages });
    }
    finally { if (File.Exists(working)) File.Delete(working); }
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    var url = app.Urls.Single();
    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
});
app.Run();

public sealed record ExpressionRequest(string? Expression);

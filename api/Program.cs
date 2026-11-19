using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using RemTool;
using RemTool.Shared;
using RemTool.Controllers;
using RemTool.Services;
using RemTool.Dal;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// REBUILD_MARKER_v2
// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<RemToolDataContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "REMTool")
                        .MigrationsAssembly("RemTool")));

var configuredFinanzasConnectionString = builder.Configuration.GetConnectionString("ConveniosFinancieros")
    ?? "Data Source=Data/convenios-financieros.db";
var finanzasConnectionBuilder = new SqliteConnectionStringBuilder(configuredFinanzasConnectionString);
if (!Path.IsPathRooted(finanzasConnectionBuilder.DataSource))
    finanzasConnectionBuilder.DataSource = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, finanzasConnectionBuilder.DataSource));
var finanzasDataDirectory = Path.GetDirectoryName(finanzasConnectionBuilder.DataSource);
if (!string.IsNullOrWhiteSpace(finanzasDataDirectory)) Directory.CreateDirectory(finanzasDataDirectory);
var finanzasConnectionString = finanzasConnectionBuilder.ToString();
builder.Services.AddDbContext<ConveniosFinancierosDbContext>(options =>
    options.UseSqlite(finanzasConnectionString,
        sqlite => sqlite.MigrationsAssembly("RemTool")));

builder.Services.AddScoped<DataController>();
builder.Services.AddScoped<REMController>();
builder.Services.AddScoped<IIndicadorService, IndicadorService>();
builder.Services.AddScoped<SeguimientoService>();
builder.Services.AddScoped<IRemAnalyzer, RemAnalyzerService>();
builder.Services.AddScoped<ConstruirRemService>();
builder.Services.AddScoped<ConvenioFinancieroService>();

// Panel interno (/api/interno): token bearer nativo de .NET 8. [Authorize] exige además que el usuario siga activo.
// ponytail: claves de Data Protection en el contenedor; un redeploy invalida los tokens (re-login). Persistir claves si molesta.
builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken(o => o.BearerTokenExpiration = TimeSpan.FromHours(8));
builder.Services.AddAuthorization(o => o.DefaultPolicy = new AuthorizationPolicyBuilder(BearerTokenDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .RequireAssertion(async ctx => ctx.Resource is HttpContext http && await http.RequestServices.GetRequiredService<RemToolDataContext>().Database
        .SqlQuery<int>($"""SELECT 1 AS "Value" FROM "REMTool".usuario_interno WHERE usuario = {ctx.User.Identity!.Name} AND activo""").AnyAsync())
    .Build());
// ponytail: partición por IP; detrás de un proxy no confiable puede colapsar a una sola IP (10 intentos/min globales).
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCorsPolicy",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
    options.AddPolicy("CorsPolicy",
        builder => builder.WithOrigins("https://estadisticas.apsmontepatria.cl/", "null")
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});


var app = builder.Build();

// ── Auto-migrate ──────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<RemToolDataContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var pending = await db.Database.GetPendingMigrationsAsync();
    if (pending.Any())
    {
        logger.LogInformation("Aplicando {Count} migración/es pendiente/s: {Migrations}",
            pending.Count(), string.Join(", ", pending));
        await db.Database.MigrateAsync();
        logger.LogInformation("Migraciones aplicadas correctamente.");
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConveniosFinancierosDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var pending = await db.Database.GetPendingMigrationsAsync();
    if (pending.Any())
    {
        logger.LogInformation("Aplicando {Count} migración/es de convenios financieros en SQLite: {Migrations}",
            pending.Count(), string.Join(", ", pending));
        await db.Database.MigrateAsync();
        logger.LogInformation("Base SQLite de convenios financieros actualizada correctamente.");
    }

    await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE;");

    await ConveniosFinancierosDataMigrator.ImportarDesdePostgreSqlSiEstaVaciaAsync(
        db,
        builder.Configuration.GetConnectionString("PostgreSQL"),
        logger);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

ExcelPackage.License.SetNonCommercialOrganization("DESAM Monte Patria");


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{   app.UseHttpsRedirection();
    app.UseCors("DevelopmentCorsPolicy");
}
else
    app.UseCors("CorsPolicy");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

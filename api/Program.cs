using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using RemTool;
using RemTool.Controllers;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<VoidDataContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "REMTool")));

builder.Services.AddScoped<DataController>();
builder.Services.AddScoped<REMController>();

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCorsPolicy",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
    options.AddPolicy("CorsPolicy",
        builder => builder.WithOrigins("https://remtools.thepit.cl") // Replace with your frontend origins
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());// If your frontend sends credentials (e.g., cookies, authorization headers)
});


var app = builder.Build();

// ── Auto-migrate ──────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<VoidDataContext>();
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

app.UseAuthorization();

app.MapControllers();

app.Run();

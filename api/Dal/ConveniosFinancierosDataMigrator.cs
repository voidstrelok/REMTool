using Microsoft.EntityFrameworkCore;
using Npgsql;
using RemTool.Shared;

namespace RemTool.Dal;

public static class ConveniosFinancierosDataMigrator
{
    public static async Task ImportarDesdePostgreSqlSiEstaVaciaAsync(
        ConveniosFinancierosDbContext destination,
        string? postgresConnectionString,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (await destination.ConvenioFinanciero.AnyAsync(ct) || string.IsNullOrWhiteSpace(postgresConnectionString)) return;

        var options = new DbContextOptionsBuilder<ConveniosFinancierosLegacyPostgresContext>()
            .UseNpgsql(postgresConnectionString)
            .Options;

        await using var source = new ConveniosFinancierosLegacyPostgresContext(options);
        try
        {
            if (!await source.ConvenioFinanciero.AnyAsync(ct)) return;

            var convenios = await source.ConvenioFinanciero.AsNoTracking().ToListAsync(ct);
            var items = await source.ItemPresupuestario.AsNoTracking().ToListAsync(ct);
            var subItems = await source.SubItemPresupuestario.AsNoTracking().ToListAsync(ct);
            var proveedores = await source.ProveedorFinanciero.AsNoTracking().ToListAsync(ct);
            var movimientos = await source.MovimientoFinanciero.AsNoTracking().ToListAsync(ct);

            await using var transaction = await destination.Database.BeginTransactionAsync(ct);
            destination.ConvenioFinanciero.AddRange(convenios.Select(x => new ConvenioFinanciero
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Codigo = x.Codigo,
                Anio = x.Anio,
                Descripcion = x.Descripcion,
                FechaInicio = x.FechaInicio,
                FechaTermino = x.FechaTermino,
                Responsable = x.Responsable,
                Estado = x.Estado,
                FechaCreacion = x.FechaCreacion
            }));
            await destination.SaveChangesAsync(ct);

            destination.ItemPresupuestario.AddRange(items.Select(x => new ItemPresupuestario
            {
                Id = x.Id,
                ConvenioFinancieroId = x.ConvenioFinancieroId,
                Nombre = x.Nombre,
                Activo = x.Activo
            }));
            await destination.SaveChangesAsync(ct);

            destination.SubItemPresupuestario.AddRange(subItems.Select(x => new SubItemPresupuestario
            {
                Id = x.Id,
                ItemPresupuestarioId = x.ItemPresupuestarioId,
                Nombre = x.Nombre,
                PresupuestoAsignado = x.PresupuestoAsignado,
                Activo = x.Activo
            }));
            destination.ProveedorFinanciero.AddRange(proveedores.Select(x => new ProveedorFinanciero
            {
                Id = x.Id,
                Nombre = x.Nombre,
                NombreNormalizado = x.NombreNormalizado,
                FechaCreacion = x.FechaCreacion
            }));
            await destination.SaveChangesAsync(ct);

            destination.MovimientoFinanciero.AddRange(movimientos.Select(x => new MovimientoFinanciero
            {
                Id = x.Id,
                ConvenioFinancieroId = x.ConvenioFinancieroId,
                ItemPresupuestarioId = x.ItemPresupuestarioId,
                SubItemPresupuestarioId = x.SubItemPresupuestarioId,
                ProveedorFinancieroId = x.ProveedorFinancieroId,
                Fecha = x.Fecha,
                Descripcion = x.Descripcion,
                Monto = x.Monto,
                TipoMovimiento = x.TipoMovimiento,
                TipoDocumento = x.TipoDocumento,
                NumeroDocumento = x.NumeroDocumento,
                Proveedor = x.Proveedor,
                Observaciones = x.Observaciones,
                FechaCreacion = x.FechaCreacion,
                FechaModificacion = x.FechaModificacion
            }));
            await destination.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Datos financieros importados a SQLite: {Convenios} convenios, {Items} ítems, {SubItems} subítems y {Movimientos} movimientos.",
                convenios.Count, items.Count, subItems.Count, movimientos.Count);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            logger.LogInformation("No existen tablas financieras anteriores en PostgreSQL; se iniciará SQLite sin datos.");
        }
    }
}

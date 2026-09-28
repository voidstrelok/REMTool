using System.Data;
using Microsoft.EntityFrameworkCore;
using RemTool.DTO;
using RemTool.Shared;

namespace RemTool.Services;

public sealed class FinanzasValidacionException(string message) : Exception(message);
public sealed class FinanzasDependenciaException(string message) : Exception(message);

public sealed class ConvenioFinancieroService(RemToolDataContext db)
{
    public async Task<IReadOnlyList<ConvenioFinancieroResumenDto>> ListarAsync(int? anio, string? estado, string? responsable, string? nombre, CancellationToken ct)
    {
        var query = db.ConvenioFinanciero.AsNoTracking().AsQueryable();
        if (anio.HasValue) query = query.Where(x => x.Anio == anio);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(x => x.Estado == estado);
        if (!string.IsNullOrWhiteSpace(responsable)) query = query.Where(x => EF.Functions.ILike(x.Responsable, $"%{responsable.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(nombre)) query = query.Where(x => EF.Functions.ILike(x.Nombre, $"%{nombre.Trim()}%"));

        var convenios = await query.OrderByDescending(x => x.Anio).ThenBy(x => x.Nombre).ToListAsync(ct);
        var ids = convenios.Select(x => x.Id).ToArray();
        var presupuestos = await db.SubItemPresupuestario.AsNoTracking()
            .Where(x => ids.Contains(x.ItemPresupuestario.ConvenioFinancieroId))
            .GroupBy(x => x.ItemPresupuestario.ConvenioFinancieroId)
            .Select(x => new { Id = x.Key, Monto = x.Sum(y => y.PresupuestoAsignado) })
            .ToDictionaryAsync(x => x.Id, x => x.Monto, ct);
        var ejecutados = await db.MovimientoFinanciero.AsNoTracking()
            .Where(x => ids.Contains(x.ConvenioFinancieroId))
            .GroupBy(x => x.ConvenioFinancieroId)
            .Select(x => new { Id = x.Key, Monto = x.Sum(y => y.Monto) })
            .ToDictionaryAsync(x => x.Id, x => x.Monto, ct);

        return convenios.Select(x => Resumen(x, presupuestos.GetValueOrDefault(x.Id), ejecutados.GetValueOrDefault(x.Id))).ToList();
    }

    public async Task<ConvenioFinancieroDetalleDto?> ObtenerAsync(int id, CancellationToken ct)
    {
        var convenio = await db.ConvenioFinanciero.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (convenio is null) return null;

        var items = await db.ItemPresupuestario.AsNoTracking()
            .Where(x => x.ConvenioFinancieroId == id)
            .Include(x => x.SubItems)
            .OrderBy(x => x.Nombre)
            .ToListAsync(ct);
        var movimientos = await db.MovimientoFinanciero.AsNoTracking()
            .Where(x => x.ConvenioFinancieroId == id)
            .Include(x => x.ItemPresupuestario)
            .Include(x => x.SubItemPresupuestario)
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        var porItem = movimientos.Where(x => x.ItemPresupuestarioId.HasValue).GroupBy(x => x.ItemPresupuestarioId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Monto));
        var porSubItem = movimientos.Where(x => x.SubItemPresupuestarioId.HasValue).GroupBy(x => x.SubItemPresupuestarioId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Monto));
        var presupuestoTotal = items.Sum(x => x.SubItems.Sum(s => s.PresupuestoAsignado));
        var ejecutadoTotal = movimientos.Sum(x => x.Monto);
        var itemsDto = items.Select(item =>
        {
            var presupuesto = item.SubItems.Sum(x => x.PresupuestoAsignado);
            var ejecutado = porItem.GetValueOrDefault(item.Id);
            var subItems = item.SubItems.OrderBy(x => x.Nombre).Select(x => MapSubItem(x, porSubItem.GetValueOrDefault(x.Id))).ToList();
            return new ItemFinancieroDto(item.Id, item.Nombre, presupuesto, ejecutado, presupuesto - ejecutado, item.Activo, subItems);
        }).ToList();

        return new ConvenioFinancieroDetalleDto(convenio.Id, convenio.Nombre, convenio.Codigo, convenio.Anio, convenio.Descripcion, convenio.FechaInicio, convenio.FechaTermino, convenio.Responsable, convenio.Estado, convenio.FechaCreacion, ResumenDetalle(presupuestoTotal, ejecutadoTotal), itemsDto, movimientos.Select(MapMovimiento).ToList());
    }

    public async Task<ConvenioFinancieroResumenDto> CrearConvenioAsync(ConvenioFinancieroRequest request, CancellationToken ct)
    {
        ValidarConvenio(request);
        var entity = new ConvenioFinanciero { Nombre = request.Nombre.Trim(), Codigo = Limpio(request.Codigo), Anio = request.Anio, Descripcion = Limpio(request.Descripcion), FechaInicio = request.FechaInicio, FechaTermino = request.FechaTermino, Responsable = request.Responsable.Trim(), Estado = request.Estado, FechaCreacion = DateTimeOffset.UtcNow };
        db.ConvenioFinanciero.Add(entity);
        await db.SaveChangesAsync(ct);
        return Resumen(entity, 0m, 0m);
    }

    public async Task<ConvenioFinancieroResumenDto?> ActualizarConvenioAsync(int id, ConvenioFinancieroRequest request, CancellationToken ct)
    {
        ValidarConvenio(request);
        var entity = await db.ConvenioFinanciero.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
        entity.Nombre = request.Nombre.Trim(); entity.Codigo = Limpio(request.Codigo); entity.Anio = request.Anio; entity.Descripcion = Limpio(request.Descripcion); entity.FechaInicio = request.FechaInicio; entity.FechaTermino = request.FechaTermino; entity.Responsable = request.Responsable.Trim(); entity.Estado = request.Estado;
        await db.SaveChangesAsync(ct);
        var presupuesto = await PresupuestoConvenioAsync(id, ct);
        var ejecutado = await db.MovimientoFinanciero.Where(x => x.ConvenioFinancieroId == id).SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        return Resumen(entity, presupuesto, ejecutado);
    }

    public async Task<ItemFinancieroDto?> GuardarItemAsync(int convenioId, int? itemId, ItemPresupuestarioRequest request, CancellationToken ct)
    {
        if (!await db.ConvenioFinanciero.AnyAsync(x => x.Id == convenioId, ct)) return null;
        var item = itemId.HasValue ? await db.ItemPresupuestario.SingleOrDefaultAsync(x => x.Id == itemId && x.ConvenioFinancieroId == convenioId, ct) : null;
        if (itemId.HasValue && item is null) return null;
        if (item is null) { item = new ItemPresupuestario { ConvenioFinancieroId = convenioId }; db.ItemPresupuestario.Add(item); }
        item.Nombre = request.Nombre.Trim();
        item.Activo = request.Activo;
        await db.SaveChangesAsync(ct);

        var subItems = await db.SubItemPresupuestario.AsNoTracking().Where(x => x.ItemPresupuestarioId == item.Id).OrderBy(x => x.Nombre).ToListAsync(ct);
        var movimientos = await db.MovimientoFinanciero.AsNoTracking().Where(x => x.ItemPresupuestarioId == item.Id).ToListAsync(ct);
        var porSubItem = movimientos.Where(x => x.SubItemPresupuestarioId.HasValue).GroupBy(x => x.SubItemPresupuestarioId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Monto));
        var presupuesto = subItems.Sum(x => x.PresupuestoAsignado);
        var ejecutado = movimientos.Sum(x => x.Monto);
        return new ItemFinancieroDto(item.Id, item.Nombre, presupuesto, ejecutado, presupuesto - ejecutado, item.Activo, subItems.Select(x => MapSubItem(x, porSubItem.GetValueOrDefault(x.Id))).ToList());
    }

    public async Task<SubItemFinancieroDto?> GuardarSubItemAsync(int convenioId, int itemId, int? subItemId, SubItemPresupuestarioRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var item = await db.ItemPresupuestario.SingleOrDefaultAsync(x => x.Id == itemId && x.ConvenioFinancieroId == convenioId, ct);
        if (item is null) return null;
        var subItem = subItemId.HasValue ? await db.SubItemPresupuestario.SingleOrDefaultAsync(x => x.Id == subItemId && x.ItemPresupuestarioId == itemId, ct) : null;
        if (subItemId.HasValue && subItem is null) return null;
        var ejecutado = subItem is null ? 0m : await db.MovimientoFinanciero.Where(x => x.SubItemPresupuestarioId == subItem.Id).SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        if (request.PresupuestoAsignado < ejecutado) throw new FinanzasValidacionException("El presupuesto del subítem no puede ser inferior a lo ya ejecutado.");
        if (subItem is null) { subItem = new SubItemPresupuestario { ItemPresupuestarioId = itemId }; db.SubItemPresupuestario.Add(subItem); }
        subItem.Nombre = request.Nombre.Trim();
        subItem.PresupuestoAsignado = request.PresupuestoAsignado;
        subItem.Activo = request.Activo;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return MapSubItem(subItem, ejecutado);
    }

    public async Task<MovimientoFinancieroDto?> GuardarMovimientoAsync(int convenioId, int? movimientoId, MovimientoFinancieroRequest request, CancellationToken ct)
    {
        if (request.TipoMovimiento != "Gasto") throw new FinanzasValidacionException("En esta versión sólo se permite el tipo Gasto.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.ConvenioFinanciero.AnyAsync(x => x.Id == convenioId, ct)) return null;
        var movimiento = movimientoId.HasValue ? await db.MovimientoFinanciero.SingleOrDefaultAsync(x => x.Id == movimientoId, ct) : null;
        if (movimientoId.HasValue && (movimiento is null || movimiento.ConvenioFinancieroId != convenioId)) return null;
        if (!request.SubItemPresupuestarioId.HasValue) throw new FinanzasValidacionException("Debe seleccionar un subítem presupuestario.");

        var subItem = await db.SubItemPresupuestario.Include(x => x.ItemPresupuestario).SingleOrDefaultAsync(x => x.Id == request.SubItemPresupuestarioId && x.ItemPresupuestario.ConvenioFinancieroId == convenioId, ct);
        if (subItem is null) throw new FinanzasValidacionException("El subítem no pertenece al convenio.");
        if ((!subItem.Activo || !subItem.ItemPresupuestario.Activo) && movimiento?.SubItemPresupuestarioId != subItem.Id) throw new FinanzasValidacionException("No se puede registrar un gasto en un subítem inactivo.");

        var sinMovimientoActual = db.MovimientoFinanciero.Where(x => !movimientoId.HasValue || x.Id != movimientoId);
        var ejecutadoSubItem = await sinMovimientoActual.Where(x => x.SubItemPresupuestarioId == subItem.Id).SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        if (ejecutadoSubItem + request.Monto > subItem.PresupuestoAsignado) throw new FinanzasValidacionException("El gasto supera el saldo disponible del subítem.");
        var presupuestoItem = await db.SubItemPresupuestario.Where(x => x.ItemPresupuestarioId == subItem.ItemPresupuestarioId).SumAsync(x => (decimal?)x.PresupuestoAsignado, ct) ?? 0m;
        var ejecutadoItem = await sinMovimientoActual.Where(x => x.ItemPresupuestarioId == subItem.ItemPresupuestarioId).SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        if (ejecutadoItem + request.Monto > presupuestoItem) throw new FinanzasValidacionException("El gasto supera el saldo disponible del ítem.");
        var presupuestoConvenio = await PresupuestoConvenioAsync(convenioId, ct);
        var ejecutadoConvenio = await sinMovimientoActual.Where(x => x.ConvenioFinancieroId == convenioId).SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        if (ejecutadoConvenio + request.Monto > presupuestoConvenio) throw new FinanzasValidacionException("El gasto supera el saldo disponible del convenio.");

        var now = DateTimeOffset.UtcNow;
        if (movimiento is null) { movimiento = new MovimientoFinanciero { ConvenioFinancieroId = convenioId, FechaCreacion = now }; db.MovimientoFinanciero.Add(movimiento); }
        ProveedorFinanciero? proveedor = null;
        var proveedorNombre = Limpio(request.Proveedor);
        if (proveedorNombre is not null)
        {
            var normalizado = proveedorNombre.ToUpperInvariant();
            proveedor = await db.ProveedorFinanciero.SingleOrDefaultAsync(x => x.NombreNormalizado == normalizado, ct);
            if (proveedor is null) { proveedor = new ProveedorFinanciero { Nombre = proveedorNombre, NombreNormalizado = normalizado, FechaCreacion = now }; db.ProveedorFinanciero.Add(proveedor); }
        }
        movimiento.ItemPresupuestarioId = subItem.ItemPresupuestarioId; movimiento.SubItemPresupuestarioId = subItem.Id; movimiento.Fecha = request.Fecha; movimiento.Descripcion = request.Descripcion.Trim(); movimiento.Monto = request.Monto; movimiento.TipoMovimiento = request.TipoMovimiento; movimiento.TipoDocumento = Limpio(request.TipoDocumento); movimiento.NumeroDocumento = Limpio(request.NumeroDocumento); movimiento.Proveedor = proveedorNombre;
        if (proveedor is null) { movimiento.ProveedorFinancieroId = null; movimiento.ProveedorFinanciero = null; } else movimiento.ProveedorFinanciero = proveedor;
        movimiento.Observaciones = Limpio(request.Observaciones); movimiento.FechaModificacion = now;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        movimiento.ItemPresupuestario = subItem.ItemPresupuestario; movimiento.SubItemPresupuestario = subItem;
        return MapMovimiento(movimiento);
    }

    public async Task<bool> EliminarMovimientoAsync(int id, CancellationToken ct) { var entity = await db.MovimientoFinanciero.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return false; db.MovimientoFinanciero.Remove(entity); await db.SaveChangesAsync(ct); return true; }

    public async Task<bool> EliminarConvenioAsync(int id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var convenio = await db.ConvenioFinanciero.SingleOrDefaultAsync(x => x.Id == id, ct); if (convenio is null) return false;
        if (await db.ItemPresupuestario.AnyAsync(x => x.ConvenioFinancieroId == id, ct) || await db.MovimientoFinanciero.AnyAsync(x => x.ConvenioFinancieroId == id, ct)) throw new FinanzasDependenciaException("No se puede eliminar el convenio porque tiene ítems o movimientos asociados. Elimine primero sus registros dependientes.");
        db.ConvenioFinanciero.Remove(convenio); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
    }

    public async Task<bool> EliminarItemAsync(int convenioId, int itemId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var item = await db.ItemPresupuestario.SingleOrDefaultAsync(x => x.Id == itemId && x.ConvenioFinancieroId == convenioId, ct); if (item is null) return false;
        if (await db.SubItemPresupuestario.AnyAsync(x => x.ItemPresupuestarioId == itemId, ct) || await db.MovimientoFinanciero.AnyAsync(x => x.ItemPresupuestarioId == itemId, ct)) throw new FinanzasDependenciaException("No se puede eliminar el ítem porque tiene subítems o movimientos asociados. Elimine primero sus registros dependientes.");
        db.ItemPresupuestario.Remove(item); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
    }

    public async Task<bool> EliminarSubItemAsync(int convenioId, int itemId, int subItemId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var subItem = await db.SubItemPresupuestario.SingleOrDefaultAsync(x => x.Id == subItemId && x.ItemPresupuestarioId == itemId && x.ItemPresupuestario.ConvenioFinancieroId == convenioId, ct); if (subItem is null) return false;
        if (await db.MovimientoFinanciero.AnyAsync(x => x.SubItemPresupuestarioId == subItemId, ct)) throw new FinanzasDependenciaException("No se puede eliminar el subítem porque tiene movimientos asociados. Elimine primero esos movimientos.");
        db.SubItemPresupuestario.Remove(subItem); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
    }

    public Task<int?> ObtenerMovimientoConvenioAsync(int id, CancellationToken ct) => db.MovimientoFinanciero.AsNoTracking().Where(x => x.Id == id).Select(x => (int?)x.ConvenioFinancieroId).SingleOrDefaultAsync(ct);
    public Task<List<string>> ListarProveedoresAsync(string? buscar, CancellationToken ct) { var query = db.ProveedorFinanciero.AsNoTracking().AsQueryable(); if (!string.IsNullOrWhiteSpace(buscar)) query = query.Where(x => EF.Functions.ILike(x.Nombre, $"%{buscar.Trim()}%")); return query.OrderBy(x => x.Nombre).Select(x => x.Nombre).ToListAsync(ct); }

    public async Task<DashboardFinancieroDto> DashboardAsync(int anio, int? mes, int? convenioId, CancellationToken ct)
    {
        var query = db.MovimientoFinanciero.AsNoTracking().Where(x => x.Fecha.Year == anio);
        if (mes.HasValue) query = query.Where(x => x.Fecha.Month == mes);
        if (convenioId.HasValue) query = query.Where(x => x.ConvenioFinancieroId == convenioId);
        var agrupados = await query.GroupBy(x => new { x.ConvenioFinancieroId, x.ConvenioFinanciero.Nombre }).Select(g => new { g.Key.ConvenioFinancieroId, g.Key.Nombre, Ejecutado = g.Sum(x => x.Monto), CantidadMovimientos = g.Count() }).OrderByDescending(x => x.Ejecutado).ToListAsync(ct);
        var rows = agrupados.Select(x => new DashboardConvenioDto(x.ConvenioFinancieroId, x.Nombre, x.Ejecutado, x.CantidadMovimientos)).ToList();
        return new(anio, mes, convenioId, rows.Sum(x => x.Ejecutado), rows.Sum(x => x.CantidadMovimientos), rows);
    }

    private async Task<decimal> PresupuestoConvenioAsync(int convenioId, CancellationToken ct) => await db.SubItemPresupuestario.Where(x => x.ItemPresupuestario.ConvenioFinancieroId == convenioId).SumAsync(x => (decimal?)x.PresupuestoAsignado, ct) ?? 0m;
    private static void ValidarConvenio(ConvenioFinancieroRequest request) { if (request.FechaTermino < request.FechaInicio) throw new FinanzasValidacionException("La fecha de término no puede ser anterior a la fecha de inicio."); }
    private static string? Limpio(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ConvenioFinancieroResumenDto Resumen(ConvenioFinanciero x, decimal presupuesto, decimal ejecutado) => new(x.Id, x.Nombre, x.Codigo, x.Anio, x.Responsable, x.Estado, presupuesto, ejecutado, presupuesto - ejecutado, presupuesto == 0 ? 0 : Math.Round(ejecutado / presupuesto * 100, 2));
    private static ResumenFinancieroDto ResumenDetalle(decimal presupuesto, decimal ejecutado) => new(presupuesto, ejecutado, presupuesto - ejecutado, presupuesto == 0 ? 0 : Math.Round(ejecutado / presupuesto * 100, 2));
    private static SubItemFinancieroDto MapSubItem(SubItemPresupuestario x, decimal ejecutado) => new(x.Id, x.ItemPresupuestarioId, x.Nombre, x.PresupuestoAsignado, ejecutado, x.PresupuestoAsignado - ejecutado, x.Activo);
    private static MovimientoFinancieroDto MapMovimiento(MovimientoFinanciero x) => new(x.Id, x.Fecha, x.Descripcion, x.Monto, x.TipoMovimiento, x.ItemPresupuestarioId, x.ItemPresupuestario?.Nombre, x.SubItemPresupuestarioId, x.SubItemPresupuestario?.Nombre, x.TipoDocumento, x.NumeroDocumento, x.Proveedor, x.Observaciones, x.FechaCreacion, x.FechaModificacion);
}

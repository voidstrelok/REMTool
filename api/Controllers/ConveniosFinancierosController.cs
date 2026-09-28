using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.DTO;
using RemTool.Services;

namespace RemTool.Controllers;

[ApiController]
[Route("api/convenios-financieros")]
public class ConveniosFinancierosController(ConvenioFinancieroService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? anio, [FromQuery] string? estado, [FromQuery] string? responsable, [FromQuery] string? nombre, CancellationToken ct)
        => Ok(await service.ListarAsync(anio, estado, responsable, nombre, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) => (await service.ObtenerAsync(id, ct)) is { } item ? Ok(item) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Crear(ConvenioFinancieroRequest request, CancellationToken ct)
        => await Ejecutar(async () => { var item = await service.CrearConvenioAsync(request, ct); return CreatedAtAction(nameof(Obtener), new { id = item.Id }, item); });

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, ConvenioFinancieroRequest request, CancellationToken ct)
        => await Ejecutar(async () => (await service.ActualizarConvenioAsync(id, request, ct)) is { } item ? Ok(item) : NotFound());

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
        => await Ejecutar(async () => await service.EliminarConvenioAsync(id, ct) ? NoContent() : NotFound());

    [HttpGet("{id:int}/items")]
    public async Task<IActionResult> Items(int id, CancellationToken ct) => (await service.ObtenerAsync(id, ct)) is { } item ? Ok(item.Items) : NotFound();

    [HttpPost("{id:int}/items")]
    public async Task<IActionResult> CrearItem(int id, ItemPresupuestarioRequest request, CancellationToken ct)
        => await Ejecutar(async () => (await service.GuardarItemAsync(id, null, request, ct)) is { } item ? Created($"api/convenios-financieros/{id}/items/{item.Id}", item) : NotFound());

    [HttpPut("{convenioId:int}/items/{itemId:int}")]
    public async Task<IActionResult> ActualizarItem(int convenioId, int itemId, ItemPresupuestarioRequest request, CancellationToken ct)
        => await Ejecutar(async () => (await service.GuardarItemAsync(convenioId, itemId, request, ct)) is { } item ? Ok(item) : NotFound());

    [HttpDelete("{convenioId:int}/items/{itemId:int}")]
    public async Task<IActionResult> EliminarItem(int convenioId, int itemId, CancellationToken ct)
        => await Ejecutar(async () => await service.EliminarItemAsync(convenioId, itemId, ct) ? NoContent() : NotFound());

    [HttpPost("{convenioId:int}/items/{itemId:int}/subitems")]
    public async Task<IActionResult> CrearSubItem(int convenioId, int itemId, SubItemPresupuestarioRequest request, CancellationToken ct)
        => await Ejecutar(async () => (await service.GuardarSubItemAsync(convenioId, itemId, null, request, ct)) is { } item ? Ok(item) : NotFound());

    [HttpPut("{convenioId:int}/items/{itemId:int}/subitems/{subItemId:int}")]
    public async Task<IActionResult> ActualizarSubItem(int convenioId, int itemId, int subItemId, SubItemPresupuestarioRequest request, CancellationToken ct)
        => await Ejecutar(async () => (await service.GuardarSubItemAsync(convenioId, itemId, subItemId, request, ct)) is { } item ? Ok(item) : NotFound());

    [HttpDelete("{convenioId:int}/items/{itemId:int}/subitems/{subItemId:int}")]
    public async Task<IActionResult> EliminarSubItem(int convenioId, int itemId, int subItemId, CancellationToken ct)
        => await Ejecutar(async () => await service.EliminarSubItemAsync(convenioId, itemId, subItemId, ct) ? NoContent() : NotFound());

    [HttpGet("proveedores")]
    public async Task<IActionResult> Proveedores([FromQuery] string? buscar, CancellationToken ct) => Ok(await service.ListarProveedoresAsync(buscar, ct));

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int anio, [FromQuery] int? mes, [FromQuery] int? convenioId, CancellationToken ct)
    {
        if (anio is < 2000 or > 2100) return BadRequest(new { message = "El año debe estar entre 2000 y 2100." });
        if (mes is < 1 or > 12) return BadRequest(new { message = "El mes debe estar entre 1 y 12." });
        return Ok(await service.DashboardAsync(anio, mes, convenioId, ct));
    }

    [HttpGet("{id:int}/movimientos")]
    public async Task<IActionResult> Movimientos(int id, CancellationToken ct) => (await service.ObtenerAsync(id, ct)) is { } item ? Ok(item.Movimientos) : NotFound();

    [HttpPost("{id:int}/movimientos")]
    public async Task<IActionResult> CrearMovimiento(int id, MovimientoFinancieroRequest request, CancellationToken ct)
        => await Ejecutar(async () => (await service.GuardarMovimientoAsync(id, null, request, ct)) is { } item ? Created($"api/movimientos-financieros/{item.Id}", item) : NotFound());

    [HttpPut("movimientos/{id:int}")]
    public async Task<IActionResult> ActualizarMovimiento(int id, MovimientoFinancieroRequest request, CancellationToken ct)
    {
        var existente = await service.ObtenerMovimientoConvenioAsync(id, ct); if (!existente.HasValue) return NotFound();
        return await Ejecutar(async () => (await service.GuardarMovimientoAsync(existente.Value, id, request, ct)) is { } item ? Ok(item) : NotFound());
    }

    [HttpDelete("movimientos/{id:int}")]
    public async Task<IActionResult> EliminarMovimiento(int id, CancellationToken ct) => await service.EliminarMovimientoAsync(id, ct) ? NoContent() : NotFound();

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (FinanzasValidacionException ex) { return BadRequest(new { message = ex.Message, errors = new { finanzas = new[] { ex.Message } } }); }
        catch (FinanzasDependenciaException ex) { return Conflict(new { message = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "No fue posible guardar por un cambio concurrente. Intente nuevamente." }); }
    }
}

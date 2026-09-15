using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.Services;
using RemTool.Shared;

namespace RemTool.Controllers;

[ApiController]
[Route("api")]
public class ReporteController(RemToolDataContext db, SeguimientoService seguimiento) : ControllerBase
{
    private async Task<string> Contexto(long? sectorId, long? establecimientoId)
    {
        if (establecimientoId is > 0)
            return "Establecimiento: " + (await db.Establecimiento.Where(e => e.Id == establecimientoId).Select(e => e.Nombre).FirstOrDefaultAsync() ?? establecimientoId.ToString());
        if (sectorId is > 0)
            return "Sector: " + (await db.Sector.Where(s => s.Id == sectorId).Select(s => s.Nombre).FirstOrDefaultAsync() ?? sectorId.ToString());
        return "Red Salud Comunal Monte Patria";
    }

    [HttpGet("informeMensual/{tipoIndicador:int}/{ano:int}")]
    public async Task<IActionResult> InformeMensual(int tipoIndicador, int ano,
        [FromQuery] long? sectorId = null, [FromQuery] long? establecimientoId = null,
        [FromQuery] int? mesCorte = null, [FromQuery] int? convenioId = null,
        [FromQuery] string? buscar = null, [FromQuery] string? estado = null, [FromQuery] string? orden = null)
    {
        if (!SeguimientoService.CorteValido(mesCorte)) return BadRequest("mesCorte debe estar entre 1 y 12.");
        var items = await seguimiento.Indicadores(ano, tipoIndicador, mesCorte, sectorId, establecimientoId, convenioId, detalle: true);
        var titulo = tipoIndicador == 2 ? "Metas Sanitarias" : tipoIndicador == 3 ? "IAAPS" : "Indicadores de convenios";
        if (convenioId is > 0)
        {
            var nombre = await db.Convenio.Where(c => c.Id == convenioId).Select(c => c.Nombre).FirstOrDefaultAsync();
            if (nombre == null) return NotFound("Convenio no encontrado.");
            titulo = nombre;
        }
        var contexto = await Contexto(sectorId, establecimientoId);
        if (!string.IsNullOrEmpty(buscar)) contexto += $" · Búsqueda: {buscar}";
        if (!string.IsNullOrEmpty(estado)) contexto += $" · Estado: {SeguimientoPdf.Estado(estado)}";
        var pdf = SeguimientoPdf.Generar(titulo, contexto, ano,
            items.FirstOrDefault()?.Evaluacion.MesCorte ?? mesCorte, EvaluadorSeguimiento.Seleccionar(items, buscar, estado, orden), ResumenSeguimiento.Crear(items, tipoIndicador != 1));
        return File(pdf, "application/pdf", $"indicadores-{tipoIndicador}-{ano}.pdf");
    }

    [HttpGet("informeConvenios/{ano:int}")]
    public async Task<IActionResult> InformeConvenios(int ano, [FromQuery] long? sectorId = null,
        [FromQuery] long? establecimientoId = null, [FromQuery] int? mesCorte = null)
    {
        if (!SeguimientoService.CorteValido(mesCorte)) return BadRequest("mesCorte debe estar entre 1 y 12.");
        var convenios = await seguimiento.Convenios(ano, mesCorte, sectorId, establecimientoId, detalle: true);
        var items = convenios.SelectMany(c => c.Indicadores).DistinctBy(i => i.Id).ToList();
        return File(SeguimientoPdf.Generar("Seguimiento de convenios", await Contexto(sectorId, establecimientoId), ano,
            convenios.FirstOrDefault()?.MesCorte ?? mesCorte, items, convenios: convenios), "application/pdf", $"convenios-{ano}.pdf");
    }

    [HttpGet("detalleIndicador/{id:int}")]
    public async Task<IActionResult> DetalleIndicador(int id, [FromQuery] long? sectorId = null,
        [FromQuery] long? establecimientoId = null, [FromQuery] int? mesCorte = null)
    {
        if (!SeguimientoService.CorteValido(mesCorte)) return BadRequest("mesCorte debe estar entre 1 y 12.");
        var item = await seguimiento.Indicador(id, mesCorte, sectorId, establecimientoId);
        if (item == null) return NotFound("Indicador no encontrado.");
        return File(SeguimientoPdf.Generar(item.Nombre, await Contexto(sectorId, establecimientoId), item.Año,
            item.Evaluacion.MesCorte, [item], soloDetalle: true), "application/pdf", $"indicador-{id}.pdf");
    }
}

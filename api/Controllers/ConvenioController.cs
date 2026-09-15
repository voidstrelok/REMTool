using Microsoft.AspNetCore.Mvc;
using RemTool.Services;

namespace RemTool.Controllers;

[ApiController]
[Route("api")]
public class ConvenioController(SeguimientoService seguimiento) : ControllerBase
{
    [HttpGet("getConvenios/{ano:int}")]
    public async Task<IActionResult> GetConvenios(int ano, [FromQuery] long? sectorId = null,
        [FromQuery] long? establecimientoId = null, [FromQuery] int? mesCorte = null, [FromQuery] bool incluirResumen = false)
    {
        if (!SeguimientoService.CorteValido(mesCorte)) return BadRequest("mesCorte debe estar entre 1 y 12.");
        var items = await seguimiento.Convenios(ano, mesCorte, sectorId, establecimientoId);
        return incluirResumen ? Ok(new { items, cumplimiento = items.Count > 0 ? items.Average(c => c.Resumen.Cumplimiento) : 0m, mesCorte = items.FirstOrDefault()?.MesCorte ?? mesCorte, criterios = EvaluadorSeguimiento.Criterios }) : Ok(items);
    }

    [HttpGet("getConvenioIndicadores/{convenioId:int}/{ano:int}")]
    public async Task<IActionResult> GetConvenioIndicadores(int convenioId, int ano, [FromQuery] long? sectorId = null,
        [FromQuery] long? establecimientoId = null, [FromQuery] int? mesCorte = null)
    {
        if (!SeguimientoService.CorteValido(mesCorte)) return BadRequest("mesCorte debe estar entre 1 y 12.");
        var item = (await seguimiento.Convenios(ano, mesCorte, sectorId, establecimientoId, convenioId)).FirstOrDefault();
        return item == null ? NotFound() : Ok(item);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.Services;
using RemTool.Shared;

namespace RemTool.Controllers;

[ApiController]
[Route("api")]
public class IndicadorController(RemToolDataContext db, IIndicadorService filtros, SeguimientoService seguimiento) : ControllerBase
{
    [HttpGet("getIndicadores/")]
    public async Task<IActionResult> GetIndicadores()
    {
        var rows = await db.ResultadoIndicador.Include(r => r.Establecimiento).Include(r => r.Indicador)
            .OrderBy(r => r.id_indicador).ToListAsync();
        var reglas = await filtros.GetFiltrosAsync(rows.Select(r => r.id_indicador));
        return Ok(rows.Where(r => reglas[r.id_indicador].Incluye(r.id_establecimiento)));
    }

    [HttpGet("getIndicador/{id_indicador:int}")]
    public async Task<IActionResult> GetIndicador(int id_indicador, [FromQuery] long? sectorId = null,
        [FromQuery] long? establecimientoId = null, [FromQuery] int? mesCorte = null)
    {
        if (!SeguimientoService.CorteValido(mesCorte)) return BadRequest("mesCorte debe estar entre 1 y 12.");
        var item = await seguimiento.Indicador(id_indicador, mesCorte, sectorId, establecimientoId);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("getIndicadores/{ano:int}/{tipo:int}")]
    [HttpGet("getIndicadores/{ano:int}/{tipo:int}/filtrado")]
    public async Task<IActionResult> GetIndicadoresTipo(int ano, int tipo, [FromQuery] long? sectorId = null,
        [FromQuery] long? establecimientoId = null, [FromQuery] int? mesCorte = null, [FromQuery] bool incluirResumen = false)
    {
        if (!SeguimientoService.CorteValido(mesCorte)) return BadRequest("mesCorte debe estar entre 1 y 12.");
        var items = await seguimiento.Indicadores(ano, tipo, mesCorte, sectorId, establecimientoId);
        return incluirResumen ? Ok(new { items, resumen = ResumenSeguimiento.Crear(items, tipo != 1), mesCorte = items.FirstOrDefault()?.Evaluacion.MesCorte ?? mesCorte, criterios = EvaluadorSeguimiento.Criterios }) : Ok(items);
    }
}

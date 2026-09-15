using RemTool.Shared;


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.Services;

namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class DataController : ControllerBase
    {

        private readonly ILogger<DataController> _logger;
        private RemToolDataContext db;
        private readonly IIndicadorService _indicadorService;

        public DataController(ILogger<DataController> logger, RemToolDataContext context, IIndicadorService indicadorService)
        {
            _logger = logger;
            db = context;
            _indicadorService = indicadorService;
        }
        [HttpGet("getEstablecimientos/")]
        public async Task<IActionResult> GetEstablecimientos(
            [FromQuery] int? indicadorId = null,
            [FromQuery] bool incluirTodos = false)
        {
            var filtro = indicadorId.HasValue
                ? await _indicadorService.GetFiltroAsync(indicadorId.Value)
                : null;
            var establecimientosQuery = db.Establecimiento.AsQueryable();
            if (!incluirTodos)
            {
                if (filtro == null)
                    establecimientosQuery = establecimientosQuery
                        .Where(e => !_indicadorService.EstablecimientosExcluidos.Contains(e.Id));
                else if (filtro.EsWhitelist)
                {
                    var incluidos = filtro.Ids.ToArray();
                    establecimientosQuery = establecimientosQuery.Where(e => incluidos.Contains(e.Id));
                }
                else
                {
                    var excluidos = filtro.Ids.ToArray();
                    establecimientosQuery = establecimientosQuery.Where(e => !excluidos.Contains(e.Id));
                }
            }
            establecimientosQuery = establecimientosQuery
                .Include(e => e.Sector)
                .OrderBy(e => e.CodDeis);
            var Establecimientos = await establecimientosQuery.ToListAsync();
            return Ok(Establecimientos);
        }

        [HttpGet("getSectores")]
        public async Task<IActionResult> GetSectores()
        {
            var sectores = await db.Sector
                .OrderBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Nombre })
                .ToListAsync();
            return Ok(sectores);
        }

        [HttpGet("getEstablecimientos/{sectorId:long}")]
        public async Task<IActionResult> GetEstablecimientosBySector(
            long sectorId,
            [FromQuery] int? indicadorId = null)
        {
            var filtro = indicadorId.HasValue
                ? await _indicadorService.GetFiltroAsync(indicadorId.Value)
                : null;
            var establecimientosQuery = db.Establecimiento.AsQueryable();
            if (filtro == null)
                establecimientosQuery = establecimientosQuery
                    .Where(e => !_indicadorService.EstablecimientosExcluidos.Contains(e.Id));
            else if (filtro.EsWhitelist)
            {
                var incluidos = filtro.Ids.ToArray();
                establecimientosQuery = establecimientosQuery.Where(e => incluidos.Contains(e.Id));
            }
            else
            {
                var excluidos = filtro.Ids.ToArray();
                establecimientosQuery = establecimientosQuery.Where(e => !excluidos.Contains(e.Id));
            }
            var establecimientosdb = await establecimientosQuery
                    .OrderBy(e => e.Nombre)
                    .ToListAsync();
            if (sectorId != 0)
            {
               establecimientosdb = establecimientosdb.Where(e => e.id_sector == sectorId).ToList();
            }

            var establecimientos = establecimientosdb.Select(e => new
            {
                e.Id,
                e.Nombre,
                e.CodDeis
            }).ToList();


            if (!establecimientos.Any())
                return NotFound($"No se encontraron establecimientos para el sector con ID {sectorId}.");

            return Ok(establecimientos);
        }
    }
}

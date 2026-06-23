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
        public async Task<IActionResult> GetEstablecimientos()
        {
            var Establecimientos = await db.Establecimiento
                .Where(e => !_indicadorService.EstablecimientosExcluidos.Contains(e.Id))
                .Include(e=>e.Sector).OrderBy(e=>e.CodDeis).ToListAsync();
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
        public async Task<IActionResult> GetEstablecimientosBySector(long sectorId)
        {
            var establecimientosdb = await db.Establecimiento
                    .Where(e => !_indicadorService.EstablecimientosExcluidos.Contains(e.Id))                    
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTool.Controllers;

[ApiController]
[Route("api/informativos")]
public class InformativoController : ControllerBase
{
    private readonly RemToolDataContext _db;

    public InformativoController(RemToolDataContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetVigentes(CancellationToken cancellationToken)
    {
        var rows = await _db.Informativo
            .AsNoTracking()
            .Where(x => x.Vigente)
            .OrderByDescending(x => x.Destacado)
            .ThenByDescending(x => x.FechaPublicacion)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                id = x.Id,
                tipo = x.Tipo,
                titulo = x.Titulo,
                contenido = x.Contenido,
                url = x.Url,
                texto_enlace = x.TextoEnlace,
                fecha_publicacion = x.FechaPublicacion,
                destacado = x.Destacado,
            })
            .ToListAsync(cancellationToken);

        var informativos = rows.Select(x => new
        {
            x.id,
            tipo = x.tipo.ToString(),
            x.titulo,
            x.contenido,
            x.url,
            x.texto_enlace,
            x.fecha_publicacion,
            x.destacado,
        });

        return Ok(new { informativos });
    }
}

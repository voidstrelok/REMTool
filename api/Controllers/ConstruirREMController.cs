using Microsoft.AspNetCore.Mvc;
using RemTool.DTO;
using RemTool.Services;

namespace RemTool.Controllers;

[ApiController]
[Route("api/construirREM")]
public sealed class ConstruirREMController : ControllerBase
{
    private readonly ConstruirRemService _service;

    public ConstruirREMController(ConstruirRemService service)
    {
        _service = service;
    }

    [HttpGet("version-actual")]
    public async Task<IActionResult> VersionActual(CancellationToken cancellationToken)
    {
        try
        {
            var version = await _service.ObtenerUltimaVersionSerieAAsync(cancellationToken);
            return Ok(new { serie = "A", version = version.Nombre, fecha = version.Fecha });
        }
        catch (ConstruirRemException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("parte")]
    public async Task<IActionResult> Parte(
        [FromForm] IFormFile? archivo,
        [FromForm] string? nombre,
        [FromForm] string? versionEsperada,
        CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest("No se ha enviado ningún archivo.");
        if (!string.Equals(Path.GetExtension(archivo.FileName), ".xlsm", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Sólo se permiten archivos con extensión .xlsm.");

        try
        {
            await using var stream = archivo.OpenReadStream();
            return Ok(await _service.ExtraerParteAsync(
                stream,
                nombre ?? string.Empty,
                versionEsperada,
                archivo.FileName,
                cancellationToken));
        }
        catch (ConstruirRemException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("revision")]
    public async Task<IActionResult> Revision(
        [FromBody] ConstruirRemRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("La solicitud es requerida.");

        try
        {
            return Ok(await _service.RevisarAsync(request, cancellationToken));
        }
        catch (ConstruirRemException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar(
        [FromBody] ConstruirRemRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("La solicitud es requerida.");

        try
        {
            var bytes = await _service.ExportarAsync(request, cancellationToken);
            var fileName = $"{request.CodDeis}A{request.Mes:D2}-construido.xlsm";
            return File(bytes, "application/vnd.ms-excel.sheet.macroEnabled.12", fileName);
        }
        catch (ConstruirRemException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

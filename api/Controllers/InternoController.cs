using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RemTool.Shared;

namespace RemTool.Controllers;

/// <summary>
/// Panel interno del departamento. Usuarios en "REMTool".usuario_interno (gestionados desde el admin).
/// [Authorize] usa la política por defecto de Program.cs: token válido + usuario activo.
/// </summary>
[ApiController]
[Route("api/interno")]
[Authorize]
public class InternoController(RemToolDataContext db) : ControllerBase
{
    const int MaxRuts = 50_000;

    public sealed record LoginRequest(string Usuario, string Password);
    public sealed record UsuarioRow(string Usuario, string Nombre, string PasswordHash);
    public sealed record FonasaHit(string Run, string? Dv, int Corte, string Establecimiento, string? Tramo);

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var u = await db.Database.SqlQuery<UsuarioRow>($"""
            SELECT usuario AS "Usuario", nombre AS "Nombre", password_hash AS "PasswordHash"
            FROM "REMTool".usuario_interno WHERE lower(usuario) = lower({req.Usuario ?? ""}) AND activo
            """).FirstOrDefaultAsync(ct);
        if (u is null || new PasswordHasher<string>().VerifyHashedPassword(u.Usuario, u.PasswordHash, req.Password ?? "") == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Usuario o contraseña incorrectos." });
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, u.Usuario), new Claim(ClaimTypes.GivenName, u.Nombre)], BearerTokenDefaults.AuthenticationScheme);
        return SignIn(new ClaimsPrincipal(identity), BearerTokenDefaults.AuthenticationScheme);
    }

    [HttpGet("me")]
    public IActionResult Me() => Ok(new { usuario = User.Identity!.Name, nombre = User.FindFirstValue(ClaimTypes.GivenName) });

    /// <summary>
    /// Comprueba una lista de RUT (texto pegado y/o Excel): DV, presencia en Fonasa (último corte) y,
    /// opcionalmente, si está en una segunda lista de cruce.
    /// </summary>
    [HttpPost("rut")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> ComprobarRut([FromForm] string? lista, IFormFile? archivo, [FromForm] string? cruce, IFormFile? archivoCruce, CancellationToken ct)
    {
        foreach (var f in new[] { archivo, archivoCruce })
            if (f is not null && Path.GetExtension(f.FileName).ToLowerInvariant() is not (".xlsx" or ".xlsm"))
                return BadRequest(new { error = $"'{f.FileName}' debe ser Excel .xlsx (guarde los .xls como .xlsx)." });
        List<string> entradas; HashSet<string> setCruce;
        var conCruce = !string.IsNullOrWhiteSpace(cruce) || archivoCruce is not null;
        try
        {
            entradas = Extraer(lista, archivo);
            // La lista de cruce puede venir sin DV: si no es un RUT válido, el número completo se toma como cuerpo.
            setCruce = conCruce ? Extraer(cruce, archivoCruce).Select(x => Rut.Parse(x) is { Valido: true } r ? r.Cuerpo
                : new string(x.Where(char.IsAsciiDigit).ToArray()).TrimStart('0')).Where(x => x != "").ToHashSet() : [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return BadRequest(new { error = "No se pudo leer el archivo Excel." });
        }
        if (entradas.Count == 0) return BadRequest(new { error = "No se encontraron RUT en la lista." });
        if (entradas.Count > MaxRuts) return BadRequest(new { error = $"Máximo {MaxRuts:N0} RUT por consulta." });

        var ruts = entradas.Select(e => (Entrada: e, Rut: Rut.Parse(e))).ToList();
        var cuerpos = ruts.Select(r => r.Rut.Cuerpo).Where(x => x != "").Distinct().ToArray();
        var fonasa = (await db.Database.SqlQuery<FonasaHit>($"""
            SELECT DISTINCT ON (f.run) f.run AS "Run", f.dv AS "Dv", extract(year FROM f.fecha_corte)::int AS "Corte", e.nombre AS "Establecimiento", f.tramo AS "Tramo"
            FROM "REMTool".fonasa f JOIN "REMTool".establecimiento e ON e.id = f.id_establecimiento
            WHERE f.run = ANY({cuerpos}) ORDER BY f.run, f.fecha_corte DESC
            """).ToListAsync(ct)).ToDictionary(x => x.Run);

        return Ok(new
        {
            conCruce,
            totalCruce = setCruce.Count,
            items = ruts.Select(r => new
            {
                entrada = r.Entrada,
                rut = r.Rut.Formateado,
                valido = r.Rut.Valido,
                dvEsperado = r.Rut.DvEsperado,
                fonasa = fonasa.GetValueOrDefault(r.Rut.Cuerpo),
                enCruce = conCruce ? setCruce.Contains(r.Rut.Cuerpo) : (bool?)null,
            }),
        });
    }

    static List<string> Extraer(string? texto, IFormFile? archivo)
    {
        var result = (texto ?? "").Split(['\n', '\r', ';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        if (archivo is not null) result.AddRange(LeerExcel(archivo));
        return result.Where(x => x.Any(char.IsDigit)).ToList();
    }

    // Primera hoja. Usa la columna cuyo encabezado contenga "rut"/"run" (y la siguiente si se llama "dv");
    // si no hay encabezado reconocible, la primera columna.
    static IEnumerable<string> LeerExcel(IFormFile archivo)
    {
        using var stream = archivo.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws?.Dimension is null) yield break;
        string Cell(int r, int c) => ws.Cells[r, c].Value?.ToString()?.Trim() ?? "";
        int lastCol = ws.Dimension.End.Column, lastRow = ws.Dimension.End.Row;
        var col = Enumerable.Range(1, lastCol).FirstOrDefault(c => Cell(1, c).ToLowerInvariant() is var h && (h.Contains("rut") || h.Contains("run")));
        var dvCol = col > 0 && col < lastCol && Cell(1, col + 1).Equals("dv", StringComparison.OrdinalIgnoreCase) ? col + 1 : 0;
        for (var r = col > 0 ? 2 : 1; r <= lastRow; r++)
            yield return Cell(r, Math.Max(col, 1)) + (dvCol > 0 ? "-" + Cell(r, dvCol) : "");
    }
}

/// <summary>RUT chileno: se ignora todo salvo dígitos y K; el último carácter es el DV.</summary>
public sealed record Rut(string Cuerpo, char Dv, char DvEsperado)
{
    public bool Valido => Cuerpo != "" && Dv == DvEsperado;
    public string Formateado => Cuerpo == "" ? "" : $"{long.Parse(Cuerpo):N0}".Replace(",", ".") + "-" + Dv;

    public static Rut Parse(string raw)
    {
        var s = new string(raw.ToUpperInvariant().Where(c => char.IsAsciiDigit(c) || c == 'K').ToArray()).TrimStart('0');
        if (s.Length < 2 || s.Length > 10 || s[..^1].Contains('K')) return new("", ' ', ' ');
        var cuerpo = s[..^1];
        return new(cuerpo, s[^1], CalcularDv(cuerpo));
    }

    public static char CalcularDv(string cuerpo)
    {
        int suma = 0, factor = 2;
        for (var i = cuerpo.Length - 1; i >= 0; i--) { suma += (cuerpo[i] - '0') * factor; factor = factor == 7 ? 2 : factor + 1; }
        return (11 - suma % 11) switch { 11 => '0', 10 => 'K', var r => (char)('0' + r) };
    }
}

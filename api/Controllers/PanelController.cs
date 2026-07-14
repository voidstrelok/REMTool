using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCalc;
using OfficeOpenXml;
using RemTool.Shared;
using RemTool.Util;

namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class PanelController : ControllerBase
    {
        private readonly RemToolDataContext _db;

        public PanelController(RemToolDataContext context)
        {
            _db = context;
        }

        [HttpGet("GetSeries/")]
        public IActionResult GetSeries()
        {
            var series = _db.SerieRem
                .OrderBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Nombre })
                .ToList();
            return Ok(series);
        }

        [HttpPost("AnalizarREM/")]
        public async Task<IActionResult> AnalizarREM(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("No se ha enviado ningún archivo.");

            using var stream = new MemoryStream();
            await archivo.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new ExcelPackage(stream);
            try
            {
                var hojaNombre = workbook.Workbook.Worksheets["NOMBRE"];
                if (hojaNombre == null)
                    return BadRequest("No se encontró la hoja 'NOMBRE'.");

                // --- Parse header fields ---
                string codDeis = (hojaNombre.Cells["C3"].Value?.ToString() ?? "")
                               + (hojaNombre.Cells["D3"].Value?.ToString() ?? "")
                               + (hojaNombre.Cells["E3"].Value?.ToString() ?? "")
                               + (hojaNombre.Cells["F3"].Value?.ToString() ?? "")
                               + (hojaNombre.Cells["G3"].Value?.ToString() ?? "")
                               + (hojaNombre.Cells["H3"].Value?.ToString() ?? "");

                string mesRaw = (hojaNombre.Cells["C6"].Value?.ToString() ?? "")
                              + (hojaNombre.Cells["D6"].Value?.ToString() ?? "");

                string versionArchivo = hojaNombre.Cells["A9"].Value?.ToString() ?? "";

                // Extract series from B17: "REM A" → "A", "REM BM" → "BM"
                string serieRaw = hojaNombre.Cells["B17"].Value?.ToString() ?? "";
                string serieAux = serieRaw.Contains(' ')
                    ? serieRaw.Substring(serieRaw.IndexOf(' ') + 1)
                    : serieRaw;
                string serie = serieAux.Split(' ')[0];

                if (!int.TryParse(mesRaw, out int mes))
                    return BadRequest("No se pudo leer el mes del archivo.");

                if (string.IsNullOrEmpty(versionArchivo))
                    return BadRequest("No se encontró la versión en la celda A9.");

                // --- Lookup Establecimiento (with Sector) ---
                var establecimiento = _db.Establecimiento
                    .Include(e => e.Sector)
                    .FirstOrDefault(e => e.CodDeis == codDeis);

                if (establecimiento == null)
                    return BadRequest($"No se encontró el establecimiento con CodDEIS '{codDeis}'.");

                // --- Lookup VersionRem to get year ---
                var versionRem = _db.VersionRem
                    .FirstOrDefault(v => v.Nombre == versionArchivo);

                if (versionRem == null)
                    return BadRequest($"No se encontró la versión '{versionArchivo}' en la base de datos.");

                int año = versionRem.Fecha.Year;

                // --- Run revision rules against raw Excel cells ---
                var reglas = _db.Regla
                    .Include(r => r.VersionREM)
                    .Include(r => r.TipoRegla)
                    .Where(r => r.VersionREM.Nombre == versionArchivo)
                    .ToList();

                var errores = new List<string>();
                var advertencias = new List<string>();

                foreach (var regla in reglas)
                {
                    try
                    {
                        string expr = Regex.Replace(regla.Expresion, Utils.RegexHoja,
                            x => Utils.ParseaHojas(x.Value, workbook));
                        expr = expr.Replace("[", "").Replace("]", "");

                        if (!(bool)new Expression(expr).Evaluate())
                        {
                            if (regla.TipoRegla.Nombre == "Advertencia")
                                advertencias.Add(regla.Mensaje);
                            else
                                errores.Add(regla.Mensaje);
                        }
                    }
                    catch
                    {
                        // Skip malformed rule expressions silently
                    }
                }

                // --- Extract prestacion data ---
                var prestaciones = _db.Prestacion
                    .Include(p => p.HojaRem)
                    .Where(p => p.VersionRem.Nombre == versionArchivo && p.IsEnabled)
                    .ToList();

                var datos = new List<PrestacionDatosDTO>();

                foreach (var prestacion in prestaciones)
                {
                    var hojaExcel = workbook.Workbook.Worksheets[prestacion.HojaRem.Nombre];
                    if (hojaExcel == null) continue;

                    var valores = new List<string>();
                    bool tieneData = false;

                    foreach (var coord in prestacion.Coordenada)
                    {
                        string valor = hojaExcel.Cells[coord].Value?.ToString() ?? "0";
                        valores.Add(valor);
                        if (valor != "0" && valor != "") tieneData = true;
                    }

                    if (tieneData)
                        datos.Add(new PrestacionDatosDTO
                        {
                            Prestacion = prestacion.CodigoPrestacion,
                            Valores = valores
                        });
                }

                return Ok(new AnalisisRemDTO
                {
                    CodDeis = codDeis,
                    NombreEstablecimiento = establecimiento.Nombre,
                    IdSector = establecimiento.id_sector,
                    NombreSector = establecimiento.Sector.Nombre,
                    Serie = serie,
                    Version = versionArchivo,
                    Mes = mes,
                    Año = año,
                    Errores = errores,
                    Advertencias = advertencias,
                    Datos = datos
                });
            }
            catch (Exception ex)
            {
                return BadRequest("Error al procesar el archivo: " + ex.Message);
            }
        }
    }
}

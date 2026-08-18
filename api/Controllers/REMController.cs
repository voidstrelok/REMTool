using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCalc;
using OfficeOpenXml;
using RemTool.Services;
using RemTool.Shared;
using RemTool.Util;


namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class REMController : ControllerBase
    {

        private readonly ILogger<REMController> _logger;
        private RemToolDataContext db;
        private readonly IRemAnalyzer _analyzer;

        public REMController(
            ILogger<REMController> logger,
            RemToolDataContext context,
            IRemAnalyzer analyzer)
        {
            _logger = logger;
            db = context;
            _analyzer = analyzer;
        }
        [HttpGet("getUltimaActualizacion/")]
        public async Task<IActionResult> GetUltimaActualizacion()
        {
            var parametros = db.Parametros.FirstOrDefault();
            return Ok(new
            {
                ultima_actualizacion = parametros.UltimaActualizacion,
                servicio_enabled = parametros.ServicioEnabled,
                monitoreo_enabled = parametros.MonitoreoEnabled,
            });
        }


        public class ResultadoRevision
        {
            public string serie { get; set; }
            public string revisado { get; set; }
            public List<string> errores { get; set; }
            public List<string> advertencias { get; set; }
        }

        [HttpPost("revisarREM/")]
        public async Task<IActionResult> RevisarREM(IFormFile archivo, CancellationToken cancellationToken)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("No se ha enviado ningún archivo.");

            if (!string.Equals(Path.GetExtension(archivo.FileName), ".xlsm", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Sólo se permiten archivos con extensión .xlsm.");

            await using var stream = archivo.OpenReadStream();
            try
            {
                var analisis = await _analyzer.AnalizarAsync(stream, cancellationToken: cancellationToken);
                var mesTxt = new DateTime(analisis.Año, analisis.Mes, 1)
                    .ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))
                    .ToUpper(CultureInfo.CreateSpecificCulture("es"));

                return Ok(new ResultadoRevision
                {
                    serie = analisis.Serie,
                    revisado = $"{analisis.CodDeis} - {analisis.NombreEstablecimiento} - {mesTxt}",
                    errores = analisis.Errores,
                    advertencias = analisis.Advertencias
                });
            }
            catch (RemAnalysisException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        enum SeriesREM
        {
            A =1,
            BM = 2,
            D = 3,
            P = 4
        }

        [HttpPost("RecolectarREM/")]
        public async Task<IActionResult> RecolectarREM(IFormFile archivo)
        {
            // flujo
            // recibe varios archivo
            //      checkea version
            //      checkea nombre establecimineto/estrategia
            //      ignora cod establecimiento
            //      recorre las hojas que no esten vacias
            //      guarda en formato bdd deis todas las prestaciones con datos (codear si prestacion <> 0 guardar else saltar)
            if (archivo == null || archivo.Length == 0)
                return BadRequest("No se ha enviado ningún archivo.");


            using (var stream = new MemoryStream())
            {
                await archivo.CopyToAsync(stream);
                stream.Position = 0;

                using (var workbook = new ExcelPackage(stream))
                {
                    try
                    {
                        // Checkea versi�n
                        var hojaNombre = workbook.Workbook.Worksheets["NOMBRE"];
                        if (hojaNombre == null)
                            return BadRequest("No se encontr� la hoja 'NOMBRE'.");

                        string versionArchivo = hojaNombre.Cells["A9"].Value?.ToString();
                        if (string.IsNullOrEmpty(versionArchivo))
                            return BadRequest("No se encontr� la versi�n en la celda A9.");

                        var VersionActual = db.VersionRem
                            .Include(v => v.SerieRem)
                            .Where(v => v.Nombre.Equals(versionArchivo))
                            .FirstOrDefault();
                        if (VersionActual == null)
                            return BadRequest($"No se encontró la versión '{versionArchivo}' en la base de datos.");

                        var HojaControl = workbook.Workbook.Worksheets["Control"];

                        var JsonRecolectado = new JsonObject()
                        {
                            ["datos"] = new JsonArray(),
                            ["errores"] = "",
                            
                        };

                        var PrestacionesVersion = db.Prestacion.Include(p => p.VersionRem).Include(p => p.HojaRem).Where(p => p.VersionRem.Nombre.Equals(versionArchivo));
                        foreach (var prestacion in PrestacionesVersion)
                        {
                            JsonObject datos = new JsonObject()
                            {
                                ["prestacion"] = prestacion.CodigoPrestacion,
                                ["valores"] = new JsonArray()
                            };

                            bool PrestacionConDatos = false;
                            var HojaREM = workbook.Workbook.Worksheets[prestacion.HojaRem.Nombre];

                            // Verifica errores en la hoja de control según la serie
                            string ErroresHoja = "0";
                            var hojaControlDict = VersionActual.SerieRem.Nombre switch
                            {
                                "A" => Datos.HojaControl,
                                "P" => Datos.HojaControlP,
                                _   => null
                            };
                            if (hojaControlDict != null
                                && hojaControlDict.TryGetValue(VersionActual.Nombre, out var hojaMap)
                                && hojaMap.TryGetValue(prestacion.HojaRem.Nombre, out var celdaControl))
                            {
                                string celdaD = celdaControl.Replace("E", "D");
                                ErroresHoja = workbook.Workbook.Worksheets["Control"]?.Cells[celdaD]?.Value?.ToString() ?? "0";
                            }

                            foreach (var dato in prestacion.Coordenada)
                            {
                                string valor = HojaREM.Cells[dato].Value?.ToString() ?? "0";
                                //Console.WriteLine(HojaREM.Cells[dato.ToString()].Style.Numberformat.Format);
                                datos["valores"].AsArray().Add(valor);
                                if (valor != "0" && valor != "")
                                    PrestacionConDatos = true;
                            }
                            if(!ErroresHoja.Equals("0"))
                                JsonRecolectado["errores"] = "Errores en hoja de control";
                            if (PrestacionConDatos)
                                JsonRecolectado["datos"].AsArray().Add(datos);

                            
                        }
                        
                        return Ok(JsonRecolectado);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest("Error al procesar el archivo: " + ex.Message);
                    }
                }
            }
        }

        public class CompilarRemRequest
        {
            public string CodDEIS { get; set; }
            public int Mes { get; set; }
            public List<EstrategiaRemDTO> Estrategias { get; set; }
        }

        public class EstrategiaRemDTO
        {
            public List<PrestacionRemDTO> Datos { get; set; }
            public string Errores { get; set; }
        }

        public class PrestacionRemDTO
        {
            public string Prestacion { get; set; }
            public List<string> Valores { get; set; }
        }

        [HttpPost("CompilarREM/")]
        public async Task<IActionResult> CompilarREM([FromBody] CompilarRemRequest request)
        {
            if (request is null)
                return BadRequest("La solicitud es requerida.");

            if (request.Mes is < 1 or > 12)
                return BadRequest("El mes debe estar entre 1 y 12.");

            if (string.IsNullOrWhiteSpace(request.CodDEIS)
                || request.CodDEIS.Length != 6
                || !request.CodDEIS.All(char.IsDigit))
                return BadRequest("El CodDEIS debe contener exactamente 6 dígitos.");

            if (string.IsNullOrWhiteSpace(Datos.versionBase) || !System.IO.File.Exists(Datos.versionBase))
                return BadRequest("La plantilla base para Serie A no está configurada o no existe.");

            using (var package = new ExcelPackage(new FileInfo(Datos.versionBase)))
            {
                var Establecimiento = db.Establecimiento.FirstOrDefault(e => e.CodDeis.Equals(request.CodDEIS));
                if(Establecimiento == null)
                {
                    return BadRequest("El establecimiento de la solicitud no existe.");
                }
                //Escribe Datos Header
                string MesTxt = new DateTime(2025, request.Mes, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("es")).ToUpper();

                var HojaNombre = package.Workbook.Worksheets["NOMBRE"];
                HojaNombre.Cells["B2"].Value = "MONTE PATRIA";
                HojaNombre.Cells["C2"].Value = 0;
                HojaNombre.Cells["D2"].Value = 4;
                HojaNombre.Cells["E2"].Value = 3;
                HojaNombre.Cells["F2"].Value = 0;
                HojaNombre.Cells["G2"].Value = 3;

                HojaNombre.Cells["B3"].Value = Establecimiento.Nombre;
                HojaNombre.Cells["C3"].Value = int.Parse(request.CodDEIS[0].ToString());
                HojaNombre.Cells["D3"].Value = int.Parse(request.CodDEIS[1].ToString());
                HojaNombre.Cells["E3"].Value = int.Parse(request.CodDEIS[2].ToString());
                HojaNombre.Cells["F3"].Value = int.Parse(request.CodDEIS[3].ToString());
                HojaNombre.Cells["G3"].Value = int.Parse(request.CodDEIS[4].ToString());
                HojaNombre.Cells["H3"].Value = int.Parse(request.CodDEIS[5].ToString());


                HojaNombre.Cells["B6"].Value = MesTxt;
                HojaNombre.Cells["C6"].Value = int.Parse(request.Mes.ToString("D2")[0].ToString());
                HojaNombre.Cells["D6"].Value = int.Parse(request.Mes.ToString("D2")[1].ToString());

                HojaNombre.Cells["B11"].Value = Establecimiento.Director;
                HojaNombre.Cells["B12"].Value = "RICARDO CONTRERAS CORTES";

                var PrestacionesVersion = db.Prestacion.Include(p=>p.HojaRem).Where(p => p.VersionRem.Nombre == HojaNombre.Cells["A9"].Value.ToString()).ToList();

                foreach (var estrategia in request.Estrategias ?? [])
                {
                    foreach (var prestacion in estrategia.Datos ?? [])
                    {
                        if (string.IsNullOrWhiteSpace(prestacion.Prestacion))
                            return BadRequest("La solicitud contiene una prestación sin código.");

                        var DatosPrestacion = PrestacionesVersion
                            .FirstOrDefault(p => p.CodigoPrestacion.Equals(prestacion.Prestacion));
                        if (DatosPrestacion is null)
                            return BadRequest($"La prestación '{prestacion.Prestacion}' no existe en la plantilla base de Serie A.");

                        if (prestacion.Valores is null)
                            continue;

                        if (prestacion.Valores.Count > DatosPrestacion.Coordenada.Count)
                            return BadRequest($"La prestación '{prestacion.Prestacion}' contiene {prestacion.Valores.Count} valores, pero la plantilla sólo admite {DatosPrestacion.Coordenada.Count}.");

                        int index = 0;
                        foreach (string valor in prestacion.Valores)
                        {
                            string celda = DatosPrestacion.Coordenada[index].ToString();
                            var hoja = DatosPrestacion.HojaRem.Nombre.ToString() ?? "";
                            var HojaREM = package.Workbook.Worksheets[hoja];
                            if (HojaREM is null)
                                return BadRequest($"No se encontró la hoja '{hoja}' para la prestación '{prestacion.Prestacion}'.");

                            if (string.IsNullOrEmpty(HojaREM.Cells[celda.ToString()].Formula))
                            {
                                string valorold = HojaREM.Cells[celda.ToString()].Value?.ToString() ?? "0";

                                if (!TryParseRemNumber(valorold, out var valorBase)
                                    || !TryParseRemNumber(valor ?? "0", out var valorNuevo))
                                    return BadRequest($"No se pudo interpretar un valor numérico para la prestación '{prestacion.Prestacion}' en la celda '{celda}'.");

                                if (!HojaREM.Cells[celda].Style.Locked)
                                    HojaREM.Cells[celda].Value = valorBase + valorNuevo;
                            }

                            index += 1;
                        }


                        }
                    }
                
                // Guardar el archivo en un MemoryStream
                using (var stream = new MemoryStream())
                {
                    package.Workbook.Calculate();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    // Retornar el archivo como descarga
                    return File(
                        stream.ToArray(),
                        "application/vnd.ms-excel.sheet.macroEnabled.12",
                        request.CodDEIS + "A" + request.Mes.ToString("D2") + "-compilado.xlsm"
                    );
                }
            }
        }

        private static bool TryParseRemNumber(string? raw, out decimal value)
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return true;

            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("es-CL"), out value);
        }

        [HttpPost("CompilarREMP/")]
        public async Task<IActionResult> CompilarREMP([FromBody] CompilarRemRequest request)
        {
            if (string.IsNullOrEmpty(Datos.versionBaseP))
                return BadRequest("La plantilla para Serie P no está configurada (Paths__BaseSP).");

            using (var package = new ExcelPackage(new FileInfo(Datos.versionBaseP)))
            {
                var Establecimiento = db.Establecimiento.FirstOrDefault(e => e.CodDeis.Equals(request.CodDEIS));
                if (Establecimiento == null)
                    return BadRequest("El establecimiento de la solicitud no existe.");

                string MesTxt = new DateTime(2025, request.Mes, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("es")).ToUpper();

                var HojaNombre = package.Workbook.Worksheets["NOMBRE"];
                HojaNombre.Cells["B2"].Value = "MONTE PATRIA";
                HojaNombre.Cells["C2"].Value = 0;
                HojaNombre.Cells["D2"].Value = 4;
                HojaNombre.Cells["E2"].Value = 3;
                HojaNombre.Cells["F2"].Value = 0;
                HojaNombre.Cells["G2"].Value = 3;

                HojaNombre.Cells["B3"].Value = Establecimiento.Nombre;
                HojaNombre.Cells["C3"].Value = int.Parse(request.CodDEIS[0].ToString());
                HojaNombre.Cells["D3"].Value = int.Parse(request.CodDEIS[1].ToString());
                HojaNombre.Cells["E3"].Value = int.Parse(request.CodDEIS[2].ToString());
                HojaNombre.Cells["F3"].Value = int.Parse(request.CodDEIS[3].ToString());
                HojaNombre.Cells["G3"].Value = int.Parse(request.CodDEIS[4].ToString());
                HojaNombre.Cells["H3"].Value = int.Parse(request.CodDEIS[5].ToString());

                HojaNombre.Cells["B6"].Value = MesTxt;
                HojaNombre.Cells["C6"].Value = int.Parse(request.Mes.ToString("D2")[0].ToString());
                HojaNombre.Cells["D6"].Value = int.Parse(request.Mes.ToString("D2")[1].ToString());

                HojaNombre.Cells["B11"].Value = Establecimiento.Director;
                HojaNombre.Cells["B12"].Value = "RICARDO CONTRERAS CORTES";

                var PrestacionesVersion = db.Prestacion
                    .Include(p => p.HojaRem)
                    .Where(p => p.VersionRem.Nombre == HojaNombre.Cells["A9"].Value.ToString())
                    .ToList();

                foreach (EstrategiaRemDTO estrategia in request.Estrategias)
                {
                    foreach (PrestacionRemDTO prestacion in estrategia.Datos)
                    {
                        var DatosPrestacion = PrestacionesVersion
                            .Where(p => p.CodigoPrestacion.Equals(prestacion.Prestacion))
                            .FirstOrDefault();
                        int index = 0;
                        foreach (string valor in prestacion.Valores)
                        {
                            string celda = DatosPrestacion.Coordenada.ElementAt(index).ToString();
                            var hoja = DatosPrestacion.HojaRem.Nombre.ToString() ?? "";
                            var HojaREM = package.Workbook.Worksheets[hoja];
                            if (string.IsNullOrEmpty(HojaREM.Cells[celda].Formula))
                            {
                                string valorold = HojaREM.Cells[celda].Value?.ToString() ?? "0";
                                try
                                {
                                    int valornuevo = int.Parse(valorold) + int.Parse(valor);
                                    if (!HojaREM.Cells[celda].Style.Locked)
                                        HojaREM.Cells[celda].Value = valornuevo;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"Error en {DatosPrestacion.CodigoPrestacion}: parseando {valorold}, {valor}, {e.Message}");
                                    return BadRequest("Error en la solicitud.");
                                }
                            }
                            index += 1;
                        }
                    }
                }

                using (var stream = new MemoryStream())
                {
                    package.Workbook.Calculate();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.ms-excel.sheet.macroEnabled.12",
                        request.CodDEIS + "P" + request.Mes.ToString("D2") + "-compilado.xlsm"
                    );
                }
            }
        }

    }
}

using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCalc;
using OfficeOpenXml;
using RemTool.Util;


namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class REMController : ControllerBase
    {

        private readonly ILogger<REMController> _logger;
        private VoidDataContext db;

        public REMController(ILogger<REMController> logger, VoidDataContext context)
        {
            _logger = logger;
            db = context;
        }
        [HttpGet("getUltimaActualizacion/")]
        public async Task<IActionResult> GetUltimaActualizacion()
        {
            var parametros = db.Parametros.FirstOrDefault();
            return Ok(new
            {
                ultima_actualizacion = parametros.UltimaActualizacion,
                servicio_enabled = parametros.ServicioEnabled,
            });
        }


        public class ResultadoRevision
        {
            public string serie { get; set; }
            public string revisado { get; set; }
            public List<string> errores { get; set; }
        }

        [HttpPost("revisarREM/")]
        public async Task<IActionResult> RevisarREM(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("No se ha enviado ningún archivo.");


            using (var stream = new MemoryStream())
            {
                await archivo.CopyToAsync(stream);
                stream.Position = 0;

                using (var workbook = new ExcelPackage(stream))
                {
                    bool hayerrores = false;
                    try
                    {
                        var hojaNombre = workbook.Workbook.Worksheets["NOMBRE"];

                        string TipoREM = hojaNombre.Cells["B17"].Value.ToString()[hojaNombre.Cells["B17"].Value.ToString().Length - 1].ToString() ?? "";
                        string CodigoREM = hojaNombre.Cells["C3"].Value.ToString() + hojaNombre.Cells["D3"].Value.ToString() + hojaNombre.Cells["E3"].Value.ToString() + hojaNombre.Cells["F3"].Value.ToString() + hojaNombre.Cells["G3"].Value.ToString() + hojaNombre.Cells["H3"].Value.ToString();
                        string MesREM = hojaNombre.Cells["C6"].Value.ToString() + hojaNombre.Cells["D6"].Value.ToString();
                        string versionArchivo = hojaNombre.Cells["A9"].Value.ToString();

                        string MesTxt = new DateTime(2025, int.Parse(MesREM), 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("es")).ToUpper();

                        var Establecimiento = db.Establecimiento.FirstOrDefault(e => e.CodDeis.Equals(CodigoREM));
                        var Reglas = db.Regla.Include(r => r.VersionREM).Where(r => r.VersionREM.Nombre.Equals(versionArchivo)).ToList();
                        ResultadoRevision ListaErrores = new ResultadoRevision { serie = "A", revisado = $"{CodigoREM} - {Establecimiento.Nombre} - {MesTxt}", errores = new List<string>() };
                        foreach (var regla in Reglas)
                        {
                            string resultado = Regex.Replace(regla.Expresion, Utils.RegexHoja, x => Utils.ParseaHojas(x.Value, workbook));
                            resultado = resultado.Replace("[", "").Replace("]", "");
                            Expression Expr = new Expression(resultado);
                            //Console.WriteLine(resultado + ((bool)Expr.Evaluate()).ToString());
                            if (!(bool)Expr.Evaluate())
                            {
                                ListaErrores.errores.Add(regla.Mensaje);
                                hayerrores = true;
                            }
                        }
                        if (hayerrores)
                            return Ok(ListaErrores);
                        else
                            return Ok("Sin errores");
                    }

                    catch (Exception e)
                    {
                        return BadRequest("Error al procesar el archivo: " + e.Message);
                    }
                }
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

                        var VersionActual = db.VersionRem.Where(v=>v.id_serie == (long)SeriesREM.A).OrderByDescending(v => v.Fecha).FirstOrDefault();
                        //if (!VersionActual.Nombre.Equals(versionArchivo))
                        //    return BadRequest("Los archivos deben tener la versi�n " + Datos.VersionesSerieA.Last() + ".");


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
                            string ErroresHoja = Datos.HojaControl[VersionActual.Nombre][prestacion.HojaRem.Nombre].Replace("E","D");
                            ErroresHoja = workbook.Workbook.Worksheets["Control"].Cells[ErroresHoja].Value?.ToString() ?? "";

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

                foreach (EstrategiaRemDTO estrategia in request.Estrategias)
                {
                    foreach (PrestacionRemDTO prestacion in estrategia.Datos)
                    {
                        var DatosPrestacion = PrestacionesVersion.Where(p=>p.CodigoPrestacion.Equals(prestacion.Prestacion)).FirstOrDefault();
                        int index = 0;
                        foreach (string valor in prestacion.Valores)
                        {
                            string celda = DatosPrestacion.Coordenada.ElementAt(index).ToString();
                            var hoja = DatosPrestacion.HojaRem.Nombre.ToString() ?? "";
                            var HojaREM = package.Workbook.Worksheets[hoja];
                            if (string.IsNullOrEmpty(HojaREM.Cells[celda.ToString()].Formula))
                            {
                                
                                string valorold = HojaREM.Cells[celda.ToString()].Value?.ToString() ?? "0";

                                try
                                {
                                    int valornuevo = int.Parse(valorold) + int.Parse(valor);
                                    if (!HojaREM.Cells[celda.ToString()].Style.Locked)
                                    {
                                        HojaREM.Cells[celda.ToString()].Value = valornuevo;
                                    }
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

    }
}

using DocumentFormat.OpenXml;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NCalc;
using OfficeOpenXml;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using FontSize = DocumentFormat.OpenXml.Wordprocessing.FontSize;
using RemTool.Shared;

namespace RemTools
{
    public partial class Utils
    {
        public void ExtraerArchivo(string rutaArchivo, string serie, int año)
        {
            string nombreArchivo = Path.GetFileNameWithoutExtension(rutaArchivo);
            var VersionSerie = Bdd.VersionRem.Include(v => v.SerieRem).Where(v => v.SerieRem.Nombre.Equals(serie)).ToList();
            using var REM = new ExcelPackage(rutaArchivo);

            ExcelWorksheet hojaNombre = REM.Workbook.Worksheets["NOMBRE"];
            string VersionArchivo = hojaNombre.Cells["A9"].Value.ToString() ?? "";
            //VersionArchivo = "Versión 1.1: Febrero 2026";
            string CodigoArchivo = nombreArchivo.Substring(0, 6);
            string TipoArchivo = hojaNombre.Cells["B17"].Value.ToString() ?? "";
            string MesArchivo = nombreArchivo.Substring(nombreArchivo.Length - 2);

            bool vVersion = VersionSerie.Exists(v => v.Nombre.Equals(VersionArchivo));

            var TipoREM = hojaNombre.Cells["B17"].Value.ToString();
            TipoREM = TipoREM.Substring(TipoREM.IndexOf(' ') + 1).Length < 2
                ? TipoREM.Substring(TipoREM.IndexOf(' ') + 1)
                : TipoREM.Substring(TipoREM.IndexOf(' ') + 1).Split(' ')[0];

            string CodigoREM = hojaNombre.Cells["C3"].Value.ToString() + hojaNombre.Cells["D3"].Value.ToString() + hojaNombre.Cells["E3"].Value.ToString() + hojaNombre.Cells["F3"].Value.ToString() + hojaNombre.Cells["G3"].Value.ToString() + hojaNombre.Cells["H3"].Value.ToString();
            string MesREM = hojaNombre.Cells["C6"].Value.ToString() + hojaNombre.Cells["D6"].Value.ToString();
            string ComunaREM = hojaNombre.Cells["C2"].Value.ToString() + hojaNombre.Cells["D2"].Value.ToString() + hojaNombre.Cells["E2"].Value.ToString() + hojaNombre.Cells["F2"].Value.ToString() + hojaNombre.Cells["G2"].Value.ToString();

            string MesTxt = new DateTime(2025, int.Parse(MesArchivo), 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("es")).ToUpper();
            var Establecimiento = Bdd.Establecimiento.Where(e => e.CodDeis.Equals(CodigoREM)).FirstOrDefault();
            Console.WriteLine("Cargando : " + Establecimiento.Nombre + " - REM " + serie + " - " + MesTxt);

            var EstructuraVersion = Bdd.VersionRem
                .Include(v => v.Prestacions)
                    .ThenInclude(p => p.HojaRem)
                .Where(v => v.Nombre.Equals(VersionArchivo) && v.SerieRem.Nombre.Equals(serie)).FirstOrDefault();

            if(EstructuraVersion== null)
            {
                Console.WriteLine("No se encontró la estructura para esta versión y serie. No se guardarán los cambios.");
                return;
            }
            string ColumnasSACSV = "";
            int maxColumnas = 0;
            foreach (var prestacion in EstructuraVersion.Prestacions)
            {
                if (prestacion.Coordenada.Count > maxColumnas) maxColumnas = prestacion.Coordenada.Count;
            }
            for (int i = 0; i < maxColumnas; i++)
                ColumnasSACSV += $",Col{(i + 1).ToString("D2")}";

            //string nombreCSV = $"salida/{DateTime.Now.Ticks}-{nombreArchivo}-{MesArchivo}.csv";
            //using var salidaCSV = new StreamWriter(nombreCSV);
            //salidaCSV.WriteLine($"Mes,IdServicio,Ano,IdEstablecimiento,CodigoPrestacion,IdRegion,IdComuna{ColumnasSACSV}");

            var RegistrosReporte = new List<Registro>();
            foreach (var prestacion in EstructuraVersion.Prestacions)
            {
                string hoja = prestacion.HojaRem.Nombre;
                var HojaREM = REM.Workbook.Worksheets[hoja];
                var DatosPrestacion = prestacion.Coordenada;
                string lineadatos = "";
                bool lineaconDatos = false;
                var datosBdd = new List<int>();

                foreach (var dato in DatosPrestacion)
                {
                    try
                    {
                        if (HojaREM.Cells[dato].Value == null)
                        {
                            lineadatos += ",";
                            datosBdd.Add(0);
                        }
                        else
                        {
                            string extraido = HojaREM.Cells[dato].Value.ToString();
                            if (extraido.Equals("") || extraido.Equals("0"))
                            {
                                lineadatos += ",";
                                datosBdd.Add(0);
                                continue;
                            }
                            float numExtraido = float.Parse(extraido);
                            int redondeado = (int)Convert.ToInt32(numExtraido);
                            if (!serie.Equals("D"))
                            {
                                lineadatos += "," + redondeado;
                                datosBdd.Add(redondeado);
                            }
                            else
                            {
                                lineadatos += "," + numExtraido.ToString(CultureInfo.InvariantCulture);
                                datosBdd.Add((int)numExtraido);
                            }
                            lineaconDatos = true;
                        }
                    }
                    catch
                    {
                        MessageBox.Show($"Error al extraer: {dato}, {hoja}. No se guardarán los cambios.");
                        return;
                    }
                }

                //if (!lineaconDatos) continue;
                //salidaCSV.WriteLine($"{int.Parse(MesArchivo)},5,2025,{CodigoArchivo},{prestacion.CodigoPrestacion},4,4303{lineadatos}");

                var prestacionBdd = Bdd.Prestacion.Include(p => p.VersionRem)
                    .Where(p => p.CodigoPrestacion.Equals(prestacion.CodigoPrestacion) && p.VersionRem.Nombre.Equals(VersionArchivo))
                    .FirstOrDefault();

                RegistrosReporte.Add(new Registro
                {
                    id_prestacion = prestacionBdd.Id,
                    Valor = datosBdd,
                });
            }

            var reporteExistente = Bdd.Reporte.Include(r => r.Registros).ThenInclude(r => r.Prestacion).ThenInclude(p => p.VersionRem)
                .Where(r => r.id_establecimiento == Establecimiento.Id &&
                                     r.Mes.Equals(int.Parse(MesREM)) &&
                                     r.Comuna.CodDeis.Equals(ComunaREM) &&
                                     r.Registros.FirstOrDefault().Prestacion.VersionRem.Nombre.Equals(VersionArchivo) &&
                                     r.Registros.FirstOrDefault().Prestacion.VersionRem.Fecha.Year == año);

            if (reporteExistente != null)
            {
                Bdd.Registro.RemoveRange(reporteExistente.SelectMany(r => r.Registros));
                Bdd.Reporte.RemoveRange(reporteExistente);
                Bdd.SaveChanges();
            }

            var establecimiento = Bdd.Establecimiento.Where(e => e.CodDeis.Equals(CodigoREM)).FirstOrDefault();
            var comuna = Bdd.Comuna.Where(c => c.CodDeis.Equals(ComunaREM)).FirstOrDefault();

            if (RegistrosReporte.Count == 0)
            {
                Console.WriteLine($"No se encontraron datos para cargar en BD. No se guardarán los cambios de {establecimiento.Nombre} {MesTxt} {VersionArchivo}");
                return;
            }

            Bdd.Reporte.Add(new Reporte
            {
                id_establecimiento = establecimiento.Id,
                id_comuna = 1,
                Mes = int.Parse(MesREM),
                Registros = RegistrosReporte
            });
            Bdd.SaveChanges();
        }

        public void RevisarRem(string serie)
        {
            MessageBox.Show($"Seleccione los archivos Serie {serie} a revisar.", $"Revisor REM Serie {serie}");

            var files = SelectFiles();
            if (files == null || files.Length == 0) return;

            var docPath = $"Revision{DateTimeOffset.Now.ToUnixTimeSeconds()}.docx";
            using var wordDoc = WordprocessingDocument.Create(docPath, WordprocessingDocumentType.Document);
            MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            AddHeading(mainPart, $"REVISIÓN REM SERIE {serie.ToUpper()}", 0);

            foreach (var file in files)
            {
                try
                {
                    using var workbook = new ExcelPackage(file);
                    var hojaNombre = workbook.Workbook.Worksheets["NOMBRE"];

                    string CodigoREM = hojaNombre.Cells["C3"].Value.ToString() + hojaNombre.Cells["D3"].Value.ToString() + hojaNombre.Cells["E3"].Value.ToString() + hojaNombre.Cells["F3"].Value.ToString() + hojaNombre.Cells["G3"].Value.ToString() + hojaNombre.Cells["H3"].Value.ToString();
                    string MesREM = hojaNombre.Cells["C6"].Value.ToString() + hojaNombre.Cells["D6"].Value.ToString();
                    string versionArchivo = hojaNombre.Cells["A9"].Value.ToString();

                    var Establecimiento = Bdd.Establecimiento.Where(e => e.CodDeis.Equals(CodigoREM)).FirstOrDefault();
                    string MesTxt = new DateTime(2025, int.Parse(MesREM), 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("es")).ToUpper();

                    Console.WriteLine($"Revisando : {Establecimiento.Nombre} - REM {serie.ToUpper()} - {MesTxt}");
                    AddHeading(mainPart, $"{CodigoREM} - {Establecimiento.Nombre} - {MesTxt}", 2);

                    var Reglas = Bdd.Regla.OrderBy(r=>r.Id)
                        .Include(r => r.VersionREM).ThenInclude(v => v.SerieRem)
                        .ToList();

                    Reglas = Reglas.Where(r => r.VersionREM.Nombre.Equals(versionArchivo) && r.VersionREM.SerieRem.Nombre.Equals(serie, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (Reglas.Count == 0)
                    {
                        AddParagraph(mainPart, "No se encontraron reglas para esta versión.");
                        continue;
                    }

                    foreach (var regla in Reglas)
                    {
                        
                        string resultado = Regex.Replace(regla.Expresion, RegexHoja, x => ParseaHojas(x.Value, workbook));
                        resultado = resultado.Replace("[", "").Replace("]", "");
                        Expression Expr = new Expression(resultado);
                        if (!(bool)Expr.Evaluate())
                            AddParagraph(mainPart, regla.Mensaje + "\n");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Se omite el archivo: {file}. Ha ocurrido un error." + e);
                }
            }

            Process.Start(new ProcessStartInfo(docPath) { UseShellExecute = true });
        }

        public void ImprimeReglas(string version)
        {
            var Reglas = Bdd.Regla.Where(r => r.VersionREM.Nombre.Equals(version)).ToList();

            if (Reglas.Count == 0)
            {
                MessageBox.Show("No se encontraron reglas para la versión especificada.", "Error");
                return;
            }

            var docPath = $"Reglas_{DateTimeOffset.Now.ToUnixTimeSeconds()}.docx";
            using var wordDoc = WordprocessingDocument.Create(docPath, WordprocessingDocumentType.Document);
            MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            AddHeading(mainPart, $"Reglas REM Serie A - {version}", 0);

            foreach (var regla in Reglas)
                AddParagraphEstilizado(mainPart, regla.Mensaje, false);

            Process.Start(new ProcessStartInfo(docPath) { UseShellExecute = true });
        }

        public void RevisarIntegridadArchivos()
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Seleccione el directorio con los archivos REM a revisar",
                UseDescriptionForTitle = true
            };
            if (folderDialog.ShowDialog() != DialogResult.OK) return;

            string directorio = folderDialog.SelectedPath;

            // Celda de la hoja CONTROL que indica la cuenta de errores, indexada por serie
            var celdasControl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "A",  "D32" },
                { "BM", "D7"  },
                { "D",  "E13" },
                { "P",  "D17" }
            };

            // Cargar todas las versiones en memoria y quedarse con la más reciente por serie
            var todasVersiones = Bdd.VersionRem
                .Include(v => v.SerieRem)
                .ToList();

            var ultimasVersiones = todasVersiones
                .GroupBy(v => v.SerieRem.Nombre, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(v => v.Fecha).First().Nombre,
                    StringComparer.OrdinalIgnoreCase);

            // Filtrar archivos con el patrón de nombre REM: 6 dígitos + serie + 2 dígitos de mes
            var regexNombre = new Regex(@"\\[0-9]{6}[A-Z]{1,2}[0-9]{2}(?i:\.xlsm)$", RegexOptions.IgnoreCase);
            var archivos = Directory.GetFiles(directorio, "*.xlsm", SearchOption.TopDirectoryOnly)
                .Where(f => regexNombre.IsMatch(f))
                .OrderBy(f => f)
                .ToArray();

            if (archivos.Length == 0)
            {
                Console.WriteLine("No se encontraron archivos REM con el formato de nombre esperado en el directorio seleccionado.");
                return;
            }

            Console.WriteLine($"--- Revisión de integridad: {archivos.Length} archivo(s) encontrado(s) en {directorio} ---");
            Console.WriteLine();

            int correctos = 0;
            int conProblemas = 0;

            foreach (var rutaArchivo in archivos)
            {
                string nombreArchivo = Path.GetFileNameWithoutExtension(rutaArchivo);

                // Extraer partes del nombre: [0-5]=código DEIS, [6..^2]=serie, [^2..]=mes
                string codigoNombre = nombreArchivo.Substring(0, 6);
                string mesNombre    = nombreArchivo.Substring(nombreArchivo.Length - 2);
                string serieNombre  = nombreArchivo.Substring(6, nombreArchivo.Length - 8);

                var errores = new List<string>();

                try
                {
                    using var workbook = new ExcelPackage(rutaArchivo);
                    var hojaNombre = workbook.Workbook.Worksheets["NOMBRE"];

                    if (hojaNombre == null)
                    {
                        errores.Add("No se encontró la hoja 'NOMBRE' en el archivo.");
                        ImprimirResultado(nombreArchivo, errores, ref correctos, ref conProblemas);
                        continue;
                    }

                    string versionArchivo = hojaNombre.Cells["A9"].Value?.ToString() ?? "";
                    string codigoREM      = (hojaNombre.Cells["C3"].Value?.ToString() ?? "")
                                         + (hojaNombre.Cells["D3"].Value?.ToString() ?? "")
                                         + (hojaNombre.Cells["E3"].Value?.ToString() ?? "")
                                         + (hojaNombre.Cells["F3"].Value?.ToString() ?? "")
                                         + (hojaNombre.Cells["G3"].Value?.ToString() ?? "")
                                         + (hojaNombre.Cells["H3"].Value?.ToString() ?? "");

                    // Extraer serie desde celda B17 (mismo método que DeconstruyeREM)
                    string serieRaw = hojaNombre.Cells["B17"].Value?.ToString() ?? "";
                    string aux      = serieRaw.Substring(serieRaw.IndexOf(' ') + 1);
                    string serieHoja = aux.Length < 2 ? aux : aux.Split(' ')[0];

                    // CHECK 1: serie del nombre de archivo == serie de la hoja NOMBRE
                    if (!serieNombre.Equals(serieHoja, StringComparison.OrdinalIgnoreCase))
                        errores.Add($"Serie incorrecta: el nombre indica '{serieNombre}' pero la hoja NOMBRE indica '{serieHoja}'.");

                    // CHECK 2: código DEIS del nombre == código DEIS interno (C3:H3)
                    if (!codigoNombre.Equals(codigoREM, StringComparison.OrdinalIgnoreCase))
                        errores.Add($"Código DEIS no coincide: nombre='{codigoNombre}', hoja NOMBRE='{codigoREM}'.");

                    // CHECK 3: versión del archivo == versión más reciente de la serie en BD
                    if (ultimasVersiones.TryGetValue(serieHoja, out string? ultimaVersion))
                    {
                        if (!versionArchivo.Equals(ultimaVersion, StringComparison.OrdinalIgnoreCase))
                            errores.Add($"Versión desactualizada: archivo='{versionArchivo}', última en BD='{ultimaVersion}'.");
                    }
                    else
                    {
                        errores.Add($"Serie '{serieHoja}' no encontrada en BD; no se pudo verificar la versión.");
                    }

                    // CHECK 4: hoja CONTROL sin errores
                    if (celdasControl.TryGetValue(serieHoja, out string? celdaControl))
                    {
                        var hojaControl = workbook.Workbook.Worksheets["CONTROL"];
                        if (hojaControl == null)
                        {
                            errores.Add("No se encontró la hoja 'CONTROL' en el archivo.");
                        }
                        else
                        {
                            var valorControl = hojaControl.Cells[celdaControl].Value;
                            int cuentaErrores = valorControl == null ? 0 : Convert.ToInt32(valorControl);
                            if (cuentaErrores != 0)
                                errores.Add($"La hoja CONTROL indica {cuentaErrores} error(es) (celda {celdaControl}).");
                        }
                    }
                    else
                    {
                        errores.Add($"Serie '{serieHoja}': celda de control no definida para esta serie.");
                    }

                    // CHECK 5: mes del nombre del archivo (últimos 2 chars) == C6+D6 de la hoja NOMBRE
                    string mesHoja = (hojaNombre.Cells["C6"].Value?.ToString() ?? "")
                                   + (hojaNombre.Cells["D6"].Value?.ToString() ?? "");
                    if (!mesNombre.Equals(mesHoja, StringComparison.OrdinalIgnoreCase))
                        errores.Add($"Mes no coincide: nombre='{mesNombre}', hoja NOMBRE C6+D6='{mesHoja}'.");


                }
                catch (Exception ex)
                {
                    errores.Add($"Error al leer el archivo: {ex.Message}");
                }

                ImprimirResultado(nombreArchivo, errores, ref correctos, ref conProblemas);
            }

            Console.WriteLine();
            Console.WriteLine($"--- Resumen: {correctos} correcto(s), {conProblemas} con problema(s) ---");
        }

        private static void ImprimirResultado(string nombreArchivo, List<string> errores, ref int correctos, ref int conProblemas)
        {
            if (errores.Count == 0)
            {
                Console.WriteLine($"[OK]    {nombreArchivo}");
                correctos++;
            }
            else
            {
                Console.WriteLine($"[ERROR] {nombreArchivo}");
                foreach (var error in errores)
                    Console.WriteLine($"        - {error}");
                conProblemas++;
            }
        }

        private static string[] SelectFiles()
        {
            using var dialog = new OpenFileDialog { Multiselect = true, Filter = "REM|*.xlsm" };
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileNames : Array.Empty<string>();
        }

        private static void AddHeading(MainDocumentPart mainPart, string text, int level)
        {
            var body = mainPart.Document.Body;
            body.AppendChild(new Paragraph(
                new ParagraphProperties(new ParagraphStyleId() { Val = "Heading" + (level + 1) }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = level == 0 ? "32" : level == 1 ? "28" : "24" }),
                    new Text(text)
                )
            ));
        }

        private static void AddParagraph(MainDocumentPart mainPart, string text)
        {
            mainPart.Document.Body.AppendChild(new Paragraph(new Run(new Text(text))));
        }

        private static void AddParagraphEstilizado(MainDocumentPart mainPart, string text, bool negrita)
        {
            var runProps = new RunProperties();
            if (negrita) runProps.Append(new Bold());
            runProps.Append(new FontSize() { Val = "24" });
            mainPart.Document.Body.AppendChild(new Paragraph(new Run(runProps, new Text(text))));
        }

        private static string ParseaHojas(string regla, ExcelPackage libro)
        {
            var hoja = libro.Workbook.Worksheets[regla.Substring(0, regla.IndexOf("["))];
            return Regex.Replace(regla.Substring(regla.IndexOf("[")), RegexCelda, x => Extrae(x.Value, hoja));
        }

        private static string Extrae(string rango, ExcelWorksheet hoja)
        {
            var valor = hoja.Cells[rango].Value;
            if (valor == null) return "0";
            if (valor.ToString().Equals("#VALUE!")) return "''";
            return valor.ToString();
        }
    }
}

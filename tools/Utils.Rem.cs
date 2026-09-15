using DocumentFormat.OpenXml;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NCalc;
using Npgsql;
using NpgsqlTypes;
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
            try
            {
                ExtraerArchivoInterno(rutaArchivo, serie, año);
            }
            finally
            {
                // Cada archivo se procesa de forma independiente. No conservar
                // reportes ni registros anteriores evita que SaveChanges tarde
                // cada vez más a medida que avanza la carga mensual.
                Bdd.ChangeTracker.Clear();
            }
        }

        private void ExtraerArchivoInterno(string rutaArchivo, string serie, int año)
        {
            string nombreArchivo = Path.GetFileNameWithoutExtension(rutaArchivo);
            using var REM = new ExcelPackage(rutaArchivo);

            ExcelWorksheet hojaNombre = REM.Workbook.Worksheets["NOMBRE"];
            string VersionArchivo = hojaNombre.Cells["A9"].Value?.ToString() ?? "";
            //VersionArchivo = "Versión 1.1: Febrero 2026";
            string MesArchivo = nombreArchivo.Substring(nombreArchivo.Length - 2);

            string CodigoREM = hojaNombre.Cells["C3"].Value.ToString() + hojaNombre.Cells["D3"].Value.ToString() + hojaNombre.Cells["E3"].Value.ToString() + hojaNombre.Cells["F3"].Value.ToString() + hojaNombre.Cells["G3"].Value.ToString() + hojaNombre.Cells["H3"].Value.ToString();
            string MesREM = hojaNombre.Cells["C6"].Value.ToString() + hojaNombre.Cells["D6"].Value.ToString();
            string ComunaREM = hojaNombre.Cells["C2"].Value.ToString() + hojaNombre.Cells["D2"].Value.ToString() + hojaNombre.Cells["E2"].Value.ToString() + hojaNombre.Cells["F2"].Value.ToString() + hojaNombre.Cells["G2"].Value.ToString();

            int mesRem = int.Parse(MesREM);
            string MesTxt = new DateTime(año, int.Parse(MesArchivo), 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("es")).ToUpper();
            var establecimiento = Bdd.Establecimiento
                .AsNoTracking()
                .FirstOrDefault(e => e.CodDeis == CodigoREM);
            if (establecimiento == null)
            {
                Console.WriteLine($"No se encontro el establecimiento {CodigoREM}. Se omite el archivo {nombreArchivo}.");
                return;
            }

            var comuna = Bdd.Comuna
                .AsNoTracking()
                .FirstOrDefault(c => c.CodDeis == ComunaREM);
            if (comuna == null)
            {
                Console.WriteLine($"No se encontro la comuna {ComunaREM}. Se omite el archivo {nombreArchivo}.");
                return;
            }

            Console.WriteLine("Cargando : " + establecimiento.Nombre + " - REM " + serie + " - " + MesTxt);

            var estructuraVersion = ObtenerEstructuraVersion(VersionArchivo, serie);

            if (estructuraVersion == null)
            {
                Console.WriteLine("No se encontró la estructura para esta versión y serie. No se guardarán los cambios.");
                return;
            }
            var cronometroExtraccion = Stopwatch.StartNew();
            var prestacionesCarga = estructuraVersion.Prestaciones;
            var registrosReporte = new List<Registro>(prestacionesCarga.Count);
            var prestacionesSinDatos = 0;

            foreach (var prestacionCarga in prestacionesCarga)
            {
                string hoja = prestacionCarga.Hoja;
                var hojaRem = REM.Workbook.Worksheets[hoja]
                    ?? throw new InvalidOperationException($"No se encontro la planilla '{hoja}' en el archivo.");
                var datosBdd = new List<int>(prestacionCarga.Coordenadas.Count);

                foreach (var dato in prestacionCarga.Coordenadas)
                {
                    try
                    {
                        var valorCelda = hojaRem.Cells[dato].Value;
                        if (valorCelda == null)
                        {
                            datosBdd.Add(0);
                        }
                        else
                        {
                            string extraido = valorCelda.ToString() ?? string.Empty;
                            if (extraido.Equals("") || extraido.Equals("0"))
                            {
                                datosBdd.Add(0);
                                continue;
                            }
                            float numExtraido = float.Parse(extraido, CultureInfo.CurrentCulture);
                            int redondeado = (int)Convert.ToInt32(numExtraido);
                            if (!serie.Equals("D", StringComparison.OrdinalIgnoreCase))
                            {
                                datosBdd.Add(redondeado);
                            }
                            else
                            {
                                datosBdd.Add((int)numExtraido);
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show($"Error al extraer: {dato}, {hoja}. No se guardarán los cambios.");
                        return;
                    }
                }

                if (datosBdd.Any(valor => valor != 0))
                {
                    registrosReporte.Add(new Registro
                    {
                        id_prestacion = prestacionCarga.Id,
                        Valor = datosBdd,
                    });
                }
                else
                {
                    prestacionesSinDatos++;
                }
            }

            cronometroExtraccion.Stop();
            Console.WriteLine($"[Registros] Datos extraídos: {establecimiento.Nombre} - {registrosReporte.Count:N0} prestaciones con datos; {prestacionesSinDatos:N0} sin datos omitidas en {cronometroExtraccion.Elapsed.TotalSeconds:F1}s.");

            Bdd.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
            using var transaction = Bdd.Database.BeginTransaction();

            var idsReportesExistentes = Bdd.Reporte
                .Where(reporte => reporte.id_establecimiento == establecimiento.Id
                    && reporte.id_comuna == comuna.Id
                    && reporte.Año == año
                    && reporte.Mes == mesRem
                    && reporte.Registros.Any(registro => registro.Prestacion.id_version == estructuraVersion.Id))
                .Select(reporte => reporte.Id)
                .ToList();

            if (idsReportesExistentes.Count > 0)
            {
                Console.WriteLine($"[Registros] Eliminando {idsReportesExistentes.Count} reporte(s) anterior(es) de {establecimiento.Nombre}...");
                Bdd.Registro
                    .Where(registro => idsReportesExistentes.Contains(registro.id_reporte))
                    .ExecuteDelete();
                Bdd.Reporte
                    .Where(reporte => idsReportesExistentes.Contains(reporte.Id))
                    .ExecuteDelete();
            }

            if (registrosReporte.Count == 0)
            {
                Console.WriteLine($"No se encontraron datos para cargar en BD. No se guardarán los cambios de {establecimiento.Nombre} {MesTxt} {VersionArchivo}");
                return;
            }

            var reporteNuevo = new Reporte
            {
                id_establecimiento = establecimiento.Id,
                id_comuna = comuna.Id,
                Mes = mesRem,
                Año = año,
            };

            Bdd.Reporte.Add(reporteNuevo);
            var cronometroCarga = Stopwatch.StartNew();
            Bdd.SaveChanges();

            GuardarRegistrosMasivo(reporteNuevo.Id, registrosReporte);
            transaction.Commit();
            transaction.Dispose();
            SincronizarSecuenciasDatos();
            cronometroCarga.Stop();
            Console.WriteLine($"[Registros] Registros guardados: {registrosReporte.Count:N0} en {cronometroCarga.Elapsed.TotalSeconds:F1}s.");
            Console.WriteLine($"[Registros] Carga completada: {establecimiento.Nombre} - {serie} - {MesTxt}.");
        }

        private void SincronizarSecuenciasDatos()
        {
            Bdd.Database.ExecuteSqlRaw(@"
SELECT setval('""REMTool"".reporte_id_seq',
    COALESCE((SELECT MAX(id) FROM ""REMTool"".reporte), 0) + 1,
    false);
SELECT setval('""REMTool"".registro_id_seq',
    COALESCE((SELECT MAX(id) FROM ""REMTool"".registro), 0) + 1,
    false);");
        }

        private void GuardarRegistrosMasivo(int idReporte, IReadOnlyList<Registro> registros)
        {
            if (Bdd.Database.GetDbConnection() is not NpgsqlConnection conexion)
                throw new InvalidOperationException("La conexión configurada no es PostgreSQL.");

            if (conexion.State != System.Data.ConnectionState.Open)
                conexion.Open();

            using var importador = conexion.BeginBinaryImport(
                "COPY \"REMTool\".registro (id_prestacion, id_reporte, valor) FROM STDIN (FORMAT BINARY)");

            foreach (var registro in registros)
            {
                importador.StartRow();
                importador.Write((long)registro.id_prestacion, NpgsqlDbType.Bigint);
                importador.Write((long)idReporte, NpgsqlDbType.Bigint);

                // The existing database stores this legacy column as numeric[].
                var valores = registro.Valor
                    .Select(valor => (decimal)valor)
                    .ToArray();
                importador.Write(valores, NpgsqlDbType.Array | NpgsqlDbType.Numeric);
            }

            importador.Complete();
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

                    // Extraer serie desde celda B17.
                    string serieRaw = hojaNombre.Cells["B17"].Value?.ToString() ?? "";
                    string serieHoja = ExtraerSerieDesdeNombre(serieRaw);

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

        private EstructuraCarga? ObtenerEstructuraVersion(string version, string serie)
        {
            var clave = $"{serie.Trim()}|{version.Trim()}";
            if (_estructuraCargaCache.TryGetValue(clave, out var estructuraCacheada))
                return estructuraCacheada;

            Console.WriteLine($"[Registros] Cargando estructura de la version '{version}' para la serie '{serie}'...");
            var versionId = Bdd.VersionRem
                .AsNoTracking()
                .Where(v => v.Nombre == version && v.SerieRem.Nombre == serie)
                .Select(v => (int?)v.Id)
                .FirstOrDefault();

            if (versionId == null)
                return null;

            var prestacionesBase = Bdd.Prestacion
                .AsNoTracking()
                .Where(prestacion => prestacion.id_version == versionId.Value)
                .OrderBy(prestacion => prestacion.HojaRem.Nombre)
                .ThenBy(prestacion => prestacion.Orden)
                .Select(prestacion => new
                {
                    prestacion.Id,
                    prestacion.Orden,
                    Hoja = prestacion.HojaRem.Nombre,
                    CoordenadasLegadas = prestacion.Coordenada
                })
                .ToList();

            var coordenadas = Bdd.CoordenadaPrestacion
                .AsNoTracking()
                .Where(coordenada => coordenada.Prestacion.id_version == versionId.Value)
                .OrderBy(coordenada => coordenada.IdPrestacion)
                .ThenBy(coordenada => coordenada.Orden)
                .Select(coordenada => new
                {
                    coordenada.IdPrestacion,
                    coordenada.CeldaBase
                })
                .ToList()
                .GroupBy(coordenada => coordenada.IdPrestacion)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => (IReadOnlyList<string>)grupo
                        .Select(coordenada => coordenada.CeldaBase)
                        .ToArray());

            var prestaciones = prestacionesBase
                .Select(prestacion => new PrestacionCarga(
                    prestacion.Id,
                    prestacion.Hoja,
                    coordenadas.TryGetValue(prestacion.Id, out var coordenadasNormalizadas)
                        && coordenadasNormalizadas.Count > 0
                        ? coordenadasNormalizadas
                        : prestacion.CoordenadasLegadas ?? new List<string>()))
                .ToList();

            var estructura = new EstructuraCarga(versionId.Value, prestaciones);
            _estructuraCargaCache[clave] = estructura;
            Console.WriteLine($"[Registros] Estructura liviana preparada en cache: {prestaciones.Count:N0} prestaciones para {version}.");

            return estructura;
        }

        private void InvalidarCacheEstructuras()
        {
            _estructuraCargaCache.Clear();
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

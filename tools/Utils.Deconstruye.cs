using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RemTool.Shared;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RemTools
{
    public partial class Utils
    {
        public void DeconstruyeREM()
        {
            string dirBase;
            string dirDiccionario;
            string dirParametros;

            using (var dialog = new OpenFileDialog
            {
                Title = "Seleccionar archivo versión base",
                Filter = "REM (*.xlsm)|*.xlsm",
                Multiselect = false
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                dirBase = dialog.FileName;
            }

            using var workbookBase = new ExcelPackage(dirBase);
            var hojaNombreBase = workbookBase.Workbook.Worksheets["NOMBRE"]
                ?? throw new InvalidOperationException("La plantilla base no contiene la hoja NOMBRE.");
            var versionArchivo = hojaNombreBase.Cells["A9"].Value?.ToString() ?? string.Empty;
            var serie = ExtraerSerieDesdeNombre(hojaNombreBase.Cells["B17"].Value?.ToString() ?? string.Empty);

            using (var dialog = new OpenFileDialog
            {
                Title = "Seleccionar diccionario de códigos para " + versionArchivo,
                Filter = "REM (*.xlsm)|*.xlsm",
                Multiselect = false
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                dirDiccionario = dialog.FileName;
            }

            using var workbookDiccionario = new ExcelPackage(dirDiccionario);
            var hojaNombreDiccionario = workbookDiccionario.Workbook.Worksheets["NOMBRE"]
                ?? throw new InvalidOperationException("El diccionario no contiene la hoja NOMBRE.");
            var versionArchivoCodigos = hojaNombreDiccionario.Cells["A9"].Value?.ToString() ?? string.Empty;
            if (!versionArchivoCodigos.Equals(versionArchivo, StringComparison.Ordinal))
            {
                MessageBox.Show("La versión del archivo de códigos no coincide con la versión del archivo base.", "Error de versiones");
                return;
            }

            using (var dialog = new OpenFileDialog
            {
                Title = "Seleccionar JSON parámetros para " + versionArchivo,
                Filter = "JSON (*.json)|*.json",
                Multiselect = false
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                dirParametros = dialog.FileName;
            }

            try
            {
                var textoJson = File.ReadAllText(dirParametros);
                var parametrosJson = JsonNode.Parse(textoJson)
                    ?? throw new InvalidOperationException("El archivo de parámetros está vacío.");

                var estructura = RemStructureImporter.Build(
                    workbookBase,
                    workbookDiccionario,
                    parametrosJson,
                    versionArchivo,
                    serie);

                ExportarMapaCompatibilidad(estructura);

                MessageBox.Show("Ingresa año y mes para la versión.");
                Console.Write("AÑO: ");
                var año = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException("Año no informado."));
                Console.Write("MES: ");
                var mes = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException("Mes no informado."));

                if (!CargarEstructuraEnBdd(estructura, año, mes))
                    return;
                MessageBox.Show("Estructura cargada exitosamente en la base de datos.", "Éxito");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar la estructura: {ex.Message}", "Error");
                Console.WriteLine($"Error detallado: {ex}");
            }
        }

        private void ExportarMapaCompatibilidad(RemStructureDefinition estructura)
        {
            var consolidados = estructura.Sections
                .SelectMany(section => section.Filas
                    .Where(row => !string.IsNullOrWhiteSpace(row.CodigoPrestacion))
                    .Select(row => new Consoli
                    {
                        Hoja = section.Hoja,
                        Prestacion = row.CodigoPrestacion!,
                        Cols = row.Coordenadas.Select(coordenada => coordenada.CeldaBase).ToList()
                    }))
                .OrderBy(item => item.Prestacion)
                .ToList();

            using var saveDialog = new SaveFileDialog
            {
                Title = "Guardar mapa de coordenadas compatible",
                Filter = "JSON (*.json)|*.json",
                DefaultExt = "json",
                FileName = $"Consolidado_Test_{DateTimeOffset.Now.ToUnixTimeSeconds()}.json",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
                File.WriteAllText(saveDialog.FileName, JsonSerializer.Serialize(consolidados));
        }

        private bool CargarEstructuraEnBdd(RemStructureDefinition estructura, int año, int mes)
        {
            try
            {
                return CargarEstructuraEnBddInterna(estructura, año, mes);
            }
            finally
            {
                // La estructura cargada no debe permanecer rastreada durante
                // la posterior importación de reportes.
                Bdd.ChangeTracker.Clear();
            }
        }

        private bool CargarEstructuraEnBddInterna(RemStructureDefinition estructura, int año, int mes)
        {
            if (string.IsNullOrWhiteSpace(estructura.Serie))
                throw new InvalidOperationException("No se pudo identificar la serie de la plantilla base.");

            Console.WriteLine($"[Estructura] Iniciando carga | Version: {estructura.Version} | Serie: {estructura.Serie} | Secciones: {estructura.Sections.Count}");

            // La sustitución de una versión puede involucrar muchos registros y relaciones.
            Bdd.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));

            var serieNombre = estructura.Serie.Trim().ToUpperInvariant();
            var serie = Bdd.SerieRem.FirstOrDefault(item => item.Nombre.ToUpper() == serieNombre)
                ?? throw new InvalidOperationException($"La serie '{estructura.Serie}' no existe en la base de datos.");

            var version = Bdd.VersionRem
                .FirstOrDefault(item => item.Nombre == estructura.Version
                    && item.id_serie == serie.Id);

            using var transaction = Bdd.Database.BeginTransaction();

            Console.WriteLine($"[Estructura] Version encontrada: {(version is null ? "no, se creara" : "si, se reemplazara si corresponde")}");

            if (version is null)
            {
                Console.WriteLine("[Estructura] Creando version en la base de datos...");
                version = new VersionRem
                {
                    Nombre = estructura.Version,
                    id_serie = serie.Id,
                    Fecha = new DateOnly(año, mes, 1)
                };
                Bdd.VersionRem.Add(version);
                Bdd.SaveChanges();
            }
            else
            {
                Console.WriteLine($"[Estructura] Consultando registros existentes de la version '{estructura.Version}'...");
                var registrosVersion = Bdd.Registro.Where(registro =>
                    Bdd.Prestacion.Any(prestacion =>
                        prestacion.Id == registro.id_prestacion
                        && prestacion.id_version == version.Id));
                var cantidadRegistros = registrosVersion.Count();

                if (cantidadRegistros > 0)
                {
                    var cantidadReportesAfectados = registrosVersion
                        .Select(registro => registro.id_reporte)
                        .Distinct()
                        .Count();

                    var confirmacion = MessageBox.Show(
                        $"La versión '{estructura.Version}' ya tiene {cantidadRegistros} registro(s) "
                        + $"en {cantidadReportesAfectados} reporte(s).\n\n"
                        + "Si continúas, se eliminarán esos datos y se reemplazará toda la estructura "
                        + "para que puedas cargar nuevamente los reportes.\n\n"
                        + "Esta acción no se puede deshacer. ¿Deseas continuar?",
                        "Confirmar reemplazo de estructura",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (confirmacion != DialogResult.Yes)
                    {
                        MessageBox.Show("Carga cancelada. No se realizaron cambios.", "Carga cancelada");
                        return false;
                    }

                    var idsReportesAfectados = registrosVersion
                        .Select(registro => registro.id_reporte)
                        .Distinct()
                        .ToList();

                    Console.WriteLine($"[Estructura] Eliminando {cantidadRegistros} registro(s) y los reportes que queden sin datos...");
                    registrosVersion.ExecuteDelete();

                    // Sólo elimina reportes que quedaron sin registros de ninguna versión.
                    Bdd.Reporte
                        .Where(reporte => idsReportesAfectados.Contains(reporte.Id)
                            && !Bdd.Registro.Any(registro => registro.id_reporte == reporte.Id))
                        .ExecuteDelete();
                }

                Console.WriteLine($"[Estructura] Eliminando estructura anterior de la version '{estructura.Version}'...");
                Bdd.Prestacion.Where(item => item.id_version == version.Id).ExecuteDelete();
                Bdd.VersionHojaRem.Where(item => item.IdVersion == version.Id).ExecuteDelete();
            }

            var hojas = estructura.Sections
                .GroupBy(section => section.Hoja, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Min(section => section.Orden))
                .ToList();

            Console.WriteLine($"[Estructura] Hojas/planillas a procesar: {hojas.Count}");
            Console.WriteLine("[Estructura] Cargando catalogo de planillas de la serie...");

            var hojasPorNombre = Bdd.HojaRem
                .Where(item => item.id_serie_rem == serie.Id)
                .ToList()
                .ToDictionary(item => item.Nombre.Trim().ToUpperInvariant());

            var hojasNuevas = new List<HojaRem>();
            var hojasActualizadas = new List<HojaRem>();
            var extraerTitulosDesdePlanilla = serie.Nombre.Equals("A", StringComparison.OrdinalIgnoreCase);
            foreach (var hojaGroup in hojas)
            {
                var hojaNombre = hojaGroup.Key.Trim().ToUpperInvariant();
                var tituloHoja = estructura.TitulosHojas.TryGetValue(hojaGroup.Key, out var titulo)
                    ? titulo.Trim()
                    : string.Empty;

                if (hojasPorNombre.TryGetValue(hojaNombre, out var hojaExistente))
                {
                    // En Serie A el título oficial vive en A6 de la plantilla
                    // base. Las demás series quedan disponibles para edición
                    // manual desde la base de datos.
                    if (extraerTitulosDesdePlanilla
                        && !string.IsNullOrWhiteSpace(tituloHoja)
                        && !string.Equals(hojaExistente.Titulo, tituloHoja, StringComparison.Ordinal))
                    {
                        hojaExistente.Titulo = tituloHoja;
                        hojasActualizadas.Add(hojaExistente);
                    }

                    continue;
                }

                var hoja = new HojaRem
                {
                    Nombre = hojaGroup.Key,
                    Titulo = extraerTitulosDesdePlanilla ? tituloHoja : string.Empty,
                    id_serie_rem = serie.Id
                };
                hojasPorNombre[hojaNombre] = hoja;
                hojasNuevas.Add(hoja);
            }

            if (hojasNuevas.Count > 0 || hojasActualizadas.Count > 0)
            {
                Console.WriteLine($"[Estructura] Guardando catalogo de planillas | Nuevas: {hojasNuevas.Count} | Titulos actualizados: {hojasActualizadas.Count}...");
                if (hojasNuevas.Count > 0)
                    Bdd.HojaRem.AddRange(hojasNuevas);
                Bdd.SaveChanges();
            }

            Console.WriteLine("[Estructura] Construyendo relaciones Version > Planilla > Seccion > Prestacion...");
            var versionHojas = new List<VersionHojaRem>();
            var autoDetectChangesAnterior = Bdd.ChangeTracker.AutoDetectChangesEnabled;
            Bdd.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                var hojaNumero = 0;
                foreach (var hojaGroup in hojas)
                {
                    hojaNumero++;
                    var hojaNombre = hojaGroup.Key.Trim().ToUpperInvariant();
                    var hoja = hojasPorNombre[hojaNombre];
                    var seccionesHoja = hojaGroup.OrderBy(section => section.Orden).ToList();

                    Console.WriteLine($"[Estructura] Planilla {hojaNumero}/{hojas.Count}: {hojaGroup.Key} | Secciones: {seccionesHoja.Count}");

                    var versionHoja = new VersionHojaRem
                    {
                        IdVersion = version.Id,
                        IdHoja = hoja.Id,
                        Orden = hojaGroup.Min(section => section.Orden),
                        VersionRem = version,
                        HojaRem = hoja
                    };

                foreach (var sectionDefinition in seccionesHoja)
                {
                    var section = new SeccionRem
                    {
                        IdHojaRem = hoja.Id,
                        Codigo = sectionDefinition.Codigo,
                        Nombre = sectionDefinition.Titulo,
                        Orden = sectionDefinition.Orden,
                        RangoDiccionario = sectionDefinition.RangoDiccionario,
                        RangoBase = sectionDefinition.RangoBase,
                        FilaInicio = sectionDefinition.FilaInicio,
                        ColumnaInicio = sectionDefinition.ColumnaInicio,
                        FilaFin = sectionDefinition.FilaFin,
                        ColumnaFin = sectionDefinition.ColumnaFin,
                        OffsetPrestacion = sectionDefinition.OffsetPrestacion,
                        OffsetColumna = sectionDefinition.OffsetColumna,
                        HtmlEstructura = RemStructureHtmlRenderer.Render(sectionDefinition),
                        VersionHoja = versionHoja
                    };

                    foreach (var rowDefinition in sectionDefinition.Filas)
                    {
                        var row = new FilaSeccionRem
                        {
                            Orden = rowDefinition.Orden,
                            FilaOrigen = rowDefinition.FilaOrigen,
                            TienePrestacion = !string.IsNullOrWhiteSpace(rowDefinition.CodigoPrestacion),
                            TipoFila = rowDefinition.TipoFila,
                            Seccion = section
                        };

                        var cells = new Dictionary<RemCellDefinition, CeldaSeccionRem>();
                        foreach (var cellDefinition in rowDefinition.Celdas)
                        {
                            var cell = new CeldaSeccionRem
                            {
                                FilaOrigen = cellDefinition.FilaOrigen,
                                ColumnaOrigen = cellDefinition.ColumnaOrigen,
                                Valor = cellDefinition.Valor,
                                CeldaDiccionario = cellDefinition.CeldaDiccionario,
                                CeldaBase = cellDefinition.CeldaBase,
                                RowSpan = cellDefinition.RowSpan,
                                ColSpan = cellDefinition.ColSpan,
                                EsCeldaAncla = cellDefinition.EsCeldaAncla,
                                EsEditable = cellDefinition.EsEditable,
                                EsEntradaPrestacion = cellDefinition.EsEntradaPrestacion,
                                EsTotal = cellDefinition.EsTotal,
                                TipoTotal = cellDefinition.TipoTotal,
                                FormulaOrigen = cellDefinition.FormulaOrigen,
                                DependenciasTotal = cellDefinition.DependenciasTotal,
                                OperacionTotal = cellDefinition.OperacionTotal,
                                EstiloOrigen = cellDefinition.EstiloOrigen,
                                ColorFondo = cellDefinition.ColorFondo,
                                Fila = row
                            };
                            row.Celdas.Add(cell);
                            cells[cellDefinition] = cell;
                        }

                        if (!string.IsNullOrWhiteSpace(rowDefinition.CodigoPrestacion))
                        {
                            var prestacion = new Prestacion
                            {
                                id_version = version.Id,
                                id_hoja = hoja.Id,
                                CodigoPrestacion = rowDefinition.CodigoPrestacion,
                                Nombre = rowDefinition.NombrePrestacion,
                                Orden = rowDefinition.Orden,
                                IsEnabled = true,
                                Seccion = section,
                                FilaSeccion = row
                            };

                            row.Prestacion = prestacion;
                            prestacion.Coordenada = rowDefinition.Coordenadas
                                .Select(coordenada => coordenada.CeldaBase)
                                .ToList();

                            foreach (var coordinateDefinition in rowDefinition.Coordenadas)
                            {
                                if (!cells.TryGetValue(coordinateDefinition.Celda, out var cell))
                                    continue;

                                prestacion.Coordenadas.Add(new CoordenadaPrestacion
                                {
                                    Orden = coordinateDefinition.Orden,
                                    CodigoColumna = coordinateDefinition.CodigoColumna,
                                    CeldaDiccionario = coordinateDefinition.CeldaDiccionario,
                                    CeldaBase = coordinateDefinition.CeldaBase,
                                    Prestacion = prestacion,
                                    Celda = cell
                                });
                            }

                            section.Prestacions.Add(prestacion);
                        }

                        section.Filas.Add(row);
                    }

                    versionHoja.Secciones.Add(section);
                }

                versionHojas.Add(versionHoja);
            }

            }
            finally
            {
                Bdd.ChangeTracker.AutoDetectChangesEnabled = autoDetectChangesAnterior;
            }

            Console.WriteLine($"[Estructura] Persistiendo estructura completa: {versionHojas.Count} planilla(s) versionada(s)...");
            Bdd.VersionHojaRem.AddRange(versionHojas);
            Bdd.SaveChanges();
            Console.WriteLine("[Estructura] Guardado completado. Confirmando transaccion...");
            transaction.Commit();
            transaction.Dispose();
            SincronizarSecuenciasEstructura();
            InvalidarCacheEstructuras();
            Console.WriteLine($"[Estructura] Carga completada | Version: {estructura.Version} | Serie: {estructura.Serie}");
            return true;
        }

        private void SincronizarSecuenciasEstructura()
        {
            Console.WriteLine("[Estructura] Sincronizando correlativos de la estructura...");
            Bdd.Database.ExecuteSqlRaw(@"
SELECT setval('""REMTool"".version_hoja_rem_id_seq',
    COALESCE((SELECT MAX(id) FROM ""REMTool"".version_hoja_rem), 0) + 1,
    false);
SELECT setval('""REMTool"".seccion_rem_id_seq',
    COALESCE((SELECT MAX(id) FROM ""REMTool"".seccion_rem), 0) + 1,
    false);
SELECT setval('""REMTool"".fila_seccion_rem_id_seq',
    COALESCE((SELECT MAX(id) FROM ""REMTool"".fila_seccion_rem), 0) + 1,
    false);
SELECT setval('""REMTool"".celda_seccion_rem_id_seq',
    COALESCE((SELECT MAX(id) FROM ""REMTool"".celda_seccion_rem), 0) + 1,
    false);
SELECT setval('""REMTool"".prestacion_id_seq',
    COALESCE((SELECT MAX(id) FROM ""REMTool"".prestacion), 0) + 1,
    false);
SELECT setval('""REMTool"".coordenada_prestacion_id_seq',
    COALESCE((SELECT MAX(id) FROM ""REMTool"".coordenada_prestacion), 0) + 1,
    false);");
        }

        private sealed class Consoli
        {
            public string Hoja { get; set; } = string.Empty;
            public string Prestacion { get; set; } = string.Empty;
            public List<string> Cols { get; set; } = new();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using RemTool.Shared;

namespace RemTools
{
    public partial class Utils
    {
        public void DeconstruyeREM()
        {
            string dir_base = "", dir_diccionario = "", dir_parametros = "", VersionArchivo = "";

            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar archivo versión base",
                Filter = "REM (*.xlsm)|*.xlsm",
                Multiselect = false
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            dir_base = dialog.FileName;

            using var workbook_base = new ExcelPackage(dir_base);
            VersionArchivo = workbook_base.Workbook.Worksheets["NOMBRE"].Cells["A9"].Value.ToString() ?? "";
            var serieRaw = workbook_base.Workbook.Worksheets["NOMBRE"].Cells["B17"].Value?.ToString() ?? "";
            string Serie = ExtraerSerieDesdeNombre(serieRaw);

            dialog = new OpenFileDialog
            {
                Title = "Seleccionar diccionario de códigos para " + VersionArchivo,
                Filter = "REM (*.xlsm)|*.xlsm",
                Multiselect = false
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            dir_diccionario = dialog.FileName;

            using var workbook_diccionario = new ExcelPackage(dir_diccionario);
            string VersionArchivoCodigos = workbook_diccionario.Workbook.Worksheets["NOMBRE"].Cells["A9"].Value.ToString() ?? "";
            if (VersionArchivoCodigos != VersionArchivo)
            {
                MessageBox.Show("La versión del archivo de códigos no coincide con la versión del archivo base.", "Error de versiones");
                return;
            }

            dialog = new OpenFileDialog
            {
                Title = "Seleccionar JSON parámetros para " + VersionArchivo,
                Filter = "JSON (*.json)|*.json",
                Multiselect = false
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            dir_parametros = dialog.FileName;

            var textoJSON = File.ReadAllText(dir_parametros);
            JsonNode? parametrosJSON = JsonNode.Parse(textoJSON);
            JsonArray? Tablas;
            try
            {
                Tablas = parametrosJSON[VersionArchivo]["Tablas"] as JsonArray;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Error al leer el archivo de parámetros: La versión no corresponde.", "Error de lectura");
                return;
            }

            var consolidados = new List<consoli>();
            foreach (var tabla in Tablas)
            {
                var Hoja = workbook_diccionario.Workbook.Worksheets[tabla["hoja"].ToString()];
                var Rango = workbook_diccionario.Workbook.Worksheets[tabla["hoja"].ToString()].Cells[tabla["rango"].ToString()];
                var HojaBase = workbook_base.Workbook.Worksheets[tabla["hoja"].ToString()];
                int fila_inicio = Rango.Start.Row;
                int col_inicio = Rango.Start.Column;
                int offsetFila = tabla["offsetPrest"].GetValue<int>();
                int offsetCol = tabla["offsetCol"].GetValue<int>();

                for (int fila = fila_inicio; fila <= Rango.End.Row; fila++)
                {
                    if (fila == 0) continue;

                    string codigo_prestacion = Hoja.Cells[fila, col_inicio].Value?.ToString() ?? "";
                    var celdas = new List<string>();

                    for (int col = col_inicio; col <= Rango.End.Column; col++)
                    {
                        if (col == 0) continue;
                        if (fila >= fila_inicio + offsetFila && col >= col_inicio + offsetCol)
                            celdas.Add(HojaBase.Cells[fila, col - 1].Address.ToString());
                    }

                    if (!string.IsNullOrEmpty(codigo_prestacion))
                    {
                        Console.WriteLine(codigo_prestacion);
                        consolidados.Add(new consoli { hoja = tabla["hoja"].ToString(), prestacion = codigo_prestacion, cols = celdas });
                    }
                }
            }

            using var SaveDialog = new SaveFileDialog
            {
                Title = "Guardar archivo",
                Filter = "JSON (*.json)|*.json",
                DefaultExt = "json",
                FileName = $"Consolidado_Test_{DateTimeOffset.Now.ToUnixTimeSeconds()}.json",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (SaveDialog.ShowDialog() != DialogResult.OK) return;
            File.WriteAllText(SaveDialog.FileName, JsonSerializer.Serialize(consolidados));

            Console.WriteLine($"Procesando versión: {VersionArchivo}");

            var versionExistente = Bdd.VersionRem.FirstOrDefault(v => v.Nombre.Equals(VersionArchivo));
            var SerieBdd = Bdd.SerieRem.FirstOrDefault(s => s.Nombre.Equals(Serie));

            MessageBox.Show("Ingresa año y mes para la versión");
            Console.Write("AÑO: ");
            var año = Console.ReadLine();
            Console.Write("MES: ");
            var mes = Console.ReadLine();

            if (versionExistente == null)
            {
                versionExistente = new VersionRem
                {
                    Nombre = VersionArchivo,
                    id_serie = SerieBdd.Id,
                    Fecha = new DateOnly(int.Parse(año), int.Parse(mes), 1)
                };
                Bdd.VersionRem.Add(versionExistente);
                Bdd.SaveChanges();
                Console.WriteLine($"Versión '{VersionArchivo}' agregada a BD");
            }
            else
            {
                Bdd.Prestacion.Where(p => p.id_version == versionExistente.Id).ExecuteDelete();
                Bdd.SaveChanges();
            }

            try
            {
                consolidados = consolidados.OrderBy(c => c.prestacion).ToList();
                foreach (var item in consolidados)
                {
                    var hojaExistente = Bdd.HojaRem.Where(h => h.Nombre.Equals(item.hoja)).FirstOrDefault();
                    if (hojaExistente == null)
                    {
                        hojaExistente = new HojaRem { Nombre = item.hoja, id_serie_rem = SerieBdd.Id };
                        Bdd.Add(hojaExistente);
                        Bdd.SaveChanges();
                    }

                    if (Bdd.Prestacion.FirstOrDefault(p => p.CodigoPrestacion.Equals(item.prestacion) && p.id_version == versionExistente.Id) == null
                        && !string.IsNullOrEmpty(item.prestacion))
                    {
                        if (item.prestacion.Length > 9 || item.prestacion.Length < 8)
                            Console.WriteLine($"{item.prestacion}: -> RARA");

                        Bdd.Prestacion.Add(new Prestacion
                        {
                            CodigoPrestacion = item.prestacion,
                            id_version = versionExistente.Id,
                            id_hoja = hojaExistente.Id,
                            IsEnabled = true,
                            Coordenada = item.cols.Select(c => c.ToString()).ToList()
                        });
                        Bdd.SaveChanges();
                    }
                }
                Console.WriteLine($"Versión '{VersionArchivo}' procesada completamente");
                MessageBox.Show("Estructura cargada exitosamente en la base de datos.", "Éxito");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar: {ex.Message}", "Error");
                Console.WriteLine($"Error detallado: {ex}");
            }
        }

        private class consoli
        {
            public string hoja { get; set; }
            public string prestacion { get; set; }
            public List<string> cols { get; set; }
        }
    }
}

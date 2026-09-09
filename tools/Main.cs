using System.Text.RegularExpressions;
using RemTool.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RemTools
{
    public partial class Main : Form
    {
        private readonly RemToolDataContext BDD;
        private readonly Utils _utils;

        public Main(RemToolDataContext dbContext)
        {
            InitializeComponent();
            BDD = dbContext;
            _utils = new Utils(dbContext);
            var generarConsolidados = new Button
            {
                Text = "Generar consolidados Excel", Location = new Point(18, 503),
                Size = new Size(504, 35), Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            ClientSize = new Size(540, 551);
            Controls.Add(generarConsolidados);
            generarConsolidados.Click += (_, _) =>
            {
                var options = BDD.GetService<DbContextOptions<RemToolDataContext>>();
                using var form = new GenerarConsolidadosForm(() => new RemToolDataContext(options));
                form.ShowDialog(this);
            };
            //_utils.CalcularIndicadores(2025);
            //_utils.CalcularIndicadores(2026);            
            //_utils.CargarReglas();
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void revisar_A_btn_Click(object sender, EventArgs e)
        {
            MenuDeshabilitado();
            _utils.RevisarRem("A");
            MenuHabilitado();
        }

        private void revisar_BM_btn_Click(object sender, EventArgs e)
        {
            MenuDeshabilitado();
            _utils.RevisarRem("BM");
            MenuHabilitado();
        }

        private void revisar_D_btn_Click(object sender, EventArgs e)
        {
            MenuDeshabilitado();
            _utils.RevisarRem("D");
            MenuHabilitado();
        }

        private void revisar_P_btn_Click(object sender, EventArgs e)
        {
            MenuDeshabilitado();
            _utils.RevisarRem("P");
            MenuHabilitado();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            MenuDeshabilitado();
            _utils.DeconstruyeREM();
            MenuHabilitado();
        }

        void MenuHabilitado()
        {
            this.revisar_A_btn.Enabled = true;
            this.revisar_BM_btn.Enabled = true;
            this.revisar_D_btn.Enabled = true;
            this.revisar_P_btn.Enabled = true;
            this.desconstruir_btn.Enabled = true;
            this.gensql_btn.Enabled = true;
            this.button1.Enabled = true;
            this.button2.Enabled = true;
            this.integridadRem_btn.Enabled = true;
        }
        void MenuDeshabilitado()
        {
            this.revisar_A_btn.Enabled = false;
            this.revisar_BM_btn.Enabled = false;
            this.revisar_D_btn.Enabled = false;
            this.revisar_P_btn.Enabled = false;
            this.desconstruir_btn.Enabled = false;
            this.gensql_btn.Enabled = false;
            this.button1.Enabled = false;
            this.button2.Enabled = false;
            this.integridadRem_btn.Enabled = false;
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            _utils.DeconstruyeREM();
        }

        private void button1_Click_3(object sender, EventArgs e)
        {
            MenuDeshabilitado();
            Console.Write("Serie a cargar (A, BM, D, P): ");
            var serie = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(serie))
            {
                MessageBox.Show("No se informo una serie.", "Informacion");
                MenuHabilitado();
                return;
            }

            if (!LeerMes("Mes inicial (1-12): ", out var mesInicio)
                || !LeerMes("Mes final (1-12): ", out var mesFin))
            {
                MessageBox.Show("La carga fue cancelada porque el rango de meses no es valido.", "Informacion");
                MenuHabilitado();
                return;
            }

            if (mesInicio > mesFin)
            {
                MessageBox.Show("El mes inicial no puede ser mayor que el mes final.", "Rango invalido");
                MenuHabilitado();
                return;
            }

            ExtraerSerie(serie, 2026, mesInicio, mesFin);
            MenuHabilitado();
        }

        public void ExtraerSerie(string serie, int año)
        {
            //ProcesarArchivosSeleccionados();
            //return;
            // Ruta del directorio a recorrer
            string directorioSerie = $@"C:\Users\Usuario\Desktop\Ricardo\dev\testopenpyxml\Libros\{año}\Serie {serie.ToUpper()}\";

            // Expresi�n regular para filtrar archivos (ejemplo: termina en A01.xlsm)
            string patron = @"\\[0-9]{6}[A-Z]{1,2}[0-9]{2}(?i:.xlsm)";
            Regex regex = new Regex(patron, RegexOptions.IgnoreCase);

            // Obtiene todos los archivos en el directorio y subdirectorios
            string[] archivos = Directory.GetFiles(directorioSerie, "*.*", SearchOption.AllDirectories);
            archivos = Array.FindAll(archivos, ruta => regex.IsMatch(ruta));

            foreach (var archivo in archivos)
            {
                if (regex.IsMatch(archivo))
                {
                    _utils.ExtraerArchivo(archivo, serie.ToUpper(), a\u00F1o);
                }
            }
        }
        public void ExtraerSerie(string serie, int año, int mesInicio, int mesFin)
        {
            if (string.IsNullOrWhiteSpace(serie))
                throw new ArgumentException("La serie es obligatoria.", nameof(serie));
            if (mesInicio is < 1 or > 12 || mesFin is < 1 or > 12 || mesInicio > mesFin)
                throw new ArgumentOutOfRangeException(nameof(mesInicio), "El rango de meses debe estar entre 1 y 12.");

            var serieNormalizada = serie.Trim().ToUpperInvariant();
            string directorioSerie = $@"C:\Users\Usuario\Desktop\Ricardo\dev\testopenpyxml\Libros\{año}\Serie {serieNormalizada}\";
            if (!Directory.Exists(directorioSerie))
            {
                MessageBox.Show($"No existe la carpeta de la serie: {directorioSerie}", "Carpeta no encontrada");
                return;
            }

            // El mes se obtiene del sufijo del nombre, por ejemplo:
            // 105307BM01.xlsm -> mes 01. Así se evita abrir planillas fuera
            // del rango solicitado.
            var regexArchivo = new Regex(
                $@"^[0-9]{{6}}{Regex.Escape(serieNormalizada)}(?<mes>[0-9]{{2}})$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            var archivos = Directory.EnumerateFiles(directorioSerie, "*.xlsm", SearchOption.AllDirectories)
                .Select(ruta => new
                {
                    Ruta = ruta,
                    Match = regexArchivo.Match(Path.GetFileNameWithoutExtension(ruta))
                })
                .Where(item => item.Match.Success
                    && int.TryParse(item.Match.Groups["mes"].Value, out var mes)
                    && mes >= mesInicio
                    && mes <= mesFin)
                .Select(item => new
                {
                    item.Ruta,
                    Mes = int.Parse(item.Match.Groups["mes"].Value)
                })
                .OrderBy(item => item.Mes)
                .ThenBy(item => item.Ruta, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (archivos.Count == 0)
            {
                MessageBox.Show(
                    $"No se encontraron archivos de la serie {serieNormalizada} entre los meses {mesInicio:00} y {mesFin:00}.",
                    "Sin archivos");
                return;
            }

            var mesesEncontrados = archivos
                .Select(item => item.Mes)
                .Distinct()
                .OrderBy(mes => mes)
                .Select(mes => mes.ToString("00"));
            Console.WriteLine(
                $"[Registros] Rango solicitado: {serieNormalizada} {año}, meses {mesInicio:00}-{mesFin:00}. "
                + $"Archivos encontrados: {archivos.Count:N0}. Meses: {string.Join(", ", mesesEncontrados)}");

            var archivoNumero = 0;
            foreach (var archivo in archivos)
            {
                archivoNumero++;
                Console.WriteLine(
                    $"[Registros] Archivo {archivoNumero}/{archivos.Count}: "
                    + $"{Path.GetFileName(archivo.Ruta)} | mes {archivo.Mes:00}");
                _utils.ExtraerArchivo(archivo.Ruta, serieNormalizada, año);
            }
        }

        private static bool LeerMes(string mensaje, out int mes)
        {
            Console.Write(mensaje);
            var texto = Console.ReadLine();
            return int.TryParse(texto, out mes) && mes is >= 1 and <= 12;
        }

        private void ProcesarArchivosSeleccionados()
        {
            // Usar el file picker para seleccionar archivos
            var archivosSeleccionados = SeleccionarArchivosXLSM();

            if (archivosSeleccionados == null || archivosSeleccionados.Length == 0)
            {
                MessageBox.Show("No se seleccionaron archivos.", "Informaci�n");
                return;
            }

            MenuDeshabilitado();

            foreach (var archivo in archivosSeleccionados)
            {
                try
                {
                    // Detectar el tipo de serie por el nombre del archivo
                    string nombreArchivo = Path.GetFileNameWithoutExtension(archivo);
                    string tipoSerie = nombreArchivo.Length >= 7 ? nombreArchivo.Substring(6, 1) : "A";

                    _utils.ExtraerArchivo(archivo, tipoSerie, 2026);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error procesando {Path.GetFileName(archivo)}: {ex.Message}");
                }
            }

            MenuHabilitado();
            MessageBox.Show($"Procesados {archivosSeleccionados.Length} archivos.", "Completado");
        }

        private string[] SeleccionarArchivosXLSM()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Seleccionar archivos REM (XLSM)",
                Filter = "Archivos REM (*.xlsm)|*.xlsm|Todos los archivos (*.*)|*.*",
                Multiselect = true,
                InitialDirectory = @"C:\Users\Usuario\Desktop\Ricardo\dev\testopenpyxml\Libros\2025\"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileNames;
            }

            return Array.Empty<string>();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MenuDeshabilitado();
            _utils.CalcularIndicadores(2026);
            MenuHabilitado();
        }

        private void integridadRem_btn_Click(object sender, EventArgs e)
        {
            MenuDeshabilitado();
            _utils.RevisarIntegridadArchivos();
            MenuHabilitado();
        }

        private void reglas_btn_Click(object sender, EventArgs e)
        {
            using var editor = new ReglaEditorForm(BDD);
            editor.ShowDialog(this);
        }

        private void indicadores_btn_Click(object sender, EventArgs e)
        {
            using var editor = new IndicadorEditorForm(BDD);
            editor.ShowDialog(this);
        }

        private void puntoResumen_btn_Click(object sender, EventArgs e)
        {
            using var editor = new PuntoResumenEditorForm(BDD);
            editor.ShowDialog(this);
        }

        private void informativos_btn_Click(object sender, EventArgs e)
        {
            using var editor = new InformativoEditorForm(BDD);
            editor.ShowDialog(this);
        }

        private void filtrosEstablecimiento_btn_Click(object sender, EventArgs e)
        {
            using var editor = new FiltroEstablecimientoEditorForm(BDD);
            editor.ShowDialog(this);
        }

        private void visor_btn_Click(object sender, EventArgs e)
        {
            using var visor = new VisorReportesForm(BDD);
            visor.ShowDialog(this);
        }
    }
}

using System.Text.RegularExpressions;
using RemTool.Shared;

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
            //_utils.CalcularIndicadores(2025);
            //_utils.CalcularIndicadores(2026);            
            //_utils.CargarReglas();
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            MenuDeshabilitado();
            _utils.RevisarRem();
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
            this.revisar_btn.Enabled = true;
            this.desconstruir_btn.Enabled = true;
            this.gensql_btn.Enabled = true;
            this.button1.Enabled = true;
            this.button2.Enabled = true;
            this.integridadRem_btn.Enabled = true;
        }
        void MenuDeshabilitado()
        {
            this.revisar_btn.Enabled = false;
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
            var serie = Console.ReadLine();
            ExtraerSerie(serie,2026);
            MenuHabilitado();
        }

        public void ExtraerSerie(string serie, int año)
        {
            //ProcesarArchivosSeleccionados();
            //return;
            // Ruta del directorio a recorrer
            string directorioSerie = $@"C:\Users\Usuario\Desktop\Ricardo\dev\testopenpyxml\Libros\{año}\Serie {serie}\";

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

        private void visor_btn_Click(object sender, EventArgs e)
        {
            using var visor = new VisorReportesForm(BDD);
            visor.ShowDialog(this);
        }
    }
}

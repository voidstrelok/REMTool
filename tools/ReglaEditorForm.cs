using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTools
{
    public partial class ReglaEditorForm : Form
    {
        private readonly RemToolDataContext _bdd;
        private List<Regla> _reglas = [];
        private List<TipoRegla> _tiposRegla = [];
        private List<SerieRem> _series = [];
        private List<VersionRem> _todasVersiones = [];
        private VersionRem? _versionSeleccionada;

        public ReglaEditorForm(RemToolDataContext dbContext)
        {
            InitializeComponent();
            _bdd = dbContext;
        }

        private void ReglaEditorForm_Load(object sender, EventArgs e)
        {
            _tiposRegla = _bdd.TipoRegla.OrderBy(t => t.Id).ToList();

            var colTipo = new DataGridViewComboBoxColumn
            {
                Name = "col_TipoRegla",
                HeaderText = "Tipo",
                DataPropertyName = "IdTipoRegla",
                DataSource = _tiposRegla,
                DisplayMember = "Nombre",
                ValueMember = "Id",
                Width = 110,
                DisplayIndex = 1
            };
            reglas_dgv.Columns.Add(colTipo);

            CargarVersiones();
        }

        private void CargarVersiones()
        {
            _series = _bdd.SerieRem.OrderBy(s => s.Nombre).ToList();
            _todasVersiones = _bdd.VersionRem
                .Include(v => v.SerieRem)
                .OrderBy(v => v.SerieRem.Nombre)
                .ThenBy(v => v.Nombre)
                .ToList();

            serie_cmb.SelectedIndexChanged -= serie_cmb_SelectedIndexChanged;
            serie_cmb.Items.Clear();
            serie_cmb.Items.Add("(Todas)");
            foreach (var serie in _series)
                serie_cmb.Items.Add(serie.Nombre);
            serie_cmb.SelectedIndex = 0;
            serie_cmb.SelectedIndexChanged += serie_cmb_SelectedIndexChanged;

            ActualizarAnosCmb(_todasVersiones);
            FiltrarVersiones();
        }

        private void version_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            _versionSeleccionada = version_cmb.SelectedItem as VersionRem;
            CargarReglas(_versionSeleccionada);
        }

        private void serie_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            var versiones = _todasVersiones.AsEnumerable();
            if (serie_cmb.SelectedIndex > 0 && serie_cmb.SelectedIndex - 1 < _series.Count)
            {
                var serie = _series[serie_cmb.SelectedIndex - 1];
                versiones = versiones.Where(v => v.id_serie == serie.Id);
            }
            ActualizarAnosCmb(versiones);
            FiltrarVersiones();
        }

        private void anio_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            FiltrarVersiones();
        }

        private void ActualizarAnosCmb(IEnumerable<VersionRem> versiones)
        {
            anio_cmb.SelectedIndexChanged -= anio_cmb_SelectedIndexChanged;
            anio_cmb.Items.Clear();
            anio_cmb.Items.Add("(Todos)");
            foreach (var anio in versiones.Select(v => v.Fecha.Year).Distinct().OrderDescending())
                anio_cmb.Items.Add(anio.ToString());
            anio_cmb.SelectedIndex = 0;
            anio_cmb.SelectedIndexChanged += anio_cmb_SelectedIndexChanged;
        }

        private void FiltrarVersiones()
        {
            var filtradas = _todasVersiones.AsEnumerable();

            if (serie_cmb.SelectedIndex > 0 && serie_cmb.SelectedIndex - 1 < _series.Count)
            {
                var serie = _series[serie_cmb.SelectedIndex - 1];
                filtradas = filtradas.Where(v => v.id_serie == serie.Id);
            }

            if (anio_cmb.SelectedIndex > 0 && int.TryParse(anio_cmb.SelectedItem?.ToString(), out int anio))
                filtradas = filtradas.Where(v => v.Fecha.Year == anio);

            var lista = filtradas.ToList();

            version_cmb.SelectedIndexChanged -= version_cmb_SelectedIndexChanged;
            version_cmb.DataSource = lista;
            version_cmb.DisplayMember = "Nombre";
            version_cmb.ValueMember = "Id";
            if (lista.Count > 0)
                version_cmb.SelectedIndex = 0;
            version_cmb.SelectedIndexChanged += version_cmb_SelectedIndexChanged;

            _versionSeleccionada = version_cmb.SelectedItem as VersionRem;
            CargarReglas(_versionSeleccionada);
        }

        private void CargarReglas(VersionRem? version)
        {
            if (version == null)
            {
                _reglas = [];
            }
            else
            {
                _reglas = _bdd.Regla
                    .Include(r => r.TipoRegla)
                    .Where(r => r.id_version == version.Id)
                    .OrderBy(r => r.Id)
                    .ToList();
            }

            reglas_dgv.DataSource = null;
            reglas_dgv.DataSource = _reglas;
        }

        private void nueva_btn_Click(object sender, EventArgs e)
        {
            if (_versionSeleccionada == null)
            {
                MessageBox.Show("Seleccione una versión antes de agregar una regla.", "Aviso");
                return;
            }

            var nueva = new Regla
            {
                id_version = _versionSeleccionada.Id,
                IdTipoRegla = 1,
                Expresion = string.Empty,
                Mensaje = string.Empty
            };

            _bdd.Regla.Add(nueva);
            _bdd.SaveChanges();

            CargarReglas(_versionSeleccionada);

            // Seleccionar la nueva fila
            if (reglas_dgv.Rows.Count > 0)
            {
                reglas_dgv.CurrentCell = reglas_dgv.Rows[reglas_dgv.Rows.Count - 1].Cells[0];
                reglas_dgv.Rows[reglas_dgv.Rows.Count - 1].Selected = true;
            }
        }

        private void eliminar_btn_Click(object sender, EventArgs e)
        {
            if (reglas_dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una regla para eliminar.", "Aviso");
                return;
            }

            var regla = reglas_dgv.SelectedRows[0].DataBoundItem as Regla;
            if (regla == null) return;

            var confirmacion = MessageBox.Show(
                $"¿Eliminar regla #{regla.Id}?\n\n{regla.Expresion}",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmacion != DialogResult.Yes) return;

            _bdd.Regla.Remove(regla);
            _bdd.SaveChanges();

            CargarReglas(_versionSeleccionada);
        }

        private void guardar_btn_Click(object sender, EventArgs e)
        {
            // Commit any pending cell edit
            reglas_dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            reglas_dgv.EndEdit();

            foreach (var regla in _reglas)
            {
                var tracked = _bdd.Regla.Local.FirstOrDefault(r => r.Id == regla.Id);
                if (tracked != null)
                {
                    tracked.Expresion = regla.Expresion;
                    tracked.Mensaje = regla.Mensaje;
                    tracked.IdTipoRegla = regla.IdTipoRegla;
                }
                else
                {
                    _bdd.Regla.Update(regla);
                }
            }

            _bdd.SaveChanges();
            MessageBox.Show("Reglas guardadas correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void reglas_dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Changes are reflected directly on the bound Regla object
        }
    }
}

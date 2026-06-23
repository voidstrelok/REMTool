using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTools
{
    public partial class ReglaEditorForm : Form
    {
        private readonly RemToolDataContext _bdd;
        private List<Regla> _reglas = [];
        private VersionRem? _versionSeleccionada;

        public ReglaEditorForm(RemToolDataContext dbContext)
        {
            InitializeComponent();
            _bdd = dbContext;
        }

        private void ReglaEditorForm_Load(object sender, EventArgs e)
        {
            CargarVersiones();
        }

        private void CargarVersiones()
        {
            var versiones = _bdd.VersionRem
                .Include(v => v.SerieRem)
                .OrderBy(v => v.SerieRem.Nombre)
                .ThenBy(v => v.Nombre)
                .ToList();

            version_cmb.DataSource = versiones;
            version_cmb.DisplayMember = "Nombre";
            version_cmb.ValueMember = "Id";

            if (versiones.Count > 0)
                version_cmb.SelectedIndex = 0;
            else
                CargarReglas(null);
        }

        private void version_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
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

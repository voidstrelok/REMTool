using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTools
{
    public partial class PuntoResumenEditorForm : Form
    {
        private readonly RemToolDataContext _bdd;
        private List<PuntoResumen> _puntos = [];
        private PuntoResumen? _sel;
        private bool _loading;

        public PuntoResumenEditorForm(RemToolDataContext dbContext)
        {
            InitializeComponent();
            _bdd = dbContext;
        }

        private void PuntoResumenEditorForm_Load(object sender, EventArgs e)
        {
            CargarSeries();
            CargarLista();
        }

        private void CargarSeries()
        {
            _loading = true;
            var series = _bdd.SerieRem.OrderBy(s => s.Nombre).ToList();
            serie_cmb.Items.Clear();
            foreach (var s in series)
                serie_cmb.Items.Add(new ComboItem(s.Nombre, s.Id));
            if (serie_cmb.Items.Count > 0)
                serie_cmb.SelectedIndex = 0;
            _loading = false;
        }

        private void CargarLista()
        {
            _puntos = _bdd.PuntoResumen
                .Include(p => p.SerieRem)
                .OrderBy(p => p.SerieRem.Nombre)
                .ThenBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToList();

            dgv.Rows.Clear();
            foreach (var p in _puntos)
            {
                dgv.Rows.Add(p.Id, p.SerieRem?.Nombre ?? "", p.Categoria, p.Nombre, p.Expresion);
            }

            LimpiarFormulario();
        }

        private void dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;
            var id = (int)dgv.SelectedRows[0].Cells["col_id"].Value;
            _sel = _puntos.FirstOrDefault(p => p.Id == id);
            if (_sel == null) return;

            _loading = true;
            nombre_txt.Text = _sel.Nombre;
            categoria_txt.Text = _sel.Categoria;
            expresion_txt.Text = _sel.Expresion;

            for (int i = 0; i < serie_cmb.Items.Count; i++)
            {
                if (((ComboItem)serie_cmb.Items[i]).Value is long id2 && id2 == _sel.IdSerieRem)
                {
                    serie_cmb.SelectedIndex = i;
                    break;
                }
            }
            _loading = false;
        }

        private void nuevo_btn_Click(object sender, EventArgs e)
        {
            _sel = null;
            LimpiarFormulario();
            nombre_txt.Focus();
        }

        private void guardar_btn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nombre_txt.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(expresion_txt.Text))
            {
                MessageBox.Show("La expresión es obligatoria.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (serie_cmb.SelectedItem is not ComboItem serieItem || serieItem.Value is not long serieId)
            {
                MessageBox.Show("Selecciona una serie.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_sel == null)
            {
                var nuevo = new PuntoResumen
                {
                    Nombre = nombre_txt.Text.Trim(),
                    Categoria = categoria_txt.Text.Trim(),
                    Expresion = expresion_txt.Text.Trim(),
                    IdSerieRem = serieId,
                };
                _bdd.PuntoResumen.Add(nuevo);
            }
            else
            {
                _sel.Nombre = nombre_txt.Text.Trim();
                _sel.Categoria = categoria_txt.Text.Trim();
                _sel.Expresion = expresion_txt.Text.Trim();
                _sel.IdSerieRem = serieId;
                _bdd.PuntoResumen.Update(_sel);
            }

            _bdd.SaveChanges();
            CargarLista();
            MessageBox.Show("Guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void eliminar_btn_Click(object sender, EventArgs e)
        {
            if (_sel == null) return;
            if (MessageBox.Show($"¿Eliminar \"{_sel.Nombre}\"?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _bdd.PuntoResumen.Remove(_sel);
            _bdd.SaveChanges();
            _sel = null;
            CargarLista();
        }

        private void LimpiarFormulario()
        {
            _loading = true;
            nombre_txt.Text = "";
            categoria_txt.Text = "";
            expresion_txt.Text = "";
            if (serie_cmb.Items.Count > 0) serie_cmb.SelectedIndex = 0;
            dgv.ClearSelection();
            eliminar_btn.Enabled = false;
            _loading = false;
        }

        private void nombre_txt_TextChanged(object sender, EventArgs e)
        {
            if (!_loading) eliminar_btn.Enabled = _sel != null;
        }

        private record ComboItem(string Text, object? Value)
        {
            public override string ToString() => Text;
        }
    }
}

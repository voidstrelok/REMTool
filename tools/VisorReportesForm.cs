using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTools
{
    internal record ReporteRow(long Id, string Establecimiento, int Mes, string Version, string Serie);

    public partial class VisorReportesForm : Form
    {
        private readonly RemToolDataContext _bdd;
        private List<ReporteRow> _todosLosReportes = new();

        public VisorReportesForm(RemToolDataContext bdd)
        {
            InitializeComponent();
            _bdd = bdd;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CargarReportes();
            CargarFiltros();
        }

        private void CargarReportes()
        {
            _todosLosReportes = _bdd.Reporte
                .AsNoTracking()
                .OrderBy(r => r.Establecimiento.Nombre)
                .ThenBy(r => r.Mes)
                .Select(r => new ReporteRow(
                    r.Id,
                    r.Establecimiento.Nombre,
                    r.Mes,
                    _bdd.Registro
                        .Where(reg => reg.id_reporte == r.Id)
                        .Select(reg => reg.Prestacion.VersionRem.Nombre)
                        .FirstOrDefault() ?? "—",
                    _bdd.Registro
                        .Where(reg => reg.id_reporte == r.Id)
                        .Select(reg => reg.Prestacion.VersionRem.SerieRem.Nombre)
                        .FirstOrDefault() ?? "—"
                ))
                .ToList();

            MostrarEnGrid(_todosLosReportes);
        }

        private void CargarFiltros()
        {
            cmbFiltroMes.Items.Clear();
            cmbFiltroMes.Items.Add("(Todos)");
            foreach (var mes in _todosLosReportes.Select(r => r.Mes).Distinct().OrderBy(m => m))
                cmbFiltroMes.Items.Add(mes);
            cmbFiltroMes.SelectedIndex = 0;

            cmbFiltroSerie.Items.Clear();
            cmbFiltroSerie.Items.Add("(Todas)");
            foreach (var serie in _todosLosReportes.Select(r => r.Serie).Distinct().OrderBy(s => s))
                cmbFiltroSerie.Items.Add(serie);
            cmbFiltroSerie.SelectedIndex = 0;

            cmbFiltroVersion.Items.Clear();
            cmbFiltroVersion.Items.Add("(Todas)");
            foreach (var version in _todosLosReportes.Select(r => r.Version).Distinct().OrderBy(v => v))
                cmbFiltroVersion.Items.Add(version);
            cmbFiltroVersion.SelectedIndex = 0;
        }

        private void MostrarEnGrid(List<ReporteRow> rows)
        {
            dgvReportes.DataSource = rows.ToList();

            if (dgvReportes.Columns.Contains("Id"))
                dgvReportes.Columns["Id"].Visible = false;
            if (dgvReportes.Columns.Contains("Establecimiento"))
                dgvReportes.Columns["Establecimiento"].HeaderText = "Establecimiento";
            if (dgvReportes.Columns.Contains("Mes"))
                dgvReportes.Columns["Mes"].HeaderText = "Mes";
            if (dgvReportes.Columns.Contains("Version"))
                dgvReportes.Columns["Version"].HeaderText = "Versión";
            if (dgvReportes.Columns.Contains("Serie"))
                dgvReportes.Columns["Serie"].HeaderText = "Serie";
        }

        private void btnFiltrar_Click(object sender, EventArgs e)
        {
            AplicarFiltrosActuales();
        }

        private void AplicarFiltrosActuales()
        {
            var resultado = _todosLosReportes.AsEnumerable();

            var textEstab = txtFiltroEstab.Text.Trim();
            if (!string.IsNullOrEmpty(textEstab))
                resultado = resultado.Where(r => r.Establecimiento.Contains(textEstab, StringComparison.OrdinalIgnoreCase));

            if (cmbFiltroMes.SelectedIndex > 0 && cmbFiltroMes.SelectedItem is int mes)
                resultado = resultado.Where(r => r.Mes == mes);

            if (cmbFiltroSerie.SelectedIndex > 0 && cmbFiltroSerie.SelectedItem is string serie)
                resultado = resultado.Where(r => r.Serie == serie);

            if (cmbFiltroVersion.SelectedIndex > 0 && cmbFiltroVersion.SelectedItem is string version)
                resultado = resultado.Where(r => r.Version == version);

            MostrarEnGrid(resultado.ToList());
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtFiltroEstab.Text = string.Empty;
            cmbFiltroMes.SelectedIndex = 0;
            cmbFiltroSerie.SelectedIndex = 0;
            cmbFiltroVersion.SelectedIndex = 0;
            MostrarEnGrid(_todosLosReportes);
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvReportes.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecciona al menos un reporte.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var seleccionados = dgvReportes.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => (ReporteRow)r.DataBoundItem)
                .ToList();

            var detalle = seleccionados.Count == 1
                ? $"\"{seleccionados[0].Establecimiento}\" (Mes {seleccionados[0].Mes}, {seleccionados[0].Serie} - {seleccionados[0].Version})"
                : $"{seleccionados.Count} reportes seleccionados";

            var confirmacion = MessageBox.Show(
                $"¿Eliminar {detalle}?\n\nSe eliminarán también todos sus registros.",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmacion != DialogResult.Yes)
                return;

            var ids = seleccionados.Select(r => r.Id).ToList();
            var registros = _bdd.Registro.Where(r => ids.Contains(r.id_reporte)).ToList();
            _bdd.Registro.RemoveRange(registros);
            var reportes = _bdd.Reporte.Where(r => ids.Contains(r.Id)).ToList();
            _bdd.Reporte.RemoveRange(reportes);
            _bdd.SaveChanges();

            foreach (var row in seleccionados)
                _todosLosReportes.Remove(row);

            AplicarFiltrosActuales();
        }

        private void btnAbrir_Click(object sender, EventArgs e)
        {
            AbrirReporteSeleccionado();
        }

        private void dgvReportes_DoubleClick(object sender, EventArgs e)
        {
            AbrirReporteSeleccionado();
        }

        private void AbrirReporteSeleccionado()
        {
            if (dgvReportes.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecciona un reporte primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = (ReporteRow)dgvReportes.SelectedRows[0].DataBoundItem;
            using var detalle = new VisorRegistrosForm(_bdd, row.Id, row.Establecimiento, row.Mes, row.Version, row.Serie);
            detalle.ShowDialog(this);
        }
    }
}

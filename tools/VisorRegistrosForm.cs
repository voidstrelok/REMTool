using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTools
{
    public partial class VisorRegistrosForm : Form
    {
        private readonly RemToolDataContext _bdd;
        private readonly long _reporteId;

        private record RegistroRow(
            long Id,
            string CodigoPrestacion,
            string Hoja,
            string Coordenada,
            string Valores);

        public VisorRegistrosForm(RemToolDataContext bdd, long reporteId, string establecimiento, int mes, string version, string serie)
        {
            InitializeComponent();
            _bdd = bdd;
            _reporteId = reporteId;
            lblReporte.Text = $"Establecimiento: {establecimiento}  |  Mes: {mes}  |  Serie: {serie}  |  Versión: {version}";
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            BuscarPrestacion();
        }

        private void txtCodPrestacion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                BuscarPrestacion();
        }

        private void BuscarPrestacion()
        {
            var codigo = txtCodPrestacion.Text.Trim();
            if (string.IsNullOrEmpty(codigo))
            {
                lblResultado.Text = "Ingresa un código de prestación para buscar.";
                dgvRegistros.DataSource = null;
                return;
            }

            var resultados = _bdd.Registro
                .Include(r => r.Prestacion)
                    .ThenInclude(p => p.HojaRem)
                .Where(r => r.id_reporte == _reporteId &&
                            r.Prestacion.CodigoPrestacion.Contains(codigo))
                .AsNoTracking()
                .Select(r => new RegistroRow(
                    r.Id,
                    r.Prestacion.CodigoPrestacion,
                    r.Prestacion.HojaRem.Nombre,
                    string.Join(", ", r.Prestacion.Coordenada),
                    string.Join(" | ", r.Valor.Select(v => v.ToString("F2")))
                ))
                .OrderBy(r => r.CodigoPrestacion)
                .ToList();

            dgvRegistros.DataSource = resultados;

            if (dgvRegistros.Columns.Contains("Id"))
                dgvRegistros.Columns["Id"].Visible = false;

            if (dgvRegistros.Columns.Contains("CodigoPrestacion"))
                dgvRegistros.Columns["CodigoPrestacion"].HeaderText = "Cód. Prestación";
            if (dgvRegistros.Columns.Contains("Hoja"))
                dgvRegistros.Columns["Hoja"].HeaderText = "Hoja";
            if (dgvRegistros.Columns.Contains("Coordenada"))
                dgvRegistros.Columns["Coordenada"].HeaderText = "Coordenada";
            if (dgvRegistros.Columns.Contains("Valores"))
                dgvRegistros.Columns["Valores"].HeaderText = "Valores";

            lblResultado.Text = resultados.Count == 0
                ? "No se encontraron registros para ese código."
                : $"{resultados.Count} registro(s) encontrado(s).";
        }
    }
}

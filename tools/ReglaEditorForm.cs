using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using System.Text.RegularExpressions;

namespace RemTools
{
    public partial class ReglaEditorForm : Form
    {
        private readonly RemToolDataContext _bdd;
        private List<Regla> _reglas = [];
        private List<SerieRem> _series = [];
        private List<VersionRem> _versiones = [];
        private List<TipoRegla> _tiposRegla = [];
        private Regla? _seleccionada;
        private bool _cargando;

        public ReglaEditorForm(RemToolDataContext dbContext)
        {
            InitializeComponent();
            _bdd = dbContext;
        }

        private void ReglaEditorForm_Load(object sender, EventArgs e)
        {
            try
            {
                CargarCatalogos();
                CargarReglas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo cargar el editor de reglas:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarCatalogos()
        {
            _cargando = true;

            _series = _bdd.SerieRem
                .OrderBy(s => s.Nombre)
                .ToList();

            _versiones = _bdd.VersionRem
                .Include(v => v.SerieRem)
                .OrderBy(v => v.SerieRem.Nombre)
                .ThenByDescending(v => v.Fecha)
                .ThenBy(v => v.Nombre)
                .ToList();

            _tiposRegla = _bdd.TipoRegla
                .OrderBy(t => t.Id)
                .ToList();

            serie_filtro_cmb.Items.Clear();
            serie_filtro_cmb.Items.Add(new ComboItem("(Todas)", null));
            serie_filtro_cmb.Items.AddRange(_series
                .Select(s => new ComboItem(s.Nombre, s.Id))
                .ToArray());
            serie_filtro_cmb.SelectedIndex = 0;

            anio_filtro_cmb.Items.Clear();
            anio_filtro_cmb.Items.Add("(Todos)");
            anio_filtro_cmb.Items.AddRange(_versiones
                .Select(v => v.Fecha.Year)
                .Distinct()
                .OrderDescending()
                .Cast<object>()
                .ToArray());
            anio_filtro_cmb.SelectedIndex = 0;

            CargarVersionesFiltro();

            var tiposFiltro = new List<ComboItem> { new("(Todos)", null) };
            tiposFiltro.AddRange(_tiposRegla.Select(t => new ComboItem(t.Nombre, t.Id)));
            tipo_filtro_cmb.DataSource = tiposFiltro;
            tipo_filtro_cmb.DisplayMember = nameof(ComboItem.Text);
            tipo_filtro_cmb.SelectedIndex = 0;

            tipo_cmb.DataSource = _tiposRegla;
            tipo_cmb.DisplayMember = nameof(TipoRegla.Nombre);
            tipo_cmb.ValueMember = nameof(TipoRegla.Id);

            CargarVersionesFormulario();
            _cargando = false;
        }

        private void CargarVersionesFiltro()
        {
            var serieId = (serie_filtro_cmb.SelectedItem as ComboItem)?.Int64Value;
            var anio = anio_filtro_cmb.SelectedItem is int year ? year : (int?)null;

            var versiones = FiltrarVersiones(serieId, anio);
            var items = new List<ComboItem> { new("(Todas)", null) };
            items.AddRange(versiones.Select(v => new ComboItem(
                $"{v.SerieRem?.Nombre ?? "?"} · {v.Nombre} ({v.Fecha:yyyy-MM-dd})",
                v.Id)));

            version_filtro_cmb.DataSource = items;
            version_filtro_cmb.DisplayMember = nameof(ComboItem.Text);
            version_filtro_cmb.SelectedIndex = 0;
        }

        private void CargarVersionesFormulario(int? versionSeleccionada = null)
        {
            var serieId = (serie_filtro_cmb.SelectedItem as ComboItem)?.Int64Value;
            var anio = anio_filtro_cmb.SelectedItem is int year ? year : (int?)null;
            var items = FiltrarVersiones(serieId, anio)
                .Select(v => new VersionItem(v))
                .ToList();

            version_cmb.DataSource = items;
            version_cmb.DisplayMember = nameof(VersionItem.Text);
            version_cmb.ValueMember = nameof(VersionItem.Id);

            if (versionSeleccionada.HasValue && items.Any(v => v.Id == versionSeleccionada.Value))
                version_cmb.SelectedValue = versionSeleccionada.Value;
            else if (items.Count > 0)
                version_cmb.SelectedIndex = 0;
        }

        private IEnumerable<VersionRem> FiltrarVersiones(long? serieId, int? anio)
        {
            var versiones = _versiones.AsEnumerable();
            if (serieId.HasValue)
                versiones = versiones.Where(v => v.id_serie == serieId.Value);
            if (anio.HasValue)
                versiones = versiones.Where(v => v.Fecha.Year == anio.Value);
            return versiones;
        }

        private void CargarReglas()
        {
            _cargando = true;

            var serieId = (serie_filtro_cmb.SelectedItem as ComboItem)?.Int64Value;
            var anio = anio_filtro_cmb.SelectedItem is int year ? year : (int?)null;
            var versionId = (version_filtro_cmb.SelectedItem as ComboItem)?.Int32Value;
            var tipoId = (tipo_filtro_cmb.SelectedItem as ComboItem)?.Int32Value;
            var texto = buscar_txt.Text.Trim();

            var query = _bdd.Regla
                .Include(r => r.TipoRegla)
                .Include(r => r.VersionREM)
                    .ThenInclude(v => v.SerieRem)
                .AsQueryable();

            if (serieId.HasValue)
                query = query.Where(r => r.VersionREM.id_serie == serieId.Value);
            if (anio.HasValue)
                query = query.Where(r => r.VersionREM.Fecha.Year == anio.Value);
            if (versionId.HasValue)
                query = query.Where(r => r.id_version == versionId.Value);
            if (tipoId.HasValue)
                query = query.Where(r => r.IdTipoRegla == tipoId.Value);

            _reglas = query
                .OrderBy(r => r.VersionREM.SerieRem.Nombre)
                .ThenByDescending(r => r.VersionREM.Fecha)
                .ThenBy(r => r.VersionREM.Nombre)
                .ThenBy(r => r.Id)
                .ToList();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                _reglas = _reglas.Where(r =>
                    r.Expresion.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    r.Mensaje.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    r.TipoRegla?.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            reglas_dgv.Rows.Clear();
            foreach (var regla in _reglas)
            {
                reglas_dgv.Rows.Add(
                    regla.Id,
                    regla.VersionREM?.SerieRem?.Nombre ?? "",
                    regla.VersionREM?.Nombre ?? $"Versión #{regla.id_version}",
                    regla.TipoRegla?.Nombre ?? $"Tipo #{regla.IdTipoRegla}",
                    regla.Expresion,
                    regla.Mensaje);
            }

            reglas_dgv.ClearSelection();
            editar_btn.Enabled = false;

            reglas_count_lbl.Text = $"{_reglas.Count} regla{(_reglas.Count == 1 ? "" : "s")}";
            _cargando = false;

            if (_seleccionada != null)
            {
                var rowIndex = _reglas.FindIndex(r => r.Id == _seleccionada.Id);
                if (rowIndex >= 0)
                {
                    reglas_dgv.Rows[rowIndex].Selected = true;
                    CargarFormulario();
                }
                else
                {
                    _seleccionada = null;
                    LimpiarFormulario();
                }
            }
            else
            {
                LimpiarFormulario();
            }
        }

        private void reglas_dgv_SelectionChanged(object sender, EventArgs e)
        {
            SeleccionarFilaActual();
        }

        private void reglas_dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                SeleccionarFilaActual();
        }

        private void reglas_dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                editar_btn_Click(sender, EventArgs.Empty);
        }

        private void SeleccionarFilaActual()
        {
            if (_cargando)
                return;

            var fila = reglas_dgv.CurrentRow;
            if (fila == null)
                return;

            var idValue = fila.Cells[col_id.Name].Value;
            if (idValue is not int id)
                return;

            _seleccionada = _reglas.FirstOrDefault(r => r.Id == id);
            editar_btn.Enabled = _seleccionada != null;
            if (_seleccionada != null)
                CargarFormulario();
        }

        private void CargarFormulario()
        {
            if (_seleccionada == null)
            {
                LimpiarFormulario();
                return;
            }

            _cargando = true;
            CargarVersionesFormulario(_seleccionada.id_version);
            version_cmb.SelectedValue = _seleccionada.id_version;
            tipo_cmb.SelectedValue = _seleccionada.IdTipoRegla;
            expresion_txt.Text = _seleccionada.Expresion;
            mensaje_txt.Text = _seleccionada.Mensaje;
            id_lbl.Text = $"ID: {_seleccionada.Id}";
            grp_formulario.Text = "Editar regla";
            eliminar_btn.Enabled = true;
            duplicar_btn.Enabled = true;
            editar_btn.Enabled = true;
            _cargando = false;

            CargarReferencias();
            ActualizarValidacion();
        }

        private void nuevo_btn_Click(object sender, EventArgs e)
        {
            _seleccionada = null;
            LimpiarFormulario();

            if ((version_filtro_cmb.SelectedItem as ComboItem)?.Int32Value is int versionId)
                version_cmb.SelectedValue = versionId;
            else if (version_cmb.Items.Count > 0)
                version_cmb.SelectedIndex = 0;

            if (tipo_cmb.Items.Count > 0)
                tipo_cmb.SelectedIndex = 0;

            expresion_txt.Focus();
        }

        private void editar_btn_Click(object sender, EventArgs e)
        {
            if (_seleccionada == null)
                SeleccionarFilaActual();

            if (_seleccionada == null)
            {
                MessageBox.Show("Seleccione una regla para editar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            CargarFormulario();
            expresion_txt.Focus();
        }

        private void duplicar_btn_Click(object sender, EventArgs e)
        {
            if (_seleccionada == null)
                return;

            var versionId = _seleccionada.id_version;
            var tipoId = _seleccionada.IdTipoRegla;
            var expresion = _seleccionada.Expresion;
            var mensaje = _seleccionada.Mensaje;

            _seleccionada = null;
            _cargando = true;
            CargarVersionesFormulario(versionId);
            version_cmb.SelectedValue = versionId;
            tipo_cmb.SelectedValue = tipoId;
            expresion_txt.Text = expresion;
            mensaje_txt.Text = mensaje;
            id_lbl.Text = "Nueva copia";
            eliminar_btn.Enabled = false;
            duplicar_btn.Enabled = false;
            _cargando = false;

            CargarReferencias();
            ActualizarValidacion();
            expresion_txt.Focus();
        }

        private void guardar_btn_Click(object sender, EventArgs e)
        {
            if (version_cmb.SelectedItem is not VersionItem versionItem)
            {
                MostrarValidacion("Seleccione una versión.", false);
                return;
            }

            if (tipo_cmb.SelectedItem is not TipoRegla tipo)
            {
                MostrarValidacion("Seleccione un tipo de regla.", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(mensaje_txt.Text))
            {
                MostrarValidacion("El mensaje es obligatorio.", false);
                mensaje_txt.Focus();
                return;
            }

            var validacion = ReglaExpressionService.Validate(expresion_txt.Text);
            MostrarValidacion(validacion.Message, validacion.IsValid);
            if (!validacion.IsValid)
            {
                expresion_txt.Focus();
                return;
            }

            if (_seleccionada == null)
            {
                _seleccionada = new Regla();
                _bdd.Regla.Add(_seleccionada);
            }

            _seleccionada.id_version = versionItem.Id;
            _seleccionada.IdTipoRegla = tipo.Id;
            _seleccionada.Expresion = expresion_txt.Text.Trim();
            _seleccionada.Mensaje = mensaje_txt.Text.Trim();

            try
            {
                _bdd.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                MostrarValidacion($"No se pudo guardar: {ex.GetBaseException().Message}", false);
                return;
            }

            var savedId = _seleccionada.Id;
            CargarReglas();
            _seleccionada = _reglas.FirstOrDefault(r => r.Id == savedId);
            if (_seleccionada != null)
                CargarFormulario();

            MessageBox.Show("Regla guardada correctamente.", "Éxito",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void eliminar_btn_Click(object sender, EventArgs e)
        {
            if (_seleccionada == null)
                return;

            if (MessageBox.Show(
                    $"¿Eliminar la regla #{_seleccionada.Id}?\n\n{_seleccionada.Mensaje}",
                    "Confirmar eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _bdd.Regla.Remove(_seleccionada);
            _bdd.SaveChanges();
            _seleccionada = null;
            CargarReglas();
        }

        private void LimpiarFormulario()
        {
            _cargando = true;
            id_lbl.Text = "Nueva regla";
            grp_formulario.Text = "Nueva regla";
            expresion_txt.Text = "";
            mensaje_txt.Text = "";
            referencias_dgv.Rows.Clear();
            eliminar_btn.Enabled = false;
            duplicar_btn.Enabled = false;
            editar_btn.Enabled = false;
            _cargando = false;
            ActualizarValidacion();
        }

        private void expresion_txt_TextChanged(object sender, EventArgs e)
        {
            if (_cargando)
                return;

            CargarReferencias();
            ActualizarValidacion();
        }

        private void CargarReferencias()
        {
            var valores = LeerValoresDePrueba();
            referencias_dgv.Rows.Clear();

            foreach (var referencia in ReglaExpressionService.ExtractReferences(expresion_txt.Text))
            {
                valores.TryGetValue(referencia, out var valor);
                referencias_dgv.Rows.Add(referencia, valor ?? "0");
            }
        }

        private Dictionary<string, string> LeerValoresDePrueba()
        {
            var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in referencias_dgv.Rows)
            {
                if (row.IsNewRow)
                    continue;

                var referencia = row.Cells[col_referencia.Name].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(referencia))
                    valores[referencia] = row.Cells[col_valor.Name].Value?.ToString() ?? "0";
            }
            return valores;
        }

        private void validar_btn_Click(object sender, EventArgs e)
        {
            ActualizarValidacion();
        }

        private void probar_btn_Click(object sender, EventArgs e)
        {
            var resultado = ReglaExpressionService.Evaluate(expresion_txt.Text, LeerValoresDePrueba());
            MostrarValidacion(resultado.Message, resultado.IsValid);
        }

        private void ActualizarValidacion()
        {
            var resultado = ReglaExpressionService.Validate(expresion_txt.Text);
            MostrarValidacion(resultado.Message, resultado.IsValid);
        }

        private void MostrarValidacion(string mensaje, bool correcto)
        {
            validacion_lbl.Text = mensaje;
            validacion_lbl.ForeColor = correcto ? Color.DarkGreen : Color.Firebrick;
        }

        private void insertar_referencia_btn_Click(object sender, EventArgs e)
        {
            var hoja = hoja_txt.Text.Trim();
            var celda = celda_txt.Text.Trim().ToUpperInvariant();

            if (!Regex.IsMatch(hoja, @"^[A-Za-z]+\d*$") ||
                !Regex.IsMatch(celda, @"^[A-Z]{1,3}\d+$"))
            {
                MostrarValidacion("Indique una hoja y celda válidas. Ejemplo: A01 / C19", false);
                return;
            }

            InsertarEnExpresion($"{hoja}[{celda}]");
        }

        private void operador_btn_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is string texto)
                InsertarEnExpresion(texto);
        }

        private void InsertarEnExpresion(string texto)
        {
            var inicio = expresion_txt.SelectionStart;
            expresion_txt.Text = expresion_txt.Text.Insert(inicio, texto);
            expresion_txt.SelectionStart = inicio + texto.Length;
            expresion_txt.Focus();
        }

        private void buscar_txt_TextChanged(object sender, EventArgs e)
        {
            if (!_cargando)
            {
                _seleccionada = null;
                CargarReglas();
            }
        }

        private void serie_filtro_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargando) return;
            _cargando = true;
            CargarVersionesFiltro();
            CargarVersionesFormulario();
            _cargando = false;
            _seleccionada = null;
            CargarReglas();
        }

        private void anio_filtro_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargando) return;
            _cargando = true;
            CargarVersionesFiltro();
            CargarVersionesFormulario();
            _cargando = false;
            _seleccionada = null;
            CargarReglas();
        }

        private void version_filtro_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_cargando)
            {
                _seleccionada = null;
                CargarReglas();
            }
        }

        private void tipo_filtro_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_cargando)
            {
                _seleccionada = null;
                CargarReglas();
            }
        }

        private record ComboItem(string Text, object? Value)
        {
            public long? Int64Value => Value is long value ? value : null;
            public int? Int32Value => Value is int value ? value : null;
            public override string ToString() => Text;
        }

        private sealed record VersionItem(VersionRem Version)
        {
            public int Id => Version.Id;
            public string Text => $"{Version.SerieRem?.Nombre ?? "?"} · {Version.Nombre} ({Version.Fecha:yyyy-MM-dd})";
            public override string ToString() => Text;
        }
    }
}

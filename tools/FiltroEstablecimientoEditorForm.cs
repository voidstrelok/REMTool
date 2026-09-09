using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using RemTool.Shared.Enum;

namespace RemTools
{
    public class FiltroEstablecimientoEditorForm : Form
    {
        private readonly RemToolDataContext _db;
        private readonly DataGridView _grid = new();
        private readonly ComboBox _indicador = new();
        private readonly ComboBox _tipo = new();
        private readonly ComboBox _establecimiento = new();
        private readonly Label _estado = new();
        private readonly Button _nuevo = new();
        private readonly Button _guardar = new();
        private readonly Button _eliminar = new();
        private List<FiltroEstablecimiento> _filtros = [];
        private List<IndicadorItem> _indicadores = [];
        private List<EstablecimientoItem> _establecimientos = [];
        private FiltroEstablecimiento? _seleccionado;
        private bool _cargando;

        public FiltroEstablecimientoEditorForm(RemToolDataContext db)
        {
            _db = db;
            InitializeForm();
            Load += (_, _) => CargarDatos();
        }

        private void InitializeForm()
        {
            Text = "Editor de filtros por establecimiento";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(920, 560);
            Size = new Size(1120, 700);

            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.MultiSelect = false;
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.SelectionChanged += (_, _) => SeleccionarFila();
            AgregarColumna("ID", "Id", 55);
            AgregarColumna("Ámbito", "Ambito", 95);
            AgregarColumna("Tipo", "Tipo", 85);
            AgregarColumna("Establecimiento", "Establecimiento", 260);
            AgregarColumna("Código DEIS", "CodDeis", 105);
            AgregarColumna("Indicador", "Indicador", 300);

            var lista = new GroupBox
            {
                Text = "Filtros configurados",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };
            lista.Controls.Add(_grid);

            ConfigurarCombos();
            _estado.AutoSize = true;
            _estado.ForeColor = Color.DarkRed;

            _nuevo.Text = "Nuevo filtro";
            _nuevo.Click += (_, _) => NuevoFiltro();
            _guardar.Text = "Guardar";
            _guardar.Click += (_, _) => GuardarFiltro();
            _eliminar.Text = "Eliminar";
            _eliminar.Click += (_, _) => EliminarFiltro();

            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(12)
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < editor.RowCount; i++)
                editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            AgregarCampo(editor, 0, "Ámbito", _indicador);
            AgregarCampo(editor, 1, "Tipo de filtro", _tipo);
            AgregarCampo(editor, 2, "Establecimiento", _establecimiento);
            editor.Controls.Add(new Label
            {
                Text = "Una regla Incluir específica convierte el indicador en lista blanca. Sin inclusiones, se aplican las exclusiones globales y específicas.",
                AutoSize = true,
                MaximumSize = new Size(410, 0),
                ForeColor = Color.DimGray,
                Margin = new Padding(3, 12, 3, 8)
            }, 1, 3);
            editor.Controls.Add(_estado, 1, 4);

            var acciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 10, 0, 0)
            };
            acciones.Controls.AddRange([_nuevo, _guardar, _eliminar]);
            editor.Controls.Add(acciones, 1, 5);

            var panelEditor = new GroupBox
            {
                Text = "Edición",
                Dock = DockStyle.Fill,
                Padding = new Padding(4)
            };
            panelEditor.Controls.Add(editor);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 650
            };
            split.Panel1.Controls.Add(lista);
            split.Panel2.Controls.Add(panelEditor);
            Controls.Add(split);
        }

        private void ConfigurarCombos()
        {
            _indicador.DropDownStyle = ComboBoxStyle.DropDownList;
            _indicador.SelectedIndexChanged += (_, _) => ActualizarTipoDisponible();

            _tipo.DropDownStyle = ComboBoxStyle.DropDownList;
            _tipo.Items.AddRange(Enum.GetValues<TipoFiltroEstablecimiento>().Cast<object>().ToArray());

            _establecimiento.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private static void AgregarCampo(TableLayoutPanel panel, int fila, string texto, Control control)
        {
            panel.Controls.Add(new Label
            {
                Text = texto,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 7, 3, 3)
            }, 0, fila);
            control.Dock = DockStyle.Top;
            panel.Controls.Add(control, 1, fila);
        }

        private void AgregarColumna(string encabezado, string propiedad, int ancho)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = encabezado,
                DataPropertyName = propiedad,
                Width = ancho,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private void CargarDatos(int? seleccionarId = null)
        {
            try
            {
                _cargando = true;
                _indicadores = _db.Indicador
                    .OrderByDescending(i => i.Año)
                    .ThenBy(i => i.Orden)
                    .ThenBy(i => i.Nombre)
                    .Select(i => new IndicadorItem(i.Id, $"#{i.Id} · {i.Año} · {i.Nombre}"))
                    .ToList();
                _establecimientos = _db.Establecimiento
                    .OrderBy(e => e.Nombre)
                    .Select(e => new EstablecimientoItem(e.Id, $"{e.Nombre}{(e.CodDeis == null ? "" : $" ({e.CodDeis})")}"))
                    .ToList();

                var indicadorItems = new List<IndicadorItem>
                {
                    new(0, "Global · aplica a todos los indicadores")
                };
                indicadorItems.AddRange(_indicadores);
                _indicador.DataSource = indicadorItems;
                _indicador.DisplayMember = nameof(IndicadorItem.Text);
                _indicador.SelectedIndex = 0;

                _establecimiento.DataSource = _establecimientos;
                _establecimiento.DisplayMember = nameof(EstablecimientoItem.Text);
                if (_establecimientos.Count > 0) _establecimiento.SelectedIndex = 0;

                CargarFiltros(seleccionarId);
            }
            catch (Exception ex)
            {
                MostrarError($"No se pudo cargar el editor: {ex.Message}");
            }
            finally
            {
                _cargando = false;
                if (_grid.CurrentRow != null)
                    SeleccionarFila();
            }
        }

        private void CargarFiltros(int? seleccionarId = null)
        {
            _filtros = _db.FiltroEstablecimiento
                .Include(f => f.Establecimiento)
                .Include(f => f.Indicador)
                .OrderBy(f => f.id_indicador.HasValue)
                .ThenBy(f => f.Indicador!.Año)
                .ThenBy(f => f.Indicador!.Orden)
                .ThenBy(f => f.Tipo)
                .ThenBy(f => f.Establecimiento.Nombre)
                .ToList();

            var filas = _filtros.Select(f => new FiltroFila(
                f.Id,
                f.id_indicador.HasValue ? "Indicador" : "Global",
                f.Tipo.ToString(),
                f.Establecimiento?.Nombre ?? $"#{f.id_establecimiento}",
                f.Establecimiento?.CodDeis ?? "",
                f.Indicador == null ? "Todos los indicadores" : $"#{f.Indicador.Id} · {f.Indicador.Año} · {f.Indicador.Nombre}"
            )).ToList();
            _grid.DataSource = filas;

            if (filas.Count == 0)
            {
                NuevoFiltro();
                return;
            }

            var index = seleccionarId.HasValue
                ? filas.FindIndex(f => f.Id == seleccionarId.Value)
                : 0;
            _grid.Rows[Math.Max(0, index)].Selected = true;
        }

        private void SeleccionarFila()
        {
            if (_cargando || _grid.CurrentRow?.DataBoundItem is not FiltroFila fila)
                return;

            _seleccionado = _filtros.FirstOrDefault(f => f.Id == fila.Id);
            if (_seleccionado == null) return;

            _cargando = true;
            SeleccionarIndicador(_seleccionado.id_indicador ?? 0);
            SeleccionarEstablecimiento(_seleccionado.id_establecimiento);
            _tipo.SelectedItem = _seleccionado.Tipo;
            _estado.Text = string.Empty;
            _eliminar.Enabled = true;
            _cargando = false;
        }

        private void SeleccionarIndicador(int id)
        {
            for (var i = 0; i < _indicador.Items.Count; i++)
            {
                if (_indicador.Items[i] is IndicadorItem item && item.Id == id)
                {
                    _indicador.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SeleccionarEstablecimiento(long id)
        {
            for (var i = 0; i < _establecimiento.Items.Count; i++)
            {
                if (_establecimiento.Items[i] is EstablecimientoItem item && item.Id == id)
                {
                    _establecimiento.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ActualizarTipoDisponible()
        {
            if (_cargando) return;
            var esGlobal = (_indicador.SelectedItem as IndicadorItem)?.Id == 0;
            if (esGlobal && _tipo.SelectedItem is TipoFiltroEstablecimiento.Incluir)
                _tipo.SelectedItem = TipoFiltroEstablecimiento.Excluir;
            _tipo.Enabled = true;
        }

        private void NuevoFiltro()
        {
            _cargando = true;
            _seleccionado = null;
            if (_indicador.Items.Count > 0) _indicador.SelectedIndex = 0;
            if (_tipo.Items.Count > 0) _tipo.SelectedItem = TipoFiltroEstablecimiento.Excluir;
            if (_establecimiento.Items.Count > 0) _establecimiento.SelectedIndex = 0;
            _estado.ForeColor = Color.DarkGreen;
            _estado.Text = "Nuevo filtro";
            _eliminar.Enabled = false;
            _grid.ClearSelection();
            _cargando = false;
        }

        private void GuardarFiltro()
        {
            _estado.Text = string.Empty;
            if (_establecimiento.SelectedItem is not EstablecimientoItem establecimiento)
            {
                MostrarError("Seleccione un establecimiento.");
                return;
            }
            if (_indicador.SelectedItem is not IndicadorItem indicador)
            {
                MostrarError("Seleccione el ámbito del filtro.");
                return;
            }
            if (_tipo.SelectedItem is not TipoFiltroEstablecimiento tipo)
            {
                MostrarError("Seleccione un tipo de filtro.");
                return;
            }
            if (indicador.Id == 0 && tipo == TipoFiltroEstablecimiento.Incluir)
            {
                MostrarError("Una inclusión global no es válida. Use una inclusión específica por indicador.");
                return;
            }

            var filtroId = _seleccionado?.Id ?? 0;
            int? indicadorId = indicador.Id == 0 ? null : indicador.Id;
            var duplicado = _db.FiltroEstablecimiento.Any(f =>
                f.Id != filtroId
                && f.id_establecimiento == establecimiento.Id
                && f.id_indicador == indicadorId);
            if (duplicado)
            {
                MostrarError("Ya existe un filtro para ese establecimiento y ámbito.");
                return;
            }

            var filtro = _seleccionado ?? new FiltroEstablecimiento();
            filtro.id_establecimiento = establecimiento.Id;
            filtro.id_indicador = indicador.Id == 0 ? null : indicador.Id;
            filtro.Tipo = tipo;
            if (_seleccionado == null) _db.FiltroEstablecimiento.Add(filtro);

            try
            {
                _db.SaveChanges();
                _seleccionado = filtro;
                _estado.ForeColor = Color.DarkGreen;
                _estado.Text = "Guardado correctamente.";
                CargarFiltros(filtro.Id);
            }
            catch (DbUpdateException ex)
            {
                MostrarError($"No se pudo guardar el filtro: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private void EliminarFiltro()
        {
            if (_seleccionado == null)
            {
                MostrarError("Seleccione un filtro.");
                return;
            }
            if (MessageBox.Show(
                    "¿Eliminar el filtro seleccionado?",
                    "Confirmar eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _db.FiltroEstablecimiento.Remove(_seleccionado);
            _db.SaveChanges();
            _seleccionado = null;
            CargarFiltros();
        }

        private void MostrarError(string mensaje)
        {
            _estado.ForeColor = Color.DarkRed;
            _estado.Text = mensaje;
        }

        private record IndicadorItem(int Id, string Text)
        {
            public override string ToString() => Text;
        }

        private record EstablecimientoItem(long Id, string Text)
        {
            public override string ToString() => Text;
        }

        private record FiltroFila(
            int Id,
            string Ambito,
            string Tipo,
            string Establecimiento,
            string CodDeis,
            string Indicador);
    }
}

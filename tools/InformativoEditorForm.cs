using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using RemTool.Shared.Enum;

namespace RemTools;

public class InformativoEditorForm : Form
{
    private readonly RemToolDataContext _db;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _tipo = new();
    private readonly TextBox _titulo = new();
    private readonly TextBox _contenido = new();
    private readonly TextBox _url = new();
    private readonly TextBox _textoEnlace = new();
    private readonly DateTimePicker _fecha = new();
    private readonly CheckBox _vigente = new();
    private readonly CheckBox _destacado = new();
    private readonly Button _guardar = new();
    private readonly Button _nuevo = new();
    private readonly Button _alternarVigencia = new();
    private readonly Button _archivar = new();
    private readonly Label _estado = new();
    private Informativo? _seleccionado;

    public InformativoEditorForm(RemToolDataContext db)
    {
        _db = db;
        InitializeForm();
        CargarInformativos();
        NuevoInformativo();
    }

    private void InitializeForm()
    {
        Text = "Gestor de informativos";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(980, 620);
        Size = new Size(1120, 720);

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowHeadersVisible = false;
        _grid.SelectionChanged += (_, _) => SeleccionarFila();
        AgregarColumna("Tipo", "Tipo", 95);
        AgregarColumna("Título", "Titulo", 240);
        AgregarColumna("Publicación", "FechaPublicacion", 100);
        AgregarColumna("Vigente", "Vigente", 70);
        AgregarColumna("Destacado", "Destacado", 80);

        var listaPanel = new GroupBox { Text = "Informativos", Dock = DockStyle.Fill, Padding = new Padding(8) };
        listaPanel.Controls.Add(_grid);

        _tipo.DropDownStyle = ComboBoxStyle.DropDownList;
        _tipo.Items.AddRange(Enum.GetValues<TipoInformativo>().Cast<object>().ToArray());
        _tipo.SelectedIndex = 0;
        _titulo.MaxLength = 180;
        _contenido.Multiline = true;
        _contenido.ScrollBars = ScrollBars.Vertical;
        _url.MaxLength = 1000;
        _textoEnlace.MaxLength = 100;
        _fecha.Format = DateTimePickerFormat.Short;
        _fecha.Value = DateTime.Today;
        _vigente.Text = "Vigente / visible en la web";
        _vigente.AutoSize = true;
        _destacado.Text = "Destacado";
        _destacado.AutoSize = true;
        _estado.AutoSize = true;
        _estado.ForeColor = Color.DarkRed;

        _guardar.Text = "Guardar";
        _guardar.Click += (_, _) => GuardarInformativo();
        _nuevo.Text = "Nuevo";
        _nuevo.Click += (_, _) => NuevoInformativo();
        _alternarVigencia.Text = "Cambiar vigencia";
        _alternarVigencia.Click += (_, _) => AlternarVigencia();
        _archivar.Text = "Archivar";
        _archivar.Click += (_, _) => Archivar();

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(10),
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 10; i++) editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles[3] = new RowStyle(SizeType.Percent, 100);

        AgregarCampo(editor, 0, "Tipo", _tipo);
        AgregarCampo(editor, 1, "Título", _titulo);
        AgregarCampo(editor, 2, "Fecha", _fecha);
        AgregarCampo(editor, 3, "Contenido", _contenido, fill: true);
        AgregarCampo(editor, 4, "URL", _url);
        AgregarCampo(editor, 5, "Texto enlace", _textoEnlace);
        editor.Controls.Add(_vigente, 1, 6);
        editor.Controls.Add(_destacado, 1, 7);
        editor.Controls.Add(_estado, 1, 8);

        var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        acciones.Controls.AddRange([_nuevo, _guardar, _alternarVigencia, _archivar]);
        editor.Controls.Add(acciones, 1, 9);

        var editorPanel = new GroupBox { Text = "Edición", Dock = DockStyle.Fill, Padding = new Padding(4) };
        editorPanel.Controls.Add(editor);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 560 };
        split.Panel1.Controls.Add(listaPanel);
        split.Panel2.Controls.Add(editorPanel);
        Controls.Add(split);
    }

    private void AgregarColumna(string header, string property, int width)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = property, Width = width, SortMode = DataGridViewColumnSortMode.NotSortable });
    }

    private static void AgregarCampo(TableLayoutPanel panel, int row, string label, Control control, bool fill = false)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 3, 3) }, 0, row);
        control.Dock = fill ? DockStyle.Fill : DockStyle.Top;
        panel.Controls.Add(control, 1, row);
    }

    private void CargarInformativos(int? seleccionarId = null)
    {
        var datos = _db.Informativo
            .AsNoTracking()
            .OrderByDescending(x => x.Vigente)
            .ThenByDescending(x => x.FechaPublicacion)
            .ThenByDescending(x => x.Id)
            .ToList();
        _grid.DataSource = datos;
        if (datos.Count == 0)
        {
            _seleccionado = null;
            return;
        }
        var index = seleccionarId is null ? 0 : datos.FindIndex(x => x.Id == seleccionarId.Value);
        _grid.Rows[Math.Max(0, index)].Selected = true;
    }

    private void SeleccionarFila()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Informativo fila) return;
        _seleccionado = _db.Informativo.Find(fila.Id);
        if (_seleccionado is null) return;
        _tipo.SelectedItem = _seleccionado.Tipo;
        _titulo.Text = _seleccionado.Titulo;
        _contenido.Text = _seleccionado.Contenido;
        _url.Text = _seleccionado.Url ?? string.Empty;
        _textoEnlace.Text = _seleccionado.TextoEnlace ?? string.Empty;
        _fecha.Value = _seleccionado.FechaPublicacion.ToDateTime(TimeOnly.MinValue);
        _vigente.Checked = _seleccionado.Vigente;
        _destacado.Checked = _seleccionado.Destacado;
        _estado.Text = string.Empty;
    }

    private void NuevoInformativo()
    {
        _seleccionado = null;
        _tipo.SelectedIndex = 0;
        _titulo.Clear();
        _contenido.Clear();
        _url.Clear();
        _textoEnlace.Clear();
        _fecha.Value = DateTime.Today;
        _vigente.Checked = true;
        _destacado.Checked = false;
        _estado.Text = "Nuevo informativo";
        _titulo.Focus();
    }

    private void GuardarInformativo()
    {
        _estado.Text = string.Empty;
        var titulo = _titulo.Text.Trim();
        var contenido = _contenido.Text.Trim();
        var url = _url.Text.Trim();
        var textoEnlace = _textoEnlace.Text.Trim();
        if (titulo.Length == 0)
        {
            MostrarError("El título es obligatorio.");
            return;
        }

        if (contenido.Length == 0)
        {
            MostrarError("El contenido es obligatorio.");
            return;
        }

        if (!Enum.TryParse<TipoInformativo>(_tipo.SelectedItem?.ToString(), out var tipo))
        {
            MostrarError("Seleccione un tipo válido.");
            return;
        }

        if (url.Length > 0 && (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            MostrarError("La URL debe comenzar con http:// o https://.");
            return;
        }

        if (url.Length > 0 && textoEnlace.Length == 0)
        {
            MostrarError("Indique el texto del enlace cuando exista una URL.");
            return;
        }

        var informativo = _seleccionado ?? new Informativo();
        informativo.Tipo = tipo;
        informativo.Titulo = titulo;
        informativo.Contenido = contenido;
        informativo.Url = url.Length == 0 ? null : url;
        informativo.TextoEnlace = url.Length == 0 ? null : textoEnlace;
        informativo.FechaPublicacion = DateOnly.FromDateTime(_fecha.Value.Date);
        informativo.Vigente = _vigente.Checked;
        informativo.Destacado = _destacado.Checked;
        if (_seleccionado is null) _db.Informativo.Add(informativo);
        _db.SaveChanges();
        _seleccionado = informativo;
        CargarInformativos(informativo.Id);
        _estado.ForeColor = Color.DarkGreen;
        _estado.Text = "Guardado correctamente.";
    }

    private void AlternarVigencia()
    {
        if (_seleccionado is null)
        {
            MostrarError("Seleccione un informativo.");
            return;
        }
        _seleccionado.Vigente = !_seleccionado.Vigente;
        _db.SaveChanges();
        CargarInformativos(_seleccionado.Id);
    }

    private void Archivar()
    {
        if (_seleccionado is null)
        {
            MostrarError("Seleccione un informativo.");
            return;
        }
        var confirmar = MessageBox.Show($"¿Archivar el informativo \"{_seleccionado.Titulo}\"?\nNo se eliminará de la base de datos.", "Confirmar archivo", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirmar != DialogResult.Yes) return;
        _seleccionado.Vigente = false;
        _db.SaveChanges();
        CargarInformativos(_seleccionado.Id);
    }

    private void MostrarError(string mensaje)
    {
        _estado.ForeColor = Color.DarkRed;
        _estado.Text = mensaje;
    }
}

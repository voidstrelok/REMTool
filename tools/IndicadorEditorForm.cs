using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RemTools;

/// <summary>New, self-contained CRUD UI for indicators and convenio assignments.</summary>
public sealed class IndicadorEditorForm : Form
{
    private readonly RemToolDataContext _db;
    private readonly ComboBox _filterYear = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _filterConvenio = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _name = new() { MaxLength = 16000 };
    private readonly NumericUpDown _year = Number(2000, 2100, 0);
    private readonly NumericUpDown _order = Number(0, 9999, 0);
    private readonly NumericUpDown _goal = Number(0, 9999999, 4);
    private readonly NumericUpDown _weight = Number(0, 9999, 4);
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _detail = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, MaxLength = 16000, Height = 90 };
    private readonly CheckBox _rate = new() { Text = "Es tasa", AutoSize = true };
    private readonly CheckBox _fixedDen = new() { Text = "Denominador fijo", AutoSize = true };
    private readonly CheckBox _monthly = new() { Text = "Mensual", AutoSize = true };
    private readonly CheckBox _octSep = new() { Text = "Período octubre-septiembre", AutoSize = true };
    private readonly CheckBox _collaborative = new() { Text = "Colaborativo", AutoSize = true };
    private readonly TextBox _formula = Multiline();
    private readonly TextBox _fixedFormula = Multiline();
    private readonly ComboBox _formulaTarget = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _newNodeType = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TreeView _ast = new() { Dock = DockStyle.Fill, HideSelection = false };
    private readonly ComboBox _astOperator = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _astNumber = Number(-9999999, 9999999, 4);
    private readonly TextBox _astPrestacion = new();
    private readonly NumericUpDown _astColumn = Number(1, 100, 0);
    private readonly ComboBox _astVariable = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private JsonNode? _astRoot;
    private bool _astLoading;
    private readonly CheckedListBox _convenios = new() { CheckOnClick = true, Dock = DockStyle.Fill };
    private Indicador? _selected;
    private List<Indicador> _items = [];
    private bool _loading;

    public IndicadorEditorForm(RemToolDataContext db)
    {
        _db = db;
        Text = "Administrar indicadores";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1050, 680);
        ClientSize = new Size(1220, 780);

        var split = new SplitContainer { Dock = DockStyle.Fill };
        Controls.Add(split);
        BuildList(split.Panel1);
        BuildEditor(split.Panel2);
        ConfigureSplitter(split, 340);
        Load += (_, _) => LoadData();
    }

    private void BuildList(Control parent)
    {
        var filter = new TableLayoutPanel { Dock = DockStyle.Top, Padding = new Padding(8), AutoSize = true, ColumnCount = 1 };
        parent.Controls.Add(_grid);
        parent.Controls.Add(filter);
        AddFilter(filter, "Año", _filterYear);
        AddFilter(filter, "Convenio", _filterConvenio);
        AddFilter(filter, "Buscar", _search);
        _filterYear.SelectedIndexChanged += (_, _) => { if (!_loading) LoadItems(); };
        _filterConvenio.SelectedIndexChanged += (_, _) => { if (!_loading) LoadItems(); };
        _search.TextChanged += (_, _) => { if (!_loading) LoadItems(); };

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Indicador.Id), HeaderText = "ID", Width = 48 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Indicador.Año), HeaderText = "Año", Width = 56 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Indicador.Orden), HeaderText = "Ord.", Width = 52 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Indicador.Nombre), HeaderText = "Indicador", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.SelectionChanged += (_, _) => SelectItem();
    }

    private void BuildEditor(Control parent)
    {
        var editor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        parent.Controls.Add(editor);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 5, 0, 0) };
        var save = new Button { Text = "Guardar cambios", AutoSize = true };
        var delete = new Button { Text = "Eliminar", AutoSize = true };
        var add = new Button { Text = "Nuevo indicador", AutoSize = true };
        buttons.Controls.AddRange([save, delete, add]);
        save.Click += (_, _) => Save();
        delete.Click += (_, _) => Delete();
        add.Click += (_, _) => Create();
        editor.Controls.Add(buttons);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var properties = new TabPage("Propiedades");
        var formulas = new TabPage("Fórmulas");
        var assignments = new TabPage("Convenios");
        tabs.TabPages.AddRange([properties, formulas, assignments]);
        editor.Controls.Add(tabs);

        var fields = new TableLayoutPanel { Dock = DockStyle.Top, Padding = new Padding(12), AutoSize = true, ColumnCount = 4 };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        properties.Controls.Add(fields);
        AddWide(fields, "Nombre", _name);
        AddPair(fields, "Año", _year, "Orden", _order);
        AddPair(fields, "Meta", _goal, "Peso", _weight);
        AddWide(fields, "Tipo", _type);
        AddWide(fields, "Detalle", _detail);
        AddChecks(fields, _rate, _fixedDen);
        AddChecks(fields, _monthly, _collaborative);
        AddWide(fields, "", _octSep);

        var formulaLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 4 };
        formulaLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formulaLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        formulaLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formulaLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        formulaLayout.Controls.Add(new Label { Text = "Fórmula (JSON o expresión heredada)", AutoSize = true }, 0, 0);
        formulaLayout.Controls.Add(_formula, 0, 1);
        formulaLayout.Controls.Add(new Label { Text = "Fórmula de denominador fijo (opcional)", AutoSize = true }, 0, 2);
        formulaLayout.Controls.Add(_fixedFormula, 0, 3);
        formulaLayout.Visible = false;
        BuildFormulaEditor(formulas);
        formulas.Controls.Add(formulaLayout);

        var convenioToolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 6, 0, 0) };
        var manage = new Button { Text = "Administrar convenios...", AutoSize = true };
        manage.Click += (_, _) => ManageConvenios();
        convenioToolbar.Controls.Add(manage);
        assignments.Controls.Add(_convenios);
        assignments.Controls.Add(convenioToolbar);
    }

    private void BuildFormulaEditor(Control parent)
    {
        _formulaTarget.Items.AddRange(["Fórmula principal", "Denominador fijo"]);
        _formulaTarget.SelectedIndex = 0;
        _newNodeType.Items.AddRange(["Operación", "Constante", "Prestación", "Variable"]);
        _newNodeType.SelectedIndex = 0;
        _astOperator.Items.AddRange(["sum", "sub", "mul", "div"]);
        _astVariable.Items.AddRange(["FONASA", "PRAIS", "POBLACION", "TOTAL"]);

        var split = new SplitContainer { Dock = DockStyle.Fill };
        ConfigureSplitter(split, 430);
        parent.Controls.Add(split);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        var addRoot = new Button { Text = "Nueva raíz", AutoSize = true };
        var addChild = new Button { Text = "Agregar hijo", AutoSize = true };
        var remove = new Button { Text = "Quitar nodo", AutoSize = true };
        toolbar.Controls.AddRange([new Label { Text = "Editar:", AutoSize = true, Margin = new Padding(3, 7, 3, 0) }, _formulaTarget,
            new Label { Text = "Nuevo nodo:", AutoSize = true, Margin = new Padding(10, 7, 3, 0) }, _newNodeType, addRoot, addChild, remove]);
        split.Panel1.Controls.Add(_ast);
        split.Panel1.Controls.Add(toolbar);

        var props = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 132, Padding = new Padding(6), ColumnCount = 4 };
        props.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); props.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        props.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); props.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        props.Controls.Add(new Label { Text = "Operador", AutoSize = true }, 0, 0); props.Controls.Add(_astOperator, 1, 0);
        props.Controls.Add(new Label { Text = "Número", AutoSize = true }, 2, 0); props.Controls.Add(_astNumber, 3, 0);
        props.Controls.Add(new Label { Text = "Prestación", AutoSize = true }, 0, 1); props.Controls.Add(_astPrestacion, 1, 1);
        props.Controls.Add(new Label { Text = "Columna", AutoSize = true }, 2, 1); props.Controls.Add(_astColumn, 3, 1);
        props.Controls.Add(new Label { Text = "Variable", AutoSize = true }, 0, 2); props.Controls.Add(_astVariable, 1, 2);
        props.SetColumnSpan(_astVariable, 3);
        split.Panel1.Controls.Add(props);

        var jsonTabs = new TabControl { Dock = DockStyle.Fill };
        var mainJson = new TabPage("JSON fórmula"); mainJson.Controls.Add(_formula);
        var fixedJson = new TabPage("JSON den. fijo"); fixedJson.Controls.Add(_fixedFormula);
        jsonTabs.TabPages.AddRange([mainJson, fixedJson]);
        split.Panel2.Controls.Add(jsonTabs);

        _formulaTarget.SelectedIndexChanged += (_, _) => RefreshAst();
        _formula.TextChanged += (_, _) => { if (!_astLoading && _formulaTarget.SelectedIndex == 0) RefreshAst(); };
        _fixedFormula.TextChanged += (_, _) => { if (!_astLoading && _formulaTarget.SelectedIndex == 1) RefreshAst(); };
        _ast.AfterSelect += (_, _) => LoadAstProperties();
        addRoot.Click += (_, _) => AddAstRoot(); addChild.Click += (_, _) => AddAstChild(); remove.Click += (_, _) => RemoveAstNode();
        _astOperator.SelectedIndexChanged += (_, _) => UpdateAstProperties();
        _astNumber.ValueChanged += (_, _) => UpdateAstProperties();
        _astPrestacion.TextChanged += (_, _) => UpdateAstProperties();
        _astColumn.ValueChanged += (_, _) => UpdateAstProperties();
        _astVariable.SelectedIndexChanged += (_, _) => UpdateAstProperties();
    }

    private TextBox ActiveFormulaText => _formulaTarget.SelectedIndex == 1 ? _fixedFormula : _formula;

    private void RefreshAst()
    {
        _astLoading = true; _ast.Nodes.Clear(); _astRoot = null;
        try
        {
            _astRoot = JsonNode.Parse(ActiveFormulaText.Text);
            if (_astRoot is JsonObject obj && obj["type"] != null) { var root = AstNode(obj); _ast.Nodes.Add(root); root.Expand(); _ast.SelectedNode = root; }
            else _ast.Nodes.Add("Sin AST: use 'Nueva raíz' para construir la fórmula.");
        }
        catch (JsonException) { _ast.Nodes.Add("JSON no válido: puede corregirlo a la derecha o crear una nueva raíz."); }
        _astLoading = false;
    }

    private static TreeNode AstNode(JsonObject obj)
    {
        var type = obj["type"]?.GetValue<string>() ?? "?";
        var text = type switch
        {
            "op" => $"Operación: {obj["op"]?.GetValue<string>() ?? "sum"}",
            "number" => $"Constante: {obj["value"]?.ToJsonString() ?? "0"}",
            "value" => $"Prestación: {obj["prestacion"]?.GetValue<string>() ?? ""} / columna {obj["columna"]?.ToJsonString() ?? "1"}",
            "variable" => $"Variable: {obj["name"]?.GetValue<string>() ?? ""}", _ => type
        };
        var node = new TreeNode(text) { Tag = obj };
        if (obj["args"] is JsonArray args) foreach (var child in args.OfType<JsonObject>()) node.Nodes.Add(AstNode(child));
        return node;
    }

    private JsonObject NewAstObject() => _newNodeType.SelectedIndex switch
    {
        0 => new JsonObject { ["type"] = "op", ["op"] = "sum", ["args"] = new JsonArray() },
        1 => new JsonObject { ["type"] = "number", ["value"] = 0 },
        2 => new JsonObject { ["type"] = "value", ["prestacion"] = "", ["columna"] = 1 },
        _ => new JsonObject { ["type"] = "variable", ["name"] = "FONASA", ["filters"] = new JsonObject() }
    };

    private void AddAstRoot() { _astRoot = NewAstObject(); WriteAst(); }
    private void AddAstChild()
    {
        if (_ast.SelectedNode?.Tag is not JsonObject parent || parent["type"]?.GetValue<string>() != "op") { MessageBox.Show(this, "Seleccione una operación para agregar un hijo.", "AST"); return; }
        var args = parent["args"] as JsonArray ?? new JsonArray(); parent["args"] = args; args.Add(NewAstObject()); WriteAst();
    }
    private void RemoveAstNode()
    {
        var node = _ast.SelectedNode;
        if (node?.Tag is not JsonObject child) return;
        if (node.Parent == null) _astRoot = null;
        else if (node.Parent.Tag is JsonObject parent && parent["args"] is JsonArray args) args.Remove(child);
        WriteAst();
    }

    private void LoadAstProperties()
    {
        if (_ast.SelectedNode?.Tag is not JsonObject obj) return;
        _astLoading = true;
        var type = obj["type"]?.GetValue<string>();
        _astOperator.Enabled = type == "op"; _astNumber.Enabled = type == "number"; _astPrestacion.Enabled = _astColumn.Enabled = type == "value"; _astVariable.Enabled = type == "variable";
        if (type == "op") _astOperator.SelectedItem = obj["op"]?.GetValue<string>() ?? "sum";
        if (type == "number") _astNumber.Value = Math.Clamp((decimal)(obj["value"]?.GetValue<double>() ?? 0), _astNumber.Minimum, _astNumber.Maximum);
        if (type == "value") { _astPrestacion.Text = obj["prestacion"]?.GetValue<string>() ?? ""; _astColumn.Value = obj["columna"]?.GetValue<int>() ?? 1; }
        if (type == "variable") _astVariable.SelectedItem = obj["name"]?.GetValue<string>() ?? "FONASA";
        _astLoading = false;
    }

    private void UpdateAstProperties()
    {
        if (_astLoading || _ast.SelectedNode?.Tag is not JsonObject obj) return;
        var type = obj["type"]?.GetValue<string>();
        if (type == "op") obj["op"] = _astOperator.SelectedItem?.ToString() ?? "sum";
        if (type == "number") obj["value"] = (double)_astNumber.Value;
        if (type == "value") { obj["prestacion"] = _astPrestacion.Text; obj["columna"] = (int)_astColumn.Value; }
        if (type == "variable") obj["name"] = _astVariable.SelectedItem?.ToString() ?? "FONASA";
        _ast.SelectedNode.Text = AstNode(obj).Text;
        WriteAst(rebuild: false);
    }

    private void WriteAst(bool rebuild = true)
    {
        _astLoading = true;
        ActiveFormulaText.Text = _astRoot?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "{}";
        _astLoading = false;
        if (rebuild) RefreshAst();
    }

    private static void ConfigureSplitter(SplitContainer split, int preferredDistance)
    {
        var configured = false;
        split.SizeChanged += (_, _) =>
        {
            if (configured || split.Width <= split.SplitterWidth + 80) return;
            split.SplitterDistance = Math.Clamp(preferredDistance, 40, split.Width - split.SplitterWidth - 40);
            configured = true;
        };
    }

    private static void AddFilter(TableLayoutPanel layout, string text, Control control)
    {
        layout.Controls.Add(new Label { Text = text, AutoSize = true }, 0, layout.RowCount++);
        control.Dock = DockStyle.Top;
        layout.Controls.Add(control, 0, layout.RowCount++);
    }

    private static void AddPair(TableLayoutPanel layout, string label1, Control control1, string label2, Control control2)
    {
        var row = layout.RowCount++;
        layout.Controls.Add(new Label { Text = label1, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(control1, 1, row);
        layout.Controls.Add(new Label { Text = label2, AutoSize = true, Anchor = AnchorStyles.Left }, 2, row);
        layout.Controls.Add(control2, 3, row);
        control1.Dock = control2.Dock = DockStyle.Fill;
    }

    private static void AddWide(TableLayoutPanel layout, string label, Control control)
    {
        var row = layout.RowCount++;
        if (!string.IsNullOrEmpty(label)) layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(control, string.IsNullOrEmpty(label) ? 0 : 1, row);
        layout.SetColumnSpan(control, string.IsNullOrEmpty(label) ? 4 : 3);
        control.Dock = DockStyle.Fill;
    }

    private static void AddChecks(TableLayoutPanel layout, CheckBox first, CheckBox second)
    {
        var row = layout.RowCount++;
        layout.Controls.Add(first, 0, row); layout.SetColumnSpan(first, 2);
        layout.Controls.Add(second, 2, row); layout.SetColumnSpan(second, 2);
    }

    private static NumericUpDown Number(decimal min, decimal max, int decimals) => new() { Minimum = min, Maximum = max, DecimalPlaces = decimals, ThousandsSeparator = true };
    private static TextBox Multiline() => new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 10F) };

    private void LoadData(int? selectedId = null)
    {
        _loading = true;
        var years = _db.Indicador.Select(i => i.Año).Distinct().OrderBy(y => y).ToList();
        _filterYear.DataSource = new List<Item> { new("Todos", null) }.Concat(years.Select(y => new Item(y.ToString(), y))).ToList();
        _filterYear.DisplayMember = nameof(Item.Text);
        var convenios = _db.Convenio.OrderBy(c => c.Nombre).ToList();
        _filterConvenio.DataSource = new List<Item> { new("Todos", null) }.Concat(convenios.Select(c => new Item(c.Nombre, c.Id))).ToList();
        _filterConvenio.DisplayMember = nameof(Item.Text);
        _convenios.Items.Clear();
        foreach (var convenio in convenios) _convenios.Items.Add(new Item(convenio.Nombre, convenio.Id));
        var types = _db.TipoIndicador.OrderBy(t => t.Nombre).ToList();
        _type.DataSource = new List<Item> { new("Sin tipo", null) }.Concat(types.Select(t => new Item(t.Nombre, t.Id))).ToList();
        _type.DisplayMember = nameof(Item.Text);
        _loading = false;
        LoadItems(selectedId);
    }

    private void LoadItems(int? selectedId = null)
    {
        var query = _db.Indicador.AsQueryable();
        if (_filterYear.SelectedItem is Item { Value: int year }) query = query.Where(i => i.Año == year);
        if (_filterConvenio.SelectedItem is Item { Value: int convenio }) query = query.Where(i => i.IndicadorConvenios.Any(x => x.id_convenio == convenio));
        var term = _search.Text.Trim();
        if (!string.IsNullOrEmpty(term)) query = query.Where(i => EF.Functions.ILike(i.Nombre, $"%{term}%"));
        _items = query.OrderBy(i => i.Año).ThenBy(i => i.Orden).ThenBy(i => i.Id).ToList();
        _grid.DataSource = null; _grid.DataSource = _items;
        var index = selectedId.HasValue ? _items.FindIndex(i => i.Id == selectedId.Value) : -1;
        if (index >= 0) { _grid.ClearSelection(); _grid.Rows[index].Selected = true; }
        else Clear();
    }

    private void SelectItem()
    {
        if (_loading) return;
        _selected = _grid.SelectedRows.Count == 1 ? _grid.SelectedRows[0].DataBoundItem as Indicador : null;
        if (_selected == null) { Clear(); return; }
        _loading = true;
        _name.Text = _selected.Nombre; _year.Value = _selected.Año; _order.Value = _selected.Orden;
        _goal.Value = (decimal)_selected.Meta; _weight.Value = (decimal)_selected.Peso; _detail.Text = _selected.Detalle ?? "";
        _rate.Checked = _selected.IsTasa; _fixedDen.Checked = _selected.IsDenFijo; _monthly.Checked = _selected.Mensual;
        _octSep.Checked = _selected.EsPeriodoOctubreSep; _collaborative.Checked = _selected.IsColaborativo;
        _formula.Text = _selected.Formula; _fixedFormula.Text = _selected.FormulaDenFijo ?? ""; SelectCombo(_type, _selected.Tipoindicador);
        var assigned = _db.IndicadorConvenio.Where(x => x.id_indicador == _selected.Id).Select(x => x.id_convenio).ToHashSet();
        for (var i = 0; i < _convenios.Items.Count; i++) _convenios.SetItemChecked(i, _convenios.Items[i] is Item item && item.Value.HasValue && assigned.Contains(item.Value.Value));
        _loading = false;
    }

    private void Clear()
    {
        _selected = null; _loading = true;
        _name.Clear(); _year.Value = DateTime.Today.Year; _order.Value = _goal.Value = _weight.Value = 0; _detail.Clear();
        _rate.Checked = _fixedDen.Checked = _monthly.Checked = _octSep.Checked = _collaborative.Checked = false; _formula.Clear(); _fixedFormula.Clear(); SelectCombo(_type, null);
        for (var i = 0; i < _convenios.Items.Count; i++) _convenios.SetItemChecked(i, false);
        _loading = false;
    }

    private static void SelectCombo(ComboBox combo, int? value)
    {
        for (var i = 0; i < combo.Items.Count; i++) if (combo.Items[i] is Item item && item.Value == value) { combo.SelectedIndex = i; return; }
        combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
    }

    private void Create()
    {
        var year = _filterYear.SelectedItem is Item { Value: int value } ? value : DateTime.Today.Year;
        var indicator = new Indicador { Nombre = "Nuevo indicador", Año = year, Orden = 999, Formula = "{}" };
        IndicatorSequenceSynchronizer.ForIndicador(_db);
        _db.Indicador.Add(indicator); _db.SaveChanges(); LoadData(indicator.Id); _name.Focus(); _name.SelectAll();
    }

    private void Delete()
    {
        if (_selected == null) { MessageBox.Show(this, "Seleccione un indicador para eliminar.", "Aviso"); return; }
        if (MessageBox.Show(this, $"¿Eliminar #{_selected.Id}: {_selected.Nombre}?", "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _db.Indicador.Remove(_selected); _db.SaveChanges(); LoadItems();
    }

    private void Save()
    {
        if (_selected == null) { MessageBox.Show(this, "Seleccione un indicador para guardar.", "Aviso"); return; }
        var name = _name.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show(this, "El nombre es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); _name.Focus(); return; }
        _selected.Nombre = name; _selected.Año = (int)_year.Value; _selected.Orden = (int)_order.Value; _selected.Meta = (float)_goal.Value; _selected.Peso = (float)_weight.Value;
        _selected.Detalle = string.IsNullOrWhiteSpace(_detail.Text) ? null : _detail.Text.Trim(); _selected.Tipoindicador = _type.SelectedItem is Item { Value: int type } ? type : null;
        _selected.IsTasa = _rate.Checked; _selected.IsDenFijo = _fixedDen.Checked; _selected.Mensual = _monthly.Checked; _selected.EsPeriodoOctubreSep = _octSep.Checked; _selected.IsColaborativo = _collaborative.Checked;
        _selected.Formula = string.IsNullOrWhiteSpace(_formula.Text) ? "{}" : _formula.Text.Trim();
        _selected.FormulaDenFijo = string.IsNullOrWhiteSpace(_fixedFormula.Text) ? null : _fixedFormula.Text.Trim();
        var wanted = _convenios.CheckedItems.OfType<Item>().Where(x => x.Value.HasValue).Select(x => x.Value!.Value).ToHashSet();
        var current = _db.IndicadorConvenio.Where(x => x.id_indicador == _selected.Id).ToList();
        _db.IndicadorConvenio.RemoveRange(current.Where(x => !wanted.Contains(x.id_convenio)));
        var existing = current.Select(x => x.id_convenio).ToHashSet();
        if (wanted.Except(existing).Any()) IndicatorSequenceSynchronizer.ForIndicadorConvenio(_db);
        foreach (var convenioId in wanted.Where(id => !existing.Contains(id))) _db.IndicadorConvenio.Add(new IndicadorConvenio { id_indicador = _selected.Id, id_convenio = convenioId });
        _db.SaveChanges(); LoadData(_selected.Id); MessageBox.Show(this, "Indicador guardado.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ManageConvenios()
    {
        using var form = new ConvenioEditorForm(_db); form.ShowDialog(this); LoadData(_selected?.Id);
    }

    private sealed record Item(string Text, int? Value) { public override string ToString() => Text; }
}

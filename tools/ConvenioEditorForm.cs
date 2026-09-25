using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTools;

/// <summary>Maintains convenios and their many-to-many indicator assignments.</summary>
public sealed class ConvenioEditorForm : Form
{
    private readonly RemToolDataContext _db;
    private readonly DataGridView _grid = new();
    private readonly TextBox _name = new();
    private readonly CheckedListBox _indicators = new();
    private readonly Button _newButton = new() { Text = "Nuevo" };
    private readonly Button _deleteButton = new() { Text = "Eliminar" };
    private readonly Button _saveButton = new() { Text = "Guardar cambios" };
    private List<Convenio> _convenios = [];
    private Convenio? _selected;
    private bool _loading;

    public ConvenioEditorForm(RemToolDataContext db)
    {
        _db = db;
        Text = "Administrar convenios";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(680, 460);
        ClientSize = new Size(800, 560);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(split);
        ConfigureSplitter(split, 260);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Convenio.Id), HeaderText = "ID", Width = 50 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Convenio.Nombre), HeaderText = "Nombre", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.SelectionChanged += (_, _) => SelectConvenio();
        split.Panel1.Controls.Add(_grid);

        var leftButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(6) };
        leftButtons.Controls.AddRange([_newButton, _deleteButton]);
        split.Panel1.Controls.Add(leftButtons);

        var details = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        split.Panel2.Controls.Add(details);
        details.Controls.Add(_indicators);
        details.Controls.Add(_saveButton);
        details.Controls.Add(_name);
        details.Controls.Add(new Label { Text = "Nombre:", AutoSize = true, Location = new Point(10, 12) });
        details.Controls.Add(new Label { Text = "Indicadores asociados:", AutoSize = true, Location = new Point(10, 75) });

        _name.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _name.Location = new Point(10, 34);
        _name.MaxLength = 16000;
        _name.Size = new Size(500, 28);

        _indicators.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _indicators.CheckOnClick = true;
        _indicators.Location = new Point(10, 98);
        _indicators.Size = new Size(500, 390);

        _saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _saveButton.Location = new Point(365, 505);
        _saveButton.Size = new Size(145, 32);

        _newButton.Click += (_, _) => NewConvenio();
        _deleteButton.Click += (_, _) => DeleteConvenio();
        _saveButton.Click += (_, _) => SaveConvenio();
        Load += (_, _) => LoadData();
    }

    private void LoadData(int? selectId = null)
    {
        _loading = true;
        _convenios = _db.Convenio.OrderBy(c => c.Nombre).ToList();
        _grid.DataSource = null;
        _grid.DataSource = _convenios;

        _indicators.Items.Clear();
        foreach (var indicator in _db.Indicador.OrderBy(i => i.Año).ThenBy(i => i.Orden).ThenBy(i => i.Id).ToList())
            _indicators.Items.Add(new IndicatorItem(indicator.Id, $"{indicator.Año} · #{indicator.Id} · {indicator.Nombre}"));

        _loading = false;
        var index = selectId.HasValue ? _convenios.FindIndex(c => c.Id == selectId.Value) : -1;
        if (index >= 0)
        {
            _grid.ClearSelection();
            _grid.Rows[index].Selected = true;
        }
        else
        {
            _selected = null;
            ClearDetails();
        }
    }

    private void SelectConvenio()
    {
        if (_loading) return;
        _selected = _grid.SelectedRows.Count == 1 ? _grid.SelectedRows[0].DataBoundItem as Convenio : null;
        if (_selected == null)
        {
            ClearDetails();
            return;
        }

        _loading = true;
        _name.Text = _selected.Nombre;
        var assigned = _db.IndicadorConvenio.Where(x => x.id_convenio == _selected.Id)
            .Select(x => x.id_indicador).ToHashSet();
        for (var index = 0; index < _indicators.Items.Count; index++)
            _indicators.SetItemChecked(index, _indicators.Items[index] is IndicatorItem item && assigned.Contains(item.Id));
        _loading = false;
    }

    private void ClearDetails()
    {
        _loading = true;
        _name.Clear();
        for (var index = 0; index < _indicators.Items.Count; index++) _indicators.SetItemChecked(index, false);
        _loading = false;
    }

    private void NewConvenio()
    {
        IndicatorSequenceSynchronizer.ForConvenio(_db);
        var existingNames = _db.Convenio.Select(c => c.Nombre).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseName = "Nuevo convenio";
        var name = baseName;
        var suffix = 2;
        while (existingNames.Contains(name)) name = $"{baseName} {suffix++}";
        var convenio = new Convenio { Nombre = name };
        _db.Convenio.Add(convenio);
        _db.SaveChanges();
        LoadData(convenio.Id);
        _name.Focus();
        _name.SelectAll();
    }

    private void DeleteConvenio()
    {
        if (_selected == null)
        {
            MessageBox.Show(this, "Seleccione un convenio para eliminar.", "Aviso");
            return;
        }
        if (MessageBox.Show(this, $"¿Eliminar el convenio '{_selected.Nombre}'?", "Confirmar eliminación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        _db.Convenio.Remove(_selected);
        _db.SaveChanges();
        _selected = null;
        LoadData();
    }

    private void SaveConvenio()
    {
        if (_selected == null)
        {
            MessageBox.Show(this, "Seleccione un convenio para guardar.", "Aviso");
            return;
        }
        var name = _name.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "El nombre del convenio es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _name.Focus();
            return;
        }
        if (_db.Convenio.Any(c => c.Id != _selected.Id && c.Nombre == name))
        {
            MessageBox.Show(this, "Ya existe un convenio con ese nombre.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _selected.Nombre = name;
        var selectedIds = _indicators.CheckedItems.OfType<IndicatorItem>().Select(i => i.Id).ToHashSet();
        var current = _db.IndicadorConvenio.Where(x => x.id_convenio == _selected.Id).ToList();
        _db.IndicadorConvenio.RemoveRange(current.Where(x => !selectedIds.Contains(x.id_indicador)));
        var currentIds = current.Select(x => x.id_indicador).ToHashSet();
        if (selectedIds.Except(currentIds).Any()) IndicatorSequenceSynchronizer.ForIndicadorConvenio(_db);
        foreach (var indicatorId in selectedIds.Where(id => !currentIds.Contains(id)))
            _db.IndicadorConvenio.Add(new IndicadorConvenio { id_convenio = _selected.Id, id_indicador = indicatorId });

        _db.SaveChanges();
        LoadData(_selected.Id);
        MessageBox.Show(this, "Convenio guardado.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private sealed record IndicatorItem(int Id, string Text)
    {
        public override string ToString() => Text;
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
}

internal static class IndicatorSequenceSynchronizer
{
    // The deployed database uses meta_id_seq for indicador, but does not have
    // the legacy meta table present in the generated model.
    public static void ForIndicador(RemToolDataContext db) => db.Database.ExecuteSqlRaw("""
        SELECT setval('"REMTool".meta_id_seq',
            GREATEST(
                COALESCE((SELECT MAX(id) FROM "REMTool".indicador), 0),
                1),
            true);
        """);

    public static void ForConvenio(RemToolDataContext db) => db.Database.ExecuteSqlRaw("""
        SELECT setval('"REMTool".convenio_id_seq',
            GREATEST(COALESCE((SELECT MAX(id) FROM "REMTool".convenio), 0), 1),
            true);
        """);

    public static void ForIndicadorConvenio(RemToolDataContext db) => db.Database.ExecuteSqlRaw("""
        SELECT setval('"REMTool".indicador_convenio_id_seq',
            GREATEST(COALESCE((SELECT MAX(id) FROM "REMTool".indicador_convenio), 0), 1),
            true);
        """);
}

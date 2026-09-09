using System.Diagnostics;
using System.Text.Json;
using RemTools.Consolidados;
using RemTool.Shared;

namespace RemTools;

public sealed class GenerarConsolidadosForm : Form
{
    private readonly ConsolidadoDataReader reader;
    private readonly NumericUpDown year = new() { Minimum = 2000, Maximum = 2100, Value = DateTime.Today.Year, Dock = DockStyle.Fill };
    private readonly ComboBox version = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox template = new() { Dock = DockStyle.Fill };
    private readonly TextBox output = new() { Dock = DockStyle.Fill };
    private readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Button generate = new() { Text = "Generar Serie A", AutoSize = true, Enabled = false };
    private readonly Button validate = new() { Text = "Validar configuración", AutoSize = true, Enabled = false };
    private readonly Button cancel = new() { Text = "Cancelar", AutoSize = true, Enabled = false };
    private readonly Button open = new() { Text = "Abrir carpeta", AutoSize = true };
    private readonly ProgressBar progressBar = new() { Dock = DockStyle.Fill, Style = ProgressBarStyle.Marquee, Visible = false };
    private readonly TableLayoutPanel fields = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 4, AutoSize = true };
    private CancellationTokenSource? operation;
    private bool closeWhenFinished;
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RemTools", "consolidados.json");
    private sealed record Settings(int Year, int VersionId, string Template, string Output);

    public GenerarConsolidadosForm(Func<RemToolDataContext> createContext)
    {
        reader = new ConsolidadoDataReader(createContext);
        Text = "Generar consolidados Excel";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 560); Size = new Size(960, 650);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 6 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.Controls.Add(new Label { Text = "Serie A · Todos los establecimientos y meses disponibles del año.\nEl archivo permite filtrar sin conexión. La subida al servidor es manual.", Dock = DockStyle.Fill, AutoSize = true }, 0, 0);
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        for (int i = 0; i < 4; i++) fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        AddField(0, "Año de los reportes", year);
        AddField(1, "Versión de la plantilla", version);
        AddField(2, "Plantilla consolidado", template);
        AddField(3, "Carpeta de publicación", output);
        var chooseTemplate = new Button { Text = "Examinar…", Dock = DockStyle.Fill };
        chooseTemplate.Click += (_, _) => { using var dialog = new OpenFileDialog { Filter = "Consolidado Excel (*.xlsx)|*.xlsx", CheckFileExists = true }; if (dialog.ShowDialog(this) == DialogResult.OK) template.Text = dialog.FileName; };
        var chooseOutput = new Button { Text = "Examinar…", Dock = DockStyle.Fill };
        chooseOutput.Click += (_, _) => { using var dialog = new FolderBrowserDialog { Description = "Carpeta local que contiene catalogo.json y las publicaciones" }; if (dialog.ShowDialog(this) == DialogResult.OK) output.Text = dialog.SelectedPath; };
        fields.Controls.Add(chooseTemplate, 2, 2); fields.Controls.Add(chooseOutput, 2, 3);
        layout.Controls.Add(fields, 0, 1);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        buttons.Controls.AddRange([validate, generate, cancel, open]); layout.Controls.Add(buttons, 0, 2);
        layout.Controls.Add(progressBar, 0, 3); layout.Controls.Add(log, 0, 4);
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Publicación: suba primero el Excel manteniendo sus subcarpetas y reemplace catalogo.json al final.", AutoSize = true }, 0, 5);
        Controls.Add(layout);
        cancel.Click += (_, _) => { operation?.Cancel(); cancel.Enabled = false; Append("Cancelación solicitada. Se detendrá al terminar la operación actual."); };
        generate.Click += async (_, _) => await RunAsync(false);
        validate.Click += async (_, _) => await RunAsync(true);
        open.Click += (_, _) => { if (Directory.Exists(output.Text)) Process.Start(new ProcessStartInfo(output.Text) { UseShellExecute = true }); };
        Shown += async (_, _) => await LoadAsync();
        FormClosing += (_, e) => { if (operation != null) { e.Cancel = true; closeWhenFinished = true; operation.Cancel(); } };
    }

    private void AddField(int row, string label, Control control)
    {
        fields.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        fields.Controls.Add(control, 1, row);
    }

    private async Task LoadAsync()
    {
        operation = new CancellationTokenSource();
        try
        {
            Settings? settings = null;
            if (File.Exists(settingsPath))
            {
                try { settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath)); }
                catch (JsonException) { Append("La configuración guardada no es válida. Seleccione nuevamente las rutas."); }
            }
            if (settings != null)
            {
                year.Value = Math.Clamp(settings.Year, 2000, 2100); template.Text = settings.Template; output.Text = settings.Output;
            }
            Append("Cargando versiones de Serie A...");
            var versions = await reader.GetVersionsAsync(operation.Token);
            version.DataSource = versions;
            if (settings != null) version.SelectedItem = versions.FirstOrDefault(v => v.Id == settings.VersionId) ?? versions.FirstOrDefault();
            generate.Enabled = validate.Enabled = versions.Count > 0;
            Append(versions.Count == 0 ? "No hay versiones Serie A en la base." : "Seleccione el consolidado original y la versión que corresponde a su estructura.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Append("No se pudieron cargar las versiones: " + ex.Message); }
        finally { operation.Dispose(); operation = null; if (closeWhenFinished) Close(); }
    }

    private async Task RunAsync(bool validateOnly)
    {
        if (version.SelectedItem is not VersionOption selected) return;
        var request = new GenerationRequest((int)year.Value, selected.Id, template.Text.Trim(), output.Text.Trim());
        operation = new CancellationTokenSource(); var token = operation.Token;
        fields.Enabled = generate.Enabled = validate.Enabled = false; cancel.Enabled = true; progressBar.Visible = true;
        var progress = new Progress<string>(Append);
        try
        {
            if (!File.Exists(request.TemplatePath)) throw new InvalidOperationException("Seleccione una plantilla existente.");
            if (string.IsNullOrWhiteSpace(request.OutputDirectory)) throw new InvalidOperationException("Seleccione la carpeta de publicación.");
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(new Settings(request.Year, selected.Id, request.TemplatePath, request.OutputDirectory)));
            if (validateOnly)
            {
                await Task.Run(async () =>
                {
                    using var prepared = ConsolidadoTemplate.Prepare(request.TemplatePath, token);
                    _ = ConsolidadoCatalogWriter.Read(request.OutputDirectory);
                    var snapshot = await reader.ReadAsync(request, progress, token);
                    var data = ConsolidadoMapper.Map(snapshot, token);
                    ((IProgress<string>)progress).Report($"Datos compatibles: {snapshot.Reports.Count:N0} reportes, {data.Rows.Count:N0} filas con actividad. La validación de fórmulas se realiza al generar.");
                }, token);
            }
            else
            {
                var result = await Task.Run(() => new ConsolidadoGenerationService(reader).GenerateAsync(request, progress, token), token);
                Append("Archivo: " + result.FilePath);
                foreach (var warning in result.Warnings) Append(warning);
            }
        }
        catch (OperationCanceledException) { Append("Operación cancelada. Se conserva el catálogo anterior."); }
        catch (Exception ex) { Append("No se completó la operación: " + ex.Message); }
        finally
        {
            operation.Dispose(); operation = null; fields.Enabled = generate.Enabled = validate.Enabled = true;
            cancel.Enabled = false; progressBar.Visible = false;
            if (closeWhenFinished) Close();
        }
    }

    private void Append(string message) => log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
}

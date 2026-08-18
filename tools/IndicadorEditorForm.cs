using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RemTools
{
    public partial class IndicadorEditorForm : Form
    {
        private readonly RemToolDataContext _bdd;
        private List<Indicador> _indicadores = [];
        private Indicador? _sel;
        private JsonNode? _formulaRoot;
        private JsonNode? _formulaDenFijoRoot;
        private bool _loading;
        private bool _denFijoHabilitado;
        private string _activeFormulaSource = "formula"; // "formula" | "denFijo"

        public IndicadorEditorForm(RemToolDataContext dbContext)
        {
            InitializeComponent();
            _bdd = dbContext;
        }

        private TreeView ActiveTree => _activeFormulaSource == "denFijo" ? denFijo_tv : formula_tv;

        private JsonNode? ActiveFormulaRoot
        {
            get => _activeFormulaSource == "denFijo" ? _formulaDenFijoRoot : _formulaRoot;
            set
            {
                if (_activeFormulaSource == "denFijo") _formulaDenFijoRoot = value;
                else _formulaRoot = value;
            }
        }

        // ── Load ─────────────────────────────────────────────────────

        private void IndicadorEditorForm_Load(object sender, EventArgs e)
        {
            CargarFiltros();
        }

        private void CargarFiltros()
        {
            _loading = true;

            var años = _bdd.Indicador.Select(i => i.Año).Distinct().OrderBy(a => a).ToList();
            año_cmb.Items.Clear();
            año_cmb.Items.Add("(Todos)");
            foreach (var a in años) año_cmb.Items.Add(a);
            año_cmb.SelectedIndex = año_cmb.Items.Count > 1 ? 1 : 0;

            var convenios = _bdd.Convenio.OrderBy(c => c.Nombre).ToList();
            var convenioItems = new List<ComboItem> { new("(Todos)", null) };
            convenioItems.AddRange(convenios.Select(c => new ComboItem(c.Nombre, c.Id)));
            convenio_cmb.DataSource = convenioItems;
            convenio_cmb.DisplayMember = "Text";
            convenio_cmb.SelectedIndex = 0;

            var tipos = _bdd.TipoIndicador.OrderBy(t => t.Nombre).ToList();
            var tipoItems = new List<ComboItem> { new("(Ninguno)", null) };
            tipoItems.AddRange(tipos.Select(t => new ComboItem(t.Nombre, t.Id)));
            tipo_cmb.DataSource = tipoItems;
            tipo_cmb.DisplayMember = "Text";
            tipo_cmb.SelectedIndex = 0;

            _loading = false;
            CargarIndicadores();
        }

        private void CargarIndicadores()
        {
            var query = _bdd.Indicador.AsQueryable();

            if (año_cmb.SelectedItem is int año)
                query = query.Where(i => i.Año == año);

            if (convenio_cmb.SelectedItem is ComboItem { IntValue: int convId })
                query = query.Where(i => i.IndicadorConvenios.Any(ic => ic.id_convenio == convId));

            _indicadores = query
                .OrderBy(i => i.Año)
                .ThenBy(i => i.Orden)
                .ThenBy(i => i.Id)
                .ToList();

            indicadores_dgv.DataSource = null;
            indicadores_dgv.DataSource = _indicadores;
        }

        // ── Selection ────────────────────────────────────────────────

        private void indicadores_dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (indicadores_dgv.SelectedRows.Count == 0)
            {
                _sel = null;
                LimpiarProps();
                return;
            }
            _sel = indicadores_dgv.SelectedRows[0].DataBoundItem as Indicador;
            CargarProps();
        }

        private void LimpiarProps()
        {
            _loading = true;
            nombre_txt.Text = string.Empty;
            meta_num.Value = 0;
            peso_num.Value = 0;
            orden_num.Value = 0;
            detalle_txt.Text = string.Empty;
            isTasa_chk.Checked = false;
            isDenFijo_chk.Checked = false;
            mensual_chk.Checked = false;
            periodoOct_chk.Checked = false;
            colaborativo_chk.Checked = false;
            tipo_cmb.SelectedIndex = 0;
            _loading = false;

            _formulaRoot = null;
            _formulaDenFijoRoot = null;
            formula_tv.Nodes.Clear();
            denFijo_tv.Nodes.Clear();
            formula_json_txt.Text = string.Empty;
            MostrarPanelNodo(null);
            _denFijoHabilitado = false;
            tab_den_fijo.Enabled = false;
        }

        private void CargarProps()
        {
            if (_sel == null) return;
            var ind = _sel;

            _loading = true;
            nombre_txt.Text = ind.Nombre;
            meta_num.Value = (decimal)ind.Meta;
            peso_num.Value = (decimal)ind.Peso;
            orden_num.Value = ind.Orden;
            detalle_txt.Text = ind.Detalle ?? string.Empty;
            isTasa_chk.Checked = ind.IsTasa;
            isDenFijo_chk.Checked = ind.IsDenFijo;
            mensual_chk.Checked = ind.Mensual;
            periodoOct_chk.Checked = ind.EsPeriodoOctubreSep;
            colaborativo_chk.Checked = ind.IsColaborativo;

            tipo_cmb.SelectedIndex = 0;
            if (ind.Tipoindicador.HasValue)
            {
                for (int i = 0; i < tipo_cmb.Items.Count; i++)
                {
                    if (tipo_cmb.Items[i] is ComboItem item && item.IntValue == ind.Tipoindicador.Value)
                    {
                        tipo_cmb.SelectedIndex = i;
                        break;
                    }
                }
            }
            _loading = false;

            _denFijoHabilitado = ind.IsDenFijo;
            tab_den_fijo.Enabled = ind.IsDenFijo;

            _formulaRoot = ParseJsonSafe(ind.Formula);
            _formulaDenFijoRoot = ParseJsonSafe(ind.FormulaDenFijo);
            PopularTreeView(formula_tv, _formulaRoot);
            PopularTreeView(denFijo_tv, _formulaDenFijoRoot);
            MostrarPanelNodo(null);

            if (tabs_formula.SelectedTab == tab_json)
                SincronizarJsonText();
        }

        // ── JSON sync ────────────────────────────────────────────────

        private static JsonNode? ParseJsonSafe(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonNode.Parse(json); }
            catch { return null; }
        }

        private void SincronizarJsonText()
        {
            var root = ActiveFormulaRoot;
            formula_json_txt.Text = root == null
                ? string.Empty
                : root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private void tabs_formula_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabs_formula.SelectedTab == tab_formula)
                _activeFormulaSource = "formula";
            else if (tabs_formula.SelectedTab == tab_den_fijo)
                _activeFormulaSource = "denFijo";
            else if (tabs_formula.SelectedTab == tab_json)
                SincronizarJsonText();
        }

        private void tabs_formula_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage == tab_den_fijo && !_denFijoHabilitado)
                e.Cancel = true;
        }

        private void aplicar_json_btn_Click(object sender, EventArgs e)
        {
            var text = formula_json_txt.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                ActiveFormulaRoot = null;
                PopularTreeView(ActiveTree, null);
                tabs_formula.SelectedTab = _activeFormulaSource == "denFijo" ? tab_den_fijo : tab_formula;
                return;
            }

            try
            {
                var node = JsonNode.Parse(text);
                ActiveFormulaRoot = node;
                PopularTreeView(ActiveTree, node);
                tabs_formula.SelectedTab = _activeFormulaSource == "denFijo" ? tab_den_fijo : tab_formula;
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON inválido:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── TreeView build ───────────────────────────────────────────

        private static TreeNode BuildTreeNode(JsonNode node)
        {
            if (node is not JsonObject obj)
                return new TreeNode($"? {node.ToJsonString()}") { Tag = node };

            string tipo = obj["type"]?.GetValue<string>() ?? "?";
            string label = tipo switch
            {
                "op"       => $"[{OpSymbol(obj["op"]?.GetValue<string>() ?? "")}] {obj["op"]?.GetValue<string>()}",
                "number"   => $"# {obj["value"]?.ToJsonString()}",
                "value"    => $"val {obj["prestacion"]?.GetValue<string>()} col {obj["columna"]?.GetValue<int>()}",
                "variable" => $"var {obj["name"]?.GetValue<string>()}{FiltersLabel(obj["filters"] as JsonObject)}",
                _          => tipo
            };

            var tn = new TreeNode(label) { Tag = node };

            if (tipo == "op" && obj["args"] is JsonArray args)
                foreach (var child in args)
                    if (child != null) tn.Nodes.Add(BuildTreeNode(child));

            return tn;
        }

        private static string OpSymbol(string op) => op switch
        {
            "sum" => "+", "sub" => "−", "mul" => "×", "div" => "÷", _ => op
        };

        private static string FiltersLabel(JsonObject? filters)
        {
            if (filters == null || filters.Count == 0) return string.Empty;
            var parts = filters.Select(kv => $"{kv.Key}={kv.Value?.ToJsonString()}");
            return $" [{string.Join(", ", parts)}]";
        }

        private static void PopularTreeView(TreeView tv, JsonNode? root)
        {
            tv.Nodes.Clear();
            if (root != null)
            {
                tv.Nodes.Add(BuildTreeNode(root));
                tv.ExpandAll();
            }
        }

        private static void RefreshTreeNodeLabel(TreeNode tn)
        {
            if (tn.Tag is not JsonNode node) return;
            tn.Text = BuildTreeNode(node).Text;
        }

        // ── Node property panels ─────────────────────────────────────

        private void formula_tv_AfterSelect(object sender, TreeViewEventArgs e)
            => MostrarPanelNodo(e.Node);

        private void denFijo_tv_AfterSelect(object sender, TreeViewEventArgs e)
            => MostrarPanelNodo(e.Node);

        private void MostrarPanelNodo(TreeNode? tn)
        {
            pnl_empty.Visible = false;
            pnl_op.Visible = false;
            pnl_number.Visible = false;
            pnl_value.Visible = false;
            pnl_variable.Visible = false;

            if (tn?.Tag is not JsonObject obj)
            {
                pnl_empty.Visible = true;
                return;
            }

            string tipo = obj["type"]?.GetValue<string>() ?? "";
            _loading = true;
            switch (tipo)
            {
                case "op":
                    pnl_op.Visible = true;
                    op_cmb.SelectedItem = obj["op"]?.GetValue<string>() ?? "sum";
                    break;
                case "number":
                    pnl_number.Visible = true;
                    val_num.Value = Math.Clamp(
                        (decimal)(obj["value"]?.GetValue<double>() ?? 0),
                        val_num.Minimum, val_num.Maximum);
                    break;
                case "value":
                    pnl_value.Visible = true;
                    prest_txt.Text = obj["prestacion"]?.GetValue<string>() ?? string.Empty;
                    col_num.Value = Math.Clamp(obj["columna"]?.GetValue<int>() ?? 1, 1, 100);
                    break;
                case "variable":
                    pnl_variable.Visible = true;
                    varname_cmb.SelectedItem = obj["name"]?.GetValue<string>() ?? "FONASA";
                    CargarFiltersGrid(obj["filters"] as JsonObject);
                    break;
                default:
                    pnl_empty.Visible = true;
                    break;
            }
            _loading = false;
        }

        private void CargarFiltersGrid(JsonObject? filters)
        {
            filters_dgv.Rows.Clear();
            if (filters == null) return;
            foreach (var kv in filters)
                filters_dgv.Rows.Add(kv.Key, kv.Value?.ToJsonString().Trim('"'));
        }

        // ── Property change handlers ─────────────────────────────────

        private void op_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            if (ActiveTree.SelectedNode?.Tag is JsonObject obj)
            {
                obj["op"] = op_cmb.SelectedItem?.ToString();
                RefreshTreeNodeLabel(ActiveTree.SelectedNode);
            }
        }

        private void val_num_ValueChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            if (ActiveTree.SelectedNode?.Tag is JsonObject obj)
            {
                obj["value"] = (double)val_num.Value;
                RefreshTreeNodeLabel(ActiveTree.SelectedNode);
            }
        }

        private void prest_txt_TextChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            if (ActiveTree.SelectedNode?.Tag is JsonObject obj)
            {
                obj["prestacion"] = prest_txt.Text;
                RefreshTreeNodeLabel(ActiveTree.SelectedNode);
            }
        }

        private void col_num_ValueChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            if (ActiveTree.SelectedNode?.Tag is JsonObject obj)
            {
                obj["columna"] = (int)col_num.Value;
                RefreshTreeNodeLabel(ActiveTree.SelectedNode);
            }
        }

        private void varname_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            if (ActiveTree.SelectedNode?.Tag is JsonObject obj)
            {
                obj["name"] = varname_cmb.SelectedItem?.ToString();
                RefreshTreeNodeLabel(ActiveTree.SelectedNode);
            }
        }

        private void filters_dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading) return;
            if (ActiveTree.SelectedNode?.Tag is JsonObject obj)
            {
                obj["filters"] = BuildFiltersObject();
                RefreshTreeNodeLabel(ActiveTree.SelectedNode);
            }
        }

        private JsonObject BuildFiltersObject()
        {
            var result = new JsonObject();
            foreach (DataGridViewRow row in filters_dgv.Rows)
            {
                if (row.IsNewRow) continue;
                var key = row.Cells[0].Value?.ToString();
                var val = row.Cells[1].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    if (double.TryParse(val,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double num))
                        result[key] = num;
                    else
                        result[key] = val;
                }
            }
            return result;
        }

        // ── Context menu ─────────────────────────────────────────────

        private void ctx_nodo_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var tn = ActiveTree.SelectedNode;
            bool isOp = tn?.Tag is JsonObject o && o["type"]?.GetValue<string>() == "op";
            agregar_hijo_mni.Enabled = isOp;
            eliminar_nodo_mni.Enabled = tn?.Parent != null;
            cambiar_tipo_mni.Enabled = tn != null;
        }

        private void agregar_hijo_mni_Click(object sender, EventArgs e)
        {
            var tn = ActiveTree.SelectedNode;
            if (tn?.Tag is not JsonObject parentObj || parentObj["type"]?.GetValue<string>() != "op") return;

            using var dlg = new NodoTipoDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var nuevoNodo = dlg.NodoCreado!;

            if (parentObj["args"] is not JsonArray args)
            {
                args = new JsonArray();
                parentObj["args"] = args;
            }

            args.Add(nuevoNodo);
            var childTn = BuildTreeNode(nuevoNodo);
            tn.Nodes.Add(childTn);
            tn.Expand();
            ActiveTree.SelectedNode = childTn;
        }

        private void eliminar_nodo_mni_Click(object sender, EventArgs e)
        {
            var tn = ActiveTree.SelectedNode;
            if (tn?.Parent == null) return;

            var conf = MessageBox.Show("¿Eliminar este nodo y sus hijos?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (conf != DialogResult.Yes) return;

            if (tn.Parent.Tag is JsonObject parentObj && parentObj["args"] is JsonArray args)
            {
                var nodeToRemove = tn.Tag as JsonNode;
                for (int i = 0; i < args.Count; i++)
                {
                    if (ReferenceEquals(args[i], nodeToRemove))
                    {
                        args.RemoveAt(i);
                        break;
                    }
                }
            }

            tn.Remove();
            MostrarPanelNodo(null);
        }

        private void cambiar_tipo_mni_Click(object sender, EventArgs e)
        {
            var tn = ActiveTree.SelectedNode;
            if (tn == null) return;

            using var dlg = new NodoTipoDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var nuevoNodo = dlg.NodoCreado!;
            bool isRoot = tn.Parent == null;

            if (isRoot)
            {
                ActiveFormulaRoot = nuevoNodo;
                PopularTreeView(ActiveTree, nuevoNodo);
            }
            else
            {
                if (tn.Parent!.Tag is JsonObject parentObj && parentObj["args"] is JsonArray args)
                {
                    var oldNode = tn.Tag as JsonNode;
                    for (int i = 0; i < args.Count; i++)
                    {
                        if (ReferenceEquals(args[i], oldNode))
                        {
                            args[i] = nuevoNodo;
                            break;
                        }
                    }
                }
                var newTn = BuildTreeNode(nuevoNodo);
                int idx = tn.Parent.Nodes.IndexOf(tn);
                tn.Parent.Nodes.RemoveAt(idx);
                tn.Parent.Nodes.Insert(idx, newTn);
                ActiveTree.SelectedNode = newTn;
            }
        }

        // ── isDenFijo toggle ─────────────────────────────────────────

        private void isDenFijo_chk_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            _denFijoHabilitado = isDenFijo_chk.Checked;
            tab_den_fijo.Enabled = isDenFijo_chk.Checked;
            if (!isDenFijo_chk.Checked && tabs_formula.SelectedTab == tab_den_fijo)
                tabs_formula.SelectedTab = tab_formula;
        }

        // ── Filter events ─────────────────────────────────────────────

        private void año_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_loading) CargarIndicadores();
        }

        private void convenio_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_loading) CargarIndicadores();
        }

        // ── CRUD ─────────────────────────────────────────────────────

        private void nuevo_btn_Click(object sender, EventArgs e)
        {
            int año = año_cmb.SelectedItem is int a ? a : DateTime.Today.Year;
            var nuevo = new Indicador
            {
                Nombre = "Nuevo indicador",
                Formula = string.Empty,
                Año = año,
                Orden = 999
            };
            _bdd.Indicador.Add(nuevo);
            _bdd.SaveChanges();
            CargarIndicadores();

            var row = _indicadores.FindIndex(i => i.Id == nuevo.Id);
            if (row >= 0)
            {
                indicadores_dgv.ClearSelection();
                indicadores_dgv.Rows[row].Selected = true;
                indicadores_dgv.FirstDisplayedScrollingRowIndex = row;
            }
        }

        private void eliminar_btn_Click(object sender, EventArgs e)
        {
            if (_sel == null)
            {
                MessageBox.Show("Seleccione un indicador para eliminar.", "Aviso");
                return;
            }

            var conf = MessageBox.Show(
                $"¿Eliminar indicador #{_sel.Id}: {_sel.Nombre}?",
                "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (conf != DialogResult.Yes) return;

            _bdd.Indicador.Remove(_sel);
            _bdd.SaveChanges();
            _sel = null;
            CargarIndicadores();
        }

        private void guardar_btn_Click(object sender, EventArgs e)
        {
            if (_sel == null)
            {
                MessageBox.Show("Seleccione un indicador para guardar.", "Aviso");
                return;
            }

            var ind = _sel;
            ind.Nombre = nombre_txt.Text;
            ind.Meta = (float)meta_num.Value;
            ind.Peso = (float)peso_num.Value;
            ind.Orden = (int)orden_num.Value;
            ind.Detalle = string.IsNullOrWhiteSpace(detalle_txt.Text) ? null : detalle_txt.Text;
            ind.IsTasa = isTasa_chk.Checked;
            ind.IsDenFijo = isDenFijo_chk.Checked;
            ind.Mensual = mensual_chk.Checked;
            ind.EsPeriodoOctubreSep = periodoOct_chk.Checked;
            ind.IsColaborativo = colaborativo_chk.Checked;
            ind.Tipoindicador = tipo_cmb.SelectedItem is ComboItem { IntValue: int tv } ? tv : null;
            ind.Formula = _formulaRoot?.ToJsonString() ?? string.Empty;
            ind.FormulaDenFijo = _formulaDenFijoRoot?.ToJsonString();

            if (_bdd.Indicador.Local.All(i => i.Id != ind.Id))
                _bdd.Indicador.Update(ind);

            _bdd.SaveChanges();

            int selRow = indicadores_dgv.SelectedRows.Count > 0 ? indicadores_dgv.SelectedRows[0].Index : -1;
            CargarIndicadores();
            if (selRow >= 0 && selRow < indicadores_dgv.Rows.Count)
            {
                indicadores_dgv.ClearSelection();
                indicadores_dgv.Rows[selRow].Selected = true;
            }

            MessageBox.Show("Indicador guardado.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Helper ───────────────────────────────────────────────────

        private class ComboItem(string text, int? value)
        {
            public string Text { get; } = text;
            public int? IntValue { get; } = value;
            public override string ToString() => Text;
        }
    }
}

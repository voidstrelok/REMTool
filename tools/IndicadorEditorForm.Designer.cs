namespace RemTools
{
    partial class IndicadorEditorForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ── Controls declaration ──────────────────────────────────
            panel_bottom = new Panel();
            nuevo_btn = new Button();
            eliminar_btn = new Button();
            guardar_btn = new Button();

            split_main = new SplitContainer();

            // Left panel
            panel_filters = new Panel();
            año_lbl = new Label();
            año_cmb = new ComboBox();
            convenio_lbl = new Label();
            convenio_cmb = new ComboBox();
            indicadores_dgv = new DataGridView();
            col_dgv_Id = new DataGridViewTextBoxColumn();
            col_dgv_Nombre = new DataGridViewTextBoxColumn();
            col_dgv_Año = new DataGridViewTextBoxColumn();

            // Right panel
            grp_props = new GroupBox();
            nombre_lbl = new Label();
            nombre_txt = new TextBox();
            meta_lbl = new Label();
            meta_num = new NumericUpDown();
            peso_lbl = new Label();
            peso_num = new NumericUpDown();
            orden_lbl = new Label();
            orden_num = new NumericUpDown();
            tipo_lbl = new Label();
            tipo_cmb = new ComboBox();
            detalle_lbl = new Label();
            detalle_txt = new TextBox();
            isTasa_chk = new CheckBox();
            isDenFijo_chk = new CheckBox();
            mensual_chk = new CheckBox();
            periodoOct_chk = new CheckBox();
            colaborativo_chk = new CheckBox();

            split_formula = new SplitContainer();

            // Formula tabs
            tabs_formula = new TabControl();
            tab_formula = new TabPage();
            formula_tv = new TreeView();
            tab_den_fijo = new TabPage();
            denFijo_tv = new TreeView();
            tab_json = new TabPage();
            pnl_json = new Panel();
            formula_json_txt = new TextBox();
            aplicar_json_btn = new Button();

            // Node props container
            pnl_nodo_props = new Panel();
            pnl_empty = new Panel();
            lbl_no_nodo = new Label();
            pnl_op = new Panel();
            op_lbl = new Label();
            op_cmb = new ComboBox();
            pnl_number = new Panel();
            val_lbl = new Label();
            val_num = new NumericUpDown();
            pnl_value = new Panel();
            prest_lbl = new Label();
            prest_txt = new TextBox();
            col_lbl = new Label();
            col_num = new NumericUpDown();
            pnl_variable = new Panel();
            varname_lbl = new Label();
            varname_cmb = new ComboBox();
            filters_lbl = new Label();
            filters_dgv = new DataGridView();
            col_filter_key = new DataGridViewTextBoxColumn();
            col_filter_val = new DataGridViewTextBoxColumn();

            // Context menu
            ctx_nodo = new ContextMenuStrip(components);
            agregar_hijo_mni = new ToolStripMenuItem();
            eliminar_nodo_mni = new ToolStripMenuItem();
            cambiar_tipo_mni = new ToolStripMenuItem();

            // ── Begin suspend ─────────────────────────────────────────
            panel_bottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)split_main).BeginInit();
            split_main.SuspendLayout();
            split_main.Panel1.SuspendLayout();
            split_main.Panel2.SuspendLayout();
            panel_filters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)indicadores_dgv).BeginInit();
            grp_props.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)meta_num).BeginInit();
            ((System.ComponentModel.ISupportInitialize)peso_num).BeginInit();
            ((System.ComponentModel.ISupportInitialize)orden_num).BeginInit();
            ((System.ComponentModel.ISupportInitialize)split_formula).BeginInit();
            split_formula.SuspendLayout();
            split_formula.Panel1.SuspendLayout();
            split_formula.Panel2.SuspendLayout();
            tabs_formula.SuspendLayout();
            tab_formula.SuspendLayout();
            tab_den_fijo.SuspendLayout();
            tab_json.SuspendLayout();
            pnl_json.SuspendLayout();
            pnl_nodo_props.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)val_num).BeginInit();
            ((System.ComponentModel.ISupportInitialize)col_num).BeginInit();
            ((System.ComponentModel.ISupportInitialize)filters_dgv).BeginInit();
            ctx_nodo.SuspendLayout();
            SuspendLayout();

            // ── panel_bottom ──────────────────────────────────────────
            panel_bottom.Controls.Add(guardar_btn);
            panel_bottom.Controls.Add(eliminar_btn);
            panel_bottom.Controls.Add(nuevo_btn);
            panel_bottom.Dock = DockStyle.Bottom;
            panel_bottom.Name = "panel_bottom";
            panel_bottom.Padding = new Padding(8, 6, 8, 6);
            panel_bottom.Size = new Size(1100, 44);
            panel_bottom.TabIndex = 0;

            nuevo_btn.Location = new Point(8, 7);
            nuevo_btn.Name = "nuevo_btn";
            nuevo_btn.Size = new Size(130, 30);
            nuevo_btn.TabIndex = 0;
            nuevo_btn.Text = "Nuevo indicador";
            nuevo_btn.UseVisualStyleBackColor = true;
            nuevo_btn.Click += nuevo_btn_Click;

            eliminar_btn.Location = new Point(146, 7);
            eliminar_btn.Name = "eliminar_btn";
            eliminar_btn.Size = new Size(110, 30);
            eliminar_btn.TabIndex = 1;
            eliminar_btn.Text = "Eliminar";
            eliminar_btn.UseVisualStyleBackColor = true;
            eliminar_btn.Click += eliminar_btn_Click;

            guardar_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guardar_btn.Location = new Point(946, 7);
            guardar_btn.Name = "guardar_btn";
            guardar_btn.Size = new Size(145, 30);
            guardar_btn.TabIndex = 2;
            guardar_btn.Text = "Guardar cambios";
            guardar_btn.UseVisualStyleBackColor = true;
            guardar_btn.Click += guardar_btn_Click;

            // ── split_main ────────────────────────────────────────────
            split_main.Dock = DockStyle.Fill;
            split_main.Location = new Point(0, 0);
            split_main.Name = "split_main";
            split_main.SplitterDistance = 265;
            split_main.TabIndex = 1;

            // ── split_main.Panel1 (left) ──────────────────────────────
            split_main.Panel1.Controls.Add(indicadores_dgv);
            split_main.Panel1.Controls.Add(panel_filters);

            panel_filters.Controls.Add(convenio_cmb);
            panel_filters.Controls.Add(convenio_lbl);
            panel_filters.Controls.Add(año_cmb);
            panel_filters.Controls.Add(año_lbl);
            panel_filters.Dock = DockStyle.Top;
            panel_filters.Name = "panel_filters";
            panel_filters.Padding = new Padding(6);
            panel_filters.Size = new Size(265, 90);
            panel_filters.TabIndex = 0;

            año_lbl.AutoSize = true;
            año_lbl.Location = new Point(6, 6);
            año_lbl.Name = "año_lbl";
            año_lbl.Text = "Año:";

            año_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            año_cmb.FormattingEnabled = true;
            año_cmb.Location = new Point(6, 24);
            año_cmb.Name = "año_cmb";
            año_cmb.Size = new Size(100, 28);
            año_cmb.TabIndex = 0;
            año_cmb.SelectedIndexChanged += año_cmb_SelectedIndexChanged;

            convenio_lbl.AutoSize = true;
            convenio_lbl.Location = new Point(116, 6);
            convenio_lbl.Name = "convenio_lbl";
            convenio_lbl.Text = "Convenio:";

            convenio_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            convenio_cmb.FormattingEnabled = true;
            convenio_cmb.Location = new Point(116, 24);
            convenio_cmb.Name = "convenio_cmb";
            convenio_cmb.Size = new Size(140, 28);
            convenio_cmb.TabIndex = 1;
            convenio_cmb.SelectedIndexChanged += convenio_cmb_SelectedIndexChanged;

            indicadores_dgv.AllowUserToAddRows = false;
            indicadores_dgv.AllowUserToDeleteRows = false;
            indicadores_dgv.AutoGenerateColumns = false;
            indicadores_dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            indicadores_dgv.Columns.AddRange(new DataGridViewColumn[] { col_dgv_Id, col_dgv_Nombre, col_dgv_Año });
            indicadores_dgv.Dock = DockStyle.Fill;
            indicadores_dgv.Name = "indicadores_dgv";
            indicadores_dgv.ReadOnly = true;
            indicadores_dgv.RowHeadersWidth = 30;
            indicadores_dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            indicadores_dgv.MultiSelect = false;
            indicadores_dgv.TabIndex = 1;
            indicadores_dgv.SelectionChanged += indicadores_dgv_SelectionChanged;

            col_dgv_Id.DataPropertyName = "Id";
            col_dgv_Id.HeaderText = "ID";
            col_dgv_Id.Name = "col_dgv_Id";
            col_dgv_Id.ReadOnly = true;
            col_dgv_Id.Width = 42;

            col_dgv_Nombre.DataPropertyName = "Nombre";
            col_dgv_Nombre.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            col_dgv_Nombre.HeaderText = "Nombre";
            col_dgv_Nombre.Name = "col_dgv_Nombre";
            col_dgv_Nombre.ReadOnly = true;

            col_dgv_Año.DataPropertyName = "Año";
            col_dgv_Año.HeaderText = "Año";
            col_dgv_Año.Name = "col_dgv_Año";
            col_dgv_Año.ReadOnly = true;
            col_dgv_Año.Width = 48;

            // ── split_main.Panel2 (right) ─────────────────────────────
            split_main.Panel2.Controls.Add(split_formula);
            split_main.Panel2.Controls.Add(grp_props);

            // ── grp_props ─────────────────────────────────────────────
            grp_props.Controls.Add(colaborativo_chk);
            grp_props.Controls.Add(periodoOct_chk);
            grp_props.Controls.Add(mensual_chk);
            grp_props.Controls.Add(isDenFijo_chk);
            grp_props.Controls.Add(isTasa_chk);
            grp_props.Controls.Add(detalle_txt);
            grp_props.Controls.Add(detalle_lbl);
            grp_props.Controls.Add(tipo_cmb);
            grp_props.Controls.Add(tipo_lbl);
            grp_props.Controls.Add(orden_num);
            grp_props.Controls.Add(orden_lbl);
            grp_props.Controls.Add(peso_num);
            grp_props.Controls.Add(peso_lbl);
            grp_props.Controls.Add(meta_num);
            grp_props.Controls.Add(meta_lbl);
            grp_props.Controls.Add(nombre_txt);
            grp_props.Controls.Add(nombre_lbl);
            grp_props.Dock = DockStyle.Top;
            grp_props.Name = "grp_props";
            grp_props.Padding = new Padding(6, 4, 6, 4);
            grp_props.Size = new Size(831, 188);
            grp_props.TabIndex = 0;
            grp_props.Text = "Propiedades";

            nombre_lbl.AutoSize = true;
            nombre_lbl.Location = new Point(8, 24);
            nombre_lbl.Name = "nombre_lbl";
            nombre_lbl.Text = "Nombre:";

            nombre_txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            nombre_txt.Location = new Point(66, 21);
            nombre_txt.Name = "nombre_txt";
            nombre_txt.Size = new Size(560, 28);
            nombre_txt.TabIndex = 0;
            nombre_txt.MaxLength = 300;

            meta_lbl.AutoSize = true;
            meta_lbl.Location = new Point(8, 58);
            meta_lbl.Name = "meta_lbl";
            meta_lbl.Text = "Meta:";

            meta_num.DecimalPlaces = 4;
            meta_num.Location = new Point(8, 76);
            meta_num.Maximum = 9999999;
            meta_num.Minimum = 0;
            meta_num.Name = "meta_num";
            meta_num.Size = new Size(80, 28);
            meta_num.TabIndex = 1;

            peso_lbl.AutoSize = true;
            peso_lbl.Location = new Point(96, 58);
            peso_lbl.Name = "peso_lbl";
            peso_lbl.Text = "Peso:";

            peso_num.DecimalPlaces = 4;
            peso_num.Location = new Point(96, 76);
            peso_num.Maximum = 9999;
            peso_num.Minimum = 0;
            peso_num.Name = "peso_num";
            peso_num.Size = new Size(80, 28);
            peso_num.TabIndex = 2;

            orden_lbl.AutoSize = true;
            orden_lbl.Location = new Point(184, 58);
            orden_lbl.Name = "orden_lbl";
            orden_lbl.Text = "Orden:";

            orden_num.Location = new Point(184, 76);
            orden_num.Maximum = 9999;
            orden_num.Minimum = 0;
            orden_num.Name = "orden_num";
            orden_num.Size = new Size(70, 28);
            orden_num.TabIndex = 3;

            tipo_lbl.AutoSize = true;
            tipo_lbl.Location = new Point(262, 58);
            tipo_lbl.Name = "tipo_lbl";
            tipo_lbl.Text = "Tipo:";

            tipo_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            tipo_cmb.FormattingEnabled = true;
            tipo_cmb.Location = new Point(262, 73);
            tipo_cmb.Name = "tipo_cmb";
            tipo_cmb.Size = new Size(220, 28);
            tipo_cmb.TabIndex = 4;

            detalle_lbl.AutoSize = true;
            detalle_lbl.Location = new Point(8, 112);
            detalle_lbl.Name = "detalle_lbl";
            detalle_lbl.Text = "Detalle:";

            detalle_txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            detalle_txt.Location = new Point(66, 109);
            detalle_txt.Name = "detalle_txt";
            detalle_txt.Size = new Size(560, 28);
            detalle_txt.TabIndex = 5;
            detalle_txt.MaxLength = 500;

            isTasa_chk.AutoSize = true;
            isTasa_chk.Location = new Point(8, 148);
            isTasa_chk.Name = "isTasa_chk";
            isTasa_chk.TabIndex = 6;
            isTasa_chk.Text = "Es tasa";

            isDenFijo_chk.AutoSize = true;
            isDenFijo_chk.Location = new Point(90, 148);
            isDenFijo_chk.Name = "isDenFijo_chk";
            isDenFijo_chk.TabIndex = 7;
            isDenFijo_chk.Text = "Den. fijo";
            isDenFijo_chk.CheckedChanged += isDenFijo_chk_CheckedChanged;

            mensual_chk.AutoSize = true;
            mensual_chk.Location = new Point(180, 148);
            mensual_chk.Name = "mensual_chk";
            mensual_chk.TabIndex = 8;
            mensual_chk.Text = "Mensual";

            periodoOct_chk.AutoSize = true;
            periodoOct_chk.Location = new Point(262, 148);
            periodoOct_chk.Name = "periodoOct_chk";
            periodoOct_chk.TabIndex = 9;
            periodoOct_chk.Text = "Período Oct-Sep";

            colaborativo_chk.AutoSize = true;
            colaborativo_chk.Location = new Point(408, 148);
            colaborativo_chk.Name = "colaborativo_chk";
            colaborativo_chk.TabIndex = 10;
            colaborativo_chk.Text = "Colaborativo";

            // ── split_formula ─────────────────────────────────────────
            split_formula.Dock = DockStyle.Fill;
            split_formula.Name = "split_formula";
            split_formula.Orientation = Orientation.Horizontal;
            split_formula.SplitterDistance = 280;
            split_formula.TabIndex = 1;

            // ── split_formula.Panel1 (trees + json) ───────────────────
            split_formula.Panel1.Controls.Add(tabs_formula);

            tabs_formula.Controls.Add(tab_formula);
            tabs_formula.Controls.Add(tab_den_fijo);
            tabs_formula.Controls.Add(tab_json);
            tabs_formula.Dock = DockStyle.Fill;
            tabs_formula.Name = "tabs_formula";
            tabs_formula.TabIndex = 0;
            tabs_formula.SelectedIndexChanged += tabs_formula_SelectedIndexChanged;
            tabs_formula.Selecting += tabs_formula_Selecting;

            // tab_formula
            tab_formula.Controls.Add(formula_tv);
            tab_formula.Name = "tab_formula";
            tab_formula.Padding = new Padding(3);
            tab_formula.Text = "Fórmula";
            tab_formula.UseVisualStyleBackColor = true;

            formula_tv.ContextMenuStrip = ctx_nodo;
            formula_tv.Dock = DockStyle.Fill;
            formula_tv.Name = "formula_tv";
            formula_tv.TabIndex = 0;
            formula_tv.AfterSelect += formula_tv_AfterSelect;

            // tab_den_fijo
            tab_den_fijo.Controls.Add(denFijo_tv);
            tab_den_fijo.Enabled = false;
            tab_den_fijo.Name = "tab_den_fijo";
            tab_den_fijo.Padding = new Padding(3);
            tab_den_fijo.Text = "Den. Fijo";
            tab_den_fijo.UseVisualStyleBackColor = true;

            denFijo_tv.ContextMenuStrip = ctx_nodo;
            denFijo_tv.Dock = DockStyle.Fill;
            denFijo_tv.Name = "denFijo_tv";
            denFijo_tv.TabIndex = 0;
            denFijo_tv.AfterSelect += denFijo_tv_AfterSelect;

            // tab_json
            tab_json.Controls.Add(pnl_json);
            tab_json.Name = "tab_json";
            tab_json.Padding = new Padding(3);
            tab_json.Text = "JSON";
            tab_json.UseVisualStyleBackColor = true;

            pnl_json.Controls.Add(formula_json_txt);
            pnl_json.Controls.Add(aplicar_json_btn);
            pnl_json.Dock = DockStyle.Fill;
            pnl_json.Name = "pnl_json";

            formula_json_txt.Dock = DockStyle.Fill;
            formula_json_txt.Font = new Font("Courier New", 9F);
            formula_json_txt.Multiline = true;
            formula_json_txt.Name = "formula_json_txt";
            formula_json_txt.ScrollBars = ScrollBars.Both;
            formula_json_txt.TabIndex = 0;
            formula_json_txt.WordWrap = false;

            aplicar_json_btn.Dock = DockStyle.Bottom;
            aplicar_json_btn.Name = "aplicar_json_btn";
            aplicar_json_btn.Size = new Size(100, 32);
            aplicar_json_btn.TabIndex = 1;
            aplicar_json_btn.Text = "Aplicar JSON →";
            aplicar_json_btn.UseVisualStyleBackColor = true;
            aplicar_json_btn.Click += aplicar_json_btn_Click;

            // ── split_formula.Panel2 (node props) ─────────────────────
            split_formula.Panel2.Controls.Add(pnl_nodo_props);

            pnl_nodo_props.Controls.Add(pnl_variable);
            pnl_nodo_props.Controls.Add(pnl_value);
            pnl_nodo_props.Controls.Add(pnl_number);
            pnl_nodo_props.Controls.Add(pnl_op);
            pnl_nodo_props.Controls.Add(pnl_empty);
            pnl_nodo_props.Dock = DockStyle.Fill;
            pnl_nodo_props.Name = "pnl_nodo_props";
            pnl_nodo_props.TabIndex = 0;

            // pnl_empty
            pnl_empty.Controls.Add(lbl_no_nodo);
            pnl_empty.Dock = DockStyle.Fill;
            pnl_empty.Name = "pnl_empty";
            pnl_empty.Visible = true;

            lbl_no_nodo.Dock = DockStyle.Fill;
            lbl_no_nodo.ForeColor = SystemColors.GrayText;
            lbl_no_nodo.Name = "lbl_no_nodo";
            lbl_no_nodo.Text = "Seleccione un nodo para editar sus propiedades.";
            lbl_no_nodo.TextAlign = ContentAlignment.MiddleCenter;

            // pnl_op
            pnl_op.Controls.Add(op_cmb);
            pnl_op.Controls.Add(op_lbl);
            pnl_op.Dock = DockStyle.Fill;
            pnl_op.Name = "pnl_op";
            pnl_op.Visible = false;

            op_lbl.AutoSize = true;
            op_lbl.Location = new Point(8, 8);
            op_lbl.Name = "op_lbl";
            op_lbl.Text = "Operación:";

            op_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            op_cmb.Items.AddRange(new object[] { "sum", "sub", "mul", "div" });
            op_cmb.Location = new Point(8, 28);
            op_cmb.Name = "op_cmb";
            op_cmb.Size = new Size(130, 28);
            op_cmb.TabIndex = 0;
            op_cmb.SelectedIndexChanged += op_cmb_SelectedIndexChanged;

            // pnl_number
            pnl_number.Controls.Add(val_num);
            pnl_number.Controls.Add(val_lbl);
            pnl_number.Dock = DockStyle.Fill;
            pnl_number.Name = "pnl_number";
            pnl_number.Visible = false;

            val_lbl.AutoSize = true;
            val_lbl.Location = new Point(8, 8);
            val_lbl.Name = "val_lbl";
            val_lbl.Text = "Valor constante:";

            val_num.DecimalPlaces = 4;
            val_num.Location = new Point(8, 28);
            val_num.Maximum = 999999999;
            val_num.Minimum = -999999999;
            val_num.Name = "val_num";
            val_num.Size = new Size(130, 28);
            val_num.TabIndex = 0;
            val_num.ValueChanged += val_num_ValueChanged;

            // pnl_value
            pnl_value.Controls.Add(col_num);
            pnl_value.Controls.Add(col_lbl);
            pnl_value.Controls.Add(prest_txt);
            pnl_value.Controls.Add(prest_lbl);
            pnl_value.Dock = DockStyle.Fill;
            pnl_value.Name = "pnl_value";
            pnl_value.Visible = false;

            prest_lbl.AutoSize = true;
            prest_lbl.Location = new Point(8, 8);
            prest_lbl.Name = "prest_lbl";
            prest_lbl.Text = "Prestación:";

            prest_txt.Location = new Point(8, 28);
            prest_txt.MaxLength = 20;
            prest_txt.Name = "prest_txt";
            prest_txt.Size = new Size(100, 28);
            prest_txt.TabIndex = 0;
            prest_txt.TextChanged += prest_txt_TextChanged;

            col_lbl.AutoSize = true;
            col_lbl.Location = new Point(120, 8);
            col_lbl.Name = "col_lbl";
            col_lbl.Text = "Columna:";

            col_num.Location = new Point(120, 28);
            col_num.Maximum = 100;
            col_num.Minimum = 1;
            col_num.Name = "col_num";
            col_num.Size = new Size(70, 28);
            col_num.TabIndex = 1;
            col_num.ValueChanged += col_num_ValueChanged;

            // pnl_variable
            pnl_variable.Controls.Add(filters_dgv);
            pnl_variable.Controls.Add(filters_lbl);
            pnl_variable.Controls.Add(varname_cmb);
            pnl_variable.Controls.Add(varname_lbl);
            pnl_variable.Dock = DockStyle.Fill;
            pnl_variable.Name = "pnl_variable";
            pnl_variable.Visible = false;

            varname_lbl.AutoSize = true;
            varname_lbl.Location = new Point(8, 8);
            varname_lbl.Name = "varname_lbl";
            varname_lbl.Text = "Variable:";

            varname_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            varname_cmb.Items.AddRange(new object[] { "FONASA", "DM2", "HTA", "EPOC" });
            varname_cmb.Location = new Point(8, 28);
            varname_cmb.Name = "varname_cmb";
            varname_cmb.Size = new Size(130, 28);
            varname_cmb.TabIndex = 0;
            varname_cmb.SelectedIndexChanged += varname_cmb_SelectedIndexChanged;

            filters_lbl.AutoSize = true;
            filters_lbl.Location = new Point(150, 8);
            filters_lbl.Name = "filters_lbl";
            filters_lbl.Text = "Filtros (clave / valor):";

            filters_dgv.AllowUserToDeleteRows = true;
            filters_dgv.AllowUserToAddRows = true;
            filters_dgv.AutoGenerateColumns = false;
            filters_dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            filters_dgv.Columns.AddRange(new DataGridViewColumn[] { col_filter_key, col_filter_val });
            filters_dgv.Location = new Point(150, 28);
            filters_dgv.Name = "filters_dgv";
            filters_dgv.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            filters_dgv.Size = new Size(500, 90);
            filters_dgv.TabIndex = 1;
            filters_dgv.RowHeadersWidth = 30;
            filters_dgv.CellEndEdit += filters_dgv_CellEndEdit;

            col_filter_key.HeaderText = "Clave";
            col_filter_key.Name = "col_filter_key";
            col_filter_key.Width = 130;

            col_filter_val.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            col_filter_val.HeaderText = "Valor";
            col_filter_val.Name = "col_filter_val";

            // ── ctx_nodo ──────────────────────────────────────────────
            ctx_nodo.Items.AddRange(new ToolStripItem[]
            {
                agregar_hijo_mni,
                eliminar_nodo_mni,
                new ToolStripSeparator(),
                cambiar_tipo_mni
            });
            ctx_nodo.Name = "ctx_nodo";
            ctx_nodo.Opening += ctx_nodo_Opening;

            agregar_hijo_mni.Name = "agregar_hijo_mni";
            agregar_hijo_mni.Text = "Agregar hijo";
            agregar_hijo_mni.Click += agregar_hijo_mni_Click;

            eliminar_nodo_mni.Name = "eliminar_nodo_mni";
            eliminar_nodo_mni.Text = "Eliminar nodo";
            eliminar_nodo_mni.Click += eliminar_nodo_mni_Click;

            cambiar_tipo_mni.Name = "cambiar_tipo_mni";
            cambiar_tipo_mni.Text = "Cambiar tipo...";
            cambiar_tipo_mni.Click += cambiar_tipo_mni_Click;

            // ── IndicadorEditorForm ───────────────────────────────────
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 700);
            Controls.Add(split_main);
            Controls.Add(panel_bottom);
            MinimumSize = new Size(900, 580);
            Name = "IndicadorEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Editor de Indicadores";
            Load += IndicadorEditorForm_Load;

            // ── End resume ────────────────────────────────────────────
            panel_bottom.ResumeLayout(false);
            split_main.Panel1.ResumeLayout(false);
            split_main.Panel1.PerformLayout();
            split_main.Panel2.ResumeLayout(false);
            split_main.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)split_main).EndInit();
            split_main.ResumeLayout(false);
            panel_filters.ResumeLayout(false);
            panel_filters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)indicadores_dgv).EndInit();
            grp_props.ResumeLayout(false);
            grp_props.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)meta_num).EndInit();
            ((System.ComponentModel.ISupportInitialize)peso_num).EndInit();
            ((System.ComponentModel.ISupportInitialize)orden_num).EndInit();
            split_formula.Panel1.ResumeLayout(false);
            split_formula.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)split_formula).EndInit();
            split_formula.ResumeLayout(false);
            tabs_formula.ResumeLayout(false);
            tab_formula.ResumeLayout(false);
            tab_den_fijo.ResumeLayout(false);
            pnl_json.ResumeLayout(false);
            pnl_json.PerformLayout();
            tab_json.ResumeLayout(false);
            pnl_nodo_props.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)val_num).EndInit();
            ((System.ComponentModel.ISupportInitialize)col_num).EndInit();
            ((System.ComponentModel.ISupportInitialize)filters_dgv).EndInit();
            ctx_nodo.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        // Left
        private Panel panel_filters;
        private Label año_lbl;
        private ComboBox año_cmb;
        private Label convenio_lbl;
        private ComboBox convenio_cmb;
        private DataGridView indicadores_dgv;
        private DataGridViewTextBoxColumn col_dgv_Id;
        private DataGridViewTextBoxColumn col_dgv_Nombre;
        private DataGridViewTextBoxColumn col_dgv_Año;

        // Properties
        private GroupBox grp_props;
        private Label nombre_lbl;
        private TextBox nombre_txt;
        private Label meta_lbl;
        private NumericUpDown meta_num;
        private Label peso_lbl;
        private NumericUpDown peso_num;
        private Label orden_lbl;
        private NumericUpDown orden_num;
        private Label tipo_lbl;
        private ComboBox tipo_cmb;
        private Label detalle_lbl;
        private TextBox detalle_txt;
        private CheckBox isTasa_chk;
        private CheckBox isDenFijo_chk;
        private CheckBox mensual_chk;
        private CheckBox periodoOct_chk;
        private CheckBox colaborativo_chk;

        // Formula tabs
        private SplitContainer split_formula;
        private TabControl tabs_formula;
        private TabPage tab_formula;
        private TreeView formula_tv;
        private TabPage tab_den_fijo;
        private TreeView denFijo_tv;
        private TabPage tab_json;
        private Panel pnl_json;
        private TextBox formula_json_txt;
        private Button aplicar_json_btn;

        // Node props
        private Panel pnl_nodo_props;
        private Panel pnl_empty;
        private Label lbl_no_nodo;
        private Panel pnl_op;
        private Label op_lbl;
        private ComboBox op_cmb;
        private Panel pnl_number;
        private Label val_lbl;
        private NumericUpDown val_num;
        private Panel pnl_value;
        private Label prest_lbl;
        private TextBox prest_txt;
        private Label col_lbl;
        private NumericUpDown col_num;
        private Panel pnl_variable;
        private Label varname_lbl;
        private ComboBox varname_cmb;
        private Label filters_lbl;
        private DataGridView filters_dgv;
        private DataGridViewTextBoxColumn col_filter_key;
        private DataGridViewTextBoxColumn col_filter_val;

        // Shared
        private SplitContainer split_main;
        private Panel panel_bottom;
        private Button nuevo_btn;
        private Button eliminar_btn;
        private Button guardar_btn;

        private ContextMenuStrip ctx_nodo;
        private ToolStripMenuItem agregar_hijo_mni;
        private ToolStripMenuItem eliminar_nodo_mni;
        private ToolStripMenuItem cambiar_tipo_mni;
    }
}

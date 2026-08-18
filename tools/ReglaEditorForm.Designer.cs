namespace RemTools
{
    partial class ReglaEditorForm
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
            reglas_dgv = new DataGridView();
            col_id = new DataGridViewTextBoxColumn();
            col_serie = new DataGridViewTextBoxColumn();
            col_version = new DataGridViewTextBoxColumn();
            col_tipo = new DataGridViewTextBoxColumn();
            col_expresion = new DataGridViewTextBoxColumn();
            col_mensaje = new DataGridViewTextBoxColumn();

            split_main = new SplitContainer();
            panel_filtros = new Panel();
            buscar_lbl = new Label();
            buscar_txt = new TextBox();
            serie_filtro_lbl = new Label();
            serie_filtro_cmb = new ComboBox();
            anio_filtro_lbl = new Label();
            anio_filtro_cmb = new ComboBox();
            version_filtro_lbl = new Label();
            version_filtro_cmb = new ComboBox();
            tipo_filtro_lbl = new Label();
            tipo_filtro_cmb = new ComboBox();
            reglas_count_lbl = new Label();

            grp_formulario = new GroupBox();
            id_lbl = new Label();
            version_lbl = new Label();
            version_cmb = new ComboBox();
            tipo_lbl = new Label();
            tipo_cmb = new ComboBox();
            expresion_lbl = new Label();
            panel_expresion_tools = new Panel();
            hoja_txt = new TextBox();
            celda_txt = new TextBox();
            insertar_referencia_btn = new Button();
            operador_suma_btn = new Button();
            operador_resta_btn = new Button();
            operador_igual_btn = new Button();
            operador_distinto_btn = new Button();
            operador_and_btn = new Button();
            operador_or_btn = new Button();
            operador_parentesis_btn = new Button();
            expresion_txt = new TextBox();
            validar_btn = new Button();
            probar_btn = new Button();
            validacion_lbl = new Label();
            referencias_lbl = new Label();
            referencias_dgv = new DataGridView();
            col_referencia = new DataGridViewTextBoxColumn();
            col_valor = new DataGridViewTextBoxColumn();
            mensaje_lbl = new Label();
            mensaje_txt = new TextBox();

            panel_bottom = new Panel();
            nuevo_btn = new Button();
            editar_btn = new Button();
            duplicar_btn = new Button();
            eliminar_btn = new Button();
            guardar_btn = new Button();

            ((System.ComponentModel.ISupportInitialize)reglas_dgv).BeginInit();
            ((System.ComponentModel.ISupportInitialize)split_main).BeginInit();
            split_main.Panel1.SuspendLayout();
            split_main.Panel2.SuspendLayout();
            split_main.SuspendLayout();
            panel_filtros.SuspendLayout();
            grp_formulario.SuspendLayout();
            panel_expresion_tools.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)referencias_dgv).BeginInit();
            panel_bottom.SuspendLayout();
            SuspendLayout();

            // reglas_dgv
            reglas_dgv.AllowUserToAddRows = false;
            reglas_dgv.AllowUserToDeleteRows = false;
            reglas_dgv.AutoGenerateColumns = false;
            reglas_dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            reglas_dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            reglas_dgv.Columns.AddRange(new DataGridViewColumn[] { col_id, col_serie, col_version, col_tipo, col_expresion, col_mensaje });
            reglas_dgv.Dock = DockStyle.Fill;
            reglas_dgv.MultiSelect = false;
            reglas_dgv.Name = "reglas_dgv";
            reglas_dgv.ReadOnly = true;
            reglas_dgv.RowHeadersWidth = 30;
            reglas_dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            reglas_dgv.TabIndex = 1;
            reglas_dgv.SelectionChanged += reglas_dgv_SelectionChanged;
            reglas_dgv.CellClick += reglas_dgv_CellClick;
            reglas_dgv.CellDoubleClick += reglas_dgv_CellDoubleClick;

            col_id.HeaderText = "ID";
            col_id.Name = "col_id";
            col_id.Visible = false;
            col_serie.HeaderText = "Serie";
            col_serie.Name = "col_serie";
            col_serie.FillWeight = 12;
            col_version.HeaderText = "Versión";
            col_version.Name = "col_version";
            col_version.FillWeight = 20;
            col_tipo.HeaderText = "Tipo";
            col_tipo.Name = "col_tipo";
            col_tipo.FillWeight = 13;
            col_expresion.HeaderText = "Expresión";
            col_expresion.Name = "col_expresion";
            col_expresion.FillWeight = 25;
            col_mensaje.HeaderText = "Mensaje";
            col_mensaje.Name = "col_mensaje";
            col_mensaje.FillWeight = 30;

            // split_main
            split_main.Dock = DockStyle.Fill;
            split_main.FixedPanel = FixedPanel.Panel2;
            split_main.Location = new Point(0, 0);
            split_main.Name = "split_main";
            split_main.Orientation = Orientation.Vertical;
            split_main.Panel1MinSize = 520;
            split_main.Panel2MinSize = 760;
            split_main.SplitterDistance = 560;
            split_main.TabIndex = 0;

            split_main.Panel1.Controls.Add(reglas_dgv);
            split_main.Panel1.Controls.Add(panel_filtros);

            // panel_filtros
            panel_filtros.Controls.Add(reglas_count_lbl);
            panel_filtros.Controls.Add(tipo_filtro_cmb);
            panel_filtros.Controls.Add(tipo_filtro_lbl);
            panel_filtros.Controls.Add(version_filtro_cmb);
            panel_filtros.Controls.Add(version_filtro_lbl);
            panel_filtros.Controls.Add(anio_filtro_cmb);
            panel_filtros.Controls.Add(anio_filtro_lbl);
            panel_filtros.Controls.Add(serie_filtro_cmb);
            panel_filtros.Controls.Add(serie_filtro_lbl);
            panel_filtros.Controls.Add(buscar_txt);
            panel_filtros.Controls.Add(buscar_lbl);
            panel_filtros.Dock = DockStyle.Top;
            panel_filtros.Height = 112;
            panel_filtros.Name = "panel_filtros";
            panel_filtros.Padding = new Padding(8, 6, 8, 6);

            buscar_lbl.AutoSize = true;
            buscar_lbl.Location = new Point(8, 8);
            buscar_lbl.Text = "Buscar regla";

            buscar_txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            buscar_txt.Location = new Point(8, 27);
            buscar_txt.Name = "buscar_txt";
            buscar_txt.PlaceholderText = "Expresión, mensaje o tipo...";
            buscar_txt.Size = new Size(668, 27);
            buscar_txt.TextChanged += buscar_txt_TextChanged;

            serie_filtro_lbl.AutoSize = true;
            serie_filtro_lbl.Location = new Point(8, 63);
            serie_filtro_lbl.Text = "Serie";
            serie_filtro_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            serie_filtro_cmb.FormattingEnabled = true;
            serie_filtro_cmb.Location = new Point(8, 82);
            serie_filtro_cmb.Name = "serie_filtro_cmb";
            serie_filtro_cmb.Size = new Size(110, 28);
            serie_filtro_cmb.SelectedIndexChanged += serie_filtro_cmb_SelectedIndexChanged;

            anio_filtro_lbl.AutoSize = true;
            anio_filtro_lbl.Location = new Point(126, 63);
            anio_filtro_lbl.Text = "Año";
            anio_filtro_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            anio_filtro_cmb.FormattingEnabled = true;
            anio_filtro_cmb.Location = new Point(126, 82);
            anio_filtro_cmb.Name = "anio_filtro_cmb";
            anio_filtro_cmb.Size = new Size(65, 28);
            anio_filtro_cmb.SelectedIndexChanged += anio_filtro_cmb_SelectedIndexChanged;

            version_filtro_lbl.AutoSize = true;
            version_filtro_lbl.Location = new Point(199, 63);
            version_filtro_lbl.Text = "Versión";
            version_filtro_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            version_filtro_cmb.FormattingEnabled = true;
            version_filtro_cmb.Location = new Point(199, 82);
            version_filtro_cmb.Name = "version_filtro_cmb";
            version_filtro_cmb.Size = new Size(190, 28);
            version_filtro_cmb.SelectedIndexChanged += version_filtro_cmb_SelectedIndexChanged;

            tipo_filtro_lbl.AutoSize = true;
            tipo_filtro_lbl.Location = new Point(397, 63);
            tipo_filtro_lbl.Text = "Tipo";
            tipo_filtro_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            tipo_filtro_cmb.FormattingEnabled = true;
            tipo_filtro_cmb.Location = new Point(397, 82);
            tipo_filtro_cmb.Name = "tipo_filtro_cmb";
            tipo_filtro_cmb.Size = new Size(125, 28);
            tipo_filtro_cmb.SelectedIndexChanged += tipo_filtro_cmb_SelectedIndexChanged;

            reglas_count_lbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            reglas_count_lbl.AutoSize = true;
            reglas_count_lbl.ForeColor = SystemColors.GrayText;
            reglas_count_lbl.Location = new Point(610, 87);
            reglas_count_lbl.Text = "0 reglas";

            // formulario
            split_main.Panel2.Controls.Add(grp_formulario);
            split_main.Panel2.Padding = new Padding(8);

            grp_formulario.Controls.Add(mensaje_txt);
            grp_formulario.Controls.Add(mensaje_lbl);
            grp_formulario.Controls.Add(referencias_dgv);
            grp_formulario.Controls.Add(referencias_lbl);
            grp_formulario.Controls.Add(validacion_lbl);
            grp_formulario.Controls.Add(probar_btn);
            grp_formulario.Controls.Add(validar_btn);
            grp_formulario.Controls.Add(expresion_txt);
            grp_formulario.Controls.Add(panel_expresion_tools);
            grp_formulario.Controls.Add(expresion_lbl);
            grp_formulario.Controls.Add(tipo_cmb);
            grp_formulario.Controls.Add(tipo_lbl);
            grp_formulario.Controls.Add(version_cmb);
            grp_formulario.Controls.Add(version_lbl);
            grp_formulario.Controls.Add(id_lbl);
            grp_formulario.Dock = DockStyle.Fill;
            grp_formulario.Name = "grp_formulario";
            grp_formulario.Text = "Regla seleccionada";

            id_lbl.AutoSize = true;
            id_lbl.ForeColor = SystemColors.GrayText;
            id_lbl.Location = new Point(12, 28);
            id_lbl.Text = "Nueva regla";

            version_lbl.AutoSize = true;
            version_lbl.Location = new Point(12, 58);
            version_lbl.Text = "Versión";
            version_cmb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            version_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            version_cmb.FormattingEnabled = true;
            version_cmb.Location = new Point(12, 77);
            version_cmb.Name = "version_cmb";
            version_cmb.Size = new Size(690, 28);

            tipo_lbl.AutoSize = true;
            tipo_lbl.Location = new Point(12, 112);
            tipo_lbl.Text = "Tipo de regla";
            tipo_cmb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tipo_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            tipo_cmb.FormattingEnabled = true;
            tipo_cmb.Location = new Point(12, 131);
            tipo_cmb.Name = "tipo_cmb";
            tipo_cmb.Size = new Size(260, 28);

            expresion_lbl.AutoSize = true;
            expresion_lbl.Location = new Point(12, 166);
            expresion_lbl.Text = "Expresión";

            // Herramientas de construcción de expresión
            panel_expresion_tools.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel_expresion_tools.BorderStyle = BorderStyle.FixedSingle;
            panel_expresion_tools.Location = new Point(12, 185);
            panel_expresion_tools.Name = "panel_expresion_tools";
            panel_expresion_tools.Size = new Size(690, 66);

            hoja_txt.Location = new Point(5, 5);
            hoja_txt.Name = "hoja_txt";
            hoja_txt.PlaceholderText = "Hoja A01";
            hoja_txt.Size = new Size(88, 27);

            celda_txt.Location = new Point(99, 5);
            celda_txt.Name = "celda_txt";
            celda_txt.PlaceholderText = "Celda C19";
            celda_txt.Size = new Size(88, 27);

            insertar_referencia_btn.Location = new Point(193, 4);
            insertar_referencia_btn.Name = "insertar_referencia_btn";
            insertar_referencia_btn.Size = new Size(122, 29);
            insertar_referencia_btn.Text = "Insertar referencia";
            insertar_referencia_btn.UseVisualStyleBackColor = true;
            insertar_referencia_btn.Click += insertar_referencia_btn_Click;

            ConfigurarOperador(operador_suma_btn, "+", " + ", 322);
            ConfigurarOperador(operador_resta_btn, "−", " - ", 361);
            ConfigurarOperador(operador_igual_btn, "==", " == ", 400);
            ConfigurarOperador(operador_distinto_btn, "!=", " != ", 439);
            ConfigurarOperador(operador_and_btn, "&&", " && ", 478);
            ConfigurarOperador(operador_or_btn, "||", " || ", 517);
            ConfigurarOperador(operador_parentesis_btn, "( )", "()", 556);

            expresion_txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            expresion_txt.Location = new Point(12, 259);
            expresion_txt.Multiline = true;
            expresion_txt.Name = "expresion_txt";
            expresion_txt.ScrollBars = ScrollBars.Both;
            expresion_txt.Size = new Size(690, 115);
            expresion_txt.TabIndex = 2;
            expresion_txt.WordWrap = false;
            expresion_txt.TextChanged += expresion_txt_TextChanged;

            validar_btn.Location = new Point(12, 382);
            validar_btn.Name = "validar_btn";
            validar_btn.Size = new Size(105, 30);
            validar_btn.Text = "Validar";
            validar_btn.UseVisualStyleBackColor = true;
            validar_btn.Click += validar_btn_Click;

            probar_btn.Location = new Point(123, 382);
            probar_btn.Name = "probar_btn";
            probar_btn.Size = new Size(145, 30);
            probar_btn.Text = "Probar expresión";
            probar_btn.UseVisualStyleBackColor = true;
            probar_btn.Click += probar_btn_Click;

            validacion_lbl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            validacion_lbl.AutoEllipsis = true;
            validacion_lbl.Location = new Point(280, 386);
            validacion_lbl.Size = new Size(422, 25);
            validacion_lbl.Text = "La expresión está vacía.";

            referencias_lbl.AutoSize = true;
            referencias_lbl.Location = new Point(12, 423);
            referencias_lbl.Text = "Valores de prueba (se usan al probar la expresión)";

            referencias_dgv.AllowUserToAddRows = false;
            referencias_dgv.AllowUserToDeleteRows = false;
            referencias_dgv.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            referencias_dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            referencias_dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            referencias_dgv.Columns.AddRange(new DataGridViewColumn[] { col_referencia, col_valor });
            referencias_dgv.Location = new Point(12, 446);
            referencias_dgv.Name = "referencias_dgv";
            referencias_dgv.RowHeadersVisible = false;
            referencias_dgv.Size = new Size(690, 105);
            referencias_dgv.TabIndex = 4;

            col_referencia.HeaderText = "Referencia";
            col_referencia.Name = "col_referencia";
            col_referencia.ReadOnly = true;
            col_referencia.FillWeight = 40;
            col_valor.HeaderText = "Valor de prueba";
            col_valor.Name = "col_valor";
            col_valor.FillWeight = 60;

            mensaje_lbl.AutoSize = true;
            mensaje_lbl.Location = new Point(12, 568);
            mensaje_lbl.Text = "Mensaje";

            mensaje_txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            mensaje_txt.Location = new Point(12, 588);
            mensaje_txt.Multiline = true;
            mensaje_txt.Name = "mensaje_txt";
            mensaje_txt.ScrollBars = ScrollBars.Vertical;
            mensaje_txt.Size = new Size(690, 145);
            mensaje_txt.TabIndex = 3;

            // botones
            panel_bottom.Controls.Add(guardar_btn);
            panel_bottom.Controls.Add(eliminar_btn);
            panel_bottom.Controls.Add(duplicar_btn);
            panel_bottom.Controls.Add(editar_btn);
            panel_bottom.Controls.Add(nuevo_btn);
            panel_bottom.Dock = DockStyle.Bottom;
            panel_bottom.Height = 52;
            panel_bottom.Name = "panel_bottom";
            panel_bottom.Padding = new Padding(8, 8, 8, 8);

            nuevo_btn.Location = new Point(8, 9);
            nuevo_btn.Name = "nuevo_btn";
            nuevo_btn.Size = new Size(115, 32);
            nuevo_btn.Text = "Nueva regla";
            nuevo_btn.UseVisualStyleBackColor = true;
            nuevo_btn.Click += nuevo_btn_Click;

            editar_btn.Location = new Point(131, 9);
            editar_btn.Name = "editar_btn";
            editar_btn.Size = new Size(115, 32);
            editar_btn.Text = "Editar regla";
            editar_btn.Enabled = false;
            editar_btn.UseVisualStyleBackColor = true;
            editar_btn.Click += editar_btn_Click;

            duplicar_btn.Location = new Point(254, 9);
            duplicar_btn.Name = "duplicar_btn";
            duplicar_btn.Size = new Size(115, 32);
            duplicar_btn.Text = "Duplicar";
            duplicar_btn.Enabled = false;
            duplicar_btn.UseVisualStyleBackColor = true;
            duplicar_btn.Click += duplicar_btn_Click;

            eliminar_btn.Location = new Point(377, 9);
            eliminar_btn.Name = "eliminar_btn";
            eliminar_btn.Size = new Size(105, 32);
            eliminar_btn.Text = "Eliminar";
            eliminar_btn.Enabled = false;
            eliminar_btn.UseVisualStyleBackColor = true;
            eliminar_btn.Click += eliminar_btn_Click;

            guardar_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guardar_btn.Location = new Point(1240, 9);
            guardar_btn.Name = "guardar_btn";
            guardar_btn.Size = new Size(145, 32);
            guardar_btn.Text = "Guardar regla";
            guardar_btn.UseVisualStyleBackColor = true;
            guardar_btn.Click += guardar_btn_Click;

            // ReglaEditorForm
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1400, 850);
            Controls.Add(split_main);
            Controls.Add(panel_bottom);
            MinimumSize = new Size(1280, 780);
            Name = "ReglaEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Editor de Reglas";
            Load += ReglaEditorForm_Load;

            ((System.ComponentModel.ISupportInitialize)reglas_dgv).EndInit();
            split_main.Panel1.ResumeLayout(false);
            split_main.Panel2.ResumeLayout(false);
            split_main.ResumeLayout(false);
            panel_filtros.ResumeLayout(false);
            panel_filtros.PerformLayout();
            grp_formulario.ResumeLayout(false);
            grp_formulario.PerformLayout();
            panel_expresion_tools.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)referencias_dgv).EndInit();
            panel_bottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void ConfigurarOperador(Button boton, string texto, string tag, int x)
        {
            boton.Location = new Point(x, 4);
            boton.Size = new Size(36, 29);
            boton.Text = texto;
            boton.Tag = tag;
            boton.UseVisualStyleBackColor = true;
            boton.Click += operador_btn_Click;
            panel_expresion_tools.Controls.Add(boton);
        }

        #endregion

        private DataGridView reglas_dgv;
        private DataGridViewTextBoxColumn col_id;
        private DataGridViewTextBoxColumn col_serie;
        private DataGridViewTextBoxColumn col_version;
        private DataGridViewTextBoxColumn col_tipo;
        private DataGridViewTextBoxColumn col_expresion;
        private DataGridViewTextBoxColumn col_mensaje;

        private SplitContainer split_main;
        private Panel panel_filtros;
        private Label buscar_lbl;
        private TextBox buscar_txt;
        private Label serie_filtro_lbl;
        private ComboBox serie_filtro_cmb;
        private Label anio_filtro_lbl;
        private ComboBox anio_filtro_cmb;
        private Label version_filtro_lbl;
        private ComboBox version_filtro_cmb;
        private Label tipo_filtro_lbl;
        private ComboBox tipo_filtro_cmb;
        private Label reglas_count_lbl;

        private GroupBox grp_formulario;
        private Label id_lbl;
        private Label version_lbl;
        private ComboBox version_cmb;
        private Label tipo_lbl;
        private ComboBox tipo_cmb;
        private Label expresion_lbl;
        private Panel panel_expresion_tools;
        private TextBox hoja_txt;
        private TextBox celda_txt;
        private Button insertar_referencia_btn;
        private Button operador_suma_btn;
        private Button operador_resta_btn;
        private Button operador_igual_btn;
        private Button operador_distinto_btn;
        private Button operador_and_btn;
        private Button operador_or_btn;
        private Button operador_parentesis_btn;
        private TextBox expresion_txt;
        private Button validar_btn;
        private Button probar_btn;
        private Label validacion_lbl;
        private Label referencias_lbl;
        private DataGridView referencias_dgv;
        private DataGridViewTextBoxColumn col_referencia;
        private DataGridViewTextBoxColumn col_valor;
        private Label mensaje_lbl;
        private TextBox mensaje_txt;

        private Panel panel_bottom;
        private Button nuevo_btn;
        private Button editar_btn;
        private Button duplicar_btn;
        private Button eliminar_btn;
        private Button guardar_btn;
    }
}

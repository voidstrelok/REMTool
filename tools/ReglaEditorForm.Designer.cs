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
            var surface = Color.White;
            var canvas = Color.FromArgb(244, 247, 251);
            var ink = Color.FromArgb(24, 35, 54);
            var muted = Color.FromArgb(100, 116, 139);
            var border = Color.FromArgb(218, 226, 236);
            var primary = Color.FromArgb(37, 99, 235);
            var primarySoft = Color.FromArgb(232, 240, 255);

            reglas_dgv = new DataGridView();
            col_id = new DataGridViewTextBoxColumn();
            col_serie = new DataGridViewTextBoxColumn();
            col_version = new DataGridViewTextBoxColumn();
            col_tipo = new DataGridViewTextBoxColumn();
            col_expresion = new DataGridViewTextBoxColumn();
            col_mensaje = new DataGridViewTextBoxColumn();

            header_panel = new Panel();
            header_accent_panel = new Panel();
            header_title_lbl = new Label();
            header_subtitle_lbl = new Label();

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
            header_panel.SuspendLayout();
            SuspendLayout();

            // header_panel
            header_panel.BackColor = surface;
            header_panel.Controls.Add(header_subtitle_lbl);
            header_panel.Controls.Add(header_title_lbl);
            header_panel.Controls.Add(header_accent_panel);
            header_panel.Dock = DockStyle.Top;
            header_panel.Height = 78;
            header_panel.Padding = new Padding(28, 14, 28, 12);
            header_panel.Name = "header_panel";

            header_accent_panel.BackColor = primary;
            header_accent_panel.Location = new Point(28, 15);
            header_accent_panel.Size = new Size(5, 46);
            header_accent_panel.Name = "header_accent_panel";

            header_title_lbl.AutoSize = true;
            header_title_lbl.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            header_title_lbl.ForeColor = ink;
            header_title_lbl.Location = new Point(46, 10);
            header_title_lbl.Name = "header_title_lbl";
            header_title_lbl.Text = "Editor de reglas";

            header_subtitle_lbl.AutoSize = true;
            header_subtitle_lbl.Font = new Font("Segoe UI", 9F);
            header_subtitle_lbl.ForeColor = muted;
            header_subtitle_lbl.Location = new Point(48, 43);
            header_subtitle_lbl.Name = "header_subtitle_lbl";
            header_subtitle_lbl.Text = "Administra, valida y prueba las reglas del sistema";

            // reglas_dgv
            reglas_dgv.AllowUserToAddRows = false;
            reglas_dgv.AllowUserToDeleteRows = false;
            reglas_dgv.AllowUserToResizeRows = false;
            reglas_dgv.AutoGenerateColumns = false;
            reglas_dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            reglas_dgv.BackgroundColor = surface;
            reglas_dgv.BorderStyle = BorderStyle.None;
            reglas_dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            reglas_dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            reglas_dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(247, 249, 252),
                ForeColor = muted,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(247, 249, 252),
                SelectionForeColor = muted,
                Padding = new Padding(4, 0, 4, 0)
            };
            reglas_dgv.ColumnHeadersHeight = 38;
            reglas_dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            reglas_dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = surface,
                ForeColor = ink,
                SelectionBackColor = primarySoft,
                SelectionForeColor = ink,
                Padding = new Padding(4, 6, 4, 6),
                WrapMode = DataGridViewTriState.False
            };
            reglas_dgv.EnableHeadersVisualStyles = false;
            reglas_dgv.GridColor = Color.FromArgb(235, 239, 244);
            reglas_dgv.MultiSelect = false;
            reglas_dgv.RowHeadersVisible = false;
            reglas_dgv.RowTemplate.Height = 42;
            reglas_dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            reglas_dgv.Columns.AddRange(new DataGridViewColumn[] { col_id, col_serie, col_version, col_tipo, col_expresion, col_mensaje });
            reglas_dgv.Dock = DockStyle.Fill;
            reglas_dgv.Name = "reglas_dgv";
            reglas_dgv.ReadOnly = true;
            reglas_dgv.TabIndex = 1;
            reglas_dgv.SelectionChanged += reglas_dgv_SelectionChanged;
            reglas_dgv.CellClick += reglas_dgv_CellClick;
            reglas_dgv.CellDoubleClick += reglas_dgv_CellDoubleClick;

            col_id.HeaderText = "ID";
            col_id.Name = "col_id";
            col_id.Visible = false;
            col_serie.HeaderText = "Serie";
            col_serie.Name = "col_serie";
            col_serie.FillWeight = 13;
            col_version.HeaderText = "Versión";
            col_version.Name = "col_version";
            col_version.FillWeight = 20;
            col_tipo.HeaderText = "Tipo";
            col_tipo.Name = "col_tipo";
            col_tipo.FillWeight = 14;
            col_expresion.HeaderText = "Expresión";
            col_expresion.Name = "col_expresion";
            col_expresion.FillWeight = 25;
            col_mensaje.HeaderText = "Mensaje";
            col_mensaje.Name = "col_mensaje";
            col_mensaje.FillWeight = 28;

            // split_main
            split_main.Dock = DockStyle.Fill;
            split_main.FixedPanel = FixedPanel.Panel2;
            split_main.BackColor = canvas;
            split_main.Location = new Point(0, 78);
            split_main.Name = "split_main";
            split_main.Orientation = Orientation.Vertical;
            split_main.Panel1MinSize = 520;
            split_main.Panel2MinSize = 760;
            split_main.SplitterDistance = 610;
            split_main.SplitterWidth = 8;
            split_main.TabIndex = 0;

            split_main.Panel1.BackColor = surface;
            split_main.Panel1.Controls.Add(reglas_dgv);
            split_main.Panel1.Controls.Add(panel_filtros);
            split_main.Panel1.Padding = new Padding(0, 0, 1, 0);

            split_main.Panel2.BackColor = canvas;
            split_main.Panel2.Controls.Add(grp_formulario);
            split_main.Panel2.Padding = new Padding(16, 14, 16, 14);

            // panel_filtros
            panel_filtros.BackColor = surface;
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
            panel_filtros.Height = 148;
            panel_filtros.Name = "panel_filtros";
            panel_filtros.Padding = new Padding(18, 12, 18, 10);

            buscar_lbl.AutoSize = true;
            buscar_lbl.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            buscar_lbl.ForeColor = muted;
            buscar_lbl.Location = new Point(18, 12);
            buscar_lbl.Text = "BUSCAR REGLA";

            buscar_txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            buscar_txt.BackColor = Color.FromArgb(248, 250, 252);
            buscar_txt.BorderStyle = BorderStyle.FixedSingle;
            buscar_txt.Font = new Font("Segoe UI", 9.5F);
            buscar_txt.Location = new Point(18, 33);
            buscar_txt.Name = "buscar_txt";
            buscar_txt.PlaceholderText = "Expresión, mensaje o tipo...";
            buscar_txt.Size = new Size(550, 29);
            buscar_txt.TextChanged += buscar_txt_TextChanged;

            serie_filtro_lbl.AutoSize = true;
            serie_filtro_lbl.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            serie_filtro_lbl.ForeColor = muted;
            serie_filtro_lbl.Location = new Point(18, 78);
            serie_filtro_lbl.Text = "SERIE";
            serie_filtro_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            serie_filtro_cmb.FlatStyle = FlatStyle.Flat;
            serie_filtro_cmb.FormattingEnabled = true;
            serie_filtro_cmb.Location = new Point(18, 96);
            serie_filtro_cmb.Name = "serie_filtro_cmb";
            serie_filtro_cmb.Size = new Size(112, 28);
            serie_filtro_cmb.SelectedIndexChanged += serie_filtro_cmb_SelectedIndexChanged;

            anio_filtro_lbl.AutoSize = true;
            anio_filtro_lbl.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            anio_filtro_lbl.ForeColor = muted;
            anio_filtro_lbl.Location = new Point(140, 78);
            anio_filtro_lbl.Text = "AÑO";
            anio_filtro_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            anio_filtro_cmb.FlatStyle = FlatStyle.Flat;
            anio_filtro_cmb.FormattingEnabled = true;
            anio_filtro_cmb.Location = new Point(140, 96);
            anio_filtro_cmb.Name = "anio_filtro_cmb";
            anio_filtro_cmb.Size = new Size(70, 28);
            anio_filtro_cmb.SelectedIndexChanged += anio_filtro_cmb_SelectedIndexChanged;

            version_filtro_lbl.AutoSize = true;
            version_filtro_lbl.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            version_filtro_lbl.ForeColor = muted;
            version_filtro_lbl.Location = new Point(222, 78);
            version_filtro_lbl.Text = "VERSIÓN";
            version_filtro_cmb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            version_filtro_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            version_filtro_cmb.FlatStyle = FlatStyle.Flat;
            version_filtro_cmb.FormattingEnabled = true;
            version_filtro_cmb.Location = new Point(222, 96);
            version_filtro_cmb.Name = "version_filtro_cmb";
            version_filtro_cmb.Size = new Size(180, 28);
            version_filtro_cmb.SelectedIndexChanged += version_filtro_cmb_SelectedIndexChanged;

            tipo_filtro_lbl.AutoSize = true;
            tipo_filtro_lbl.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            tipo_filtro_lbl.ForeColor = muted;
            tipo_filtro_lbl.Location = new Point(414, 78);
            tipo_filtro_lbl.Text = "TIPO";
            tipo_filtro_cmb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tipo_filtro_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            tipo_filtro_cmb.FlatStyle = FlatStyle.Flat;
            tipo_filtro_cmb.FormattingEnabled = true;
            tipo_filtro_cmb.Location = new Point(414, 96);
            tipo_filtro_cmb.Name = "tipo_filtro_cmb";
            tipo_filtro_cmb.Size = new Size(154, 28);
            tipo_filtro_cmb.SelectedIndexChanged += tipo_filtro_cmb_SelectedIndexChanged;

            reglas_count_lbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            reglas_count_lbl.AutoSize = true;
            reglas_count_lbl.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            reglas_count_lbl.ForeColor = primary;
            reglas_count_lbl.Location = new Point(492, 14);
            reglas_count_lbl.Text = "0 reglas";

            // grp_formulario
            grp_formulario.BackColor = surface;
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
            grp_formulario.ForeColor = ink;
            grp_formulario.Name = "grp_formulario";
            grp_formulario.Padding = new Padding(16, 28, 16, 16);
            grp_formulario.Text = "Detalle de la regla";

            id_lbl.AutoSize = true;
            id_lbl.Font = new Font("Segoe UI", 8.5F);
            id_lbl.ForeColor = muted;
            id_lbl.Location = new Point(16, 31);
            id_lbl.Text = "Nueva regla";

            version_lbl.AutoSize = true;
            version_lbl.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            version_lbl.ForeColor = muted;
            version_lbl.Location = new Point(16, 58);
            version_lbl.Text = "VERSIÓN";
            version_cmb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            version_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            version_cmb.FlatStyle = FlatStyle.Flat;
            version_cmb.FormattingEnabled = true;
            version_cmb.Location = new Point(16, 77);
            version_cmb.Name = "version_cmb";
            version_cmb.Size = new Size(720, 29);

            tipo_lbl.AutoSize = true;
            tipo_lbl.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            tipo_lbl.ForeColor = muted;
            tipo_lbl.Location = new Point(16, 117);
            tipo_lbl.Text = "TIPO DE REGLA";
            tipo_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            tipo_cmb.FlatStyle = FlatStyle.Flat;
            tipo_cmb.FormattingEnabled = true;
            tipo_cmb.Location = new Point(16, 136);
            tipo_cmb.Name = "tipo_cmb";
            tipo_cmb.Size = new Size(270, 29);

            expresion_lbl.AutoSize = true;
            expresion_lbl.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            expresion_lbl.ForeColor = muted;
            expresion_lbl.Location = new Point(16, 181);
            expresion_lbl.Text = "EXPRESIÓN";

            // panel_expresion_tools
            panel_expresion_tools.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel_expresion_tools.BackColor = Color.FromArgb(248, 250, 252);
            panel_expresion_tools.BorderStyle = BorderStyle.FixedSingle;
            panel_expresion_tools.Location = new Point(16, 202);
            panel_expresion_tools.Name = "panel_expresion_tools";
            panel_expresion_tools.Size = new Size(720, 55);

            hoja_txt.BorderStyle = BorderStyle.FixedSingle;
            hoja_txt.Location = new Point(8, 12);
            hoja_txt.Name = "hoja_txt";
            hoja_txt.PlaceholderText = "Hoja A01";
            hoja_txt.Size = new Size(84, 27);

            celda_txt.BorderStyle = BorderStyle.FixedSingle;
            celda_txt.Location = new Point(98, 12);
            celda_txt.Name = "celda_txt";
            celda_txt.PlaceholderText = "Celda C19";
            celda_txt.Size = new Size(84, 27);

            insertar_referencia_btn.Location = new Point(190, 10);
            insertar_referencia_btn.Name = "insertar_referencia_btn";
            insertar_referencia_btn.Size = new Size(132, 31);
            insertar_referencia_btn.Text = "＋ Referencia";
            ConfigurarBotonSecundario(insertar_referencia_btn, border, ink);
            insertar_referencia_btn.Click += insertar_referencia_btn_Click;

            ConfigurarOperador(operador_suma_btn, "+", " + ", 336, border, ink);
            ConfigurarOperador(operador_resta_btn, "−", " - ", 374, border, ink);
            ConfigurarOperador(operador_igual_btn, "==", " == ", 412, border, ink);
            ConfigurarOperador(operador_distinto_btn, "!=", " != ", 450, border, ink);
            ConfigurarOperador(operador_and_btn, "&&", " && ", 488, border, ink);
            ConfigurarOperador(operador_or_btn, "||", " || ", 526, border, ink);
            ConfigurarOperador(operador_parentesis_btn, "( )", "()", 564, border, ink);

            expresion_txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            expresion_txt.BackColor = Color.FromArgb(252, 253, 255);
            expresion_txt.BorderStyle = BorderStyle.FixedSingle;
            expresion_txt.Font = new Font("Cascadia Mono", 10F);
            expresion_txt.Location = new Point(16, 266);
            expresion_txt.Multiline = true;
            expresion_txt.Name = "expresion_txt";
            expresion_txt.ScrollBars = ScrollBars.Both;
            expresion_txt.Size = new Size(720, 112);
            expresion_txt.TabIndex = 2;
            expresion_txt.WordWrap = false;
            expresion_txt.TextChanged += expresion_txt_TextChanged;

            validar_btn.Location = new Point(16, 389);
            validar_btn.Name = "validar_btn";
            validar_btn.Size = new Size(96, 31);
            validar_btn.Text = "✓ Validar";
            ConfigurarBotonSecundario(validar_btn, border, ink);
            validar_btn.Click += validar_btn_Click;

            probar_btn.Location = new Point(120, 389);
            probar_btn.Name = "probar_btn";
            probar_btn.Size = new Size(126, 31);
            probar_btn.Text = "▶ Probar";
            ConfigurarBotonSecundario(probar_btn, border, ink);
            probar_btn.Click += probar_btn_Click;

            validacion_lbl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            validacion_lbl.AutoEllipsis = true;
            validacion_lbl.BackColor = Color.FromArgb(248, 250, 252);
            validacion_lbl.BorderStyle = BorderStyle.FixedSingle;
            validacion_lbl.Font = new Font("Segoe UI", 8.5F);
            validacion_lbl.Location = new Point(260, 389);
            validacion_lbl.Padding = new Padding(9, 6, 9, 4);
            validacion_lbl.Size = new Size(476, 31);
            validacion_lbl.Text = "La expresión está vacía.";

            referencias_lbl.AutoSize = true;
            referencias_lbl.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            referencias_lbl.ForeColor = muted;
            referencias_lbl.Location = new Point(16, 437);
            referencias_lbl.Text = "VALORES DE PRUEBA";

            referencias_dgv.AllowUserToAddRows = false;
            referencias_dgv.AllowUserToDeleteRows = false;
            referencias_dgv.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            referencias_dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            referencias_dgv.BackgroundColor = surface;
            referencias_dgv.BorderStyle = BorderStyle.FixedSingle;
            referencias_dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            referencias_dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(247, 249, 252),
                ForeColor = muted,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            referencias_dgv.ColumnHeadersHeight = 31;
            referencias_dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            referencias_dgv.Columns.AddRange(new DataGridViewColumn[] { col_referencia, col_valor });
            referencias_dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = surface,
                ForeColor = ink,
                SelectionBackColor = primarySoft,
                SelectionForeColor = ink,
                Padding = new Padding(4, 4, 4, 4)
            };
            referencias_dgv.EnableHeadersVisualStyles = false;
            referencias_dgv.GridColor = Color.FromArgb(235, 239, 244);
            referencias_dgv.Location = new Point(16, 458);
            referencias_dgv.Name = "referencias_dgv";
            referencias_dgv.RowHeadersVisible = false;
            referencias_dgv.RowTemplate.Height = 30;
            referencias_dgv.Size = new Size(720, 96);
            referencias_dgv.TabIndex = 4;

            col_referencia.HeaderText = "Referencia";
            col_referencia.Name = "col_referencia";
            col_referencia.ReadOnly = true;
            col_referencia.FillWeight = 40;
            col_valor.HeaderText = "Valor de prueba";
            col_valor.Name = "col_valor";
            col_valor.FillWeight = 60;

            mensaje_lbl.AutoSize = true;
            mensaje_lbl.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            mensaje_lbl.ForeColor = muted;
            mensaje_lbl.Location = new Point(16, 575);
            mensaje_lbl.Text = "MENSAJE QUE SE MOSTRARÁ";

            mensaje_txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            mensaje_txt.BackColor = Color.FromArgb(252, 253, 255);
            mensaje_txt.BorderStyle = BorderStyle.FixedSingle;
            mensaje_txt.Location = new Point(16, 596);
            mensaje_txt.Multiline = true;
            mensaje_txt.Name = "mensaje_txt";
            mensaje_txt.ScrollBars = ScrollBars.Vertical;
            mensaje_txt.Size = new Size(720, 104);
            mensaje_txt.TabIndex = 3;

            // panel_bottom
            panel_bottom.BackColor = surface;
            panel_bottom.Controls.Add(guardar_btn);
            panel_bottom.Controls.Add(eliminar_btn);
            panel_bottom.Controls.Add(duplicar_btn);
            panel_bottom.Controls.Add(editar_btn);
            panel_bottom.Controls.Add(nuevo_btn);
            panel_bottom.Dock = DockStyle.Bottom;
            panel_bottom.Height = 64;
            panel_bottom.Name = "panel_bottom";
            panel_bottom.Padding = new Padding(24, 14, 24, 12);

            nuevo_btn.Location = new Point(24, 14);
            nuevo_btn.Name = "nuevo_btn";
            nuevo_btn.Size = new Size(118, 36);
            nuevo_btn.Text = "＋ Nueva regla";
            ConfigurarBotonSecundario(nuevo_btn, border, ink);
            nuevo_btn.Click += nuevo_btn_Click;

            editar_btn.Location = new Point(150, 14);
            editar_btn.Name = "editar_btn";
            editar_btn.Size = new Size(108, 36);
            editar_btn.Text = "Editar";
            editar_btn.Enabled = false;
            ConfigurarBotonSecundario(editar_btn, border, ink);
            editar_btn.Click += editar_btn_Click;

            duplicar_btn.Location = new Point(266, 14);
            duplicar_btn.Name = "duplicar_btn";
            duplicar_btn.Size = new Size(108, 36);
            duplicar_btn.Text = "Duplicar";
            duplicar_btn.Enabled = false;
            ConfigurarBotonSecundario(duplicar_btn, border, ink);
            duplicar_btn.Click += duplicar_btn_Click;

            eliminar_btn.Location = new Point(382, 14);
            eliminar_btn.Name = "eliminar_btn";
            eliminar_btn.Size = new Size(108, 36);
            eliminar_btn.Text = "Eliminar";
            eliminar_btn.Enabled = false;
            ConfigurarBotonSecundario(eliminar_btn, border, Color.FromArgb(180, 52, 52));
            eliminar_btn.Click += eliminar_btn_Click;

            guardar_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guardar_btn.Location = new Point(1234, 14);
            guardar_btn.Name = "guardar_btn";
            guardar_btn.Size = new Size(142, 36);
            guardar_btn.Text = "Guardar regla";
            ConfigurarBotonPrimario(guardar_btn, primary);
            guardar_btn.Click += guardar_btn_Click;

            // ReglaEditorForm
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = canvas;
            ClientSize = new Size(1440, 900);
            Controls.Add(split_main);
            Controls.Add(panel_bottom);
            Controls.Add(header_panel);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(1280, 820);
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
            header_panel.ResumeLayout(false);
            header_panel.PerformLayout();
            ResumeLayout(false);
        }

        private void ConfigurarOperador(Button boton, string texto, string tag, int x, Color border, Color ink)
        {
            boton.Location = new Point(x, 11);
            boton.Size = new Size(34, 31);
            boton.Text = texto;
            boton.Tag = tag;
            ConfigurarBotonSecundario(boton, border, ink);
            boton.Click += operador_btn_Click;
            panel_expresion_tools.Controls.Add(boton);
        }

        private static void ConfigurarBotonSecundario(Button boton, Color border, Color ink)
        {
            boton.BackColor = Color.White;
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderColor = border;
            boton.FlatAppearance.BorderSize = 1;
            boton.ForeColor = ink;
            boton.Cursor = Cursors.Hand;
            boton.UseVisualStyleBackColor = false;
        }

        private static void ConfigurarBotonPrimario(Button boton, Color primary)
        {
            boton.BackColor = primary;
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 0;
            boton.ForeColor = Color.White;
            boton.Cursor = Cursors.Hand;
            boton.UseVisualStyleBackColor = false;
        }

        #endregion

        private DataGridView reglas_dgv;
        private DataGridViewTextBoxColumn col_id;
        private DataGridViewTextBoxColumn col_serie;
        private DataGridViewTextBoxColumn col_version;
        private DataGridViewTextBoxColumn col_tipo;
        private DataGridViewTextBoxColumn col_expresion;
        private DataGridViewTextBoxColumn col_mensaje;

        private Panel header_panel;
        private Panel header_accent_panel;
        private Label header_title_lbl;
        private Label header_subtitle_lbl;

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

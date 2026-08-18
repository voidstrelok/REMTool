namespace RemTools
{
    partial class PuntoResumenEditorForm
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
            dgv = new DataGridView();
            col_id = new DataGridViewTextBoxColumn();
            col_serie = new DataGridViewTextBoxColumn();
            col_categoria = new DataGridViewTextBoxColumn();
            col_nombre = new DataGridViewTextBoxColumn();
            col_expresion = new DataGridViewTextBoxColumn();

            grp_form = new GroupBox();
            lbl_nombre = new Label();
            nombre_txt = new TextBox();
            lbl_categoria = new Label();
            categoria_txt = new TextBox();
            lbl_expresion = new Label();
            expresion_txt = new TextBox();
            lbl_serie = new Label();
            serie_cmb = new ComboBox();
            lbl_hint = new Label();

            pnl_buttons = new Panel();
            nuevo_btn = new Button();
            guardar_btn = new Button();
            eliminar_btn = new Button();

            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            grp_form.SuspendLayout();
            pnl_buttons.SuspendLayout();
            SuspendLayout();

            // ── dgv ──────────────────────────────────────────────────
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.Location = new Point(8, 8);
            dgv.Name = "dgv";
            dgv.Size = new Size(440, 490);
            dgv.TabIndex = 0;
            dgv.SelectionChanged += dgv_SelectionChanged;

            col_id.Name = "col_id";
            col_id.HeaderText = "Id";
            col_id.Visible = false;

            col_serie.Name = "col_serie";
            col_serie.HeaderText = "Serie";
            col_serie.FillWeight = 15;

            col_categoria.Name = "col_categoria";
            col_categoria.HeaderText = "Categoría";
            col_categoria.FillWeight = 25;

            col_nombre.Name = "col_nombre";
            col_nombre.HeaderText = "Nombre";
            col_nombre.FillWeight = 30;

            col_expresion.Name = "col_expresion";
            col_expresion.HeaderText = "Expresión";
            col_expresion.FillWeight = 30;

            dgv.Columns.AddRange(col_id, col_serie, col_categoria, col_nombre, col_expresion);

            // ── grp_form ─────────────────────────────────────────────
            grp_form.Text = "Punto Resumen";
            grp_form.Location = new Point(460, 8);
            grp_form.Name = "grp_form";
            grp_form.Size = new Size(310, 490);
            grp_form.TabIndex = 1;

            lbl_serie.Text = "Serie";
            lbl_serie.Location = new Point(10, 24);
            lbl_serie.Size = new Size(290, 18);

            serie_cmb.Location = new Point(10, 44);
            serie_cmb.Name = "serie_cmb";
            serie_cmb.Size = new Size(290, 26);
            serie_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            serie_cmb.TabIndex = 0;

            lbl_categoria.Text = "Categoría";
            lbl_categoria.Location = new Point(10, 82);
            lbl_categoria.Size = new Size(290, 18);

            categoria_txt.Location = new Point(10, 102);
            categoria_txt.Name = "categoria_txt";
            categoria_txt.Size = new Size(290, 26);
            categoria_txt.TabIndex = 1;

            lbl_nombre.Text = "Nombre";
            lbl_nombre.Location = new Point(10, 140);
            lbl_nombre.Size = new Size(290, 18);

            nombre_txt.Location = new Point(10, 160);
            nombre_txt.Name = "nombre_txt";
            nombre_txt.Size = new Size(290, 26);
            nombre_txt.TabIndex = 2;
            nombre_txt.TextChanged += nombre_txt_TextChanged;

            lbl_expresion.Text = "Expresión";
            lbl_expresion.Location = new Point(10, 198);
            lbl_expresion.Size = new Size(290, 18);

            expresion_txt.Location = new Point(10, 218);
            expresion_txt.Name = "expresion_txt";
            expresion_txt.Size = new Size(290, 26);
            expresion_txt.TabIndex = 3;

            lbl_hint.Text = "Formato: [CodigoPrestacion][N]\nEjemplo: [A010201][1] + [A010202][1]\n(N = índice 1-based de la columna)";
            lbl_hint.Location = new Point(10, 256);
            lbl_hint.Size = new Size(290, 60);
            lbl_hint.ForeColor = SystemColors.GrayText;
            lbl_hint.Font = new Font(lbl_hint.Font.FontFamily, 8f);

            grp_form.Controls.AddRange(new Control[] {
                lbl_serie, serie_cmb,
                lbl_categoria, categoria_txt,
                lbl_nombre, nombre_txt,
                lbl_expresion, expresion_txt,
                lbl_hint
            });

            // ── pnl_buttons ─────────────────────────────────────────
            pnl_buttons.Location = new Point(8, 510);
            pnl_buttons.Name = "pnl_buttons";
            pnl_buttons.Size = new Size(762, 44);
            pnl_buttons.TabIndex = 2;

            nuevo_btn.Location = new Point(0, 6);
            nuevo_btn.Name = "nuevo_btn";
            nuevo_btn.Size = new Size(120, 32);
            nuevo_btn.TabIndex = 0;
            nuevo_btn.Text = "Nuevo";
            nuevo_btn.UseVisualStyleBackColor = true;
            nuevo_btn.Click += nuevo_btn_Click;

            guardar_btn.Location = new Point(128, 6);
            guardar_btn.Name = "guardar_btn";
            guardar_btn.Size = new Size(120, 32);
            guardar_btn.TabIndex = 1;
            guardar_btn.Text = "Guardar";
            guardar_btn.UseVisualStyleBackColor = true;
            guardar_btn.Click += guardar_btn_Click;

            eliminar_btn.Location = new Point(256, 6);
            eliminar_btn.Name = "eliminar_btn";
            eliminar_btn.Size = new Size(120, 32);
            eliminar_btn.TabIndex = 2;
            eliminar_btn.Text = "Eliminar";
            eliminar_btn.Enabled = false;
            eliminar_btn.UseVisualStyleBackColor = true;
            eliminar_btn.Click += eliminar_btn_Click;

            pnl_buttons.Controls.AddRange(new Control[] { nuevo_btn, guardar_btn, eliminar_btn });

            // ── Form ─────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(778, 562);
            Controls.AddRange(new Control[] { dgv, grp_form, pnl_buttons });
            Name = "PuntoResumenEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Editor de Puntos Resumen";
            Load += PuntoResumenEditorForm_Load;

            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            grp_form.ResumeLayout(false);
            pnl_buttons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dgv;
        private DataGridViewTextBoxColumn col_id;
        private DataGridViewTextBoxColumn col_serie;
        private DataGridViewTextBoxColumn col_categoria;
        private DataGridViewTextBoxColumn col_nombre;
        private DataGridViewTextBoxColumn col_expresion;

        private GroupBox grp_form;
        private Label lbl_serie;
        private ComboBox serie_cmb;
        private Label lbl_categoria;
        private TextBox categoria_txt;
        private Label lbl_nombre;
        private TextBox nombre_txt;
        private Label lbl_expresion;
        private TextBox expresion_txt;
        private Label lbl_hint;

        private Panel pnl_buttons;
        private Button nuevo_btn;
        private Button guardar_btn;
        private Button eliminar_btn;
    }
}

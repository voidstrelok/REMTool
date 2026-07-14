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
            panel_top = new Panel();
            serie_lbl = new Label();
            serie_cmb = new ComboBox();
            anio_lbl = new Label();
            anio_cmb = new ComboBox();
            version_lbl = new Label();
            version_cmb = new ComboBox();
            panel_bottom = new Panel();
            guardar_btn = new Button();
            eliminar_btn = new Button();
            nueva_btn = new Button();
            reglas_dgv = new DataGridView();
            col_Id = new DataGridViewTextBoxColumn();
            col_Expresion = new DataGridViewTextBoxColumn();
            col_Mensaje = new DataGridViewTextBoxColumn();
            panel_top.SuspendLayout();
            panel_bottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)reglas_dgv).BeginInit();
            SuspendLayout();
            // 
            // panel_top
            // 
            panel_top.Controls.Add(version_cmb);
            panel_top.Controls.Add(version_lbl);
            panel_top.Controls.Add(anio_cmb);
            panel_top.Controls.Add(anio_lbl);
            panel_top.Controls.Add(serie_cmb);
            panel_top.Controls.Add(serie_lbl);
            panel_top.Dock = DockStyle.Top;
            panel_top.Location = new Point(0, 0);
            panel_top.Name = "panel_top";
            panel_top.Padding = new Padding(8, 6, 8, 6);
            panel_top.Size = new Size(784, 42);
            panel_top.TabIndex = 0;
            // 
            // version_lbl
            // 
            version_lbl.AutoSize = true;
            version_lbl.Location = new Point(350, 12);
            version_lbl.Name = "version_lbl";
            version_lbl.Size = new Size(57, 20);
            version_lbl.TabIndex = 0;
            version_lbl.Text = "Versión:";
            // 
            // serie_lbl
            // 
            serie_lbl.AutoSize = true;
            serie_lbl.Location = new Point(8, 12);
            serie_lbl.Name = "serie_lbl";
            serie_lbl.TabIndex = 2;
            serie_lbl.Text = "Serie:";
            // 
            // serie_cmb
            // 
            serie_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            serie_cmb.FormattingEnabled = true;
            serie_cmb.Location = new Point(62, 9);
            serie_cmb.Name = "serie_cmb";
            serie_cmb.Size = new Size(155, 28);
            serie_cmb.TabIndex = 1;
            serie_cmb.SelectedIndexChanged += serie_cmb_SelectedIndexChanged;
            // 
            // anio_lbl
            // 
            anio_lbl.AutoSize = true;
            anio_lbl.Location = new Point(225, 12);
            anio_lbl.Name = "anio_lbl";
            anio_lbl.TabIndex = 3;
            anio_lbl.Text = "A\u00F1o:";
            // 
            // anio_cmb
            // 
            anio_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            anio_cmb.FormattingEnabled = true;
            anio_cmb.Location = new Point(266, 9);
            anio_cmb.Name = "anio_cmb";
            anio_cmb.Size = new Size(75, 28);
            anio_cmb.TabIndex = 2;
            anio_cmb.SelectedIndexChanged += anio_cmb_SelectedIndexChanged;
            // 
            // version_cmb
            // 
            version_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            version_cmb.FormattingEnabled = true;
            version_cmb.Location = new Point(416, 9);
            version_cmb.Name = "version_cmb";
            version_cmb.Size = new Size(352, 28);
            version_cmb.TabIndex = 3;
            version_cmb.SelectedIndexChanged += version_cmb_SelectedIndexChanged;
            // 
            // panel_bottom
            // 
            panel_bottom.Controls.Add(guardar_btn);
            panel_bottom.Controls.Add(eliminar_btn);
            panel_bottom.Controls.Add(nueva_btn);
            panel_bottom.Dock = DockStyle.Bottom;
            panel_bottom.Location = new Point(0, 478);
            panel_bottom.Name = "panel_bottom";
            panel_bottom.Padding = new Padding(8, 6, 8, 6);
            panel_bottom.Size = new Size(784, 48);
            panel_bottom.TabIndex = 2;
            // 
            // nueva_btn
            // 
            nueva_btn.Location = new Point(8, 8);
            nueva_btn.Name = "nueva_btn";
            nueva_btn.Size = new Size(130, 32);
            nueva_btn.TabIndex = 0;
            nueva_btn.Text = "Nueva regla";
            nueva_btn.UseVisualStyleBackColor = true;
            nueva_btn.Click += nueva_btn_Click;
            // 
            // eliminar_btn
            // 
            eliminar_btn.Location = new Point(146, 8);
            eliminar_btn.Name = "eliminar_btn";
            eliminar_btn.Size = new Size(130, 32);
            eliminar_btn.TabIndex = 1;
            eliminar_btn.Text = "Eliminar";
            eliminar_btn.UseVisualStyleBackColor = true;
            eliminar_btn.Click += eliminar_btn_Click;
            // 
            // guardar_btn
            // 
            guardar_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guardar_btn.Location = new Point(638, 8);
            guardar_btn.Name = "guardar_btn";
            guardar_btn.Size = new Size(138, 32);
            guardar_btn.TabIndex = 2;
            guardar_btn.Text = "Guardar cambios";
            guardar_btn.UseVisualStyleBackColor = true;
            guardar_btn.Click += guardar_btn_Click;
            // 
            // reglas_dgv
            // 
            reglas_dgv.AllowUserToAddRows = false;
            reglas_dgv.AllowUserToDeleteRows = false;
            reglas_dgv.AutoGenerateColumns = false;
            reglas_dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            reglas_dgv.Columns.AddRange(new DataGridViewColumn[] { col_Id, col_Expresion, col_Mensaje });
            reglas_dgv.Dock = DockStyle.Fill;
            reglas_dgv.Location = new Point(0, 42);
            reglas_dgv.Name = "reglas_dgv";
            reglas_dgv.RowHeadersWidth = 51;
            reglas_dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            reglas_dgv.Size = new Size(784, 436);
            reglas_dgv.TabIndex = 1;
            reglas_dgv.CellEndEdit += reglas_dgv_CellEndEdit;
            // 
            // col_Id
            // 
            col_Id.DataPropertyName = "Id";
            col_Id.HeaderText = "ID";
            col_Id.Name = "col_Id";
            col_Id.ReadOnly = true;
            col_Id.Width = 60;
            // 
            // col_Expresion
            // 
            col_Expresion.DataPropertyName = "Expresion";
            col_Expresion.HeaderText = "Expresión";
            col_Expresion.Name = "col_Expresion";
            col_Expresion.Width = 380;
            // 
            // col_Mensaje
            // 
            col_Mensaje.DataPropertyName = "Mensaje";
            col_Mensaje.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            col_Mensaje.HeaderText = "Mensaje";
            col_Mensaje.Name = "col_Mensaje";
            // 
            // ReglaEditorForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 526);
            Controls.Add(reglas_dgv);
            Controls.Add(panel_bottom);
            Controls.Add(panel_top);
            MinimumSize = new Size(600, 400);
            Name = "ReglaEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Editor de Reglas";
            Load += ReglaEditorForm_Load;
            panel_top.ResumeLayout(false);
            panel_top.PerformLayout();
            panel_bottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)reglas_dgv).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel_top;
        private Label serie_lbl;
        private ComboBox serie_cmb;
        private Label anio_lbl;
        private ComboBox anio_cmb;
        private Label version_lbl;
        private ComboBox version_cmb;
        private DataGridView reglas_dgv;
        private DataGridViewTextBoxColumn col_Id;
        private DataGridViewTextBoxColumn col_Expresion;
        private DataGridViewTextBoxColumn col_Mensaje;
        private Panel panel_bottom;
        private Button nueva_btn;
        private Button eliminar_btn;
        private Button guardar_btn;
    }
}

namespace RemTools
{
    partial class VisorReportesForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            dgvReportes = new DataGridView();
            btnAbrir = new Button();
            lblTitulo = new Label();
            lblFiltroEstab = new Label();
            txtFiltroEstab = new TextBox();
            lblFiltroMes = new Label();
            cmbFiltroMes = new ComboBox();
            lblFiltroSerie = new Label();
            cmbFiltroSerie = new ComboBox();
            lblFiltroVersion = new Label();
            cmbFiltroVersion = new ComboBox();
            btnFiltrar = new Button();
            btnLimpiar = new Button();
            btnEliminar = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvReportes).BeginInit();
            SuspendLayout();
            // 
            // lblTitulo
            // 
            lblTitulo.Location = new Point(12, 9);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(760, 24);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "Reportes en base de datos";
            lblTitulo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            // 
            // lblFiltroEstab
            // 
            lblFiltroEstab.Location = new Point(12, 42);
            lblFiltroEstab.Name = "lblFiltroEstab";
            lblFiltroEstab.Size = new Size(100, 22);
            lblFiltroEstab.TabIndex = 1;
            lblFiltroEstab.Text = "Establecimiento:";
            lblFiltroEstab.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txtFiltroEstab
            // 
            txtFiltroEstab.Location = new Point(116, 41);
            txtFiltroEstab.Name = "txtFiltroEstab";
            txtFiltroEstab.Size = new Size(180, 27);
            txtFiltroEstab.TabIndex = 2;
            // 
            // lblFiltroMes
            // 
            lblFiltroMes.Location = new Point(305, 42);
            lblFiltroMes.Name = "lblFiltroMes";
            lblFiltroMes.Size = new Size(36, 22);
            lblFiltroMes.TabIndex = 3;
            lblFiltroMes.Text = "Mes:";
            lblFiltroMes.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbFiltroMes
            // 
            cmbFiltroMes.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFiltroMes.Location = new Point(344, 41);
            cmbFiltroMes.Name = "cmbFiltroMes";
            cmbFiltroMes.Size = new Size(70, 28);
            cmbFiltroMes.TabIndex = 4;
            // 
            // lblFiltroSerie
            // 
            lblFiltroSerie.Location = new Point(423, 42);
            lblFiltroSerie.Name = "lblFiltroSerie";
            lblFiltroSerie.Size = new Size(42, 22);
            lblFiltroSerie.TabIndex = 5;
            lblFiltroSerie.Text = "Serie:";
            lblFiltroSerie.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbFiltroSerie
            // 
            cmbFiltroSerie.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFiltroSerie.Location = new Point(468, 41);
            cmbFiltroSerie.Name = "cmbFiltroSerie";
            cmbFiltroSerie.Size = new Size(90, 28);
            cmbFiltroSerie.TabIndex = 6;
            // 
            // lblFiltroVersion
            // 
            lblFiltroVersion.Location = new Point(567, 42);
            lblFiltroVersion.Name = "lblFiltroVersion";
            lblFiltroVersion.Size = new Size(56, 22);
            lblFiltroVersion.TabIndex = 7;
            lblFiltroVersion.Text = "Versión:";
            lblFiltroVersion.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbFiltroVersion
            // 
            cmbFiltroVersion.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFiltroVersion.Location = new Point(626, 41);
            cmbFiltroVersion.Name = "cmbFiltroVersion";
            cmbFiltroVersion.Size = new Size(146, 28);
            cmbFiltroVersion.TabIndex = 8;
            // 
            // btnFiltrar
            // 
            btnFiltrar.Location = new Point(12, 76);
            btnFiltrar.Name = "btnFiltrar";
            btnFiltrar.Size = new Size(370, 30);
            btnFiltrar.TabIndex = 9;
            btnFiltrar.Text = "Filtrar";
            btnFiltrar.UseVisualStyleBackColor = true;
            btnFiltrar.Click += btnFiltrar_Click;
            // 
            // btnLimpiar
            // 
            btnLimpiar.Location = new Point(390, 76);
            btnLimpiar.Name = "btnLimpiar";
            btnLimpiar.Size = new Size(382, 30);
            btnLimpiar.TabIndex = 10;
            btnLimpiar.Text = "Limpiar filtros";
            btnLimpiar.UseVisualStyleBackColor = true;
            btnLimpiar.Click += btnLimpiar_Click;
            // 
            // dgvReportes
            // 
            dgvReportes.AllowUserToAddRows = false;
            dgvReportes.AllowUserToDeleteRows = false;
            dgvReportes.ReadOnly = true;
            dgvReportes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReportes.MultiSelect = true;
            dgvReportes.Location = new Point(12, 114);
            dgvReportes.Name = "dgvReportes";
            dgvReportes.Size = new Size(760, 400);
            dgvReportes.TabIndex = 11;
            dgvReportes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReportes.DoubleClick += dgvReportes_DoubleClick;
            // 
            // btnAbrir
            // 
            btnAbrir.Location = new Point(12, 522);
            btnAbrir.Name = "btnAbrir";
            btnAbrir.Size = new Size(570, 40);
            btnAbrir.TabIndex = 12;
            btnAbrir.Text = "Ver Registros del Reporte Seleccionado";
            btnAbrir.UseVisualStyleBackColor = true;
            btnAbrir.Click += btnAbrir_Click;
            // 
            // btnEliminar
            // 
            btnEliminar.Location = new Point(590, 522);
            btnEliminar.Name = "btnEliminar";
            btnEliminar.Size = new Size(182, 40);
            btnEliminar.TabIndex = 13;
            btnEliminar.Text = "Eliminar Reporte";
            btnEliminar.UseVisualStyleBackColor = true;
            btnEliminar.ForeColor = Color.DarkRed;
            btnEliminar.Click += btnEliminar_Click;
            // 
            // VisorReportesForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 574);
            Controls.Add(lblTitulo);
            Controls.Add(lblFiltroEstab);
            Controls.Add(txtFiltroEstab);
            Controls.Add(lblFiltroMes);
            Controls.Add(cmbFiltroMes);
            Controls.Add(lblFiltroSerie);
            Controls.Add(cmbFiltroSerie);
            Controls.Add(lblFiltroVersion);
            Controls.Add(cmbFiltroVersion);
            Controls.Add(btnFiltrar);
            Controls.Add(btnLimpiar);
            Controls.Add(dgvReportes);
            Controls.Add(btnAbrir);
            Controls.Add(btnEliminar);
            Name = "VisorReportesForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Visor de Reportes";
            ((System.ComponentModel.ISupportInitialize)dgvReportes).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private DataGridView dgvReportes;
        private Button btnAbrir;
        private Label lblTitulo;
        private Label lblFiltroEstab;
        private TextBox txtFiltroEstab;
        private Label lblFiltroMes;
        private ComboBox cmbFiltroMes;
        private Label lblFiltroSerie;
        private ComboBox cmbFiltroSerie;
        private Label lblFiltroVersion;
        private ComboBox cmbFiltroVersion;
        private Button btnFiltrar;
        private Button btnLimpiar;
        private Button btnEliminar;
    }
}

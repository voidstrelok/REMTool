namespace RemTools
{
    partial class VisorRegistrosForm
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
            lblReporte = new Label();
            lblBuscar = new Label();
            txtCodPrestacion = new TextBox();
            btnBuscar = new Button();
            dgvRegistros = new DataGridView();
            lblResultado = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvRegistros).BeginInit();
            SuspendLayout();
            // 
            // lblReporte
            // 
            lblReporte.Location = new Point(12, 9);
            lblReporte.Name = "lblReporte";
            lblReporte.Size = new Size(860, 28);
            lblReporte.TabIndex = 0;
            lblReporte.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            // 
            // lblBuscar
            // 
            lblBuscar.Location = new Point(12, 46);
            lblBuscar.Name = "lblBuscar";
            lblBuscar.Size = new Size(160, 26);
            lblBuscar.TabIndex = 1;
            lblBuscar.Text = "Código de Prestación:";
            lblBuscar.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txtCodPrestacion
            // 
            txtCodPrestacion.Location = new Point(178, 46);
            txtCodPrestacion.Name = "txtCodPrestacion";
            txtCodPrestacion.Size = new Size(300, 27);
            txtCodPrestacion.TabIndex = 2;
            txtCodPrestacion.KeyDown += txtCodPrestacion_KeyDown;
            // 
            // btnBuscar
            // 
            btnBuscar.Location = new Point(488, 44);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(100, 30);
            btnBuscar.TabIndex = 3;
            btnBuscar.Text = "Buscar";
            btnBuscar.UseVisualStyleBackColor = true;
            btnBuscar.Click += btnBuscar_Click;
            // 
            // lblResultado
            // 
            lblResultado.Location = new Point(12, 82);
            lblResultado.Name = "lblResultado";
            lblResultado.Size = new Size(860, 20);
            lblResultado.TabIndex = 4;
            lblResultado.ForeColor = SystemColors.GrayText;
            // 
            // dgvRegistros
            // 
            dgvRegistros.AllowUserToAddRows = false;
            dgvRegistros.AllowUserToDeleteRows = false;
            dgvRegistros.ReadOnly = true;
            dgvRegistros.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRegistros.Location = new Point(12, 108);
            dgvRegistros.Name = "dgvRegistros";
            dgvRegistros.Size = new Size(860, 460);
            dgvRegistros.TabIndex = 5;
            dgvRegistros.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            // 
            // VisorRegistrosForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(884, 580);
            Controls.Add(lblReporte);
            Controls.Add(lblBuscar);
            Controls.Add(txtCodPrestacion);
            Controls.Add(btnBuscar);
            Controls.Add(lblResultado);
            Controls.Add(dgvRegistros);
            Name = "VisorRegistrosForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Visor de Registros";
            ((System.ComponentModel.ISupportInitialize)dgvRegistros).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblReporte;
        private Label lblBuscar;
        private TextBox txtCodPrestacion;
        private Button btnBuscar;
        private DataGridView dgvRegistros;
        private Label lblResultado;
    }
}

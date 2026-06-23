namespace RemTools
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            revisar_btn = new Button();
            desconstruir_btn = new Button();
            gensql_btn = new Button();
            button1 = new Button();
            button2 = new Button();
            reglas_btn = new Button();
            visor_btn = new Button();
            integridadRem_btn = new Button();
            indicadores_btn = new Button();
            SuspendLayout();
            // 
            // revisar_btn
            // 
            revisar_btn.Location = new Point(12, 12);
            revisar_btn.Name = "revisar_btn";
            revisar_btn.Size = new Size(489, 69);
            revisar_btn.TabIndex = 0;
            revisar_btn.Text = "Revisar Serie A";
            revisar_btn.UseVisualStyleBackColor = true;
            revisar_btn.Click += button1_Click;
            // 
            // desconstruir_btn
            // 
            desconstruir_btn.Location = new Point(12, 86);
            desconstruir_btn.Name = "desconstruir_btn";
            desconstruir_btn.Size = new Size(489, 69);
            desconstruir_btn.TabIndex = 1;
            desconstruir_btn.Text = "Deconstruir REM (Todas las series)";
            desconstruir_btn.UseVisualStyleBackColor = true;
            desconstruir_btn.Click += button1_Click_1;
            // 
            // gensql_btn
            // 
            gensql_btn.Location = new Point(12, 161);
            gensql_btn.Name = "gensql_btn";
            gensql_btn.Size = new Size(489, 69);
            gensql_btn.TabIndex = 2;
            gensql_btn.Text = "Cargar Estructura a BDD";
            gensql_btn.UseVisualStyleBackColor = true;
            gensql_btn.Click += button1_Click_2;
            // 
            // button1
            // 
            button1.Location = new Point(12, 236);
            button1.Name = "button1";
            button1.Size = new Size(234, 69);
            button1.TabIndex = 3;
            button1.Text = "Extraer registros a BDD";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click_3;
            // 
            // button2
            // 
            button2.Location = new Point(267, 236);
            button2.Name = "button2";
            button2.Size = new Size(234, 69);
            button2.TabIndex = 4;
            button2.Text = "Cargar Indicadores 2026";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // reglas_btn
            // 
            reglas_btn.Location = new Point(12, 311);
            reglas_btn.Name = "reglas_btn";
            reglas_btn.Size = new Size(489, 69);
            reglas_btn.TabIndex = 5;
            reglas_btn.Text = "Editor de Reglas";
            reglas_btn.UseVisualStyleBackColor = true;
            reglas_btn.Click += reglas_btn_Click;
            // 
            // visor_btn
            // 
            visor_btn.Location = new Point(12, 386);
            visor_btn.Name = "visor_btn";
            visor_btn.Size = new Size(489, 69);
            visor_btn.TabIndex = 6;
            visor_btn.Text = "Visor de Registros";
            visor_btn.UseVisualStyleBackColor = true;
            visor_btn.Click += visor_btn_Click;
            // 
            // integridadRem_btn
            // 
            integridadRem_btn.Location = new Point(12, 461);
            integridadRem_btn.Name = "integridadRem_btn";
            integridadRem_btn.Size = new Size(489, 69);
            integridadRem_btn.TabIndex = 7;
            integridadRem_btn.Text = "Revisar integridad archivos";
            integridadRem_btn.UseVisualStyleBackColor = true;
            integridadRem_btn.Click += integridadRem_btn_Click;
            // 
            // indicadores_btn
            // 
            indicadores_btn.Location = new Point(12, 536);
            indicadores_btn.Name = "indicadores_btn";
            indicadores_btn.Size = new Size(489, 69);
            indicadores_btn.TabIndex = 8;
            indicadores_btn.Text = "Editor de Indicadores";
            indicadores_btn.UseVisualStyleBackColor = true;
            indicadores_btn.Click += indicadores_btn_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(513, 629);
            Controls.Add(indicadores_btn);
            Controls.Add(integridadRem_btn);
            Controls.Add(visor_btn);
            Controls.Add(reglas_btn);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(gensql_btn);
            Controls.Add(desconstruir_btn);
            Controls.Add(revisar_btn);
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "REM Tools v0.1";
            Load += Main_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button revisar_btn;
        private Button desconstruir_btn;
        private Button gensql_btn;
        private Button button1;
        private Button button2;
        private Button reglas_btn;
        private Button visor_btn;
        private Button integridadRem_btn;
        private Button indicadores_btn;
    }
}

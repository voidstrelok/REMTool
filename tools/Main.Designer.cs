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
            grpRevisar = new GroupBox();
            grpProcesamiento = new GroupBox();
            grpConfig = new GroupBox();
            revisar_A_btn = new Button();
            revisar_BM_btn = new Button();
            revisar_D_btn = new Button();
            revisar_P_btn = new Button();
            desconstruir_btn = new Button();
            gensql_btn = new Button();
            button1 = new Button();
            button2 = new Button();
            integridadRem_btn = new Button();
            reglas_btn = new Button();
            indicadores_btn = new Button();
            visor_btn = new Button();
            grpRevisar.SuspendLayout();
            grpProcesamiento.SuspendLayout();
            grpConfig.SuspendLayout();
            SuspendLayout();
            //
            // grpRevisar
            //
            grpRevisar.Controls.Add(revisar_A_btn);
            grpRevisar.Controls.Add(revisar_BM_btn);
            grpRevisar.Controls.Add(revisar_D_btn);
            grpRevisar.Controls.Add(revisar_P_btn);
            grpRevisar.Location = new Point(10, 10);
            grpRevisar.Name = "grpRevisar";
            grpRevisar.Size = new Size(520, 67);
            grpRevisar.TabIndex = 0;
            grpRevisar.TabStop = false;
            grpRevisar.Text = "Revisar REM";
            //
            // revisar_A_btn
            //
            revisar_A_btn.Location = new Point(8, 22);
            revisar_A_btn.Name = "revisar_A_btn";
            revisar_A_btn.Size = new Size(121, 35);
            revisar_A_btn.TabIndex = 0;
            revisar_A_btn.Text = "Serie A";
            revisar_A_btn.UseVisualStyleBackColor = true;
            revisar_A_btn.Click += revisar_A_btn_Click;
            //
            // revisar_BM_btn
            //
            revisar_BM_btn.Location = new Point(135, 22);
            revisar_BM_btn.Name = "revisar_BM_btn";
            revisar_BM_btn.Size = new Size(121, 35);
            revisar_BM_btn.TabIndex = 1;
            revisar_BM_btn.Text = "Serie BM";
            revisar_BM_btn.UseVisualStyleBackColor = true;
            revisar_BM_btn.Click += revisar_BM_btn_Click;
            //
            // revisar_D_btn
            //
            revisar_D_btn.Location = new Point(262, 22);
            revisar_D_btn.Name = "revisar_D_btn";
            revisar_D_btn.Size = new Size(121, 35);
            revisar_D_btn.TabIndex = 2;
            revisar_D_btn.Text = "Serie D";
            revisar_D_btn.UseVisualStyleBackColor = true;
            revisar_D_btn.Click += revisar_D_btn_Click;
            //
            // revisar_P_btn
            //
            revisar_P_btn.Location = new Point(389, 22);
            revisar_P_btn.Name = "revisar_P_btn";
            revisar_P_btn.Size = new Size(121, 35);
            revisar_P_btn.TabIndex = 3;
            revisar_P_btn.Text = "Serie P";
            revisar_P_btn.UseVisualStyleBackColor = true;
            revisar_P_btn.Click += revisar_P_btn_Click;
            //
            // grpProcesamiento
            //
            grpProcesamiento.Controls.Add(desconstruir_btn);
            grpProcesamiento.Controls.Add(gensql_btn);
            grpProcesamiento.Controls.Add(button1);
            grpProcesamiento.Controls.Add(button2);
            grpProcesamiento.Controls.Add(integridadRem_btn);
            grpProcesamiento.Location = new Point(10, 85);
            grpProcesamiento.Name = "grpProcesamiento";
            grpProcesamiento.Size = new Size(520, 196);
            grpProcesamiento.TabIndex = 1;
            grpProcesamiento.TabStop = false;
            grpProcesamiento.Text = "Procesamiento";
            //
            // desconstruir_btn
            //
            desconstruir_btn.Location = new Point(8, 22);
            desconstruir_btn.Name = "desconstruir_btn";
            desconstruir_btn.Size = new Size(504, 35);
            desconstruir_btn.TabIndex = 0;
            desconstruir_btn.Text = "Deconstruir REM (Todas las series)";
            desconstruir_btn.UseVisualStyleBackColor = true;
            desconstruir_btn.Click += button1_Click_1;
            //
            // gensql_btn
            //
            gensql_btn.Location = new Point(8, 65);
            gensql_btn.Name = "gensql_btn";
            gensql_btn.Size = new Size(504, 35);
            gensql_btn.TabIndex = 1;
            gensql_btn.Text = "Cargar Estructura a BDD";
            gensql_btn.UseVisualStyleBackColor = true;
            gensql_btn.Click += button1_Click_2;
            //
            // button1 - Extraer Registros
            //
            button1.Location = new Point(8, 108);
            button1.Name = "button1";
            button1.Size = new Size(249, 35);
            button1.TabIndex = 2;
            button1.Text = "Extraer Registros a BDD";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click_3;
            //
            // button2 - Cargar Indicadores
            //
            button2.Location = new Point(263, 108);
            button2.Name = "button2";
            button2.Size = new Size(249, 35);
            button2.TabIndex = 3;
            button2.Text = "Cargar Indicadores 2026";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            //
            // integridadRem_btn
            //
            integridadRem_btn.Location = new Point(8, 151);
            integridadRem_btn.Name = "integridadRem_btn";
            integridadRem_btn.Size = new Size(504, 35);
            integridadRem_btn.TabIndex = 4;
            integridadRem_btn.Text = "Revisar Integridad Archivos";
            integridadRem_btn.UseVisualStyleBackColor = true;
            integridadRem_btn.Click += integridadRem_btn_Click;
            //
            // grpConfig
            //
            grpConfig.Controls.Add(reglas_btn);
            grpConfig.Controls.Add(indicadores_btn);
            grpConfig.Controls.Add(visor_btn);
            grpConfig.Location = new Point(10, 289);
            grpConfig.Name = "grpConfig";
            grpConfig.Size = new Size(520, 110);
            grpConfig.TabIndex = 2;
            grpConfig.TabStop = false;
            grpConfig.Text = "Configuración";
            //
            // reglas_btn
            //
            reglas_btn.Location = new Point(8, 22);
            reglas_btn.Name = "reglas_btn";
            reglas_btn.Size = new Size(249, 35);
            reglas_btn.TabIndex = 0;
            reglas_btn.Text = "Editor de Reglas";
            reglas_btn.UseVisualStyleBackColor = true;
            reglas_btn.Click += reglas_btn_Click;
            //
            // indicadores_btn
            //
            indicadores_btn.Location = new Point(263, 22);
            indicadores_btn.Name = "indicadores_btn";
            indicadores_btn.Size = new Size(249, 35);
            indicadores_btn.TabIndex = 1;
            indicadores_btn.Text = "Editor de Indicadores";
            indicadores_btn.UseVisualStyleBackColor = true;
            indicadores_btn.Click += indicadores_btn_Click;
            //
            // visor_btn
            //
            visor_btn.Location = new Point(8, 65);
            visor_btn.Name = "visor_btn";
            visor_btn.Size = new Size(504, 35);
            visor_btn.TabIndex = 2;
            visor_btn.Text = "Visor de Registros";
            visor_btn.UseVisualStyleBackColor = true;
            visor_btn.Click += visor_btn_Click;
            //
            // Main
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(540, 409);
            Controls.Add(grpRevisar);
            Controls.Add(grpProcesamiento);
            Controls.Add(grpConfig);
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "REM Tools v0.1";
            Load += Main_Load;
            grpRevisar.ResumeLayout(false);
            grpProcesamiento.ResumeLayout(false);
            grpConfig.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private GroupBox grpRevisar;
        private GroupBox grpProcesamiento;
        private GroupBox grpConfig;
        private Button revisar_A_btn;
        private Button revisar_BM_btn;
        private Button revisar_D_btn;
        private Button revisar_P_btn;
        private Button desconstruir_btn;
        private Button gensql_btn;
        private Button button1;
        private Button button2;
        private Button integridadRem_btn;
        private Button reglas_btn;
        private Button indicadores_btn;
        private Button visor_btn;
    }
}

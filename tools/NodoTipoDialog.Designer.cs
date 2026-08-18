namespace RemTools
{
    partial class NodoTipoDialog
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
            tipo_lbl = new Label();
            tipo_cmb = new ComboBox();
            op_lbl = new Label();
            op_cmb = new ComboBox();
            aceptar_btn = new Button();
            cancelar_btn = new Button();
            SuspendLayout();

            // tipo_lbl
            tipo_lbl.AutoSize = true;
            tipo_lbl.Location = new Point(12, 14);
            tipo_lbl.Name = "tipo_lbl";
            tipo_lbl.Text = "Tipo de nodo:";

            // tipo_cmb
            tipo_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            tipo_cmb.FormattingEnabled = true;
            tipo_cmb.Location = new Point(12, 34);
            tipo_cmb.Name = "tipo_cmb";
            tipo_cmb.Size = new Size(310, 28);
            tipo_cmb.TabIndex = 0;
            tipo_cmb.SelectedIndexChanged += tipo_cmb_SelectedIndexChanged;

            // op_lbl
            op_lbl.AutoSize = true;
            op_lbl.Location = new Point(12, 72);
            op_lbl.Name = "op_lbl";
            op_lbl.Text = "Operación:";
            op_lbl.Visible = true;

            // op_cmb
            op_cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            op_cmb.FormattingEnabled = true;
            op_cmb.Location = new Point(12, 92);
            op_cmb.Name = "op_cmb";
            op_cmb.Size = new Size(130, 28);
            op_cmb.TabIndex = 1;
            op_cmb.Visible = true;

            // aceptar_btn
            aceptar_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            aceptar_btn.Location = new Point(136, 140);
            aceptar_btn.Name = "aceptar_btn";
            aceptar_btn.Size = new Size(90, 30);
            aceptar_btn.TabIndex = 2;
            aceptar_btn.Text = "Aceptar";
            aceptar_btn.UseVisualStyleBackColor = true;
            aceptar_btn.Click += aceptar_btn_Click;

            // cancelar_btn
            cancelar_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelar_btn.Location = new Point(234, 140);
            cancelar_btn.Name = "cancelar_btn";
            cancelar_btn.Size = new Size(90, 30);
            cancelar_btn.TabIndex = 3;
            cancelar_btn.Text = "Cancelar";
            cancelar_btn.UseVisualStyleBackColor = true;
            cancelar_btn.Click += cancelar_btn_Click;

            // NodoTipoDialog
            AcceptButton = aceptar_btn;
            CancelButton = cancelar_btn;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(338, 186);
            Controls.Add(cancelar_btn);
            Controls.Add(aceptar_btn);
            Controls.Add(op_cmb);
            Controls.Add(op_lbl);
            Controls.Add(tipo_cmb);
            Controls.Add(tipo_lbl);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "NodoTipoDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Tipo de nodo";
            Load += NodoTipoDialog_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label tipo_lbl;
        private ComboBox tipo_cmb;
        private Label op_lbl;
        private ComboBox op_cmb;
        private Button aceptar_btn;
        private Button cancelar_btn;
    }
}

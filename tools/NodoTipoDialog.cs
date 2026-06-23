using System.Text.Json.Nodes;

namespace RemTools
{
    public partial class NodoTipoDialog : Form
    {
        public JsonNode? NodoCreado { get; private set; }

        public NodoTipoDialog()
        {
            InitializeComponent();
        }

        private void NodoTipoDialog_Load(object sender, EventArgs e)
        {
            tipo_cmb.Items.AddRange(new object[]
            {
                "op (operación)",
                "number (constante)",
                "value (prestación REM)",
                "variable (población)"
            });
            tipo_cmb.SelectedIndex = 0;

            op_cmb.Items.AddRange(new object[] { "sum", "sub", "mul", "div" });
            op_cmb.SelectedIndex = 0;
        }

        private void tipo_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isOp = tipo_cmb.SelectedItem?.ToString()?.StartsWith("op") == true;
            op_lbl.Visible = isOp;
            op_cmb.Visible = isOp;
        }

        private void aceptar_btn_Click(object sender, EventArgs e)
        {
            var tipo = tipo_cmb.SelectedItem?.ToString() ?? "";

            if (tipo.StartsWith("op"))
            {
                var op = op_cmb.SelectedItem?.ToString() ?? "sum";
                NodoCreado = new JsonObject
                {
                    ["type"] = "op",
                    ["op"] = op,
                    ["args"] = new JsonArray()
                };
            }
            else if (tipo.StartsWith("number"))
            {
                NodoCreado = new JsonObject
                {
                    ["type"] = "number",
                    ["value"] = 0.0
                };
            }
            else if (tipo.StartsWith("value"))
            {
                NodoCreado = new JsonObject
                {
                    ["type"] = "value",
                    ["prestacion"] = "A01",
                    ["columna"] = 1
                };
            }
            else if (tipo.StartsWith("variable"))
            {
                NodoCreado = new JsonObject
                {
                    ["type"] = "variable",
                    ["name"] = "FONASA",
                    ["filters"] = new JsonObject()
                };
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelar_btn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}

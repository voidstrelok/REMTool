namespace RemTool.Shared
{
    public partial class IndicadorConvenio
    {
        public int Id { get; set; }

        public int id_indicador { get; set; }

        public int id_convenio { get; set; }

        public virtual Indicador Indicador { get; set; } = null!;

        public virtual Convenio Convenio { get; set; } = null!;
    }
}

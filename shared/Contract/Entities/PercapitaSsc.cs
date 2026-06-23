namespace RemTool.Shared
{
    public partial class PercapitaSsc
    {
        public long Id { get; set; }

        public long id_establecimiento { get; set; }

        public int Edad { get; set; }

        public int Inscritos { get; set; }

        public string Sexo { get; set; } = string.Empty;

        public int AñoCorte { get; set; }

        public virtual Establecimiento Establecimiento { get; set; } = null!;
    }
}

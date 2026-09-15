using System.Collections.Generic;

namespace RemTool.Shared
{
    public partial class FilaSeccionRem
    {
        public int Id { get; set; }

        public long IdSeccion { get; set; }

        public int Orden { get; set; }

        public int FilaOrigen { get; set; }

        public bool TienePrestacion { get; set; }

        public string TipoFila { get; set; } = String.Empty;

        public virtual SeccionRem Seccion { get; set; } = null!;

        public virtual Prestacion? Prestacion { get; set; }

        public virtual ICollection<CeldaSeccionRem> Celdas { get; set; } = new HashSet<CeldaSeccionRem>();
    }
}

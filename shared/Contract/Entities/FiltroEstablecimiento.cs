using RemTool.Shared.Enum;

namespace RemTool.Shared
{
    public partial class FiltroEstablecimiento
    {
        public int Id { get; set; }

        public long id_establecimiento { get; set; }

        /// <summary>
        /// null = regla global (aplica a todos los indicadores).
        /// Valor = regla específica para ese indicador.
        /// </summary>
        public int? id_indicador { get; set; }

        /// <summary>
        /// Excluir: lista negra – este establecimiento no participa en el cálculo.
        /// Incluir: lista blanca – solo estos establecimientos participan (sobreescribe exclusiones globales).
        /// </summary>
        public TipoFiltroEstablecimiento Tipo { get; set; }

        public virtual Establecimiento Establecimiento { get; set; } = null!;

        public virtual Indicador? Indicador { get; set; }
    }
}

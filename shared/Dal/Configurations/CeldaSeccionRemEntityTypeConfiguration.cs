using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class CeldaSeccionRemEntityTypeConfiguration : IEntityTypeConfiguration<CeldaSeccionRem>
    {
        public void Configure(EntityTypeBuilder<CeldaSeccionRem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Fila)
                .WithMany(x => x.Celdas)
                .HasForeignKey(x => x.IdFila)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".celda_seccion_rem_id_seq'::regclass)");

            builder.Property(x => x.IdFila).HasColumnName("id_fila");
            builder.Property(x => x.FilaOrigen).HasColumnName("fila_origen");
            builder.Property(x => x.ColumnaOrigen).HasColumnName("columna_origen");
            builder.Property(x => x.Valor).HasColumnName("valor").HasColumnType("text");
            builder.Property(x => x.CeldaDiccionario).HasColumnName("celda_diccionario").HasColumnType("character varying");
            builder.Property(x => x.CeldaBase).HasColumnName("celda_base").HasColumnType("character varying");
            builder.Property(x => x.RowSpan).HasColumnName("row_span");
            builder.Property(x => x.ColSpan).HasColumnName("col_span");
            builder.Property(x => x.EsCeldaAncla).HasColumnName("es_celda_ancla");
            builder.Property(x => x.EsEditable).HasColumnName("es_editable");
            builder.Property(x => x.EsEntradaPrestacion).HasColumnName("es_entrada_prestacion");
            builder.Property(x => x.EsTotal).HasColumnName("es_total");
            builder.Property(x => x.TipoTotal).HasColumnName("tipo_total").HasColumnType("character varying");
            builder.Property(x => x.FormulaOrigen).HasColumnName("formula_origen").HasColumnType("text");
            builder.Property(x => x.DependenciasTotal).HasColumnName("dependencias_total").HasColumnType("text");
            builder.Property(x => x.OperacionTotal).HasColumnName("operacion_total").HasColumnType("character varying");
            builder.Property(x => x.EstiloOrigen).HasColumnName("estilo_origen");
            builder.Property(x => x.ColorFondo).HasColumnName("color_fondo").HasColumnType("character varying");

            builder.ToTable("celda_seccion_rem", "REMTool");
        }
    }
}

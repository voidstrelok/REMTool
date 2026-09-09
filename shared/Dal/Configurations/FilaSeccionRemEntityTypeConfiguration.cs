using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class FilaSeccionRemEntityTypeConfiguration : IEntityTypeConfiguration<FilaSeccionRem>
    {
        public void Configure(EntityTypeBuilder<FilaSeccionRem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Seccion)
                .WithMany(x => x.Filas)
                .HasForeignKey(x => x.IdSeccion)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.IdSeccion, x.Orden }).IsUnique();

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".fila_seccion_rem_id_seq'::regclass)");

            builder.Property(x => x.IdSeccion).HasColumnName("id_seccion");
            builder.Property(x => x.Orden).HasColumnName("orden");
            builder.Property(x => x.FilaOrigen).HasColumnName("fila_origen");
            builder.Property(x => x.TienePrestacion).HasColumnName("tiene_prestacion");
            builder.Property(x => x.TipoFila).HasColumnName("tipo_fila").HasColumnType("character varying");

            builder.ToTable("fila_seccion_rem", "REMTool");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class SeccionRemEntityTypeConfiguration : IEntityTypeConfiguration<SeccionRem>
    {
        public void Configure(EntityTypeBuilder<SeccionRem> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder.HasOne(x => x.VersionHoja)
                .WithMany(x => x.Secciones)
                .HasForeignKey(x => x.IdVersionHoja)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(64, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".seccion_rem_id_seq'::regclass)");

            builder
                .Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying");

            builder.Property(x => x.IdVersionHoja).HasColumnName("id_version_hoja");
            builder.Property(x => x.Codigo).HasColumnName("codigo").HasColumnType("character varying");
            builder.Property(x => x.Orden).HasColumnName("orden");
            builder.Property(x => x.RangoDiccionario).HasColumnName("rango_diccionario").HasColumnType("character varying");
            builder.Property(x => x.RangoBase).HasColumnName("rango_base").HasColumnType("character varying");
            builder.Property(x => x.FilaInicio).HasColumnName("fila_inicio");
            builder.Property(x => x.ColumnaInicio).HasColumnName("columna_inicio");
            builder.Property(x => x.FilaFin).HasColumnName("fila_fin");
            builder.Property(x => x.ColumnaFin).HasColumnName("columna_fin");
            builder.Property(x => x.OffsetPrestacion).HasColumnName("offset_prestacion");
            builder.Property(x => x.OffsetColumna).HasColumnName("offset_columna");
            builder.Property(x => x.HtmlEstructura).HasColumnName("html_estructura").HasColumnType("text");

            builder.HasIndex(x => new { x.IdVersionHoja, x.Orden }).IsUnique();

            builder
                .Property(x => x.IdHojaRem)
                .HasColumnName("id_hoja_rem")
                .HasPrecision(64, 0);

            builder
                .ToTable("seccion_rem", "REMTool");
        }
    }
}

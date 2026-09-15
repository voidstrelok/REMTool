using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class PrestacionEntityTypeConfiguration : IEntityTypeConfiguration<Prestacion>
    {
        public void Configure(EntityTypeBuilder<Prestacion> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder.HasIndex(x => x.id_version);

            builder
                .HasOne(x => x.VersionRem)
                .WithMany(x => x.Prestacions)
                .HasForeignKey(x => x.id_version)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .HasOne(x => x.HojaRem)
                .WithMany(x => x.Prestacions)
                .HasForeignKey(x => x.id_hoja)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Seccion)
                .WithMany(x => x.Prestacions)
                .HasForeignKey(x => x.IdSeccion)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.FilaSeccion)
                .WithOne(x => x.Prestacion)
                .HasForeignKey<Prestacion>(x => x.IdFilaSeccion)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".prestacion_id_seq'::regclass)");

            builder
                .Property(x => x.CodigoPrestacion)
                .HasColumnName("codigo_prestacion")
                .HasColumnType("character varying");

            builder.Property(x => x.IdSeccion).HasColumnName("id_seccion");
            builder.Property(x => x.IdFilaSeccion).HasColumnName("id_fila_seccion");
            builder.Property(x => x.Nombre).HasColumnName("nombre").HasColumnType("character varying");
            builder.Property(x => x.Orden).HasColumnName("orden");

            builder
                .Property(x => x.IsEnabled)
                .HasColumnName("is_enabled");

            builder
                .Property(x => x.Coordenada)
                .HasColumnName("coordenada");

            builder
                .ToTable("prestacion", "REMTool");
        }
    }
}

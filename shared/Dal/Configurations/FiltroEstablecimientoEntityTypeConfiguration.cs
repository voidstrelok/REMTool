using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class FiltroEstablecimientoEntityTypeConfiguration : IEntityTypeConfiguration<FiltroEstablecimiento>
    {
        public void Configure(EntityTypeBuilder<FiltroEstablecimiento> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0);

            builder
                .Property(x => x.id_establecimiento)
                .HasColumnName("id_establecimiento")
                .HasPrecision(64, 0);

            builder
                .Property(x => x.id_indicador)
                .HasColumnName("id_indicador")
                .HasPrecision(32, 0)
                .IsRequired(false);

            builder
                .Property(x => x.Tipo)
                .HasColumnName("tipo")
                .HasConversion<int>()
                .HasPrecision(32, 0);

            builder
                .HasOne(x => x.Establecimiento)
                .WithMany(x => x.FiltrosEstablecimiento)
                .HasForeignKey(x => x.id_establecimiento)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(x => x.Indicador)
                .WithMany(x => x.FiltrosEstablecimiento)
                .HasForeignKey(x => x.id_indicador)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder
                .HasIndex(x => new { x.id_establecimiento, x.id_indicador })
                .IsUnique()
                .HasFilter("\"id_indicador\" IS NOT NULL");

            builder
                .HasIndex(x => x.id_establecimiento)
                .IsUnique()
                .HasFilter("\"id_indicador\" IS NULL");

            builder.ToTable("filtro_establecimiento", "REMTool");
        }
    }
}

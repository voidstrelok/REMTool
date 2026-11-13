using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared;

public class InformeRemEntityTypeConfiguration : IEntityTypeConfiguration<InformeRem>
{
    public void Configure(EntityTypeBuilder<InformeRem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd().HasColumnName("id");
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(180).IsRequired();
        builder.Property(x => x.IdSerieRem).HasColumnName("id_serie_rem");
        builder.Property(x => x.SeccionIds).HasColumnName("seccion_ids").HasColumnType("bigint[]").IsRequired();

        builder.HasOne(x => x.SerieRem)
            .WithMany()
            .HasForeignKey(x => x.IdSerieRem)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.IdSerieRem, x.Nombre }).IsUnique();

        builder.ToTable("informe_rem", "REMTool");
    }
}

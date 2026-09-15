using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared;

public class InformativoEntityTypeConfiguration : IEntityTypeConfiguration<Informativo>
{
    public void Configure(EntityTypeBuilder<Informativo> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(x => x.Tipo)
            .HasColumnName("tipo")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(x => x.Contenido)
            .HasColumnName("contenido")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Url)
            .HasColumnName("url")
            .HasMaxLength(1000);

        builder.Property(x => x.TextoEnlace)
            .HasColumnName("texto_enlace")
            .HasMaxLength(100);

        builder.Property(x => x.FechaPublicacion)
            .HasColumnName("fecha_publicacion")
            .HasColumnType("date")
            .HasConversion(typeof(DateOnlyValueConverter), typeof(DateOnlyValueComparer))
            .IsRequired();

        builder.Property(x => x.Vigente)
            .HasColumnName("vigente")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.Destacado)
            .HasColumnName("destacado")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => new { x.Vigente, x.Destacado, x.FechaPublicacion });

        builder.ToTable("informativo", "REMTool");
    }
}

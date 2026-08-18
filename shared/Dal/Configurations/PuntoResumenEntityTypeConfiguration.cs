using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class PuntoResumenEntityTypeConfiguration : IEntityTypeConfiguration<PuntoResumen>
    {
        public void Configure(EntityTypeBuilder<PuntoResumen> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");

            builder.Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying")
                .IsRequired();

            builder.Property(x => x.Categoria)
                .HasColumnName("categoria")
                .HasColumnType("character varying")
                .IsRequired();

            builder.Property(x => x.Expresion)
                .HasColumnName("expresion")
                .HasColumnType("character varying")
                .IsRequired();

            builder.Property(x => x.IdSerieRem)
                .HasColumnName("id_serie_rem");

            builder.HasOne(x => x.SerieRem)
                .WithMany(s => s.PuntosResumen)
                .HasForeignKey(x => x.IdSerieRem);

            builder.ToTable("punto_resumen", "REMTool");
        }
    }
}

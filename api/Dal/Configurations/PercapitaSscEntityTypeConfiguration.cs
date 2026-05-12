using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool
{
    public class PercapitaSscEntityTypeConfiguration : IEntityTypeConfiguration<PercapitaSsc>
    {
        public void Configure(EntityTypeBuilder<PercapitaSsc> builder)
        {
            builder
                .ToTable("PERCAPITA_SSC", "REMTool")
                .HasKey(x => x.Id);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");

            builder
                .Property(x => x.id_establecimiento)
                .HasColumnName("id_establecimiento");

            builder
                .Property(x => x.Edad)
                .HasColumnName("edad");

            builder
                .Property(x => x.Inscritos)
                .HasColumnName("inscritos");

            builder
                .Property(x => x.Sexo)
                .HasColumnName("sexo")
                .HasColumnType("character varying");

            builder
                .Property(x => x.AñoCorte)
                .HasColumnName("año_corte");

            builder
                .HasOne(x => x.Establecimiento)
                .WithMany()
                .HasForeignKey(x => x.id_establecimiento)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

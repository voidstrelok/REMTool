using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class EstablecimientoEntityTypeConfiguration : IEntityTypeConfiguration<Establecimiento>
    {
        public void Configure(EntityTypeBuilder<Establecimiento> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .HasOne(x => x.Sector)
                .WithMany(x => x.Establecimientos)
                .HasForeignKey(x => x.id_sector)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(64, 0);

            builder
                .Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying");

            builder
                .Property(x => x.Director)
                .HasColumnName("director")
                .HasColumnType("character varying");

            builder
                .Property(x => x.CodDeis)
                .HasColumnName("cod_deis")
                .HasColumnType("character varying");

            builder
                .ToTable("establecimiento", "REMTool");
        }
    }
}

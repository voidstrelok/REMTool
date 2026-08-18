using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class ComunaEntityTypeConfiguration : IEntityTypeConfiguration<Comuna>
    {
        public void Configure(EntityTypeBuilder<Comuna> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(64, 0);

            builder
                .Property(x => x.CodDeis)
                .HasColumnName("cod_deis")
                .HasColumnType("character varying");

            builder
                .Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying");

            builder
                .Property(x => x.IdServicio)
                .HasColumnName("id_servicio")
                .HasPrecision(64, 0);

            builder
                .ToTable("comuna", "REMTool");
        }
    }
}

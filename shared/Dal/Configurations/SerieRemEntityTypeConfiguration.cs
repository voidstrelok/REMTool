using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class SerieRemEntityTypeConfiguration : IEntityTypeConfiguration<SerieRem>
    {
        public void Configure(EntityTypeBuilder<SerieRem> builder)
        {
            builder
                .HasKey(x => x.Id);

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
                .ToTable("serie_rem", "REMTool");
        }
    }
}

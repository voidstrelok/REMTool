using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class TipoReglaEntityTypeConfiguration : IEntityTypeConfiguration<TipoRegla>
    {
        public void Configure(EntityTypeBuilder<TipoRegla> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".tipo_regla_id_seq'::regclass)");

            builder
                .Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying");

            builder
                .ToTable("tipo_regla", "REMTool");
        }
    }
}

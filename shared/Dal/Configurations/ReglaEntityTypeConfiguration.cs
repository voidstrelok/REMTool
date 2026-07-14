using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class ReglaEntityTypeConfiguration : IEntityTypeConfiguration<Regla>
    {
        public void Configure(EntityTypeBuilder<Regla> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .HasOne(x => x.VersionREM)
                .WithMany(x => x.Reglas)
                .HasForeignKey(x => x.id_version)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".regla_id_seq'::regclass)");

            builder
                .Property(x => x.Expresion)
                .HasColumnName("expresion")
                .HasColumnType("character varying");

            builder
                .Property(x => x.Mensaje)
                .HasColumnName("mensaje")
                .HasColumnType("character varying");

            builder
                .Property(x => x.IdTipoRegla)
                .HasColumnName("id_tipo_regla")
                .HasDefaultValue(1);

            builder
                .HasOne(x => x.TipoRegla)
                .WithMany(x => x.Reglas)
                .HasForeignKey(x => x.IdTipoRegla)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .ToTable("regla", "REMTool");
        }
    }
}

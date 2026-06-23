using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class VersionRemEntityTypeConfiguration : IEntityTypeConfiguration<VersionRem>
    {
        public void Configure(EntityTypeBuilder<VersionRem> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .HasOne(x => x.SerieRem)
                .WithMany(x => x.VersionRems)
                .HasForeignKey(x => x.id_serie)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".version_rem_id_seq'::regclass)");

            builder
                .Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying");

            builder
                .Property(x => x.Fecha)
                .HasColumnName("fecha")
                .HasColumnType("date")
                .HasConversion(typeof(DateOnlyValueConverter), typeof(DateOnlyValueComparer));

            builder
                .ToTable("version_rem", "REMTool");
        }
    }
}

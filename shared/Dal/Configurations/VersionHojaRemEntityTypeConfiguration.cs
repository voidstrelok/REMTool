using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class VersionHojaRemEntityTypeConfiguration : IEntityTypeConfiguration<VersionHojaRem>
    {
        public void Configure(EntityTypeBuilder<VersionHojaRem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.VersionRem)
                .WithMany(x => x.Hojas)
                .HasForeignKey(x => x.IdVersion)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.HojaRem)
                .WithMany(x => x.Versiones)
                .HasForeignKey(x => x.IdHoja)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(x => new { x.IdVersion, x.IdHoja }).IsUnique();

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".version_hoja_rem_id_seq'::regclass)");

            builder.Property(x => x.IdVersion).HasColumnName("id_version");
            builder.Property(x => x.IdHoja).HasColumnName("id_hoja");
            builder.Property(x => x.Orden).HasColumnName("orden");

            builder.ToTable("version_hoja_rem", "REMTool");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool
{
    public class IndicadorConvenioEntityTypeConfiguration : IEntityTypeConfiguration<IndicadorConvenio>
    {
        public void Configure(EntityTypeBuilder<IndicadorConvenio> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".indicador_convenio_id_seq'::regclass)");

            builder
                .Property(x => x.id_indicador)
                .HasColumnName("id_indicador")
                .HasPrecision(32, 0);

            builder
                .Property(x => x.id_convenio)
                .HasColumnName("id_convenio")
                .HasPrecision(32, 0);

            builder
                .HasOne(x => x.Indicador)
                .WithMany(x => x.IndicadorConvenios)
                .HasForeignKey(x => x.id_indicador)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(x => x.Convenio)
                .WithMany(x => x.IndicadorConvenios)
                .HasForeignKey(x => x.id_convenio)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .ToTable("indicador_convenio", "REMTool");
        }
    }
}

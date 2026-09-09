using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class RegistroEntityTypeConfiguration : IEntityTypeConfiguration<Registro>
    {
        public void Configure(EntityTypeBuilder<Registro> builder)
        {
            builder
                .HasKey(x => x.Id);

            // The extractor searches and deletes registrations by report. The
            // second column also supports the version lookup used during reloads.
            builder.HasIndex(x => new { x.id_reporte, x.id_prestacion });

            builder
                .HasOne(x => x.Prestacion)
                .WithMany(x => x.Registros)
                .HasForeignKey(x => x.id_prestacion)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .HasOne(x => x.Reporte)
                .WithMany(x => x.Registros)
                .HasForeignKey(x => x.id_reporte)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".registro_id_seq'::regclass)");

            builder
                .Property(x => x.Valor)
                .HasColumnName("valor");

            builder
                .ToTable("registro", "REMTool");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class ReporteEntityTypeConfiguration : IEntityTypeConfiguration<Reporte>
    {
        public void Configure(EntityTypeBuilder<Reporte> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .HasOne(x => x.Comuna)
                .WithMany(x => x.Reportes)
                .HasForeignKey(x => x.id_comuna)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .HasOne(x => x.Establecimiento)
                .WithMany(x => x.Reportes)
                .HasForeignKey(x => x.id_establecimiento)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".reporte_id_seq'::regclass)");

            builder
                .Property(x => x.Mes)
                .HasColumnName("mes")
                .HasPrecision(32, 0);

            builder
                .ToTable("reporte", "REMTool");
        }
    }
}

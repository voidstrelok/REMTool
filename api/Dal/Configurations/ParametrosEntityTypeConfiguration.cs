using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class ParametrosEntityTypeConfiguration : IEntityTypeConfiguration<Parametros>
    {
        public void Configure(EntityTypeBuilder<Parametros> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0);

            builder
                .Property(x => x.ServicioEnabled)
                .HasColumnName("servicio_enabled");

            builder
                .Property(x => x.UltimaActualizacion)
                .HasColumnName("ultima_actualizacion")
                .HasColumnType("date")
                .HasConversion(typeof(DateOnlyValueConverter), typeof(DateOnlyValueComparer));

            builder
                .ToTable("parametros", "REMTool");
        }
    }
}

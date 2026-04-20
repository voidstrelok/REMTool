using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class SeccionRemEntityTypeConfiguration : IEntityTypeConfiguration<SeccionRem>
    {
        public void Configure(EntityTypeBuilder<SeccionRem> builder)
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
                .Property(x => x.IdHojaRem)
                .HasColumnName("id_hoja_rem")
                .HasPrecision(64, 0);

            builder
                .ToTable("seccion_rem", "REMTool");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class TipoIndicadorEntityTypeConfiguration : IEntityTypeConfiguration<TipoIndicador>
    {
        public void Configure(EntityTypeBuilder<TipoIndicador> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".tipo_indicador_id_seq'::regclass)");

            builder
                .Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying");

            builder
                .ToTable("tipo_indicador", "REMTool");
        }
    }
}

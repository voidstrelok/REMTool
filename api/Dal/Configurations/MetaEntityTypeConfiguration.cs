using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class MetaEntityTypeConfiguration : IEntityTypeConfiguration<Meta>
    {
        public void Configure(EntityTypeBuilder<Meta> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".meta_id_seq'::regclass)");

            builder
                .Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying");

            builder
                .Property(x => x.Formula)
                .HasColumnName("formula")
                .HasColumnType("jsonb");

            builder
                .Property(x => x.Año)
                .HasColumnName("año")
                .HasPrecision(32, 0);

            builder
                .ToTable("meta", "REMTool");
        }
    }
}

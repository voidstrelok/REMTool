using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class HojaRemEntityTypeConfiguration : IEntityTypeConfiguration<HojaRem>
    {
        public void Configure(EntityTypeBuilder<HojaRem> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .HasOne(x => x.SerieRem)
                .WithMany(x => x.HojaRems)
                .HasForeignKey(x => x.id_serie_rem)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".hoja_rem_id_seq'::regclass)");

            builder
                .Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying");

            builder
                .ToTable("hoja_rem", "REMTool");
        }
    }
}

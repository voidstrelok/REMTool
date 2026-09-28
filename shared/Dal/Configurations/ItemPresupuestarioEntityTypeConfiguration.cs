using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared;

public class ItemPresupuestarioEntityTypeConfiguration : IEntityTypeConfiguration<ItemPresupuestario>
{
    public void Configure(EntityTypeBuilder<ItemPresupuestario> builder)
    {
        builder.ToTable("item_presupuestario", "REMTool");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ConvenioFinancieroId).HasColumnName("convenio_financiero_id");
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.HasOne(x => x.ConvenioFinanciero).WithMany(x => x.Items).HasForeignKey(x => x.ConvenioFinancieroId).OnDelete(DeleteBehavior.Cascade);
    }
}

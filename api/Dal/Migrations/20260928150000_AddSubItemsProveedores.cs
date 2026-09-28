using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RemTool.Shared;

#nullable disable
namespace RemTool.Dal.Migrations;

[DbContext(typeof(RemToolDataContext))]
[Migration("20260928150000_AddSubItemsProveedores")]
public partial class AddSubItemsProveedores : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable(name:"proveedor_financiero", schema:"REMTool", columns:t => new { id=t.Column<int>(nullable:false).Annotation("Npgsql:ValueGenerationStrategy",NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), nombre=t.Column<string>(type:"character varying(200)",maxLength:200,nullable:false), nombre_normalizado=t.Column<string>(type:"character varying(200)",maxLength:200,nullable:false), fecha_creacion=t.Column<DateTimeOffset>(type:"timestamp with time zone",nullable:false) }, constraints:c=>c.PrimaryKey("PK_proveedor_financiero",x=>x.id));
        m.CreateTable(name:"sub_item_presupuestario", schema:"REMTool", columns:t => new { id=t.Column<int>(nullable:false).Annotation("Npgsql:ValueGenerationStrategy",NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), item_presupuestario_id=t.Column<int>(nullable:false), nombre=t.Column<string>(type:"character varying(150)",maxLength:150,nullable:false), activo=t.Column<bool>(nullable:false) }, constraints:c=>{c.PrimaryKey("PK_sub_item_presupuestario",x=>x.id);c.ForeignKey("FK_sub_item_item",x=>x.item_presupuestario_id,principalSchema:"REMTool",principalTable:"item_presupuestario",principalColumn:"id",onDelete:ReferentialAction.Cascade);});
        m.AddColumn<int>(name:"sub_item_presupuestario_id",table:"movimiento_financiero",schema:"REMTool",nullable:true);
        m.AddColumn<int>(name:"proveedor_financiero_id",table:"movimiento_financiero",schema:"REMTool",nullable:true);
        m.CreateIndex(name:"IX_proveedor_financiero_nombre_normalizado",table:"proveedor_financiero",column:"nombre_normalizado",schema:"REMTool",unique:true);
        m.CreateIndex(name:"IX_sub_item_presupuestario_item_nombre",table:"sub_item_presupuestario",columns:new[]{"item_presupuestario_id","nombre"},schema:"REMTool",unique:true);
        m.CreateIndex(name:"IX_movimiento_financiero_sub_item",table:"movimiento_financiero",column:"sub_item_presupuestario_id",schema:"REMTool");
        m.CreateIndex(name:"IX_movimiento_financiero_proveedor",table:"movimiento_financiero",column:"proveedor_financiero_id",schema:"REMTool");
        m.Sql("INSERT INTO \"REMTool\".sub_item_presupuestario (item_presupuestario_id,nombre,activo) SELECT id,'General',TRUE FROM \"REMTool\".item_presupuestario;");
        m.Sql("UPDATE \"REMTool\".movimiento_financiero m SET sub_item_presupuestario_id=s.id FROM \"REMTool\".sub_item_presupuestario s WHERE s.item_presupuestario_id=m.item_presupuestario_id AND s.nombre='General';");
        m.Sql("INSERT INTO \"REMTool\".proveedor_financiero (nombre,nombre_normalizado,fecha_creacion) SELECT MIN(TRIM(proveedor)),UPPER(TRIM(proveedor)),NOW() FROM \"REMTool\".movimiento_financiero WHERE proveedor IS NOT NULL AND TRIM(proveedor)<>'' GROUP BY UPPER(TRIM(proveedor));");
        m.Sql("UPDATE \"REMTool\".movimiento_financiero m SET proveedor_financiero_id=p.id FROM \"REMTool\".proveedor_financiero p WHERE UPPER(TRIM(m.proveedor))=p.nombre_normalizado;");
        m.AddForeignKey(name:"FK_movimiento_financiero_sub_item",table:"movimiento_financiero",column:"sub_item_presupuestario_id",schema:"REMTool",principalSchema:"REMTool",principalTable:"sub_item_presupuestario",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        m.AddForeignKey(name:"FK_movimiento_financiero_proveedor",table:"movimiento_financiero",column:"proveedor_financiero_id",schema:"REMTool",principalSchema:"REMTool",principalTable:"proveedor_financiero",principalColumn:"id",onDelete:ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder m) { m.DropForeignKey("FK_movimiento_financiero_sub_item","movimiento_financiero","REMTool");m.DropForeignKey("FK_movimiento_financiero_proveedor","movimiento_financiero","REMTool");m.DropTable("sub_item_presupuestario","REMTool");m.DropTable("proveedor_financiero","REMTool");m.DropColumn("sub_item_presupuestario_id","movimiento_financiero","REMTool");m.DropColumn("proveedor_financiero_id","movimiento_financiero","REMTool"); }
}

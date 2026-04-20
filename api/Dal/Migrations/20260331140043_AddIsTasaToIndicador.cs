using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Dal.Migrations
{
    public partial class AddIsTasaToIndicador : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_tasa",
                schema: "REMTool",
                table: "indicador",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_tasa",
                schema: "REMTool",
                table: "indicador");
        }
    }
}

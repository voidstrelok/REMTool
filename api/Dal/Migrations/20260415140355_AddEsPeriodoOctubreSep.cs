using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddEsPeriodoOctubreSep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "es_periodo_octubre_sep",
                schema: "REMTool",
                table: "indicador",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "es_periodo_octubre_sep",
                schema: "REMTool",
                table: "indicador");
        }
    }
}

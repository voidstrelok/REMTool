using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoreoEnabledToParametros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "monitoreo_enabled",
                schema: "REMTool",
                table: "parametros",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "monitoreo_enabled",
                schema: "REMTool",
                table: "parametros");
        }
    }
}

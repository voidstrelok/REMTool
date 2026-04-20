using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddDesplazarDenominador3MesesAIndicador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "desplazar_denominador_3_meses",
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
                name: "desplazar_denominador_3_meses",
                schema: "REMTool",
                table: "indicador");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddNumeradorPDenominadorP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "denominador_p",
                schema: "REMTool",
                table: "resultado_indicador",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "numerador_p",
                schema: "REMTool",
                table: "resultado_indicador",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "denominador_p",
                schema: "REMTool",
                table: "resultado_indicador");

            migrationBuilder.DropColumn(
                name: "numerador_p",
                schema: "REMTool",
                table: "resultado_indicador");
        }
    }
}

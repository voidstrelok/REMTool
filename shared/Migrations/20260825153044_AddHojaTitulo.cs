using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddHojaTitulo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "titulo",
                schema: "REMTool",
                table: "hoja_rem",
                type: "character varying",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "titulo",
                schema: "REMTool",
                table: "hoja_rem");
        }
    }
}

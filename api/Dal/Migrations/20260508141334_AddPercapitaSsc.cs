using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RemTool.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddPercapitaSsc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PERCAPITA_SSC",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_establecimiento = table.Column<long>(type: "bigint", nullable: false),
                    edad = table.Column<int>(type: "integer", nullable: false),
                    inscritos = table.Column<int>(type: "integer", nullable: false),
                    sexo = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERCAPITA_SSC", x => x.id);
                    table.ForeignKey(
                        name: "FK_PERCAPITA_SSC_establecimiento_id_establecimiento",
                        column: x => x.id_establecimiento,
                        principalSchema: "REMTool",
                        principalTable: "establecimiento",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PERCAPITA_SSC_id_establecimiento",
                schema: "REMTool",
                table: "PERCAPITA_SSC",
                column: "id_establecimiento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PERCAPITA_SSC",
                schema: "REMTool");
        }
    }
}

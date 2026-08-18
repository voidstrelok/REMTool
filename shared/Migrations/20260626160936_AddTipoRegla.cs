using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoRegla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SEQUENCE IF NOT EXISTS ""REMTool"".tipo_regla_id_seq
                    AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
            ");

            migrationBuilder.CreateTable(
                name: "tipo_regla",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false,
                        defaultValueSql: "nextval('\"REMTool\".tipo_regla_id_seq'::regclass)"),
                    nombre = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_regla", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "REMTool",
                table: "tipo_regla",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Error" },
                    { 2, "Advertencia" }
                });

            migrationBuilder.Sql(@"SELECT setval('""REMTool"".tipo_regla_id_seq', 2, true);");

            migrationBuilder.AddColumn<int>(
                name: "id_tipo_regla",
                schema: "REMTool",
                table: "regla",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddForeignKey(
                name: "FK_regla_tipo_regla",
                schema: "REMTool",
                table: "regla",
                column: "id_tipo_regla",
                principalSchema: "REMTool",
                principalTable: "tipo_regla",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_regla_tipo_regla",
                schema: "REMTool",
                table: "regla");

            migrationBuilder.DropColumn(
                name: "id_tipo_regla",
                schema: "REMTool",
                table: "regla");

            migrationBuilder.DropTable(
                name: "tipo_regla",
                schema: "REMTool");

            migrationBuilder.Sql(@"DROP SEQUENCE IF EXISTS ""REMTool"".tipo_regla_id_seq;");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddConvenioAndIndicadorConvenio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "desplazar_denominador_3_meses",
                schema: "REMTool",
                table: "indicador");

            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS \"REMTool\".convenio_id_seq INCREMENT 1 START 1 MINVALUE 1;");
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS \"REMTool\".indicador_convenio_id_seq INCREMENT 1 START 1 MINVALUE 1;");

            migrationBuilder.CreateTable(
                name: "convenio",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false, defaultValueSql: "nextval('\"REMTool\".convenio_id_seq'::regclass)"),
                    nombre = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_convenio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "indicador_convenio",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false, defaultValueSql: "nextval('\"REMTool\".indicador_convenio_id_seq'::regclass)"),
                    id_indicador = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false),
                    id_convenio = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_indicador_convenio", x => x.id);
                    table.ForeignKey(
                        name: "FK_indicador_convenio_convenio_id_convenio",
                        column: x => x.id_convenio,
                        principalSchema: "REMTool",
                        principalTable: "convenio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_indicador_convenio_indicador_id_indicador",
                        column: x => x.id_indicador,
                        principalSchema: "REMTool",
                        principalTable: "indicador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_indicador_convenio_id_convenio",
                schema: "REMTool",
                table: "indicador_convenio",
                column: "id_convenio");

            migrationBuilder.CreateIndex(
                name: "IX_indicador_convenio_id_indicador",
                schema: "REMTool",
                table: "indicador_convenio",
                column: "id_indicador");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "indicador_convenio",
                schema: "REMTool");

            migrationBuilder.DropTable(
                name: "convenio",
                schema: "REMTool");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS \"REMTool\".indicador_convenio_id_seq;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS \"REMTool\".convenio_id_seq;");

            migrationBuilder.AddColumn<bool>(
                name: "desplazar_denominador_3_meses",
                schema: "REMTool",
                table: "indicador",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class FixSeccionRemIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SEQUENCE IF NOT EXISTS ""REMTool"".seccion_rem_id_seq
                    AS bigint START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;

                SELECT setval(
                    '""REMTool"".seccion_rem_id_seq',
                    COALESCE((SELECT MAX(id) FROM ""REMTool"".seccion_rem), 0) + 1,
                    false);

                ALTER TABLE ""REMTool"".seccion_rem
                    ALTER COLUMN id SET DEFAULT nextval('""REMTool"".seccion_rem_id_seq'::regclass);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""REMTool"".seccion_rem
                    ALTER COLUMN id DROP DEFAULT;
                DROP SEQUENCE IF EXISTS ""REMTool"".seccion_rem_id_seq;
            ");
        }
    }
}

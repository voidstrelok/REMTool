using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RemTool.Shared;

#nullable disable

namespace RemTool.Dal.Migrations
{
    /// <summary>Marca resultados cargados a mano para que el cálculo de indicadores no los borre.</summary>
    [DbContext(typeof(RemToolDataContext))]
    [Migration("20261118120000_AddEsManualToResultadoIndicador")]
    public partial class AddEsManualToResultadoIndicador : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""REMTool"".resultado_indicador ADD COLUMN IF NOT EXISTS es_manual boolean NOT NULL DEFAULT false;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""REMTool"".resultado_indicador DROP COLUMN IF EXISTS es_manual;");
        }
    }
}

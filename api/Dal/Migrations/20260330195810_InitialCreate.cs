using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Dal.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline migration: the database already exists.
            // This method is intentionally empty so that running
            // "dotnet ef database update" only registers this migration
            // in __EFMigrationsHistory without executing any DDL.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty for a baseline migration.
        }
    }
}

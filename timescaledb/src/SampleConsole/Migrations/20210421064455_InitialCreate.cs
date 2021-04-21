using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SampleConsole.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conditions",
                columns: table => new
                {
                    time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    location = table.Column<string>(type: "text", nullable: true),
                    temperature = table.Column<double>(type: "double precision", nullable: true),
                    humidity = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                });
            migrationBuilder.Sql("SELECT create_hypertable('conditions', 'time')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conditions");
        }
    }
}

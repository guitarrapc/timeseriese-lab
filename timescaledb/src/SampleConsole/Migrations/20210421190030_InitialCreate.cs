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

            migrationBuilder.CreateTable(
                name: "simpledata",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    temperature = table.Column<double>(type: "double precision", nullable: false),
                    humidity = table.Column<double>(type: "double precision", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });
            migrationBuilder.Sql("SELECT create_hypertable('simpledata', 'id', chunk_time_interval => 100000)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conditions");

            migrationBuilder.DropTable(
                name: "simpledata");
        }
    }
}

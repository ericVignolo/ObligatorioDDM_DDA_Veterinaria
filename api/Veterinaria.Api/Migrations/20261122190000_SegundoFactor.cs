using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class SegundoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecretoSegundoFactor",
                table: "Cuentas",
                type: "varchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SegundoFactorActivo",
                table: "Cuentas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "UltimoPasoSegundoFactor",
                table: "Cuentas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretoSegundoFactor",
                table: "Cuentas");

            migrationBuilder.DropColumn(
                name: "SegundoFactorActivo",
                table: "Cuentas");

            migrationBuilder.DropColumn(
                name: "UltimoPasoSegundoFactor",
                table: "Cuentas");
        }
    }
}

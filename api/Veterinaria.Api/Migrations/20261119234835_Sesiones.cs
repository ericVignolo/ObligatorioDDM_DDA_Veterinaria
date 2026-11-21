using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sesiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadaHasta",
                table: "Cuentas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntentosFallidos",
                table: "Cuentas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SesionesRefresco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    HashToken = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Vence = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevocadaEn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Origen = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesRefresco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionesRefresco_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BloqueadaHasta", "IntentosFallidos" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BloqueadaHasta", "IntentosFallidos" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BloqueadaHasta", "IntentosFallidos" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BloqueadaHasta", "IntentosFallidos" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "BloqueadaHasta", "IntentosFallidos" },
                values: new object[] { null, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_SesionesRefresco_CuentaId_RevocadaEn",
                table: "SesionesRefresco",
                columns: new[] { "CuentaId", "RevocadaEn" });

            migrationBuilder.CreateIndex(
                name: "IX_SesionesRefresco_HashToken",
                table: "SesionesRefresco",
                column: "HashToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SesionesRefresco");

            migrationBuilder.DropColumn(
                name: "BloqueadaHasta",
                table: "Cuentas");

            migrationBuilder.DropColumn(
                name: "IntentosFallidos",
                table: "Cuentas");
        }
    }
}

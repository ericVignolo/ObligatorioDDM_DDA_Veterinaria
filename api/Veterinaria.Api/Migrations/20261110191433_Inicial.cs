using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CuentaId = table.Column<int>(type: "int", nullable: true),
                    Accion = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Entidad = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    EntidadId = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Origen = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Detalle = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clinicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    RazonSocial = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Lema = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Presentacion = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    Direccion = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Ciudad = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    TelefonoGuardia = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Correo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Horario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Cobertura = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinicas", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Apellido = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Correo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    TipoDocumento = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Calle = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    NumeroPuerta = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Apartamento = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Localidad = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Departamento = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Referencias = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    HashContrasena = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Verificada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaBaja = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DiasNoLaborables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiasNoLaborables", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentosLegales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Titulo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Version = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    VigenteDesde = table.Column<DateTime>(type: "date", nullable: false),
                    Ruta = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosLegales", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Especies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Especies", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Parametros",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Valor = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametros", x => x.Clave);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Prestaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    PrecioDesde = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ReservableEnLinea = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    Publicado = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestaciones", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Recursos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recursos", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CodigosCuenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    HashCodigo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Vence = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IntentosRestantes = table.Column<int>(type: "int", nullable: false),
                    Usado = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosCuenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodigosCuenta_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Profesionales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CuentaId = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Especialidad = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Matricula = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Publicado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profesionales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profesionales_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AceptacionesLegales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    DocumentoLegalId = table.Column<int>(type: "int", nullable: false),
                    FechaAceptacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AceptacionesLegales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AceptacionesLegales_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AceptacionesLegales_DocumentosLegales_DocumentoLegalId",
                        column: x => x.DocumentoLegalId,
                        principalTable: "DocumentosLegales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Animales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    EspecieId = table.Column<int>(type: "int", nullable: false),
                    Raza = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Sexo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "date", nullable: true),
                    Microchip = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true),
                    Caravana = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    EsGrupo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CantidadGrupo = table.Column<int>(type: "int", nullable: true),
                    SenasParticulares = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Antecedentes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Animales_Especies_EspecieId",
                        column: x => x.EspecieId,
                        principalTable: "Especies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CuentaRoles",
                columns: table => new
                {
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentaRoles", x => new { x.CuentaId, x.RolId });
                    table.ForeignKey(
                        name: "FK_CuentaRoles_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuentaRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ausencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ProfesionalId = table.Column<int>(type: "int", nullable: false),
                    Desde = table.Column<DateTime>(type: "date", nullable: false),
                    Hasta = table.Column<DateTime>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ausencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ausencias_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FranjasHorarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ProfesionalId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Dia = table.Column<int>(type: "int", nullable: false),
                    Inicio = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Fin = table.Column<TimeSpan>(type: "time(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FranjasHorarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FranjasHorarias_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProfesionalEspecies",
                columns: table => new
                {
                    ProfesionalId = table.Column<int>(type: "int", nullable: false),
                    EspecieId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfesionalEspecies", x => new { x.ProfesionalId, x.EspecieId });
                    table.ForeignKey(
                        name: "FK_ProfesionalEspecies_Especies_EspecieId",
                        column: x => x.EspecieId,
                        principalTable: "Especies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfesionalEspecies_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProfesionalPrestaciones",
                columns: table => new
                {
                    ProfesionalId = table.Column<int>(type: "int", nullable: false),
                    PrestacionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfesionalPrestaciones", x => new { x.ProfesionalId, x.PrestacionId });
                    table.ForeignKey(
                        name: "FK_ProfesionalPrestaciones_Prestaciones_PrestacionId",
                        column: x => x.PrestacionId,
                        principalTable: "Prestaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfesionalPrestaciones_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Responsabilidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    Desde = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Hasta = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responsabilidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Responsabilidades_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Responsabilidades_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    PrestacionId = table.Column<int>(type: "int", nullable: false),
                    ProfesionalId = table.Column<int>(type: "int", nullable: false),
                    Inicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Fin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MotivoCancelacion = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    InicioAnterior = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turnos_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Cuentas_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Prestaciones_PrestacionId",
                        column: x => x.PrestacionId,
                        principalTable: "Prestaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TurnoRecursos",
                columns: table => new
                {
                    TurnoId = table.Column<int>(type: "int", nullable: false),
                    RecursoId = table.Column<int>(type: "int", nullable: false),
                    Inicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Fin = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnoRecursos", x => new { x.TurnoId, x.RecursoId });
                    table.ForeignKey(
                        name: "FK_TurnoRecursos_Recursos_RecursoId",
                        column: x => x.RecursoId,
                        principalTable: "Recursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TurnoRecursos_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Clinicas",
                columns: new[] { "Id", "Ciudad", "Cobertura", "Correo", "Direccion", "Horario", "Lema", "Nombre", "Presentacion", "RazonSocial", "Telefono", "TelefonoGuardia" },
                values: new object[] { 1, "Nueva Palmira, Colonia", "Asistencia a domicilio hasta 30 km desde la clínica.", "contacto@vitalia.uy", "Calle 18 de Julio 1234", "Lunes a viernes de 9 a 19 h · Sábados de 9 a 13 h", "Clínica veterinaria para animales de compañía y de campo", "VITALIA", "Atendemos perros, gatos, caballos, ovinos y bovinos. Trabajamos en la clínica y, cuando hace falta, vamos a tu casa o a tu campo.", "VITALIA Servicios Veterinarios S.A.S.", "+598 4544 0000", "+598 99 000 000" });

            migrationBuilder.InsertData(
                table: "DiasNoLaborables",
                columns: new[] { "Id", "Fecha", "Motivo" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Navidad" },
                    { 2, new DateTime(2027, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Año Nuevo" },
                    { 3, new DateTime(2027, 1, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de Reyes" }
                });

            migrationBuilder.InsertData(
                table: "DocumentosLegales",
                columns: new[] { "Id", "Codigo", "Ruta", "Titulo", "Version", "VigenteDesde" },
                values: new object[,]
                {
                    { 1, "privacidad", "privacidad", "Política de Privacidad", "1.0", new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "terminos", "terminos", "Términos y Condiciones", "1.0", new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, "cookies", "cookies", "Política de Cookies", "1.0", new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Especies",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Perro" },
                    { 2, "Gato" },
                    { 3, "Ave" },
                    { 4, "Conejo" },
                    { 5, "Reptil" },
                    { 6, "Otra" },
                    { 7, "Vaca" },
                    { 8, "Oveja" },
                    { 9, "Cabra" },
                    { 10, "Cerdo" },
                    { 11, "Caballo" }
                });

            migrationBuilder.InsertData(
                table: "Parametros",
                columns: new[] { "Clave", "Descripcion", "Valor" },
                values: new object[,]
                {
                    { "agenda.anticipacion_minima_horas", "Horas mínimas entre la reserva y el turno.", "2" },
                    { "agenda.cancelacion_sin_costo_horas", "Horas antes del turno hasta las que se cancela sin costo.", "24" },
                    { "agenda.horizonte_dias", "Días hacia adelante en que se puede reservar.", "60" },
                    { "agenda.intervalo_inicio_minutos", "Cada cuántos minutos puede empezar un turno.", "15" },
                    { "asistencia.cobertura_km", "Zona de cobertura desde la clínica.", "30" },
                    { "asistencia.escalado_rojo_minutos", "Espera de aceptación de un rojo antes de escalar a Recepción.", "10" },
                    { "avisos.refuerzo_dias_antes", "Días de anticipación del aviso de refuerzo de vacunas y desparasitaciones.", "5" },
                    { "registro.codigo_intentos", "Intentos por código.", "5" },
                    { "registro.codigo_vigencia_minutos", "Minutos de validez del código enviado por correo.", "15" },
                    { "registro.dias_sin_verificar", "Días tras los que se elimina una cuenta no verificada.", "7" },
                    { "registro.reenvios_por_hora", "Reenvíos de código permitidos por hora.", "3" }
                });

            migrationBuilder.InsertData(
                table: "Prestaciones",
                columns: new[] { "Id", "Descripcion", "DuracionMinutos", "Nombre", "PrecioDesde", "Publicado", "ReservableEnLinea" },
                values: new object[,]
                {
                    { 1, "Revisión completa: anamnesis, examen físico, diagnóstico y tratamiento. Animales de compañía, equinos y otras especies.", 30, "Consulta clínica general", 1200m, true, true },
                    { 2, "Aplicación con registro de producto, lote y fecha de refuerzo.", 15, "Vacunación", 600m, true, true },
                    { 3, "Tratamiento antiparasitario indicado por el veterinario.", 15, "Desparasitación", 450m, true, true },
                    { 4, "Revisión individual o por grupo. Se coordina con la clínica.", 0, "Revisión de ovinos y bovinos", 1500m, true, false },
                    { 5, "Fisioterapia para caballos. Requiere evaluación previa.", 0, "Rehabilitación equina", 2500m, true, false },
                    { 6, "Baño, secado, corte y cuidado estético de perros y gatos.", 60, "Baño y estética", 900m, true, true },
                    { 7, "No publicado: no debe aparecer.", 0, "Guardería", 700m, false, false }
                });

            migrationBuilder.InsertData(
                table: "Profesionales",
                columns: new[] { "Id", "Activo", "CuentaId", "Descripcion", "Especialidad", "Matricula", "Nombre", "Publicado" },
                values: new object[,]
                {
                    { 1, true, null, "Consultas generales, vacunación y controles de perros, gatos y otros animales de compañía.", "Clínica de pequeños animales", "", "Dra. Lucía Ferreira", true },
                    { 2, true, null, "Atención de bovinos, ovinos, caprinos y porcinos, revisiones de rodeo y visitas a campo.", "Grandes animales", "", "Dr. Martín Silveira", true },
                    { 3, true, null, "Clínica y rehabilitación de caballos. La rehabilitación requiere evaluación previa.", "Equinos", "", "Dra. Valentina Acosta", true },
                    { 4, true, null, "No publicado: no debe aparecer.", "Clínica general", "", "Dr. Andrés Pereira", false },
                    { 5, true, null, "Baño, corte y cuidado estético de perros y gatos. No realiza atención clínica.", "Baño y estética", "", "Sofía Méndez", true }
                });

            migrationBuilder.InsertData(
                table: "Recursos",
                columns: new[] { "Id", "Activo", "Nombre", "Tipo" },
                values: new object[,]
                {
                    { 1, true, "Consultorio 1", "Consultorio" },
                    { 2, true, "Consultorio 2", "Consultorio" },
                    { 3, true, "Sala de estética", "Sala" },
                    { 4, true, "Corral de grandes animales", "Corral" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Cliente" },
                    { 2, "Recepcionista" },
                    { 3, "Veterinario" },
                    { 4, "Administrador" }
                });

            migrationBuilder.InsertData(
                table: "Ausencias",
                columns: new[] { "Id", "Desde", "Hasta", "Motivo", "ProfesionalId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Capacitación", 1 },
                    { 2, new DateTime(2026, 11, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 12, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Licencia", 2 }
                });

            migrationBuilder.InsertData(
                table: "FranjasHorarias",
                columns: new[] { "Id", "Dia", "Fin", "Inicio", "ProfesionalId", "Tipo" },
                values: new object[,]
                {
                    { 1, 1, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 1, "Jornada" },
                    { 2, 2, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 1, "Jornada" },
                    { 3, 3, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 1, "Jornada" },
                    { 4, 4, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 1, "Jornada" },
                    { 5, 5, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 1, "Jornada" },
                    { 6, 1, new TimeSpan(0, 12, 0, 0, 0), new TimeSpan(0, 11, 30, 0, 0), 1, "Descanso" },
                    { 7, 2, new TimeSpan(0, 12, 0, 0, 0), new TimeSpan(0, 11, 30, 0, 0), 1, "Descanso" },
                    { 8, 3, new TimeSpan(0, 12, 0, 0, 0), new TimeSpan(0, 11, 30, 0, 0), 1, "Descanso" },
                    { 9, 4, new TimeSpan(0, 12, 0, 0, 0), new TimeSpan(0, 11, 30, 0, 0), 1, "Descanso" },
                    { 10, 5, new TimeSpan(0, 12, 0, 0, 0), new TimeSpan(0, 11, 30, 0, 0), 1, "Descanso" },
                    { 11, 1, new TimeSpan(0, 19, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), 2, "Jornada" },
                    { 12, 2, new TimeSpan(0, 19, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), 2, "Jornada" },
                    { 13, 3, new TimeSpan(0, 19, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), 2, "Jornada" },
                    { 14, 4, new TimeSpan(0, 19, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), 2, "Jornada" },
                    { 15, 5, new TimeSpan(0, 19, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), 2, "Jornada" },
                    { 16, 6, new TimeSpan(0, 13, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 2, "Jornada" },
                    { 17, 1, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 16, 30, 0, 0), 2, "Descanso" },
                    { 18, 2, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 16, 30, 0, 0), 2, "Descanso" },
                    { 19, 3, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 16, 30, 0, 0), 2, "Descanso" },
                    { 20, 4, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 16, 30, 0, 0), 2, "Descanso" },
                    { 21, 5, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 16, 30, 0, 0), 2, "Descanso" },
                    { 22, 2, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 3, "Jornada" },
                    { 23, 4, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 3, "Jornada" },
                    { 24, 2, new TimeSpan(0, 13, 30, 0, 0), new TimeSpan(0, 12, 30, 0, 0), 3, "Descanso" },
                    { 25, 4, new TimeSpan(0, 13, 30, 0, 0), new TimeSpan(0, 12, 30, 0, 0), 3, "Descanso" },
                    { 26, 1, new TimeSpan(0, 18, 0, 0, 0), new TimeSpan(0, 10, 0, 0, 0), 5, "Jornada" },
                    { 27, 2, new TimeSpan(0, 18, 0, 0, 0), new TimeSpan(0, 10, 0, 0, 0), 5, "Jornada" },
                    { 28, 3, new TimeSpan(0, 18, 0, 0, 0), new TimeSpan(0, 10, 0, 0, 0), 5, "Jornada" },
                    { 29, 4, new TimeSpan(0, 18, 0, 0, 0), new TimeSpan(0, 10, 0, 0, 0), 5, "Jornada" },
                    { 30, 5, new TimeSpan(0, 18, 0, 0, 0), new TimeSpan(0, 10, 0, 0, 0), 5, "Jornada" },
                    { 31, 6, new TimeSpan(0, 13, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 5, "Jornada" },
                    { 32, 1, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), 5, "Descanso" },
                    { 33, 2, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), 5, "Descanso" },
                    { 34, 3, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), 5, "Descanso" },
                    { 35, 4, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), 5, "Descanso" },
                    { 36, 5, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), 5, "Descanso" }
                });

            migrationBuilder.InsertData(
                table: "ProfesionalEspecies",
                columns: new[] { "EspecieId", "ProfesionalId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 2 },
                    { 8, 2 },
                    { 9, 2 },
                    { 10, 2 },
                    { 11, 3 },
                    { 1, 5 },
                    { 2, 5 }
                });

            migrationBuilder.InsertData(
                table: "ProfesionalPrestaciones",
                columns: new[] { "PrestacionId", "ProfesionalId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 1, 2 },
                    { 2, 2 },
                    { 3, 2 },
                    { 1, 3 },
                    { 6, 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AceptacionesLegales_CuentaId_DocumentoLegalId",
                table: "AceptacionesLegales",
                columns: new[] { "CuentaId", "DocumentoLegalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AceptacionesLegales_DocumentoLegalId",
                table: "AceptacionesLegales",
                column: "DocumentoLegalId");

            migrationBuilder.CreateIndex(
                name: "IX_Animales_EspecieId",
                table: "Animales",
                column: "EspecieId");

            migrationBuilder.CreateIndex(
                name: "IX_Animales_Microchip",
                table: "Animales",
                column: "Microchip",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Entidad_EntidadId",
                table: "Auditorias",
                columns: new[] { "Entidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Fecha",
                table: "Auditorias",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Ausencias_ProfesionalId_Desde_Hasta",
                table: "Ausencias",
                columns: new[] { "ProfesionalId", "Desde", "Hasta" });

            migrationBuilder.CreateIndex(
                name: "IX_CodigosCuenta_CuentaId_Tipo_FechaCreacion",
                table: "CodigosCuenta",
                columns: new[] { "CuentaId", "Tipo", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_CuentaRoles_RolId",
                table: "CuentaRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_Correo",
                table: "Cuentas",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_TipoDocumento_NumeroDocumento",
                table: "Cuentas",
                columns: new[] { "TipoDocumento", "NumeroDocumento" });

            migrationBuilder.CreateIndex(
                name: "IX_DiasNoLaborables_Fecha",
                table: "DiasNoLaborables",
                column: "Fecha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosLegales_Codigo_Version",
                table: "DocumentosLegales",
                columns: new[] { "Codigo", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Especies_Nombre",
                table: "Especies",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FranjasHorarias_ProfesionalId_Dia",
                table: "FranjasHorarias",
                columns: new[] { "ProfesionalId", "Dia" });

            migrationBuilder.CreateIndex(
                name: "IX_Profesionales_CuentaId",
                table: "Profesionales",
                column: "CuentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfesionalEspecies_EspecieId",
                table: "ProfesionalEspecies",
                column: "EspecieId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfesionalPrestaciones_PrestacionId",
                table: "ProfesionalPrestaciones",
                column: "PrestacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Responsabilidades_AnimalId",
                table: "Responsabilidades",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Responsabilidades_CuentaId_AnimalId_Hasta",
                table: "Responsabilidades",
                columns: new[] { "CuentaId", "AnimalId", "Hasta" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TurnoRecursos_RecursoId_Inicio_Fin",
                table: "TurnoRecursos",
                columns: new[] { "RecursoId", "Inicio", "Fin" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_AnimalId_Inicio_Fin",
                table: "Turnos",
                columns: new[] { "AnimalId", "Inicio", "Fin" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ClienteId_Inicio",
                table: "Turnos",
                columns: new[] { "ClienteId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_PrestacionId",
                table: "Turnos",
                column: "PrestacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ProfesionalId_Inicio_Fin",
                table: "Turnos",
                columns: new[] { "ProfesionalId", "Inicio", "Fin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AceptacionesLegales");

            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.DropTable(
                name: "Ausencias");

            migrationBuilder.DropTable(
                name: "Clinicas");

            migrationBuilder.DropTable(
                name: "CodigosCuenta");

            migrationBuilder.DropTable(
                name: "CuentaRoles");

            migrationBuilder.DropTable(
                name: "DiasNoLaborables");

            migrationBuilder.DropTable(
                name: "FranjasHorarias");

            migrationBuilder.DropTable(
                name: "Parametros");

            migrationBuilder.DropTable(
                name: "ProfesionalEspecies");

            migrationBuilder.DropTable(
                name: "ProfesionalPrestaciones");

            migrationBuilder.DropTable(
                name: "Responsabilidades");

            migrationBuilder.DropTable(
                name: "TurnoRecursos");

            migrationBuilder.DropTable(
                name: "DocumentosLegales");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Recursos");

            migrationBuilder.DropTable(
                name: "Turnos");

            migrationBuilder.DropTable(
                name: "Animales");

            migrationBuilder.DropTable(
                name: "Prestaciones");

            migrationBuilder.DropTable(
                name: "Profesionales");

            migrationBuilder.DropTable(
                name: "Especies");

            migrationBuilder.DropTable(
                name: "Cuentas");
        }
    }
}

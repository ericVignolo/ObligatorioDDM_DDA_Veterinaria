using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class ModulosClinicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanceladoPor",
                table: "Turnos",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreadoPorId",
                table: "Turnos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsSobreturno",
                table: "Turnos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoraLlegada",
                table: "Turnos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoSobreturno",
                table: "Turnos",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "MatriculaVigenteHasta",
                table: "Profesionales",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereConsentimiento",
                table: "Prestaciones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CorreoNuevoPendiente",
                table: "Cuentas",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Eliminada",
                table: "Cuentas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "Cuentas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Auditorias",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Articulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Unidad = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    StockMinimo = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articulos", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Boxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boxes", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Consultas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TurnoId = table.Column<int>(type: "int", nullable: true),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    ProfesionalId = table.Column<int>(type: "int", nullable: false),
                    AutorId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Anamnesis = table.Column<string>(type: "longtext", nullable: false),
                    ExamenFisico = table.Column<string>(type: "longtext", nullable: false),
                    TemperaturaC = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    FrecuenciaCardiaca = table.Column<int>(type: "int", nullable: true),
                    FrecuenciaRespiratoria = table.Column<int>(type: "int", nullable: true),
                    PesoKg = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Alergias = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Diagnostico = table.Column<string>(type: "longtext", nullable: false),
                    TipoDiagnostico = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Tratamiento = table.Column<string>(type: "longtext", nullable: false),
                    Indicaciones = table.Column<string>(type: "longtext", nullable: false),
                    AlergiaConfirmada = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    FechaFirma = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HashContenido = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consultas_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consultas_Cuentas_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consultas_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consultas_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentosConsentimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PrestacionId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Titulo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Texto = table.Column<string>(type: "longtext", nullable: false),
                    FechaVigencia = table.Column<DateTime>(type: "date", nullable: false),
                    Vigente = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosConsentimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosConsentimiento_Prestaciones_PrestacionId",
                        column: x => x.PrestacionId,
                        principalTable: "Prestaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LecturasHistoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LecturasHistoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LecturasHistoria_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LecturasHistoria_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Numeradores",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Ultimo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Numeradores", x => x.Clave);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PoliticasConservacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Categoria = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Finalidad = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Acceso = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Conservacion = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    AlEliminar = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasConservacion", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SolicitudesAsistencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: true),
                    Especie = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Sintomas = table.Column<string>(type: "longtext", nullable: false),
                    Modalidad = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Direccion = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Referencias = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    UbicacionGps = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Latitud = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitud = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    DistanciaKm = table.Column<int>(type: "int", nullable: false),
                    TelefonoContacto = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Canal = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ClaveIdempotencia = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    NivelCliente = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Nivel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaOferta = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    VeterinarioId = table.Column<int>(type: "int", nullable: true),
                    FechaAceptacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EtaMinutos = table.Column<int>(type: "int", nullable: true),
                    Costo = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ReevaluacionPedida = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MotivoCoordinacion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    MotivoCierre = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Resultado = table.Column<string>(type: "longtext", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesAsistencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesAsistencia_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesAsistencia_Cuentas_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesAsistencia_Cuentas_VeterinarioId",
                        column: x => x.VeterinarioId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SolicitudesDerecho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResueltaPorId = table.Column<int>(type: "int", nullable: true),
                    Detalle = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesDerecho", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesDerecho_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesDerecho_Cuentas_ResueltaPorId",
                        column: x => x.ResueltaPorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TarifasPrestacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PrestacionId = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    VigenteDesde = table.Column<DateTime>(type: "date", nullable: false),
                    RegistradoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifasPrestacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarifasPrestacion_Cuentas_RegistradoPorId",
                        column: x => x.RegistradoPorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TarifasPrestacion_Prestaciones_PrestacionId",
                        column: x => x.PrestacionId,
                        principalTable: "Prestaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TiposComunicacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Esencial = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanalPorDefecto = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposComunicacion", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertasStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ArticuloId = table.Column<int>(type: "int", nullable: false),
                    StockQueQuedo = table.Column<int>(type: "int", nullable: false),
                    StockMinimo = table.Column<int>(type: "int", nullable: false),
                    Origen = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Atendida = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasStock_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Lotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ArticuloId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Vencimiento = table.Column<DateTime>(type: "date", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Inhabilitado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lotes", x => x.Id);
                    table.CheckConstraint("CK_Lotes_CantidadNoNegativa", "`Cantidad` >= 0");
                    table.ForeignKey(
                        name: "FK_Lotes_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductosSanitarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Laboratorio = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    ArticuloId = table.Column<int>(type: "int", nullable: false),
                    IntervaloRefuerzoDias = table.Column<int>(type: "int", nullable: false),
                    IntervaloMinimoDias = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductosSanitarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductosSanitarios_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Internaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    TelefonoContacto = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Observaciones = table.Column<string>(type: "longtext", nullable: false),
                    FechaAdmision = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AdmitidoPorId = table.Column<int>(type: "int", nullable: true),
                    BoxId = table.Column<int>(type: "int", nullable: true),
                    PlanTerapeutico = table.Column<string>(type: "longtext", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    VeterinarioIngresoId = table.Column<int>(type: "int", nullable: true),
                    EvolucionFinal = table.Column<string>(type: "longtext", nullable: false),
                    IndicacionesDomicilio = table.Column<string>(type: "longtext", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    VeterinarioAltaId = table.Column<int>(type: "int", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Internaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Internaciones_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Internaciones_Boxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "Boxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Internaciones_Cuentas_AdmitidoPorId",
                        column: x => x.AdmitidoPorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Internaciones_Cuentas_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Internaciones_Cuentas_VeterinarioAltaId",
                        column: x => x.VeterinarioAltaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Internaciones_Cuentas_VeterinarioIngresoId",
                        column: x => x.VeterinarioIngresoId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Enmiendas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ConsultaId = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Texto = table.Column<string>(type: "longtext", nullable: false),
                    AutorId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    HashContenido = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enmiendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enmiendas_Consultas_ConsultaId",
                        column: x => x.ConsultaId,
                        principalTable: "Consultas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enmiendas_Cuentas_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Consentimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TurnoId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    PrestacionId = table.Column<int>(type: "int", nullable: false),
                    DocumentoConsentimientoId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Respuesta = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    NombreEscrito = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    Canal = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Hash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consentimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consentimientos_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consentimientos_Cuentas_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consentimientos_DocumentosConsentimiento_DocumentoConsentimi~",
                        column: x => x.DocumentoConsentimientoId,
                        principalTable: "DocumentosConsentimiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consentimientos_Prestaciones_PrestacionId",
                        column: x => x.PrestacionId,
                        principalTable: "Prestaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consentimientos_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cargos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ConsultaId = table.Column<int>(type: "int", nullable: true),
                    SolicitudAsistenciaId = table.Column<int>(type: "int", nullable: true),
                    TurnoId = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: true),
                    PrestacionId = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EsSena = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cargos_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cargos_Consultas_ConsultaId",
                        column: x => x.ConsultaId,
                        principalTable: "Consultas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cargos_Cuentas_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cargos_Prestaciones_PrestacionId",
                        column: x => x.PrestacionId,
                        principalTable: "Prestaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cargos_SolicitudesAsistencia_SolicitudAsistenciaId",
                        column: x => x.SolicitudAsistenciaId,
                        principalTable: "SolicitudesAsistencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cargos_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EventosAsistencia",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SolicitudAsistenciaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Texto = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    AutorId = table.Column<int>(type: "int", nullable: true),
                    NivelAnterior = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    NivelNuevo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAsistencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosAsistencia_Cuentas_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosAsistencia_SolicitudesAsistencia_SolicitudAsistenciaId",
                        column: x => x.SolicitudAsistenciaId,
                        principalTable: "SolicitudesAsistencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OfertasAsistencia",
                columns: table => new
                {
                    SolicitudAsistenciaId = table.Column<int>(type: "int", nullable: false),
                    VeterinarioId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Respuesta = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfertasAsistencia", x => new { x.SolicitudAsistenciaId, x.VeterinarioId });
                    table.ForeignKey(
                        name: "FK_OfertasAsistencia_Cuentas_VeterinarioId",
                        column: x => x.VeterinarioId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfertasAsistencia_SolicitudesAsistencia_SolicitudAsistenciaId",
                        column: x => x.SolicitudAsistenciaId,
                        principalTable: "SolicitudesAsistencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Comunicaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    TipoComunicacionId = table.Column<int>(type: "int", nullable: false),
                    Asunto = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Cuerpo = table.Column<string>(type: "longtext", nullable: false),
                    Canal = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Destino = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Intentos = table.Column<int>(type: "int", nullable: false),
                    UltimoError = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Clave = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    ReenvioDeId = table.Column<long>(type: "bigint", nullable: true),
                    GeneradaPorId = table.Column<int>(type: "int", nullable: true),
                    Leida = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comunicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comunicaciones_Comunicaciones_ReenvioDeId",
                        column: x => x.ReenvioDeId,
                        principalTable: "Comunicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comunicaciones_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comunicaciones_Cuentas_GeneradaPorId",
                        column: x => x.GeneradaPorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comunicaciones_TiposComunicacion_TipoComunicacionId",
                        column: x => x.TipoComunicacionId,
                        principalTable: "TiposComunicacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PreferenciasComunicacion",
                columns: table => new
                {
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    TipoComunicacionId = table.Column<int>(type: "int", nullable: false),
                    Canal = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasComunicacion", x => new { x.CuentaId, x.TipoComunicacionId });
                    table.ForeignKey(
                        name: "FK_PreferenciasComunicacion_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreferenciasComunicacion_TiposComunicacion_TipoComunicacionId",
                        column: x => x.TipoComunicacionId,
                        principalTable: "TiposComunicacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ArticuloId = table.Column<int>(type: "int", nullable: false),
                    LoteId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Origen = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Motivo = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PersonaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Cuentas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalTable: "Lotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AplicacionesSanitarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    ConsultaId = table.Column<int>(type: "int", nullable: true),
                    ProductoSanitarioId = table.Column<int>(type: "int", nullable: false),
                    Producto = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Laboratorio = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    LoteId = table.Column<int>(type: "int", nullable: true),
                    NumeroLote = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    LoteVence = table.Column<DateTime>(type: "date", nullable: true),
                    FechaAplicacion = table.Column<DateTime>(type: "date", nullable: false),
                    FechaRefuerzo = table.Column<DateTime>(type: "date", nullable: false),
                    AplicadaPorId = table.Column<int>(type: "int", nullable: true),
                    JustificacionAdelanto = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacionesSanitarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AplicacionesSanitarias_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AplicacionesSanitarias_Consultas_ConsultaId",
                        column: x => x.ConsultaId,
                        principalTable: "Consultas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AplicacionesSanitarias_Cuentas_AplicadaPorId",
                        column: x => x.AplicadaPorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AplicacionesSanitarias_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalTable: "Lotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AplicacionesSanitarias_ProductosSanitarios_ProductoSanitario~",
                        column: x => x.ProductoSanitarioId,
                        principalTable: "ProductosSanitarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductoSanitarioEspecies",
                columns: table => new
                {
                    ProductoSanitarioId = table.Column<int>(type: "int", nullable: false),
                    EspecieId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoSanitarioEspecies", x => new { x.ProductoSanitarioId, x.EspecieId });
                    table.ForeignKey(
                        name: "FK_ProductoSanitarioEspecies_Especies_EspecieId",
                        column: x => x.EspecieId,
                        principalTable: "Especies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductoSanitarioEspecies_ProductosSanitarios_ProductoSanita~",
                        column: x => x.ProductoSanitarioId,
                        principalTable: "ProductosSanitarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NovedadesInternacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InternacionId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Texto = table.Column<string>(type: "longtext", nullable: false),
                    Visible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RectificaId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AutorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovedadesInternacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NovedadesInternacion_Cuentas_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NovedadesInternacion_Internaciones_InternacionId",
                        column: x => x.InternacionId,
                        principalTable: "Internaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NovedadesInternacion_NovedadesInternacion_RectificaId",
                        column: x => x.RectificaId,
                        principalTable: "NovedadesInternacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CargoId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Medio = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Referencia = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CobradoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.CheckConstraint("CK_Pagos_MontoPositivo", "`Monto` > 0");
                    table.ForeignKey(
                        name: "FK_Pagos_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_Cuentas_CobradoPorId",
                        column: x => x.CobradoPorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReservasStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ConsultaId = table.Column<int>(type: "int", nullable: false),
                    ArticuloId = table.Column<int>(type: "int", nullable: false),
                    LoteId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    AplicacionSanitariaId = table.Column<int>(type: "int", nullable: true),
                    Confirmada = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservasStock_AplicacionesSanitarias_AplicacionSanitariaId",
                        column: x => x.AplicacionSanitariaId,
                        principalTable: "AplicacionesSanitarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasStock_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasStock_Consultas_ConsultaId",
                        column: x => x.ConsultaId,
                        principalTable: "Consultas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasStock_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalTable: "Lotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NotasCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    PagoId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Motivo = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EmitidaPorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasCredito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasCredito_Cuentas_EmitidaPorId",
                        column: x => x.EmitidaPorId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotasCredito_Pagos_PagoId",
                        column: x => x.PagoId,
                        principalTable: "Pagos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Articulos",
                columns: new[] { "Id", "Activo", "Nombre", "StockMinimo", "Tipo", "Unidad" },
                values: new object[,]
                {
                    { 1, true, "Vacuna polivalente canina", 5, "Vacuna", "dosis" },
                    { 2, true, "Vacuna antirrábica", 5, "Vacuna", "dosis" },
                    { 3, true, "Vacuna triple felina", 4, "Vacuna", "dosis" },
                    { 4, true, "Vacuna antitetánica equina", 3, "Vacuna", "dosis" },
                    { 5, true, "Vacuna clostridial", 20, "Vacuna", "dosis" },
                    { 6, true, "Antiparasitario interno", 5, "Antiparasitario", "dosis" },
                    { 7, true, "Antiparasitario externo", 5, "Antiparasitario", "dosis" },
                    { 8, true, "Jeringa descartable 3 ml", 20, "Insumo", "unidad" },
                    { 9, true, "Guantes de examen", 10, "Insumo", "par" },
                    { 10, true, "Gasa estéril", 10, "Insumo", "sobre" },
                    { 11, true, "Suero fisiológico 500 ml", 4, "Insumo", "unidad" },
                    { 12, true, "Venda elástica", 5, "Insumo", "unidad" }
                });

            migrationBuilder.InsertData(
                table: "Boxes",
                columns: new[] { "Id", "Activo", "Nombre", "Tipo" },
                values: new object[,]
                {
                    { 1, true, "Box 1", "Pequeños animales" },
                    { 2, true, "Box 2", "Pequeños animales" },
                    { 3, true, "Box 3", "Pequeños animales" },
                    { 4, true, "Box equino", "Grandes animales" }
                });

            migrationBuilder.InsertData(
                table: "Cuentas",
                columns: new[] { "Id", "Activa", "Apartamento", "Apellido", "Calle", "Correo", "CorreoNuevoPendiente", "Departamento", "Eliminada", "FechaBaja", "FechaCreacion", "FechaEliminacion", "HashContrasena", "Localidad", "Nombre", "NumeroDocumento", "NumeroPuerta", "Referencias", "Telefono", "TipoDocumento", "Verificada" },
                values: new object[,]
                {
                    { 1, true, "", "Benítez", "", "recepcion@vitalia.uy", "", "", false, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Vml0YWxpYVNlbWlsbGEwMQ==.rmvKwlUzU6PwWHiDTL50BHoRzzzOXikAepJ8q13h22k=", "", "Laura", "", "", "", "", "", true },
                    { 2, true, "", "Ferreira", "", "ferreira@vitalia.uy", "", "", false, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Vml0YWxpYVNlbWlsbGEwMQ==.rmvKwlUzU6PwWHiDTL50BHoRzzzOXikAepJ8q13h22k=", "", "Lucía", "", "", "", "", "", true },
                    { 3, true, "", "Silveira", "", "silveira@vitalia.uy", "", "", false, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Vml0YWxpYVNlbWlsbGEwMQ==.rmvKwlUzU6PwWHiDTL50BHoRzzzOXikAepJ8q13h22k=", "", "Martín", "", "", "", "", "", true },
                    { 4, true, "", "Acosta", "", "acosta@vitalia.uy", "", "", false, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Vml0YWxpYVNlbWlsbGEwMQ==.rmvKwlUzU6PwWHiDTL50BHoRzzzOXikAepJ8q13h22k=", "", "Valentina", "", "", "", "", "", true },
                    { 5, true, "", "Rivas", "", "admin@vitalia.uy", "", "", false, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Vml0YWxpYVNlbWlsbGEwMQ==.rmvKwlUzU6PwWHiDTL50BHoRzzzOXikAepJ8q13h22k=", "", "Gonzalo", "", "", "", "", "", true }
                });

            migrationBuilder.InsertData(
                table: "Numeradores",
                columns: new[] { "Clave", "Ultimo" },
                values: new object[,]
                {
                    { "Comprobante", 0 },
                    { "NotaCredito", 0 }
                });

            migrationBuilder.InsertData(
                table: "PoliticasConservacion",
                columns: new[] { "Id", "Acceso", "AlEliminar", "Categoria", "Conservacion", "Finalidad" },
                values: new object[,]
                {
                    { 1, "Vos, Recepción y el Administrador.", "Se reemplazan por valores neutros.", "Datos de la cuenta (nombre, contacto, documento, domicilio)", "Mientras la cuenta esté activa.", "Identificarte, contactarte y atender tus animales." },
                    { 2, "Vos (solo lectura), el personal clínico y el Administrador; toda lectura queda auditada.", "Se conserva, sin identificar al titular.", "Animales e historia clínica", "El plazo que fije la normativa veterinaria aplicable (a confirmar en la revisión legal previa a producción).", "Continuidad de la atención veterinaria y obligaciones sanitarias." },
                    { 3, "Vos, el personal clínico y el Administrador.", "Se conservan, sin identificar al titular.", "Consentimientos informados", "Junto con la historia clínica.", "Constancia de tu decisión antes de un procedimiento." },
                    { 4, "Vos, Recepción y el Administrador.", "Se conservan, sin identificar al titular.", "Cargos, pagos y comprobantes", "El plazo que fijen las obligaciones contables y tributarias aplicables (a confirmar).", "Registro de lo cobrado en la clínica." },
                    { 5, "Vos, Recepción y el Administrador.", "Se borran.", "Comunicaciones enviadas y preferencias", "Mientras la cuenta esté activa.", "Entregarte resúmenes, avisos y comprobantes por el canal que elegiste." },
                    { 6, "El personal asignado.", "No aplica: no se conserva el seguimiento.", "Ubicación en una asistencia a domicilio", "Se deja de usar al terminar la asistencia.", "Despachar al profesional durante la asistencia (RNF-LEG-09)." },
                    { 7, "El Administrador.", "Se reemplazan junto con la cuenta.", "Aceptaciones de términos y privacidad", "Mientras la cuenta esté activa.", "Probar qué versión aceptaste y cuándo (RN-43)." }
                });

            migrationBuilder.UpdateData(
                table: "Prestaciones",
                keyColumn: "Id",
                keyValue: 1,
                column: "RequiereConsentimiento",
                value: false);

            migrationBuilder.UpdateData(
                table: "Prestaciones",
                keyColumn: "Id",
                keyValue: 2,
                column: "RequiereConsentimiento",
                value: false);

            migrationBuilder.UpdateData(
                table: "Prestaciones",
                keyColumn: "Id",
                keyValue: 3,
                column: "RequiereConsentimiento",
                value: false);

            migrationBuilder.UpdateData(
                table: "Prestaciones",
                keyColumn: "Id",
                keyValue: 4,
                column: "RequiereConsentimiento",
                value: false);

            migrationBuilder.UpdateData(
                table: "Prestaciones",
                keyColumn: "Id",
                keyValue: 5,
                column: "RequiereConsentimiento",
                value: false);

            migrationBuilder.UpdateData(
                table: "Prestaciones",
                keyColumn: "Id",
                keyValue: 6,
                column: "RequiereConsentimiento",
                value: false);

            migrationBuilder.UpdateData(
                table: "Prestaciones",
                keyColumn: "Id",
                keyValue: 7,
                column: "RequiereConsentimiento",
                value: false);

            migrationBuilder.InsertData(
                table: "Prestaciones",
                columns: new[] { "Id", "Descripcion", "DuracionMinutos", "Nombre", "PrecioDesde", "Publicado", "RequiereConsentimiento", "ReservableEnLinea" },
                values: new object[] { 8, "Cirugía programada con anestesia general. Requiere firmar el consentimiento informado antes del día del turno.", 90, "Castración", 4500m, true, true, true });

            migrationBuilder.UpdateData(
                table: "Profesionales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CuentaId", "Matricula", "MatriculaVigenteHasta" },
                values: new object[] { 2, "VET-4521", new DateTime(2027, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Profesionales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CuentaId", "Matricula", "MatriculaVigenteHasta" },
                values: new object[] { 3, "VET-3378", new DateTime(2027, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Profesionales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CuentaId", "Matricula", "MatriculaVigenteHasta" },
                values: new object[] { 4, "VET-5102", new DateTime(2027, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Profesionales",
                keyColumn: "Id",
                keyValue: 4,
                column: "MatriculaVigenteHasta",
                value: null);

            migrationBuilder.UpdateData(
                table: "Profesionales",
                keyColumn: "Id",
                keyValue: 5,
                column: "MatriculaVigenteHasta",
                value: null);

            migrationBuilder.InsertData(
                table: "TarifasPrestacion",
                columns: new[] { "Id", "Precio", "PrestacionId", "RegistradoPorId", "VigenteDesde" },
                values: new object[,]
                {
                    { 1, 1200m, 1, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 600m, 2, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 450m, 3, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, 1500m, 4, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, 2500m, 5, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, 900m, 6, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 7, 700m, 7, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "TiposComunicacion",
                columns: new[] { "Id", "CanalPorDefecto", "Descripcion", "Esencial", "Nombre" },
                values: new object[,]
                {
                    { 1, "Correo", "Resumen de la consulta cuando el veterinario la firma (RF-CLI-10).", true, "Resumen clínico" },
                    { 2, "Correo", "Comprobante de un cobro, cuando Recepción lo envía.", true, "Comprobante de cobro" },
                    { 3, "Correo", "Cambios que afectan tu atención: cierres, horarios, feriados.", true, "Aviso de la clínica" },
                    { 4, "Correo", "Vacunas y desparasitaciones próximas a vencer de tus animales (RF-CLI-07).", false, "Aviso de refuerzo" },
                    { 5, "No recibir", "Alertas de la clínica para animales de tu misma especie, como un brote en la zona.", false, "Alerta sanitaria" }
                });

            migrationBuilder.InsertData(
                table: "CuentaRoles",
                columns: new[] { "CuentaId", "RolId" },
                values: new object[,]
                {
                    { 1, 2 },
                    { 2, 3 },
                    { 3, 3 },
                    { 4, 3 },
                    { 5, 4 }
                });

            migrationBuilder.InsertData(
                table: "DocumentosConsentimiento",
                columns: new[] { "Id", "FechaVigencia", "PrestacionId", "Texto", "Titulo", "Version", "Vigente" },
                values: new object[] { 1, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Autorizo al equipo veterinario de VITALIA a realizar la castración del animal indicado, bajo anestesia general. Me explicaron en qué consiste el procedimiento, los cuidados previos (ayuno de 8 horas de comida y 2 de agua) y los posteriores. Entiendo que toda anestesia y cirugía tienen riesgos, poco frecuentes pero posibles: reacciones a los fármacos, sangrado, infección de la herida y, excepcionalmente, complicaciones graves o la muerte. Autorizo al veterinario a tomar las medidas necesarias si surge una complicación durante la cirugía y me comprometo a informar cualquier enfermedad, medicación o alergia del animal. Puedo hacer preguntas antes de firmar y retirar este consentimiento avisando a la clínica antes del procedimiento.", "Consentimiento informado para castración con anestesia general", "1.0", true });

            migrationBuilder.InsertData(
                table: "Lotes",
                columns: new[] { "Id", "ArticuloId", "Cantidad", "FechaIngreso", "Inhabilitado", "Numero", "Vencimiento" },
                values: new object[,]
                {
                    { 1, 1, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L1-A", new DateTime(2026, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 1, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L1-B", new DateTime(2027, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 2, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L2-A", new DateTime(2026, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, 2, 1, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L2-B", new DateTime(2027, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, 3, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L3-A", new DateTime(2026, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, 3, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L3-B", new DateTime(2027, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 7, 4, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L4-A", new DateTime(2026, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 8, 4, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L4-B", new DateTime(2027, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 9, 5, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L5-A", new DateTime(2026, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 10, 5, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L5-B", new DateTime(2027, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 11, 6, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L6-A", new DateTime(2026, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 12, 6, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L6-B", new DateTime(2027, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 13, 7, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L7-A", new DateTime(2026, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 14, 7, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "L7-B", new DateTime(2027, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 15, 8, 120, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "JER-2611", new DateTime(2029, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 16, 9, 40, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "GUA-2610", new DateTime(2028, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 17, 10, 8, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "GAS-2609", new DateTime(2026, 12, 15, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 18, 10, 30, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "GAS-2611", new DateTime(2027, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 19, 11, 6, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "SUE-2608", new DateTime(2027, 7, 31, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 20, 12, 15, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "VEN-2605", new DateTime(2028, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "ProductosSanitarios",
                columns: new[] { "Id", "ArticuloId", "IntervaloMinimoDias", "IntervaloRefuerzoDias", "Laboratorio", "Nombre", "Tipo" },
                values: new object[,]
                {
                    { 1, 1, 330, 365, "Laboratorio Demo Norte", "Vacuna polivalente canina", "Vacuna" },
                    { 2, 2, 330, 365, "Laboratorio Demo Sur", "Vacuna antirrábica", "Vacuna" },
                    { 3, 3, 330, 365, "Laboratorio Demo Norte", "Vacuna triple felina", "Vacuna" },
                    { 4, 4, 330, 365, "Laboratorio Demo Campo", "Vacuna antitetánica equina", "Vacuna" },
                    { 5, 5, 150, 180, "Laboratorio Demo Campo", "Vacuna clostridial", "Vacuna" },
                    { 6, 6, 75, 90, "Laboratorio Demo Sur", "Antiparasitario interno", "Antiparasitario" },
                    { 7, 7, 25, 30, "Laboratorio Demo Sur", "Antiparasitario externo", "Antiparasitario" }
                });

            migrationBuilder.InsertData(
                table: "ProfesionalPrestaciones",
                columns: new[] { "PrestacionId", "ProfesionalId" },
                values: new object[] { 8, 1 });

            migrationBuilder.InsertData(
                table: "TarifasPrestacion",
                columns: new[] { "Id", "Precio", "PrestacionId", "RegistradoPorId", "VigenteDesde" },
                values: new object[] { 8, 4500m, 8, null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "MovimientosStock",
                columns: new[] { "Id", "ArticuloId", "Cantidad", "Fecha", "LoteId", "Motivo", "Origen", "PersonaId", "Tipo" },
                values: new object[,]
                {
                    { 1L, 1, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 2L, 1, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 3L, 2, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 4L, 2, 1, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 5L, 3, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 6L, 3, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 7L, 4, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 8L, 4, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 9L, 5, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 9, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 10L, 5, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 10, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 11L, 6, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 11, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 12L, 6, 10, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 12, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 13L, 7, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 13, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 14L, 7, 3, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 14, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 15L, 8, 120, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 15, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 16L, 9, 40, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 16, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 17L, 10, 8, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 17, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 18L, 10, 30, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 18, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 19L, 11, 6, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 19, "Carga inicial de demostración", "Carga inicial", null, "Compra" },
                    { 20L, 12, 15, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 20, "Carga inicial de demostración", "Carga inicial", null, "Compra" }
                });

            migrationBuilder.InsertData(
                table: "ProductoSanitarioEspecies",
                columns: new[] { "EspecieId", "ProductoSanitarioId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 2, 2 },
                    { 2, 3 },
                    { 11, 4 },
                    { 7, 5 },
                    { 8, 5 },
                    { 9, 5 },
                    { 1, 6 },
                    { 2, 6 },
                    { 1, 7 },
                    { 2, 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_CreadoPorId",
                table: "Turnos",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasStock_ArticuloId",
                table: "AlertasStock",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasStock_Atendida_Fecha",
                table: "AlertasStock",
                columns: new[] { "Atendida", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesSanitarias_AnimalId_ProductoSanitarioId_FechaApl~",
                table: "AplicacionesSanitarias",
                columns: new[] { "AnimalId", "ProductoSanitarioId", "FechaAplicacion" });

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesSanitarias_AplicadaPorId",
                table: "AplicacionesSanitarias",
                column: "AplicadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesSanitarias_ConsultaId",
                table: "AplicacionesSanitarias",
                column: "ConsultaId");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesSanitarias_FechaRefuerzo",
                table: "AplicacionesSanitarias",
                column: "FechaRefuerzo");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesSanitarias_LoteId",
                table: "AplicacionesSanitarias",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesSanitarias_ProductoSanitarioId",
                table: "AplicacionesSanitarias",
                column: "ProductoSanitarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_Nombre",
                table: "Articulos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_Nombre",
                table: "Boxes",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_AnimalId",
                table: "Cargos",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_ClienteId_Fecha",
                table: "Cargos",
                columns: new[] { "ClienteId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_ConsultaId",
                table: "Cargos",
                column: "ConsultaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_PrestacionId",
                table: "Cargos",
                column: "PrestacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_SolicitudAsistenciaId",
                table: "Cargos",
                column: "SolicitudAsistenciaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_TurnoId",
                table: "Cargos",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comunicaciones_CuentaId_TipoComunicacionId_Clave",
                table: "Comunicaciones",
                columns: new[] { "CuentaId", "TipoComunicacionId", "Clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comunicaciones_Estado_FechaCreacion",
                table: "Comunicaciones",
                columns: new[] { "Estado", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_Comunicaciones_GeneradaPorId",
                table: "Comunicaciones",
                column: "GeneradaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comunicaciones_ReenvioDeId",
                table: "Comunicaciones",
                column: "ReenvioDeId");

            migrationBuilder.CreateIndex(
                name: "IX_Comunicaciones_TipoComunicacionId",
                table: "Comunicaciones",
                column: "TipoComunicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Consentimientos_AnimalId",
                table: "Consentimientos",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Consentimientos_ClienteId",
                table: "Consentimientos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Consentimientos_DocumentoConsentimientoId",
                table: "Consentimientos",
                column: "DocumentoConsentimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Consentimientos_PrestacionId",
                table: "Consentimientos",
                column: "PrestacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Consentimientos_TurnoId_Fecha",
                table: "Consentimientos",
                columns: new[] { "TurnoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_AnimalId_FechaInicio",
                table: "Consultas",
                columns: new[] { "AnimalId", "FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_AutorId",
                table: "Consultas",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_ProfesionalId",
                table: "Consultas",
                column: "ProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_TurnoId",
                table: "Consultas",
                column: "TurnoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosConsentimiento_PrestacionId_Version",
                table: "DocumentosConsentimiento",
                columns: new[] { "PrestacionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enmiendas_AutorId",
                table: "Enmiendas",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Enmiendas_ConsultaId",
                table: "Enmiendas",
                column: "ConsultaId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAsistencia_AutorId",
                table: "EventosAsistencia",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAsistencia_SolicitudAsistenciaId",
                table: "EventosAsistencia",
                column: "SolicitudAsistenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Internaciones_AdmitidoPorId",
                table: "Internaciones",
                column: "AdmitidoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Internaciones_AnimalId_Estado",
                table: "Internaciones",
                columns: new[] { "AnimalId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Internaciones_BoxId_Estado",
                table: "Internaciones",
                columns: new[] { "BoxId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Internaciones_ClienteId",
                table: "Internaciones",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Internaciones_VeterinarioAltaId",
                table: "Internaciones",
                column: "VeterinarioAltaId");

            migrationBuilder.CreateIndex(
                name: "IX_Internaciones_VeterinarioIngresoId",
                table: "Internaciones",
                column: "VeterinarioIngresoId");

            migrationBuilder.CreateIndex(
                name: "IX_LecturasHistoria_AnimalId_Fecha",
                table: "LecturasHistoria",
                columns: new[] { "AnimalId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_LecturasHistoria_CuentaId",
                table: "LecturasHistoria",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_ArticuloId_Inhabilitado_Vencimiento",
                table: "Lotes",
                columns: new[] { "ArticuloId", "Inhabilitado", "Vencimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_ArticuloId_Numero",
                table: "Lotes",
                columns: new[] { "ArticuloId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ArticuloId_Fecha",
                table: "MovimientosStock",
                columns: new[] { "ArticuloId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_LoteId",
                table: "MovimientosStock",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_PersonaId",
                table: "MovimientosStock",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_EmitidaPorId",
                table: "NotasCredito",
                column: "EmitidaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_Numero",
                table: "NotasCredito",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_PagoId",
                table: "NotasCredito",
                column: "PagoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NovedadesInternacion_AutorId",
                table: "NovedadesInternacion",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_NovedadesInternacion_InternacionId",
                table: "NovedadesInternacion",
                column: "InternacionId");

            migrationBuilder.CreateIndex(
                name: "IX_NovedadesInternacion_RectificaId",
                table: "NovedadesInternacion",
                column: "RectificaId");

            migrationBuilder.CreateIndex(
                name: "IX_OfertasAsistencia_VeterinarioId",
                table: "OfertasAsistencia",
                column: "VeterinarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_CargoId",
                table: "Pagos",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_CobradoPorId",
                table: "Pagos",
                column: "CobradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_Numero",
                table: "Pagos",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasComunicacion_TipoComunicacionId",
                table: "PreferenciasComunicacion",
                column: "TipoComunicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoSanitarioEspecies_EspecieId",
                table: "ProductoSanitarioEspecies",
                column: "EspecieId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosSanitarios_ArticuloId",
                table: "ProductosSanitarios",
                column: "ArticuloId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservasStock_AplicacionSanitariaId",
                table: "ReservasStock",
                column: "AplicacionSanitariaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasStock_ArticuloId",
                table: "ReservasStock",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasStock_ConsultaId",
                table: "ReservasStock",
                column: "ConsultaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasStock_LoteId_Confirmada",
                table: "ReservasStock",
                columns: new[] { "LoteId", "Confirmada" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesAsistencia_AnimalId",
                table: "SolicitudesAsistencia",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesAsistencia_ClienteId_ClaveIdempotencia",
                table: "SolicitudesAsistencia",
                columns: new[] { "ClienteId", "ClaveIdempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesAsistencia_Estado_Nivel_FechaCreacion",
                table: "SolicitudesAsistencia",
                columns: new[] { "Estado", "Nivel", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesAsistencia_VeterinarioId",
                table: "SolicitudesAsistencia",
                column: "VeterinarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesDerecho_CuentaId_Estado",
                table: "SolicitudesDerecho",
                columns: new[] { "CuentaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesDerecho_ResueltaPorId",
                table: "SolicitudesDerecho",
                column: "ResueltaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_TarifasPrestacion_PrestacionId_VigenteDesde",
                table: "TarifasPrestacion",
                columns: new[] { "PrestacionId", "VigenteDesde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TarifasPrestacion_RegistradoPorId",
                table: "TarifasPrestacion",
                column: "RegistradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposComunicacion_Nombre",
                table: "TiposComunicacion",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Cuentas_CreadoPorId",
                table: "Turnos",
                column: "CreadoPorId",
                principalTable: "Cuentas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Cuentas_CreadoPorId",
                table: "Turnos");

            migrationBuilder.DropTable(
                name: "AlertasStock");

            migrationBuilder.DropTable(
                name: "Comunicaciones");

            migrationBuilder.DropTable(
                name: "Consentimientos");

            migrationBuilder.DropTable(
                name: "Enmiendas");

            migrationBuilder.DropTable(
                name: "EventosAsistencia");

            migrationBuilder.DropTable(
                name: "LecturasHistoria");

            migrationBuilder.DropTable(
                name: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "NotasCredito");

            migrationBuilder.DropTable(
                name: "NovedadesInternacion");

            migrationBuilder.DropTable(
                name: "Numeradores");

            migrationBuilder.DropTable(
                name: "OfertasAsistencia");

            migrationBuilder.DropTable(
                name: "PoliticasConservacion");

            migrationBuilder.DropTable(
                name: "PreferenciasComunicacion");

            migrationBuilder.DropTable(
                name: "ProductoSanitarioEspecies");

            migrationBuilder.DropTable(
                name: "ReservasStock");

            migrationBuilder.DropTable(
                name: "SolicitudesDerecho");

            migrationBuilder.DropTable(
                name: "TarifasPrestacion");

            migrationBuilder.DropTable(
                name: "DocumentosConsentimiento");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Internaciones");

            migrationBuilder.DropTable(
                name: "TiposComunicacion");

            migrationBuilder.DropTable(
                name: "AplicacionesSanitarias");

            migrationBuilder.DropTable(
                name: "Cargos");

            migrationBuilder.DropTable(
                name: "Boxes");

            migrationBuilder.DropTable(
                name: "Lotes");

            migrationBuilder.DropTable(
                name: "ProductosSanitarios");

            migrationBuilder.DropTable(
                name: "Consultas");

            migrationBuilder.DropTable(
                name: "SolicitudesAsistencia");

            migrationBuilder.DropTable(
                name: "Articulos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_CreadoPorId",
                table: "Turnos");

            migrationBuilder.DeleteData(
                table: "CuentaRoles",
                keyColumns: new[] { "CuentaId", "RolId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "CuentaRoles",
                keyColumns: new[] { "CuentaId", "RolId" },
                keyValues: new object[] { 2, 3 });

            migrationBuilder.DeleteData(
                table: "CuentaRoles",
                keyColumns: new[] { "CuentaId", "RolId" },
                keyValues: new object[] { 3, 3 });

            migrationBuilder.DeleteData(
                table: "CuentaRoles",
                keyColumns: new[] { "CuentaId", "RolId" },
                keyValues: new object[] { 4, 3 });

            migrationBuilder.DeleteData(
                table: "CuentaRoles",
                keyColumns: new[] { "CuentaId", "RolId" },
                keyValues: new object[] { 5, 4 });

            migrationBuilder.DeleteData(
                table: "ProfesionalPrestaciones",
                keyColumns: new[] { "PrestacionId", "ProfesionalId" },
                keyValues: new object[] { 8, 1 });

            migrationBuilder.DeleteData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Prestaciones",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "CanceladoPor",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "EsSobreturno",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "HoraLlegada",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "MotivoSobreturno",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "MatriculaVigenteHasta",
                table: "Profesionales");

            migrationBuilder.DropColumn(
                name: "RequiereConsentimiento",
                table: "Prestaciones");

            migrationBuilder.DropColumn(
                name: "CorreoNuevoPendiente",
                table: "Cuentas");

            migrationBuilder.DropColumn(
                name: "Eliminada",
                table: "Cuentas");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "Cuentas");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Auditorias");

            migrationBuilder.UpdateData(
                table: "Profesionales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CuentaId", "Matricula" },
                values: new object[] { null, "" });

            migrationBuilder.UpdateData(
                table: "Profesionales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CuentaId", "Matricula" },
                values: new object[] { null, "" });

            migrationBuilder.UpdateData(
                table: "Profesionales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CuentaId", "Matricula" },
                values: new object[] { null, "" });
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HealthAuthorizations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialEsquemaYCatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "AutorizacionesHiLo",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "Contratantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Documento = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModificadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intermediarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModificadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intermediarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposPrestadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModificadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposPrestadores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposServicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModificadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposServicios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Afiliados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroAfiliado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Documento = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    ContratanteId = table.Column<int>(type: "int", nullable: false),
                    IntermediarioId = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModificadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Afiliados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Afiliados_Contratantes_ContratanteId",
                        column: x => x.ContratanteId,
                        principalTable: "Contratantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Afiliados_Intermediarios_IntermediarioId",
                        column: x => x.IntermediarioId,
                        principalTable: "Intermediarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Prestadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Documento = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    TipoPrestadorId = table.Column<int>(type: "int", nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModificadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prestadores_TiposPrestadores_TipoPrestadorId",
                        column: x => x.TipoPrestadorId,
                        principalTable: "TiposPrestadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Autorizaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Numero = table.Column<string>(type: "varchar(14)", unicode: false, maxLength: 14, nullable: false, computedColumnSql: "'AUT-' + RIGHT('0000000000' + CAST([Id] AS varchar(19)), 10)", stored: true),
                    AfiliadoId = table.Column<int>(type: "int", nullable: false),
                    PrestadorId = table.Column<int>(type: "int", nullable: false),
                    ContratanteId = table.Column<int>(type: "int", nullable: false),
                    IntermediarioId = table.Column<int>(type: "int", nullable: true),
                    FechaServicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Diagnostico = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Estado = table.Column<byte>(type: "tinyint", nullable: false),
                    FechaInactivacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoInactivacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MontoReclamado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoArs = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoAfiliado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClaveIdempotencia = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModificadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Autorizaciones", x => x.Id);
                    table.CheckConstraint("CK_Autorizaciones_Estado", "[Estado] IN (1, 2)");
                    table.CheckConstraint("CK_Autorizaciones_Inactivacion", "([Estado] = 1 AND [FechaInactivacion] IS NULL AND [MotivoInactivacion] IS NULL) OR ([Estado] = 2 AND [FechaInactivacion] IS NOT NULL AND [MotivoInactivacion] IS NOT NULL)");
                    table.CheckConstraint("CK_Autorizaciones_Montos", "[MontoReclamado] > 0 AND [MontoArs] >= 0 AND [MontoAfiliado] >= 0 AND [MontoArs] + [MontoAfiliado] = [MontoReclamado]");
                    table.ForeignKey(
                        name: "FK_Autorizaciones_Afiliados_AfiliadoId",
                        column: x => x.AfiliadoId,
                        principalTable: "Afiliados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizaciones_Contratantes_ContratanteId",
                        column: x => x.ContratanteId,
                        principalTable: "Contratantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizaciones_Intermediarios_IntermediarioId",
                        column: x => x.IntermediarioId,
                        principalTable: "Intermediarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizaciones_Prestadores_PrestadorId",
                        column: x => x.PrestadorId,
                        principalTable: "Prestadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutorizacionesDetalle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutorizacionId = table.Column<long>(type: "bigint", nullable: false),
                    TipoServicioId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MontoReclamado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PorcentajeCobertura = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MontoArs = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoAfiliado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutorizacionesDetalle", x => x.Id);
                    table.CheckConstraint("CK_AutorizacionesDetalle_Montos", "[MontoReclamado] > 0 AND [MontoArs] >= 0 AND [MontoAfiliado] >= 0 AND [MontoArs] + [MontoAfiliado] = [MontoReclamado]");
                    table.CheckConstraint("CK_AutorizacionesDetalle_Porcentaje", "[PorcentajeCobertura] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_AutorizacionesDetalle_Autorizaciones_AutorizacionId",
                        column: x => x.AutorizacionId,
                        principalTable: "Autorizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutorizacionesDetalle_TiposServicios_TipoServicioId",
                        column: x => x.TipoServicioId,
                        principalTable: "TiposServicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Contratantes",
                columns: new[] { "Id", "Activo", "Codigo", "CreadoEn", "CreadoPor", "Documento", "ModificadoEn", "ModificadoPor", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "CTR-001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "130000001", null, null, "Constructora Los Andes SRL" },
                    { 2, true, "CTR-002", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "130000002", null, null, "Banco Comercial del Este SA" },
                    { 3, true, "CTR-003", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "130000003", null, null, "Distribuidora Alimentos del Norte SRL" },
                    { 4, true, "CTR-004", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "00100000099", null, null, "Plan Individual" }
                });

            migrationBuilder.InsertData(
                table: "Intermediarios",
                columns: new[] { "Id", "Activo", "Codigo", "CreadoEn", "CreadoPor", "ModificadoEn", "ModificadoPor", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "INT-001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Corredora de Seguros del Caribe" },
                    { 2, true, "INT-002", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Asesores de Salud Quisqueya" },
                    { 3, true, "INT-003", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Agentes Unidos de Seguros" }
                });

            migrationBuilder.InsertData(
                table: "TiposPrestadores",
                columns: new[] { "Id", "Activo", "Codigo", "CreadoEn", "CreadoPor", "ModificadoEn", "ModificadoPor", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "MED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Médico" },
                    { 2, true, "FAR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Farmacia" },
                    { 3, true, "CLI", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Clínica" },
                    { 4, true, "LAB", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Laboratorio" }
                });

            migrationBuilder.InsertData(
                table: "TiposServicios",
                columns: new[] { "Id", "Activo", "Codigo", "CreadoEn", "CreadoPor", "ModificadoEn", "ModificadoPor", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "CON", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Consulta" },
                    { 2, true, "EME", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Emergencia" },
                    { 3, true, "HOS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Hospitalización" },
                    { 4, true, "LAB", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Laboratorio" },
                    { 5, true, "IMG", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", null, null, "Imágenes" }
                });

            migrationBuilder.InsertData(
                table: "Afiliados",
                columns: new[] { "Id", "Activo", "Apellidos", "ContratanteId", "CreadoEn", "CreadoPor", "Documento", "FechaNacimiento", "IntermediarioId", "ModificadoEn", "ModificadoPor", "Nombres", "NumeroAfiliado" },
                values: new object[,]
                {
                    { 1, true, "Rodríguez Peña", 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000001", new DateOnly(1985, 3, 14), 1, null, null, "María", "AF-000001" },
                    { 2, true, "Fernández Díaz", 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000002", new DateOnly(1979, 7, 2), 1, null, null, "José", "AF-000002" },
                    { 3, true, "Santos Reyes", 2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000003", new DateOnly(1992, 11, 23), 2, null, null, "Ana", "AF-000003" },
                    { 4, true, "Castillo Mejía", 2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000004", new DateOnly(1968, 1, 30), 2, null, null, "Luis", "AF-000004" },
                    { 5, true, "Jiménez Torres", 2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000005", new DateOnly(2001, 5, 9), null, null, null, "Carmen", "AF-000005" },
                    { 6, true, "Guzmán Núñez", 3, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000006", new DateOnly(1975, 9, 17), 3, null, null, "Pedro", "AF-000006" },
                    { 7, true, "Vargas Cruz", 3, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000007", new DateOnly(1988, 12, 5), 3, null, null, "Rosa", "AF-000007" },
                    { 8, true, "Herrera Polanco", 3, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000008", new DateOnly(1995, 4, 21), null, null, null, "Miguel", "AF-000008" },
                    { 9, true, "Batista Familia", 4, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000009", new DateOnly(1960, 8, 11), 1, null, null, "Elena", "AF-000009" },
                    { 10, true, "De la Cruz Ortiz", 4, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "40200000010", new DateOnly(2010, 2, 28), null, null, null, "Ramón", "AF-000010" }
                });

            migrationBuilder.InsertData(
                table: "Prestadores",
                columns: new[] { "Id", "Activo", "Codigo", "CreadoEn", "CreadoPor", "Documento", "ModificadoEn", "ModificadoPor", "Nombre", "TipoPrestadorId" },
                values: new object[,]
                {
                    { 1, true, "PRE-001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "00100000101", null, null, "Dr. Carlos Martínez - Medicina Interna", 1 },
                    { 2, true, "PRE-002", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "00100000102", null, null, "Dra. Laura Gómez - Pediatría", 1 },
                    { 3, true, "PRE-003", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "130000101", null, null, "Farmacia Salud Total", 2 },
                    { 4, true, "PRE-004", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "130000102", null, null, "Farmacia del Pueblo", 2 },
                    { 5, true, "PRE-005", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "130000103", null, null, "Clínica Central", 3 },
                    { 6, true, "PRE-006", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "130000104", null, null, "Centro Médico del Valle", 3 },
                    { 7, true, "PRE-007", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "130000105", null, null, "Laboratorio Clínico Referencia", 4 },
                    { 8, true, "PRE-008", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed", "130000106", null, null, "Laboratorio e Imágenes Diagnósticas", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_ContratanteId",
                table: "Afiliados",
                column: "ContratanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_Documento",
                table: "Afiliados",
                column: "Documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_IntermediarioId",
                table: "Afiliados",
                column: "IntermediarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_NumeroAfiliado",
                table: "Afiliados",
                column: "NumeroAfiliado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_AfiliadoId",
                table: "Autorizaciones",
                column: "AfiliadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_ClaveIdempotencia",
                table: "Autorizaciones",
                column: "ClaveIdempotencia",
                unique: true,
                filter: "[ClaveIdempotencia] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_ContratanteId",
                table: "Autorizaciones",
                column: "ContratanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_FechaServicio",
                table: "Autorizaciones",
                column: "FechaServicio");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_IntermediarioId",
                table: "Autorizaciones",
                column: "IntermediarioId",
                filter: "[IntermediarioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_Numero",
                table: "Autorizaciones",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_PrestadorId",
                table: "Autorizaciones",
                column: "PrestadorId");

            migrationBuilder.CreateIndex(
                name: "IX_AutorizacionesDetalle_AutorizacionId",
                table: "AutorizacionesDetalle",
                column: "AutorizacionId");

            migrationBuilder.CreateIndex(
                name: "IX_AutorizacionesDetalle_TipoServicioId",
                table: "AutorizacionesDetalle",
                column: "TipoServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratantes_Codigo",
                table: "Contratantes",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contratantes_Documento",
                table: "Contratantes",
                column: "Documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Intermediarios_Codigo",
                table: "Intermediarios",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestadores_Codigo",
                table: "Prestadores",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestadores_Documento",
                table: "Prestadores",
                column: "Documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestadores_TipoPrestadorId",
                table: "Prestadores",
                column: "TipoPrestadorId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposPrestadores_Codigo",
                table: "TiposPrestadores",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposServicios_Codigo",
                table: "TiposServicios",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutorizacionesDetalle");

            migrationBuilder.DropTable(
                name: "Autorizaciones");

            migrationBuilder.DropTable(
                name: "TiposServicios");

            migrationBuilder.DropTable(
                name: "Afiliados");

            migrationBuilder.DropTable(
                name: "Prestadores");

            migrationBuilder.DropTable(
                name: "Contratantes");

            migrationBuilder.DropTable(
                name: "Intermediarios");

            migrationBuilder.DropTable(
                name: "TiposPrestadores");

            migrationBuilder.DropSequence(
                name: "AutorizacionesHiLo");
        }
    }
}

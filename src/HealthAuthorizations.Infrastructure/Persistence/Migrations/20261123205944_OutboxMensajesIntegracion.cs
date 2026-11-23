using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthAuthorizations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxMensajesIntegracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OcurridoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TraceParent = table.Column<string>(type: "varchar(55)", unicode: false, maxLength: 55, nullable: true),
                    ProcesadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Intentos = table.Column<int>(type: "int", nullable: false),
                    UltimoError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BloqueadoHasta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Pendientes",
                table: "OutboxMessages",
                column: "OcurridoEn",
                filter: "[ProcesadoEn] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}

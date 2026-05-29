using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class CriarPartidaEstatistica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartidaEstatisticas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartidaId = table.Column<int>(type: "integer", nullable: false),
                    AcsScore = table.Column<int>(type: "integer", nullable: false),
                    EconScore = table.Column<int>(type: "integer", nullable: false),
                    RoundasGanhas = table.Column<int>(type: "integer", nullable: false),
                    RoundasPerdidas = table.Column<int>(type: "integer", nullable: false),
                    HeadshotPercentual = table.Column<double>(type: "double precision", nullable: false),
                    BodyShotCount = table.Column<int>(type: "integer", nullable: false),
                    LegShotCount = table.Column<int>(type: "integer", nullable: false),
                    ArmaFavorita = table.Column<string>(type: "text", nullable: false),
                    DanosCausados = table.Column<int>(type: "integer", nullable: false),
                    DanosRecebidos = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidaEstatisticas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartidaEstatisticas_Partidas_PartidaId",
                        column: x => x.PartidaId,
                        principalTable: "Partidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartidaEstatisticas_PartidaId",
                table: "PartidaEstatisticas",
                column: "PartidaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartidaEstatisticas");
        }
    }
}

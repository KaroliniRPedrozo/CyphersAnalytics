using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRelacionamento1N_UsuarioPartida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partidas_UsuarioId",
                table: "Partidas",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Partidas_Usuarios_UsuarioId",
                table: "Partidas",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partidas_Usuarios_UsuarioId",
                table: "Partidas");

            migrationBuilder.DropIndex(
                name: "IX_Partidas_UsuarioId",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Partidas");
        }
    }
}

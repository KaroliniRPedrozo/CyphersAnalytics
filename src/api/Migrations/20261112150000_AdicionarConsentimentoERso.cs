using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using api.database;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261112150000_AdicionarConsentimentoERso")]
    public partial class AdicionarConsentimentoERso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentimentoEm", table: "Usuarios",
                type: "timestamp with time zone", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermosVersao", table: "Usuarios", type: "text", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RsoPuuid", table: "Usuarios", type: "text", nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RsoVerificadoEm", table: "Usuarios",
                type: "timestamp with time zone", nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RsoPuuid", table: "Usuarios", column: "RsoPuuid",
                unique: true, filter: "\"RsoPuuid\" IS NOT NULL");

            // Remove o usuario de exemplo (professor@ufsc.br) criado por uma migration anterior:
            // dados de teste nao devem existir em um banco de producao.
            migrationBuilder.Sql("DELETE FROM \"Usuarios\" WHERE \"Id\" = 9999 AND \"Email\" = 'professor@ufsc.br';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Usuarios_RsoPuuid", table: "Usuarios");
            migrationBuilder.DropColumn(name: "ConsentimentoEm", table: "Usuarios");
            migrationBuilder.DropColumn(name: "TermosVersao", table: "Usuarios");
            migrationBuilder.DropColumn(name: "RsoPuuid", table: "Usuarios");
            migrationBuilder.DropColumn(name: "RsoVerificadoEm", table: "Usuarios");
        }
    }
}

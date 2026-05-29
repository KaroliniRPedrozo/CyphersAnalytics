using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    public partial class AdicionarDadosDeExemplo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insere um Usuário de exemplo no banco para a avaliação do professor
            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Email", "SenhaHash", "GameName", "TagLine", "CriadoEm" },
                values: new object[] { 9999, "professor@ufsc.br", "$2a$11$D7L/w/6C6P/w4z6G1B9.WeO.j6H/xR1m5sK9M4m5sK9M4m5sK9M4m", "Professor", "UFSC", DateTime.UtcNow }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove o usuário de exemplo caso a migration seja revertida
            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 9999
            );
        }
    }
}
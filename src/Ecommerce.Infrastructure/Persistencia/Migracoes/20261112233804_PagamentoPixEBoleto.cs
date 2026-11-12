using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class PagamentoPixEBoleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoPix",
                table: "VND_Pagamentos",
                type: "NVARCHAR2(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiraEmUtc",
                table: "VND_Pagamentos",
                type: "TIMESTAMP(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinhaDigitavel",
                table: "VND_Pagamentos",
                type: "NVARCHAR2(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metodo",
                table: "VND_Pagamentos",
                type: "NVARCHAR2(20)",
                maxLength: 20,
                nullable: false,
                // Não "" (no Oracle string vazia é NULL e a coluna é NOT NULL). As linhas antigas são
                // do Checkout Pro, sem método registrado; "Pix" só preenche, não muda nenhum fluxo.
                defaultValue: "Pix");

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "IDN_Usuarios",
                type: "NVARCHAR2(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IDN_Usuarios_Cpf",
                table: "IDN_Usuarios",
                column: "Cpf",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IDN_Usuarios_Cpf",
                table: "IDN_Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoPix",
                table: "VND_Pagamentos");

            migrationBuilder.DropColumn(
                name: "ExpiraEmUtc",
                table: "VND_Pagamentos");

            migrationBuilder.DropColumn(
                name: "LinhaDigitavel",
                table: "VND_Pagamentos");

            migrationBuilder.DropColumn(
                name: "Metodo",
                table: "VND_Pagamentos");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "IDN_Usuarios");
        }
    }
}

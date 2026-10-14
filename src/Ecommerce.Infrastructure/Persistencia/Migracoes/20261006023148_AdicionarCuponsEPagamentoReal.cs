using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarCuponsEPagamentoReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoCupom",
                table: "VND_Pedidos",
                type: "NVARCHAR2(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Desconto",
                table: "VND_Pedidos",
                type: "DECIMAL(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IdTransacao",
                table: "VND_Pagamentos",
                type: "NVARCHAR2(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrlPagamento",
                table: "VND_Pagamentos",
                type: "NVARCHAR2(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                table: "CAT_Produtos",
                type: "NCLOB",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldMaxLength: 4000);

            migrationBuilder.CreateTable(
                name: "VND_Cupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Codigo = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    Tipo = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorMinimoDoPedido = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidoAteUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LimiteDeUsos = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    QuantidadeDeUsos = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Ativo = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VND_Cupons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VND_Cupons_Codigo",
                table: "VND_Cupons",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VND_Cupons");

            migrationBuilder.DropColumn(
                name: "CodigoCupom",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "Desconto",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "IdTransacao",
                table: "VND_Pagamentos");

            migrationBuilder.DropColumn(
                name: "UrlPagamento",
                table: "VND_Pagamentos");

            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                table: "CAT_Produtos",
                type: "NCLOB",
                maxLength: 4000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}

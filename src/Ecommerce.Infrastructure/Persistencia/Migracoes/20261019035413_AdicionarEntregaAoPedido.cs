using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarEntregaAoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntregaBairro",
                table: "VND_Pedidos",
                type: "NVARCHAR2(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaCep",
                table: "VND_Pedidos",
                type: "NVARCHAR2(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaCidade",
                table: "VND_Pedidos",
                type: "NVARCHAR2(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaComplemento",
                table: "VND_Pedidos",
                type: "NVARCHAR2(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaEstado",
                table: "VND_Pedidos",
                type: "NVARCHAR2(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaLogradouro",
                table: "VND_Pedidos",
                type: "NVARCHAR2(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaNumero",
                table: "VND_Pedidos",
                type: "NVARCHAR2(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetodoDeEnvio",
                table: "VND_Pedidos",
                type: "NVARCHAR2(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrazoEntregaDias",
                table: "VND_Pedidos",
                type: "NUMBER(10)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorFrete",
                table: "VND_Pedidos",
                type: "DECIMAL(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntregaBairro",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaCep",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaCidade",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaComplemento",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaEstado",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaLogradouro",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaNumero",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "MetodoDeEnvio",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "PrazoEntregaDias",
                table: "VND_Pedidos");

            migrationBuilder.DropColumn(
                name: "ValorFrete",
                table: "VND_Pedidos");
        }
    }
}

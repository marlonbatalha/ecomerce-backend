using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class OtimizarIndicesEBuscaDeProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VND_Pedidos_UsuarioId",
                table: "VND_Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_EST_AjustesDeEstoque_VarianteDeProdutoId",
                table: "EST_AjustesDeEstoque");

            migrationBuilder.DropIndex(
                name: "IX_CAT_VariantesDeProduto_ProdutoId",
                table: "CAT_VariantesDeProduto");

            migrationBuilder.DropIndex(
                name: "IX_CAT_Produtos_CategoriaId",
                table: "CAT_Produtos");

            // Editado à mão: o gerado era NOT NULL com default '' — no Oracle '' é NULL, então
            // falharia em tabela com linhas. Cria nullable, preenche as linhas existentes com a
            // mesma normalização de NormalizadorDeTexto.ParaBusca (maiúsculo, sem acento,
            // espaços colapsados) e só então torna obrigatória.
            migrationBuilder.AddColumn<string>(
                name: "NomeParaBusca",
                table: "CAT_Produtos",
                type: "NVARCHAR2(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "CAT_Produtos"
                SET "NomeParaBusca" = REGEXP_REPLACE(
                    TRIM(UPPER(TRANSLATE("Nome",
                        N'áàâãäéèêëíìîïóòôõöúùûüçñÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇÑ',
                        N'aaaaaeeeeiiiiooooouuuucnAAAAAEEEEIIIIOOOOOUUUUCN'))),
                    '[[:space:]]+', ' ')
                """);

            migrationBuilder.AlterColumn<string>(
                name: "NomeParaBusca",
                table: "CAT_Produtos",
                type: "NVARCHAR2(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VND_Pedidos_CriadoEmUtc",
                table: "VND_Pedidos",
                column: "CriadoEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VND_Pedidos_Status_CriadoEmUtc",
                table: "VND_Pedidos",
                columns: new[] { "Status", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VND_Pedidos_UsuarioId_CriadoEmUtc",
                table: "VND_Pedidos",
                columns: new[] { "UsuarioId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IDN_TokensAtualizacao_ExpiraEmUtc",
                table: "IDN_TokensAtualizacao",
                column: "ExpiraEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EST_AjustesDeEstoque_VarianteDeProdutoId_CriadoEmUtc",
                table: "EST_AjustesDeEstoque",
                columns: new[] { "VarianteDeProdutoId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CAT_VariantesDeProduto_ProdutoId_Ativo_Estoque",
                table: "CAT_VariantesDeProduto",
                columns: new[] { "ProdutoId", "Ativo", "Estoque" });

            migrationBuilder.CreateIndex(
                name: "IX_CAT_Produtos_Ativo_CriadoEmUtc",
                table: "CAT_Produtos",
                columns: new[] { "Ativo", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CAT_Produtos_CategoriaId_Preco",
                table: "CAT_Produtos",
                columns: new[] { "CategoriaId", "Preco" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VND_Pedidos_CriadoEmUtc",
                table: "VND_Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_VND_Pedidos_Status_CriadoEmUtc",
                table: "VND_Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_VND_Pedidos_UsuarioId_CriadoEmUtc",
                table: "VND_Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_IDN_TokensAtualizacao_ExpiraEmUtc",
                table: "IDN_TokensAtualizacao");

            migrationBuilder.DropIndex(
                name: "IX_EST_AjustesDeEstoque_VarianteDeProdutoId_CriadoEmUtc",
                table: "EST_AjustesDeEstoque");

            migrationBuilder.DropIndex(
                name: "IX_CAT_VariantesDeProduto_ProdutoId_Ativo_Estoque",
                table: "CAT_VariantesDeProduto");

            migrationBuilder.DropIndex(
                name: "IX_CAT_Produtos_Ativo_CriadoEmUtc",
                table: "CAT_Produtos");

            migrationBuilder.DropIndex(
                name: "IX_CAT_Produtos_CategoriaId_Preco",
                table: "CAT_Produtos");

            migrationBuilder.DropColumn(
                name: "NomeParaBusca",
                table: "CAT_Produtos");

            migrationBuilder.CreateIndex(
                name: "IX_VND_Pedidos_UsuarioId",
                table: "VND_Pedidos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EST_AjustesDeEstoque_VarianteDeProdutoId",
                table: "EST_AjustesDeEstoque",
                column: "VarianteDeProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_CAT_VariantesDeProduto_ProdutoId",
                table: "CAT_VariantesDeProduto",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_CAT_Produtos_CategoriaId",
                table: "CAT_Produtos",
                column: "CategoriaId");
        }
    }
}

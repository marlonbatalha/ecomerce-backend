using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class ReorganizarTabelasPorContexto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carrinhos_Usuarios_UsuarioId",
                table: "Carrinhos");

            migrationBuilder.DropForeignKey(
                name: "FK_Categorias_Categorias_CategoriaPaiId",
                table: "Categorias");

            migrationBuilder.DropForeignKey(
                name: "FK_ClaimsDePapel_Papeis_RoleId",
                table: "ClaimsDePapel");

            migrationBuilder.DropForeignKey(
                name: "FK_ClaimsDeUsuario_Usuarios_UserId",
                table: "ClaimsDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_Enderecos_Usuarios_UsuarioId",
                table: "Enderecos");

            migrationBuilder.DropForeignKey(
                name: "FK_ImagensDeProduto_Produtos_ProdutoId",
                table: "ImagensDeProduto");

            migrationBuilder.DropForeignKey(
                name: "FK_ItensDoCarrinho_Carrinhos_CarrinhoId",
                table: "ItensDoCarrinho");

            migrationBuilder.DropForeignKey(
                name: "FK_ItensDoPedido_Pedidos_PedidoId",
                table: "ItensDoPedido");

            migrationBuilder.DropForeignKey(
                name: "FK_LoginsDeUsuario_Usuarios_UserId",
                table: "LoginsDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_Categorias_CategoriaId",
                table: "Produtos");

            migrationBuilder.DropForeignKey(
                name: "FK_TokensAtualizacao_Usuarios_UsuarioId",
                table: "TokensAtualizacao");

            migrationBuilder.DropForeignKey(
                name: "FK_TokensDeUsuario_Usuarios_UserId",
                table: "TokensDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosPapeis_Papeis_RoleId",
                table: "UsuariosPapeis");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosPapeis_Usuarios_UserId",
                table: "UsuariosPapeis");

            migrationBuilder.DropForeignKey(
                name: "FK_VariantesDeProduto_Produtos_ProdutoId",
                table: "VariantesDeProduto");

            migrationBuilder.DropTable(
                name: "AjustesDeEstoque");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VariantesDeProduto",
                table: "VariantesDeProduto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuariosPapeis",
                table: "UsuariosPapeis");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TokensDeUsuario",
                table: "TokensDeUsuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TokensAtualizacao",
                table: "TokensAtualizacao");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Produtos",
                table: "Produtos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pedidos",
                table: "Pedidos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Papeis",
                table: "Papeis");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pagamentos",
                table: "Pagamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LoginsDeUsuario",
                table: "LoginsDeUsuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItensDoPedido",
                table: "ItensDoPedido");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItensDoCarrinho",
                table: "ItensDoCarrinho");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImagensDeProduto",
                table: "ImagensDeProduto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Enderecos",
                table: "Enderecos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClaimsDeUsuario",
                table: "ClaimsDeUsuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClaimsDePapel",
                table: "ClaimsDePapel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categorias",
                table: "Categorias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Carrinhos",
                table: "Carrinhos");

            migrationBuilder.RenameTable(
                name: "VariantesDeProduto",
                newName: "CAT_VariantesDeProduto");

            migrationBuilder.RenameTable(
                name: "UsuariosPapeis",
                newName: "IDN_UsuariosPapeis");

            migrationBuilder.RenameTable(
                name: "Usuarios",
                newName: "IDN_Usuarios");

            migrationBuilder.RenameTable(
                name: "TokensDeUsuario",
                newName: "IDN_TokensDeUsuario");

            migrationBuilder.RenameTable(
                name: "TokensAtualizacao",
                newName: "IDN_TokensAtualizacao");

            migrationBuilder.RenameTable(
                name: "Produtos",
                newName: "CAT_Produtos");

            migrationBuilder.RenameTable(
                name: "Pedidos",
                newName: "VND_Pedidos");

            migrationBuilder.RenameTable(
                name: "Papeis",
                newName: "IDN_Papeis");

            migrationBuilder.RenameTable(
                name: "Pagamentos",
                newName: "VND_Pagamentos");

            migrationBuilder.RenameTable(
                name: "LoginsDeUsuario",
                newName: "IDN_LoginsDeUsuario");

            migrationBuilder.RenameTable(
                name: "ItensDoPedido",
                newName: "VND_ItensDoPedido");

            migrationBuilder.RenameTable(
                name: "ItensDoCarrinho",
                newName: "VND_ItensDoCarrinho");

            migrationBuilder.RenameTable(
                name: "ImagensDeProduto",
                newName: "CAT_ImagensDeProduto");

            migrationBuilder.RenameTable(
                name: "Enderecos",
                newName: "CTA_Enderecos");

            migrationBuilder.RenameTable(
                name: "ClaimsDeUsuario",
                newName: "IDN_ClaimsDeUsuario");

            migrationBuilder.RenameTable(
                name: "ClaimsDePapel",
                newName: "IDN_ClaimsDePapel");

            migrationBuilder.RenameTable(
                name: "Categorias",
                newName: "CAT_Categorias");

            migrationBuilder.RenameTable(
                name: "Carrinhos",
                newName: "VND_Carrinhos");

            migrationBuilder.RenameIndex(
                name: "IX_VariantesDeProduto_Sku",
                table: "CAT_VariantesDeProduto",
                newName: "IX_CAT_VariantesDeProduto_Sku");

            migrationBuilder.RenameIndex(
                name: "IX_VariantesDeProduto_ProdutoId",
                table: "CAT_VariantesDeProduto",
                newName: "IX_CAT_VariantesDeProduto_ProdutoId");

            migrationBuilder.RenameIndex(
                name: "IX_UsuariosPapeis_RoleId",
                table: "IDN_UsuariosPapeis",
                newName: "IX_IDN_UsuariosPapeis_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_TokensAtualizacao_UsuarioId",
                table: "IDN_TokensAtualizacao",
                newName: "IX_IDN_TokensAtualizacao_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_TokensAtualizacao_HashToken",
                table: "IDN_TokensAtualizacao",
                newName: "IX_IDN_TokensAtualizacao_HashToken");

            migrationBuilder.RenameIndex(
                name: "IX_Produtos_Sku",
                table: "CAT_Produtos",
                newName: "IX_CAT_Produtos_Sku");

            migrationBuilder.RenameIndex(
                name: "IX_Produtos_CategoriaId",
                table: "CAT_Produtos",
                newName: "IX_CAT_Produtos_CategoriaId");

            migrationBuilder.RenameIndex(
                name: "IX_Pedidos_UsuarioId",
                table: "VND_Pedidos",
                newName: "IX_VND_Pedidos_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Pagamentos_ReferenciaExterna",
                table: "VND_Pagamentos",
                newName: "IX_VND_Pagamentos_ReferenciaExterna");

            migrationBuilder.RenameIndex(
                name: "IX_Pagamentos_PedidoId",
                table: "VND_Pagamentos",
                newName: "IX_VND_Pagamentos_PedidoId");

            migrationBuilder.RenameIndex(
                name: "IX_LoginsDeUsuario_UserId",
                table: "IDN_LoginsDeUsuario",
                newName: "IX_IDN_LoginsDeUsuario_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ItensDoPedido_PedidoId",
                table: "VND_ItensDoPedido",
                newName: "IX_VND_ItensDoPedido_PedidoId");

            migrationBuilder.RenameIndex(
                name: "IX_ItensDoCarrinho_CarrinhoId",
                table: "VND_ItensDoCarrinho",
                newName: "IX_VND_ItensDoCarrinho_CarrinhoId");

            migrationBuilder.RenameIndex(
                name: "IX_ImagensDeProduto_ProdutoId",
                table: "CAT_ImagensDeProduto",
                newName: "IX_CAT_ImagensDeProduto_ProdutoId");

            migrationBuilder.RenameIndex(
                name: "IX_Enderecos_UsuarioId",
                table: "CTA_Enderecos",
                newName: "IX_CTA_Enderecos_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_ClaimsDeUsuario_UserId",
                table: "IDN_ClaimsDeUsuario",
                newName: "IX_IDN_ClaimsDeUsuario_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ClaimsDePapel_RoleId",
                table: "IDN_ClaimsDePapel",
                newName: "IX_IDN_ClaimsDePapel_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Categorias_Slug",
                table: "CAT_Categorias",
                newName: "IX_CAT_Categorias_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_Categorias_CategoriaPaiId",
                table: "CAT_Categorias",
                newName: "IX_CAT_Categorias_CategoriaPaiId");

            migrationBuilder.RenameIndex(
                name: "IX_Carrinhos_UsuarioId",
                table: "VND_Carrinhos",
                newName: "IX_VND_Carrinhos_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Carrinhos_IdSessao",
                table: "VND_Carrinhos",
                newName: "IX_VND_Carrinhos_IdSessao");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CAT_VariantesDeProduto",
                table: "CAT_VariantesDeProduto",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IDN_UsuariosPapeis",
                table: "IDN_UsuariosPapeis",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_IDN_Usuarios",
                table: "IDN_Usuarios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IDN_TokensDeUsuario",
                table: "IDN_TokensDeUsuario",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_IDN_TokensAtualizacao",
                table: "IDN_TokensAtualizacao",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CAT_Produtos",
                table: "CAT_Produtos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VND_Pedidos",
                table: "VND_Pedidos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IDN_Papeis",
                table: "IDN_Papeis",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VND_Pagamentos",
                table: "VND_Pagamentos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IDN_LoginsDeUsuario",
                table: "IDN_LoginsDeUsuario",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_VND_ItensDoPedido",
                table: "VND_ItensDoPedido",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VND_ItensDoCarrinho",
                table: "VND_ItensDoCarrinho",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CAT_ImagensDeProduto",
                table: "CAT_ImagensDeProduto",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CTA_Enderecos",
                table: "CTA_Enderecos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IDN_ClaimsDeUsuario",
                table: "IDN_ClaimsDeUsuario",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IDN_ClaimsDePapel",
                table: "IDN_ClaimsDePapel",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CAT_Categorias",
                table: "CAT_Categorias",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VND_Carrinhos",
                table: "VND_Carrinhos",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "EST_AjustesDeEstoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    VarianteDeProdutoId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    EstoqueAnterior = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EstoqueNovo = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Motivo = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EST_AjustesDeEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EST_AjustesDeEstoque_IDN_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "IDN_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EST_AjustesDeEstoque_UsuarioId",
                table: "EST_AjustesDeEstoque",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EST_AjustesDeEstoque_VarianteDeProdutoId",
                table: "EST_AjustesDeEstoque",
                column: "VarianteDeProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_CAT_Categorias_CAT_Categorias_CategoriaPaiId",
                table: "CAT_Categorias",
                column: "CategoriaPaiId",
                principalTable: "CAT_Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CAT_ImagensDeProduto_CAT_Produtos_ProdutoId",
                table: "CAT_ImagensDeProduto",
                column: "ProdutoId",
                principalTable: "CAT_Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CAT_Produtos_CAT_Categorias_CategoriaId",
                table: "CAT_Produtos",
                column: "CategoriaId",
                principalTable: "CAT_Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CAT_VariantesDeProduto_CAT_Produtos_ProdutoId",
                table: "CAT_VariantesDeProduto",
                column: "ProdutoId",
                principalTable: "CAT_Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CTA_Enderecos_IDN_Usuarios_UsuarioId",
                table: "CTA_Enderecos",
                column: "UsuarioId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_ClaimsDePapel_IDN_Papeis_RoleId",
                table: "IDN_ClaimsDePapel",
                column: "RoleId",
                principalTable: "IDN_Papeis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_ClaimsDeUsuario_IDN_Usuarios_UserId",
                table: "IDN_ClaimsDeUsuario",
                column: "UserId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_LoginsDeUsuario_IDN_Usuarios_UserId",
                table: "IDN_LoginsDeUsuario",
                column: "UserId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_TokensAtualizacao_IDN_Usuarios_UsuarioId",
                table: "IDN_TokensAtualizacao",
                column: "UsuarioId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_TokensDeUsuario_IDN_Usuarios_UserId",
                table: "IDN_TokensDeUsuario",
                column: "UserId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Papeis_RoleId",
                table: "IDN_UsuariosPapeis",
                column: "RoleId",
                principalTable: "IDN_Papeis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Usuarios_UserId",
                table: "IDN_UsuariosPapeis",
                column: "UserId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VND_Carrinhos_IDN_Usuarios_UsuarioId",
                table: "VND_Carrinhos",
                column: "UsuarioId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VND_ItensDoCarrinho_VND_Carrinhos_CarrinhoId",
                table: "VND_ItensDoCarrinho",
                column: "CarrinhoId",
                principalTable: "VND_Carrinhos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VND_ItensDoPedido_VND_Pedidos_PedidoId",
                table: "VND_ItensDoPedido",
                column: "PedidoId",
                principalTable: "VND_Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VND_Pedidos_IDN_Usuarios_UsuarioId",
                table: "VND_Pedidos",
                column: "UsuarioId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CAT_Categorias_CAT_Categorias_CategoriaPaiId",
                table: "CAT_Categorias");

            migrationBuilder.DropForeignKey(
                name: "FK_CAT_ImagensDeProduto_CAT_Produtos_ProdutoId",
                table: "CAT_ImagensDeProduto");

            migrationBuilder.DropForeignKey(
                name: "FK_CAT_Produtos_CAT_Categorias_CategoriaId",
                table: "CAT_Produtos");

            migrationBuilder.DropForeignKey(
                name: "FK_CAT_VariantesDeProduto_CAT_Produtos_ProdutoId",
                table: "CAT_VariantesDeProduto");

            migrationBuilder.DropForeignKey(
                name: "FK_CTA_Enderecos_IDN_Usuarios_UsuarioId",
                table: "CTA_Enderecos");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_ClaimsDePapel_IDN_Papeis_RoleId",
                table: "IDN_ClaimsDePapel");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_ClaimsDeUsuario_IDN_Usuarios_UserId",
                table: "IDN_ClaimsDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_LoginsDeUsuario_IDN_Usuarios_UserId",
                table: "IDN_LoginsDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_TokensAtualizacao_IDN_Usuarios_UsuarioId",
                table: "IDN_TokensAtualizacao");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_TokensDeUsuario_IDN_Usuarios_UserId",
                table: "IDN_TokensDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Papeis_RoleId",
                table: "IDN_UsuariosPapeis");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Usuarios_UserId",
                table: "IDN_UsuariosPapeis");

            migrationBuilder.DropForeignKey(
                name: "FK_VND_Carrinhos_IDN_Usuarios_UsuarioId",
                table: "VND_Carrinhos");

            migrationBuilder.DropForeignKey(
                name: "FK_VND_ItensDoCarrinho_VND_Carrinhos_CarrinhoId",
                table: "VND_ItensDoCarrinho");

            migrationBuilder.DropForeignKey(
                name: "FK_VND_ItensDoPedido_VND_Pedidos_PedidoId",
                table: "VND_ItensDoPedido");

            migrationBuilder.DropForeignKey(
                name: "FK_VND_Pedidos_IDN_Usuarios_UsuarioId",
                table: "VND_Pedidos");

            migrationBuilder.DropTable(
                name: "EST_AjustesDeEstoque");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VND_Pedidos",
                table: "VND_Pedidos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VND_Pagamentos",
                table: "VND_Pagamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VND_ItensDoPedido",
                table: "VND_ItensDoPedido");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VND_ItensDoCarrinho",
                table: "VND_ItensDoCarrinho");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VND_Carrinhos",
                table: "VND_Carrinhos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IDN_UsuariosPapeis",
                table: "IDN_UsuariosPapeis");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IDN_Usuarios",
                table: "IDN_Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IDN_TokensDeUsuario",
                table: "IDN_TokensDeUsuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IDN_TokensAtualizacao",
                table: "IDN_TokensAtualizacao");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IDN_Papeis",
                table: "IDN_Papeis");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IDN_LoginsDeUsuario",
                table: "IDN_LoginsDeUsuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IDN_ClaimsDeUsuario",
                table: "IDN_ClaimsDeUsuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IDN_ClaimsDePapel",
                table: "IDN_ClaimsDePapel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CTA_Enderecos",
                table: "CTA_Enderecos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CAT_VariantesDeProduto",
                table: "CAT_VariantesDeProduto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CAT_Produtos",
                table: "CAT_Produtos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CAT_ImagensDeProduto",
                table: "CAT_ImagensDeProduto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CAT_Categorias",
                table: "CAT_Categorias");

            migrationBuilder.RenameTable(
                name: "VND_Pedidos",
                newName: "Pedidos");

            migrationBuilder.RenameTable(
                name: "VND_Pagamentos",
                newName: "Pagamentos");

            migrationBuilder.RenameTable(
                name: "VND_ItensDoPedido",
                newName: "ItensDoPedido");

            migrationBuilder.RenameTable(
                name: "VND_ItensDoCarrinho",
                newName: "ItensDoCarrinho");

            migrationBuilder.RenameTable(
                name: "VND_Carrinhos",
                newName: "Carrinhos");

            migrationBuilder.RenameTable(
                name: "IDN_UsuariosPapeis",
                newName: "UsuariosPapeis");

            migrationBuilder.RenameTable(
                name: "IDN_Usuarios",
                newName: "Usuarios");

            migrationBuilder.RenameTable(
                name: "IDN_TokensDeUsuario",
                newName: "TokensDeUsuario");

            migrationBuilder.RenameTable(
                name: "IDN_TokensAtualizacao",
                newName: "TokensAtualizacao");

            migrationBuilder.RenameTable(
                name: "IDN_Papeis",
                newName: "Papeis");

            migrationBuilder.RenameTable(
                name: "IDN_LoginsDeUsuario",
                newName: "LoginsDeUsuario");

            migrationBuilder.RenameTable(
                name: "IDN_ClaimsDeUsuario",
                newName: "ClaimsDeUsuario");

            migrationBuilder.RenameTable(
                name: "IDN_ClaimsDePapel",
                newName: "ClaimsDePapel");

            migrationBuilder.RenameTable(
                name: "CTA_Enderecos",
                newName: "Enderecos");

            migrationBuilder.RenameTable(
                name: "CAT_VariantesDeProduto",
                newName: "VariantesDeProduto");

            migrationBuilder.RenameTable(
                name: "CAT_Produtos",
                newName: "Produtos");

            migrationBuilder.RenameTable(
                name: "CAT_ImagensDeProduto",
                newName: "ImagensDeProduto");

            migrationBuilder.RenameTable(
                name: "CAT_Categorias",
                newName: "Categorias");

            migrationBuilder.RenameIndex(
                name: "IX_VND_Pedidos_UsuarioId",
                table: "Pedidos",
                newName: "IX_Pedidos_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_VND_Pagamentos_ReferenciaExterna",
                table: "Pagamentos",
                newName: "IX_Pagamentos_ReferenciaExterna");

            migrationBuilder.RenameIndex(
                name: "IX_VND_Pagamentos_PedidoId",
                table: "Pagamentos",
                newName: "IX_Pagamentos_PedidoId");

            migrationBuilder.RenameIndex(
                name: "IX_VND_ItensDoPedido_PedidoId",
                table: "ItensDoPedido",
                newName: "IX_ItensDoPedido_PedidoId");

            migrationBuilder.RenameIndex(
                name: "IX_VND_ItensDoCarrinho_CarrinhoId",
                table: "ItensDoCarrinho",
                newName: "IX_ItensDoCarrinho_CarrinhoId");

            migrationBuilder.RenameIndex(
                name: "IX_VND_Carrinhos_UsuarioId",
                table: "Carrinhos",
                newName: "IX_Carrinhos_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_VND_Carrinhos_IdSessao",
                table: "Carrinhos",
                newName: "IX_Carrinhos_IdSessao");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_UsuariosPapeis_RoleId",
                table: "UsuariosPapeis",
                newName: "IX_UsuariosPapeis_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_TokensAtualizacao_UsuarioId",
                table: "TokensAtualizacao",
                newName: "IX_TokensAtualizacao_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_TokensAtualizacao_HashToken",
                table: "TokensAtualizacao",
                newName: "IX_TokensAtualizacao_HashToken");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_LoginsDeUsuario_UserId",
                table: "LoginsDeUsuario",
                newName: "IX_LoginsDeUsuario_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_ClaimsDeUsuario_UserId",
                table: "ClaimsDeUsuario",
                newName: "IX_ClaimsDeUsuario_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_ClaimsDePapel_RoleId",
                table: "ClaimsDePapel",
                newName: "IX_ClaimsDePapel_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_CTA_Enderecos_UsuarioId",
                table: "Enderecos",
                newName: "IX_Enderecos_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_CAT_VariantesDeProduto_Sku",
                table: "VariantesDeProduto",
                newName: "IX_VariantesDeProduto_Sku");

            migrationBuilder.RenameIndex(
                name: "IX_CAT_VariantesDeProduto_ProdutoId",
                table: "VariantesDeProduto",
                newName: "IX_VariantesDeProduto_ProdutoId");

            migrationBuilder.RenameIndex(
                name: "IX_CAT_Produtos_Sku",
                table: "Produtos",
                newName: "IX_Produtos_Sku");

            migrationBuilder.RenameIndex(
                name: "IX_CAT_Produtos_CategoriaId",
                table: "Produtos",
                newName: "IX_Produtos_CategoriaId");

            migrationBuilder.RenameIndex(
                name: "IX_CAT_ImagensDeProduto_ProdutoId",
                table: "ImagensDeProduto",
                newName: "IX_ImagensDeProduto_ProdutoId");

            migrationBuilder.RenameIndex(
                name: "IX_CAT_Categorias_Slug",
                table: "Categorias",
                newName: "IX_Categorias_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_CAT_Categorias_CategoriaPaiId",
                table: "Categorias",
                newName: "IX_Categorias_CategoriaPaiId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pedidos",
                table: "Pedidos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pagamentos",
                table: "Pagamentos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItensDoPedido",
                table: "ItensDoPedido",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItensDoCarrinho",
                table: "ItensDoCarrinho",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Carrinhos",
                table: "Carrinhos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuariosPapeis",
                table: "UsuariosPapeis",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TokensDeUsuario",
                table: "TokensDeUsuario",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TokensAtualizacao",
                table: "TokensAtualizacao",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Papeis",
                table: "Papeis",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LoginsDeUsuario",
                table: "LoginsDeUsuario",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClaimsDeUsuario",
                table: "ClaimsDeUsuario",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClaimsDePapel",
                table: "ClaimsDePapel",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Enderecos",
                table: "Enderecos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VariantesDeProduto",
                table: "VariantesDeProduto",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Produtos",
                table: "Produtos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImagensDeProduto",
                table: "ImagensDeProduto",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categorias",
                table: "Categorias",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AjustesDeEstoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    EstoqueAnterior = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EstoqueNovo = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Motivo = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    VarianteDeProdutoId = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjustesDeEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AjustesDeEstoque_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AjustesDeEstoque_UsuarioId",
                table: "AjustesDeEstoque",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AjustesDeEstoque_VarianteDeProdutoId",
                table: "AjustesDeEstoque",
                column: "VarianteDeProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Carrinhos_Usuarios_UsuarioId",
                table: "Carrinhos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Categorias_Categorias_CategoriaPaiId",
                table: "Categorias",
                column: "CategoriaPaiId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimsDePapel_Papeis_RoleId",
                table: "ClaimsDePapel",
                column: "RoleId",
                principalTable: "Papeis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimsDeUsuario_Usuarios_UserId",
                table: "ClaimsDeUsuario",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Enderecos_Usuarios_UsuarioId",
                table: "Enderecos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImagensDeProduto_Produtos_ProdutoId",
                table: "ImagensDeProduto",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItensDoCarrinho_Carrinhos_CarrinhoId",
                table: "ItensDoCarrinho",
                column: "CarrinhoId",
                principalTable: "Carrinhos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItensDoPedido_Pedidos_PedidoId",
                table: "ItensDoPedido",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LoginsDeUsuario_Usuarios_UserId",
                table: "LoginsDeUsuario",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_Categorias_CategoriaId",
                table: "Produtos",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TokensAtualizacao_Usuarios_UsuarioId",
                table: "TokensAtualizacao",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TokensDeUsuario_Usuarios_UserId",
                table: "TokensDeUsuario",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosPapeis_Papeis_RoleId",
                table: "UsuariosPapeis",
                column: "RoleId",
                principalTable: "Papeis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosPapeis_Usuarios_UserId",
                table: "UsuariosPapeis",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariantesDeProduto_Produtos_ProdutoId",
                table: "VariantesDeProduto",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

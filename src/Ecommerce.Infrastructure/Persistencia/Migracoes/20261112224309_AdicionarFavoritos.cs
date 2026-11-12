using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarFavoritos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CTA_Favoritos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CTA_Favoritos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CTA_Favoritos_CAT_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "CAT_Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CTA_Favoritos_IDN_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "IDN_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CTA_Favoritos_ProdutoId",
                table: "CTA_Favoritos",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_CTA_Favoritos_UsuarioId_ProdutoId",
                table: "CTA_Favoritos",
                columns: new[] { "UsuarioId", "ProdutoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CTA_Favoritos");
        }
    }
}

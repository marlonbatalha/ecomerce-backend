using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarSlugAoProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "CAT_Produtos",
                type: "NVARCHAR2(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CAT_Produtos_Slug",
                table: "CAT_Produtos",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CAT_Produtos_Slug",
                table: "CAT_Produtos");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "CAT_Produtos");
        }
    }
}

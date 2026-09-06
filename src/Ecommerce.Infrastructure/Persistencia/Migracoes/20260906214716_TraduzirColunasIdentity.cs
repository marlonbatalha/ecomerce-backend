using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class TraduzirColunasIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "FK_IDN_TokensDeUsuario_IDN_Usuarios_UserId",
                table: "IDN_TokensDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Papeis_RoleId",
                table: "IDN_UsuariosPapeis");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Usuarios_UserId",
                table: "IDN_UsuariosPapeis");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "IDN_Usuarios");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "IDN_Papeis");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "IDN_UsuariosPapeis",
                newName: "PapelId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "IDN_UsuariosPapeis",
                newName: "UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_UsuariosPapeis_RoleId",
                table: "IDN_UsuariosPapeis",
                newName: "IX_IDN_UsuariosPapeis_PapelId");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "IDN_Usuarios",
                newName: "NomeDeUsuario");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                table: "IDN_Usuarios",
                newName: "AutenticacaoDoisFatoresAtiva");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "IDN_Usuarios",
                newName: "CarimboDeSeguranca");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                table: "IDN_Usuarios",
                newName: "TelefoneConfirmado");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "IDN_Usuarios",
                newName: "Telefone");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "IDN_Usuarios",
                newName: "HashDaSenha");

            migrationBuilder.RenameColumn(
                name: "NormalizedUserName",
                table: "IDN_Usuarios",
                newName: "NomeDeUsuarioNormalizado");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmail",
                table: "IDN_Usuarios",
                newName: "EmailNormalizado");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "IDN_Usuarios",
                newName: "BloqueadoAte");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                table: "IDN_Usuarios",
                newName: "BloqueioAtivo");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "IDN_Usuarios",
                newName: "EmailConfirmado");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "IDN_Usuarios",
                newName: "CarimboDeConcorrencia");

            migrationBuilder.RenameColumn(
                name: "AccessFailedCount",
                table: "IDN_Usuarios",
                newName: "TentativasDeAcessoFalhas");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "IDN_TokensDeUsuario",
                newName: "Valor");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "IDN_TokensDeUsuario",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "IDN_TokensDeUsuario",
                newName: "ProvedorDeLogin");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "IDN_TokensDeUsuario",
                newName: "UsuarioId");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "IDN_Papeis",
                newName: "NomeNormalizado");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "IDN_Papeis",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "IDN_Papeis",
                newName: "CarimboDeConcorrencia");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "IDN_LoginsDeUsuario",
                newName: "UsuarioId");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                table: "IDN_LoginsDeUsuario",
                newName: "NomeDeExibicaoDoProvedor");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                table: "IDN_LoginsDeUsuario",
                newName: "ChaveDoProvedor");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "IDN_LoginsDeUsuario",
                newName: "ProvedorDeLogin");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_LoginsDeUsuario_UserId",
                table: "IDN_LoginsDeUsuario",
                newName: "IX_IDN_LoginsDeUsuario_UsuarioId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "IDN_ClaimsDeUsuario",
                newName: "UsuarioId");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "IDN_ClaimsDeUsuario",
                newName: "ValorDaClaim");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "IDN_ClaimsDeUsuario",
                newName: "TipoDeClaim");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_ClaimsDeUsuario_UserId",
                table: "IDN_ClaimsDeUsuario",
                newName: "IX_IDN_ClaimsDeUsuario_UsuarioId");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "IDN_ClaimsDePapel",
                newName: "PapelId");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "IDN_ClaimsDePapel",
                newName: "ValorDaClaim");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "IDN_ClaimsDePapel",
                newName: "TipoDeClaim");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_ClaimsDePapel_RoleId",
                table: "IDN_ClaimsDePapel",
                newName: "IX_IDN_ClaimsDePapel_PapelId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "IDN_Usuarios",
                column: "NomeDeUsuarioNormalizado",
                unique: true,
                filter: "\"NomeDeUsuarioNormalizado\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "IDN_Papeis",
                column: "NomeNormalizado",
                unique: true,
                filter: "\"NomeNormalizado\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_ClaimsDePapel_IDN_Papeis_PapelId",
                table: "IDN_ClaimsDePapel",
                column: "PapelId",
                principalTable: "IDN_Papeis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_ClaimsDeUsuario_IDN_Usuarios_UsuarioId",
                table: "IDN_ClaimsDeUsuario",
                column: "UsuarioId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_LoginsDeUsuario_IDN_Usuarios_UsuarioId",
                table: "IDN_LoginsDeUsuario",
                column: "UsuarioId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_TokensDeUsuario_IDN_Usuarios_UsuarioId",
                table: "IDN_TokensDeUsuario",
                column: "UsuarioId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Papeis_PapelId",
                table: "IDN_UsuariosPapeis",
                column: "PapelId",
                principalTable: "IDN_Papeis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Usuarios_UsuarioId",
                table: "IDN_UsuariosPapeis",
                column: "UsuarioId",
                principalTable: "IDN_Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IDN_ClaimsDePapel_IDN_Papeis_PapelId",
                table: "IDN_ClaimsDePapel");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_ClaimsDeUsuario_IDN_Usuarios_UsuarioId",
                table: "IDN_ClaimsDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_LoginsDeUsuario_IDN_Usuarios_UsuarioId",
                table: "IDN_LoginsDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_TokensDeUsuario_IDN_Usuarios_UsuarioId",
                table: "IDN_TokensDeUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Papeis_PapelId",
                table: "IDN_UsuariosPapeis");

            migrationBuilder.DropForeignKey(
                name: "FK_IDN_UsuariosPapeis_IDN_Usuarios_UsuarioId",
                table: "IDN_UsuariosPapeis");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "IDN_Usuarios");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "IDN_Papeis");

            migrationBuilder.RenameColumn(
                name: "PapelId",
                table: "IDN_UsuariosPapeis",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "IDN_UsuariosPapeis",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_UsuariosPapeis_PapelId",
                table: "IDN_UsuariosPapeis",
                newName: "IX_IDN_UsuariosPapeis_RoleId");

            migrationBuilder.RenameColumn(
                name: "TentativasDeAcessoFalhas",
                table: "IDN_Usuarios",
                newName: "AccessFailedCount");

            migrationBuilder.RenameColumn(
                name: "TelefoneConfirmado",
                table: "IDN_Usuarios",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "Telefone",
                table: "IDN_Usuarios",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "NomeDeUsuarioNormalizado",
                table: "IDN_Usuarios",
                newName: "NormalizedUserName");

            migrationBuilder.RenameColumn(
                name: "NomeDeUsuario",
                table: "IDN_Usuarios",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "HashDaSenha",
                table: "IDN_Usuarios",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "EmailNormalizado",
                table: "IDN_Usuarios",
                newName: "NormalizedEmail");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmado",
                table: "IDN_Usuarios",
                newName: "EmailConfirmed");

            migrationBuilder.RenameColumn(
                name: "CarimboDeSeguranca",
                table: "IDN_Usuarios",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "CarimboDeConcorrencia",
                table: "IDN_Usuarios",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "BloqueioAtivo",
                table: "IDN_Usuarios",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "BloqueadoAte",
                table: "IDN_Usuarios",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "AutenticacaoDoisFatoresAtiva",
                table: "IDN_Usuarios",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "Valor",
                table: "IDN_TokensDeUsuario",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "IDN_TokensDeUsuario",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "ProvedorDeLogin",
                table: "IDN_TokensDeUsuario",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "IDN_TokensDeUsuario",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "NomeNormalizado",
                table: "IDN_Papeis",
                newName: "NormalizedName");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "IDN_Papeis",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "CarimboDeConcorrencia",
                table: "IDN_Papeis",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "IDN_LoginsDeUsuario",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "NomeDeExibicaoDoProvedor",
                table: "IDN_LoginsDeUsuario",
                newName: "ProviderDisplayName");

            migrationBuilder.RenameColumn(
                name: "ChaveDoProvedor",
                table: "IDN_LoginsDeUsuario",
                newName: "ProviderKey");

            migrationBuilder.RenameColumn(
                name: "ProvedorDeLogin",
                table: "IDN_LoginsDeUsuario",
                newName: "LoginProvider");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_LoginsDeUsuario_UsuarioId",
                table: "IDN_LoginsDeUsuario",
                newName: "IX_IDN_LoginsDeUsuario_UserId");

            migrationBuilder.RenameColumn(
                name: "ValorDaClaim",
                table: "IDN_ClaimsDeUsuario",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "IDN_ClaimsDeUsuario",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "TipoDeClaim",
                table: "IDN_ClaimsDeUsuario",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_ClaimsDeUsuario_UsuarioId",
                table: "IDN_ClaimsDeUsuario",
                newName: "IX_IDN_ClaimsDeUsuario_UserId");

            migrationBuilder.RenameColumn(
                name: "ValorDaClaim",
                table: "IDN_ClaimsDePapel",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "TipoDeClaim",
                table: "IDN_ClaimsDePapel",
                newName: "ClaimType");

            migrationBuilder.RenameColumn(
                name: "PapelId",
                table: "IDN_ClaimsDePapel",
                newName: "RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_IDN_ClaimsDePapel_PapelId",
                table: "IDN_ClaimsDePapel",
                newName: "IX_IDN_ClaimsDePapel_RoleId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "IDN_Usuarios",
                column: "NormalizedUserName",
                unique: true,
                filter: "\"NormalizedUserName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "IDN_Papeis",
                column: "NormalizedName",
                unique: true,
                filter: "\"NormalizedName\" IS NOT NULL");

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
        }
    }
}

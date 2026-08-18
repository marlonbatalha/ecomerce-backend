namespace Ecommerce.Application.Catalogo;

/// <summary>Todo ajuste de estoque é auditado (ver <c>AjusteDeEstoqueDto</c>) — motivo é obrigatório.</summary>
public record AjustarEstoqueComando(Guid ProdutoId, Guid VarianteId, int NovoEstoque, Guid UsuarioId, string Motivo);

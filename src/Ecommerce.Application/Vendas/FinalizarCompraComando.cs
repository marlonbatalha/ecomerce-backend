namespace Ecommerce.Application.Vendas;

public record FinalizarCompraComando(Guid UsuarioId, Guid CarrinhoId, string? CodigoCupom = null);

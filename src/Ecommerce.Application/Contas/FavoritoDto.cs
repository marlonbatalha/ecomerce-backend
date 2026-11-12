using Ecommerce.Application.Catalogo;

namespace Ecommerce.Application.Contas;

/// <summary>
/// Item da lista de favoritos: o resumo do produto (o mesmo da listagem do catálogo), se dá pra
/// comprar agora e quando foi favoritado. Produto inativo ou sem variante com estoque continua na
/// lista (a pessoa escolheu guardar), só aparece como indisponível.
/// </summary>
public record FavoritoDto(ProdutoResumoDto Produto, bool Disponivel, DateTime FavoritadoEmUtc);

using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Estoque;

/// <summary>
/// Registro imutável de um ajuste manual de estoque feito pelo admin — auditoria (quem, quando,
/// de/para quanto e por quê). Não tem regra de negócio além da criação: é um fato já ocorrido,
/// não algo que se edita depois.
/// </summary>
public class AjusteDeEstoque : Entidade
{
    public const int TamanhoMaximoMotivo = 300;

    public Guid VarianteDeProdutoId { get; private set; }

    public Guid UsuarioId { get; private set; }

    public int EstoqueAnterior { get; private set; }

    public int EstoqueNovo { get; private set; }

    public string Motivo { get; private set; } = null!;

    public DateTime CriadoEmUtc { get; private set; }

    private AjusteDeEstoque()
    {
    }

    private AjusteDeEstoque(Guid id, Guid varianteDeProdutoId, Guid usuarioId, int estoqueAnterior, int estoqueNovo, string motivo)
        : base(id)
    {
        VarianteDeProdutoId = varianteDeProdutoId;
        UsuarioId = usuarioId;
        EstoqueAnterior = estoqueAnterior;
        EstoqueNovo = estoqueNovo;
        Motivo = motivo;
        CriadoEmUtc = DateTime.UtcNow;
    }

    public static AjusteDeEstoque Criar(Guid varianteDeProdutoId, Guid usuarioId, int estoqueAnterior, int estoqueNovo, string motivo)
    {
        if (varianteDeProdutoId == Guid.Empty)
        {
            throw new ExcecaoDeDominio("O ajuste de estoque precisa estar associado a uma variante.");
        }

        if (usuarioId == Guid.Empty)
        {
            throw new ExcecaoDeDominio("O ajuste de estoque precisa registrar quem fez a alteração.");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ExcecaoDeDominio("O motivo do ajuste de estoque é obrigatório.");
        }

        if (motivo.Trim().Length > TamanhoMaximoMotivo)
        {
            throw new ExcecaoDeDominio($"O motivo não pode ter mais de {TamanhoMaximoMotivo} caracteres.");
        }

        if (estoqueAnterior < 0 || estoqueNovo < 0)
        {
            throw new ExcecaoDeDominio("Estoque não pode ser negativo.");
        }

        return new AjusteDeEstoque(Guid.NewGuid(), varianteDeProdutoId, usuarioId, estoqueAnterior, estoqueNovo, motivo.Trim());
    }
}

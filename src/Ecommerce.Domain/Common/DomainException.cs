namespace Ecommerce.Domain.Common;

/// <summary>
/// Violação de uma regra de negócio do domínio (ex.: nome obrigatório, transição de estado
/// inválida). Diferente de uma exceção técnica — sinaliza que quem chamou tentou uma operação
/// que fere uma invariante da entidade, e deve ser tratada pela camada acima (Application/Api)
/// como erro de validação, não como falha inesperada.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}

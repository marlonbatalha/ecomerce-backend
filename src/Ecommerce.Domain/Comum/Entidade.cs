namespace Ecommerce.Domain.Comum;

/// <summary>
/// Base para toda entidade do domínio: identidade estável (<see cref="Id"/>) e igualdade
/// por identidade (duas entidades são iguais se forem do mesmo tipo e tiverem o mesmo Id),
/// não por valor dos campos.
/// </summary>
public abstract class Entidade
{
    public Guid Id { get; protected init; }

    protected Entidade()
    {
    }

    protected Entidade(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("O Id de uma entidade não pode ser vazio.", nameof(id));
        }

        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entidade outra)
        {
            return false;
        }

        if (ReferenceEquals(this, outra))
        {
            return true;
        }

        if (GetType() != outra.GetType())
        {
            return false;
        }

        return Id == outra.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entidade? esquerda, Entidade? direita) => Equals(esquerda, direita);

    public static bool operator !=(Entidade? esquerda, Entidade? direita) => !Equals(esquerda, direita);
}

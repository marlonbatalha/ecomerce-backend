namespace Ecommerce.Domain.Common;

/// <summary>
/// Base para toda entidade do domínio: identidade estável (<see cref="Id"/>) e igualdade
/// por identidade (duas entidades são iguais se forem do mesmo tipo e tiverem o mesmo Id),
/// não por valor dos campos.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; }

    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("O Id de uma entidade não pode ser vazio.", nameof(id));
        }

        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}

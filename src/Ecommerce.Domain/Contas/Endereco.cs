using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Contas;

/// <summary>
/// Endereço de entrega/cobrança salvo por um usuário. Não é agregado de nada — cada instância é
/// independente (como <c>Categoria</c>); a regra "só um endereço padrão por usuário" é coordenada
/// pelo serviço de aplicação entre as instâncias do mesmo <see cref="UsuarioId"/>, não por esta
/// entidade sozinha (ver <c>IServicoEnderecos.MarcarComoPadraoAsync</c>).
/// </summary>
public class Endereco : Entidade
{
    public const int TamanhoMaximoRotulo = 50;
    public const int TamanhoMaximoLogradouro = 200;
    public const int TamanhoMaximoNumero = 20;
    public const int TamanhoMaximoComplemento = 100;
    public const int TamanhoMaximoBairro = 100;
    public const int TamanhoMaximoCidade = 100;
    public const int TamanhoEstado = 2;

    public Guid UsuarioId { get; private set; }

    public string Rotulo { get; private set; } = null!;

    public string Logradouro { get; private set; } = null!;

    public string Numero { get; private set; } = null!;

    public string? Complemento { get; private set; }

    public string Bairro { get; private set; } = null!;

    public string Cidade { get; private set; } = null!;

    /// <summary>UF — duas letras maiúsculas (ex.: "SP").</summary>
    public string Estado { get; private set; } = null!;

    /// <summary>Normalizado como "NNNNN-NNN".</summary>
    public string Cep { get; private set; } = null!;

    public bool Padrao { get; private set; }

    private Endereco()
    {
    }

    private Endereco(
        Guid id, Guid usuarioId, string rotulo, string logradouro, string numero, string? complemento,
        string bairro, string cidade, string estado, string cep)
        : base(id)
    {
        UsuarioId = usuarioId;
        Rotulo = rotulo;
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
        Bairro = bairro;
        Cidade = cidade;
        Estado = estado;
        Cep = cep;
    }

    public static Endereco Criar(
        Guid usuarioId, string rotulo, string logradouro, string numero, string? complemento,
        string bairro, string cidade, string estado, string cep)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ExcecaoDeDominio("O endereço precisa estar associado a um usuário.");
        }

        var (rotuloValido, logradouroValido, numeroValido, complementoValido, bairroValido, cidadeValida, estadoValido, cepValido) =
            GarantirDadosValidos(rotulo, logradouro, numero, complemento, bairro, cidade, estado, cep);

        return new Endereco(
            Guid.NewGuid(), usuarioId, rotuloValido, logradouroValido, numeroValido, complementoValido,
            bairroValido, cidadeValida, estadoValido, cepValido);
    }

    public void Atualizar(
        string rotulo, string logradouro, string numero, string? complemento,
        string bairro, string cidade, string estado, string cep)
    {
        var (rotuloValido, logradouroValido, numeroValido, complementoValido, bairroValido, cidadeValida, estadoValido, cepValido) =
            GarantirDadosValidos(rotulo, logradouro, numero, complemento, bairro, cidade, estado, cep);

        Rotulo = rotuloValido;
        Logradouro = logradouroValido;
        Numero = numeroValido;
        Complemento = complementoValido;
        Bairro = bairroValido;
        Cidade = cidadeValida;
        Estado = estadoValido;
        Cep = cepValido;
    }

    public void MarcarComoPadrao() => Padrao = true;

    public void DesmarcarComoPadrao() => Padrao = false;

    private static (string Rotulo, string Logradouro, string Numero, string? Complemento, string Bairro, string Cidade, string Estado, string Cep)
        GarantirDadosValidos(
            string rotulo, string logradouro, string numero, string? complemento,
            string bairro, string cidade, string estado, string cep)
    {
        GarantirNaoVazio(rotulo, "rótulo", TamanhoMaximoRotulo);
        GarantirNaoVazio(logradouro, "logradouro", TamanhoMaximoLogradouro);
        GarantirNaoVazio(numero, "número", TamanhoMaximoNumero);
        GarantirNaoVazio(bairro, "bairro", TamanhoMaximoBairro);
        GarantirNaoVazio(cidade, "cidade", TamanhoMaximoCidade);

        if (complemento is { Length: > 0 } && complemento.Trim().Length > TamanhoMaximoComplemento)
        {
            throw new ExcecaoDeDominio($"O complemento não pode ter mais de {TamanhoMaximoComplemento} caracteres.");
        }

        var estadoNormalizado = (estado ?? string.Empty).Trim().ToUpperInvariant();
        if (estadoNormalizado.Length != TamanhoEstado)
        {
            throw new ExcecaoDeDominio("O estado (UF) precisa ter 2 letras.");
        }

        var cepNormalizado = NormalizarCep(cep);

        return (
            rotulo.Trim(),
            logradouro.Trim(),
            numero.Trim(),
            string.IsNullOrWhiteSpace(complemento) ? null : complemento.Trim(),
            bairro.Trim(),
            cidade.Trim(),
            estadoNormalizado,
            cepNormalizado);
    }

    private static void GarantirNaoVazio(string valor, string nomeCampo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ExcecaoDeDominio($"O campo '{nomeCampo}' é obrigatório.");
        }

        if (valor.Trim().Length > tamanhoMaximo)
        {
            throw new ExcecaoDeDominio($"O campo '{nomeCampo}' não pode ter mais de {tamanhoMaximo} caracteres.");
        }
    }

    private static string NormalizarCep(string cep)
    {
        var digitos = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length != 8)
        {
            throw new ExcecaoDeDominio("O CEP precisa ter 8 dígitos.");
        }

        return $"{digitos[..5]}-{digitos[5..]}";
    }
}

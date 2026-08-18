using Ecommerce.Application.Contas;
using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Contas;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Contas;

public class ServicoEnderecos(ContextoBancoDados contexto) : IServicoEnderecos
{
    public async Task<EnderecoDto> CriarAsync(CriarEnderecoComando comando, CancellationToken ct = default)
    {
        var endereco = Endereco.Criar(
            comando.UsuarioId, comando.Rotulo, comando.Logradouro, comando.Numero, comando.Complemento,
            comando.Bairro, comando.Cidade, comando.Estado, comando.Cep);

        // Primeiro endereço do usuário já nasce padrão — sem isso, um cliente com um único
        // endereço não teria nenhum marcado como padrão.
        var jaTemEndereco = await contexto.Enderecos.AnyAsync(e => e.UsuarioId == comando.UsuarioId, ct);
        if (!jaTemEndereco)
        {
            endereco.MarcarComoPadrao();
        }

        contexto.Enderecos.Add(endereco);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(endereco);
    }

    public async Task<EnderecoDto> AtualizarAsync(AtualizarEnderecoComando comando, CancellationToken ct = default)
    {
        var endereco = await ObterOuFalharAsync(comando.EnderecoId, comando.UsuarioId, ct);

        endereco.Atualizar(
            comando.Rotulo, comando.Logradouro, comando.Numero, comando.Complemento,
            comando.Bairro, comando.Cidade, comando.Estado, comando.Cep);

        await contexto.SaveChangesAsync(ct);
        return MapearParaDto(endereco);
    }

    public async Task RemoverAsync(Guid enderecoId, Guid usuarioId, CancellationToken ct = default)
    {
        var endereco = await ObterOuFalharAsync(enderecoId, usuarioId, ct);
        contexto.Enderecos.Remove(endereco);
        await contexto.SaveChangesAsync(ct);
    }

    public async Task<EnderecoDto> MarcarComoPadraoAsync(Guid enderecoId, Guid usuarioId, CancellationToken ct = default)
    {
        var enderecosDoUsuario = await contexto.Enderecos.Where(e => e.UsuarioId == usuarioId).ToListAsync(ct);
        var endereco = enderecosDoUsuario.FirstOrDefault(e => e.Id == enderecoId)
            ?? throw new ExcecaoDeDominio("Endereço não encontrado.");

        foreach (var outro in enderecosDoUsuario.Where(e => e.Id != enderecoId && e.Padrao))
        {
            outro.DesmarcarComoPadrao();
        }

        endereco.MarcarComoPadrao();
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(endereco);
    }

    public async Task<IReadOnlyCollection<EnderecoDto>> ListarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var enderecos = await contexto.Enderecos
            .AsNoTracking()
            .Where(e => e.UsuarioId == usuarioId)
            .OrderByDescending(e => e.Padrao)
            .ToListAsync(ct);

        return enderecos.Select(MapearParaDto).ToList();
    }

    private async Task<Endereco> ObterOuFalharAsync(Guid enderecoId, Guid usuarioId, CancellationToken ct)
    {
        var endereco = await contexto.Enderecos.FirstOrDefaultAsync(e => e.Id == enderecoId, ct)
            ?? throw new ExcecaoDeDominio("Endereço não encontrado.");

        if (endereco.UsuarioId != usuarioId)
        {
            // Mesma mensagem de "não existe" — não confirma pra quem não é dono que o endereço existe.
            throw new ExcecaoDeDominio("Endereço não encontrado.");
        }

        return endereco;
    }

    private static EnderecoDto MapearParaDto(Endereco endereco) => new(
        endereco.Id, endereco.Rotulo, endereco.Logradouro, endereco.Numero, endereco.Complemento,
        endereco.Bairro, endereco.Cidade, endereco.Estado, endereco.Cep, endereco.Padrao);
}

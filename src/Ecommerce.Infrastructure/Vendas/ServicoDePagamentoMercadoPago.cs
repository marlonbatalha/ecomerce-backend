using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Vendas;

/// <summary>
/// Mercado Pago, checkout transparente: o checkout cria o pagamento direto em <c>POST /v1/payments</c>
/// (Pix ou boleto) e a loja mostra ao cliente o código Pix ou o boleto, sem redirecionar. O
/// resultado chega por webhook (<c>PagamentosController.WebhookMercadoPago</c>), que só avisa "o
/// pagamento X mudou"; o status confiável vem de <see cref="ConsultarPagamentoAsync"/>.
/// Sandbox = mesmas URLs com o access token de teste (<c>TEST-...</c>); não há host separado.
/// Integração via HTTP direto (API REST v1) em vez do SDK oficial: são só duas chamadas.
/// </summary>
public class ServicoDePagamentoMercadoPago(
    HttpClient http,
    IOptions<OpcoesPagamentos> opcoesPagamentos,
    IOptions<OpcoesAplicacao> opcoesAplicacao,
    TimeProvider relogio,
    ILogger<ServicoDePagamentoMercadoPago> logger) : IServicoDePagamento
{
    public const string UrlBase = "https://api.mercadopago.com/";

    /// <summary>Boleto emitido pelo Bradesco; o outro boleto do Mercado Pago é o "pec" (lotérica).</summary>
    internal const string MetodoBoleto = "bolbradesco";

    internal const string MetodoPix = "pix";

    /// <summary>ISO 8601 com milissegundos e fuso, como nos exemplos do Mercado Pago.</summary>
    private const string FormatoDeData = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";

    private OpcoesMercadoPago Opcoes => opcoesPagamentos.Value.MercadoPago;

    public async Task<CobrancaIniciada> IniciarCobrancaAsync(SolicitacaoDeCobranca solicitacao, CancellationToken ct = default)
    {
        var urlApi = opcoesAplicacao.Value.UrlPublicaApi.TrimEnd('/');
        var pagador = solicitacao.Pagador;
        var (nome, sobrenome) = SepararNome(pagador.Nome);
        var ehBoleto = solicitacao.Metodo == MetodoDePagamento.Boleto;

        var corpo = new PagamentoRequisicao(
            solicitacao.Valor,
            solicitacao.Descricao,
            ehBoleto ? MetodoBoleto : MetodoPix,
            solicitacao.PedidoId.ToString(),
            // notification_url precisa ser pública/HTTPS. Em dev (sem UrlPublicaApi) fica de fora e
            // vale o webhook cadastrado no painel do Mercado Pago, se houver.
            string.IsNullOrEmpty(urlApi) ? null : $"{urlApi}/api/v1/pagamentos/webhook/mercadopago",
            CalcularValidade(solicitacao.Metodo).ToString(FormatoDeData, CultureInfo.InvariantCulture),
            new PagadorRequisicao(
                pagador.Email,
                nome,
                sobrenome,
                new IdentificacaoRequisicao("CPF", pagador.Cpf),
                // O boleto exige o endereço do pagador; o Pix aceita, mas não precisa.
                ehBoleto
                    ? new EnderecoRequisicao(
                        new string(pagador.Endereco.Cep.Where(char.IsAsciiDigit).ToArray()),
                        pagador.Endereco.Logradouro,
                        pagador.Endereco.Numero,
                        pagador.Endereco.Bairro,
                        pagador.Endereco.Cidade,
                        pagador.Endereco.Uf)
                    : null));

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "v1/payments")
        {
            Content = JsonContent.Create(corpo),
        };
        AdicionarAutorizacao(requisicao);
        // Obrigatório em /v1/payments. Repetir a chamada (retry de rede) devolve o mesmo pagamento
        // em vez de criar outro para o mesmo pedido.
        requisicao.Headers.Add("X-Idempotency-Key", $"{solicitacao.PedidoId}:{solicitacao.Metodo}");

        using var resposta = await http.SendAsync(requisicao, ct);
        if (!resposta.IsSuccessStatusCode)
        {
            var erro = await resposta.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Mercado Pago recusou o {Metodo} do pedido {PedidoId}: {Status} {Corpo}",
                solicitacao.Metodo, solicitacao.PedidoId, (int)resposta.StatusCode, erro);
            throw new ExcecaoDeDominio(MensagemDeFalha(solicitacao.Metodo));
        }

        var criado = await resposta.Content.ReadFromJsonAsync<PagamentoCriadoResposta>(ct)
            ?? throw new InvalidOperationException("Resposta vazia do Mercado Pago ao criar o pagamento.");

        if (criado.Status is "rejected" or "cancelled")
        {
            logger.LogWarning(
                "Mercado Pago criou o {Metodo} do pedido {PedidoId} já como {Status} ({Detalhe})",
                solicitacao.Metodo, solicitacao.PedidoId, criado.Status, criado.StatusDetail);
            throw new ExcecaoDeDominio(MensagemDeFalha(solicitacao.Metodo));
        }

        var instrucoes = ehBoleto
            ? new InstrucoesDaCobranca(
                UrlPagamento: criado.TransactionDetails?.ExternalResourceUrl,
                // A documentação do boleto não lista este campo; quando vier, poupa abrir o PDF.
                LinhaDigitavel: criado.TransactionDetails?.DigitableLine,
                ExpiraEmUtc: criado.DateOfExpiration?.UtcDateTime)
            : new InstrucoesDaCobranca(
                UrlPagamento: criado.PointOfInteraction?.TransactionData?.TicketUrl,
                CodigoPix: criado.PointOfInteraction?.TransactionData?.QrCode,
                ExpiraEmUtc: criado.DateOfExpiration?.UtcDateTime);

        if (ehBoleto ? string.IsNullOrEmpty(instrucoes.UrlPagamento) : string.IsNullOrEmpty(instrucoes.CodigoPix))
        {
            // Sem isso o cliente não tem como pagar: melhor falhar o checkout (nada é gravado).
            logger.LogError("Mercado Pago não devolveu as instruções do {Metodo} do pedido {PedidoId} (pagamento {Id})", solicitacao.Metodo, solicitacao.PedidoId, criado.Id);
            throw new ExcecaoDeDominio(MensagemDeFalha(solicitacao.Metodo));
        }

        return new CobrancaIniciada(criado.Id.ToString(CultureInfo.InvariantCulture), instrucoes);
    }

    /// <summary>
    /// Limites do Mercado Pago: Pix de 30 min a 30 dias; boleto de 1 a 30 dias (o padrão deles é 3,
    /// porque a compensação leva até 2 dias úteis). Configuração fora disso é ajustada ao limite.
    /// </summary>
    internal DateTimeOffset CalcularValidade(MetodoDePagamento metodo)
    {
        var agora = relogio.GetUtcNow();
        return metodo == MetodoDePagamento.Boleto
            ? agora.AddDays(Math.Clamp(Opcoes.ValidadeDoBoletoEmDias, 1, 30))
            : agora.AddMinutes(Math.Clamp(Opcoes.ValidadeDoPixEmMinutos, 30, 30 * 24 * 60));
    }

    /// <summary>"Maria da Silva" vira ("Maria", "da Silva"); nome de uma palavra se repete no sobrenome.</summary>
    internal static (string Nome, string Sobrenome) SepararNome(string nomeCompleto)
    {
        var partes = nomeCompleto.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return partes.Length switch
        {
            0 => ("Cliente", "Cliente"),
            1 => (partes[0], partes[0]),
            _ => (partes[0], partes[1]),
        };
    }

    private static string MensagemDeFalha(MetodoDePagamento metodo) => metodo == MetodoDePagamento.Boleto
        ? "Não foi possível gerar o boleto. Confira seu CPF e o endereço de entrega e tente de novo."
        : "Não foi possível gerar o Pix. Confira seu CPF e tente de novo em instantes.";

    public async Task<PagamentoConsultado?> ConsultarPagamentoAsync(string idTransacao, CancellationToken ct = default)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, $"v1/payments/{Uri.EscapeDataString(idTransacao)}");
        AdicionarAutorizacao(requisicao);

        using var resposta = await http.SendAsync(requisicao, ct);
        if (resposta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        resposta.EnsureSuccessStatusCode();

        var pagamento = await resposta.Content.ReadFromJsonAsync<PagamentoResposta>(ct);
        if (pagamento is null || !Guid.TryParse(pagamento.ExternalReference, out var pedidoId))
        {
            // Pagamento que não foi gerado por este checkout (ex.: cobrança avulsa na conta).
            return null;
        }

        return new PagamentoConsultado(pedidoId, pagamento.Id.ToString(CultureInfo.InvariantCulture), TraduzirStatus(pagamento.Status));
    }

    /// <summary>
    /// Ver "status" em https://www.mercadopago.com.br/developers/pt/reference/payments/_payments_id/get.
    /// "cancelled" é o Pix ou boleto que venceu sem pagamento; "rejected" é recusa definitiva (no
    /// checkout transparente cada pagamento é único, sem nova tentativa nele). Os dois encerram o
    /// pedido e devolvem o estoque.
    /// </summary>
    internal static ResultadoDePagamento TraduzirStatus(string? status) => status switch
    {
        "approved" => ResultadoDePagamento.Aprovado,
        "cancelled" or "rejected" => ResultadoDePagamento.Recusado,
        "refunded" or "charged_back" => ResultadoDePagamento.Estornado,
        _ => ResultadoDePagamento.Pendente,
    };

    /// <summary>
    /// Valida o cabeçalho <c>x-signature</c> ("ts=...,v1=...") — HMAC-SHA256 do manifesto
    /// <c>id:{data.id};request-id:{x-request-id};ts:{ts};</c> com a assinatura secreta do webhook.
    /// Ver https://www.mercadopago.com.br/developers/pt/docs/your-integrations/notifications/webhooks.
    /// </summary>
    public static bool AssinaturaValida(string? cabecalhoAssinatura, string? idRequisicao, string? idDado, string chaveSecreta)
    {
        if (string.IsNullOrWhiteSpace(cabecalhoAssinatura) || string.IsNullOrWhiteSpace(chaveSecreta))
        {
            return false;
        }

        string? ts = null;
        string? v1 = null;
        foreach (var parte in cabecalhoAssinatura.Split(','))
        {
            var chaveValor = parte.Split('=', 2, StringSplitOptions.TrimEntries);
            if (chaveValor.Length != 2)
            {
                continue;
            }

            if (chaveValor[0] == "ts")
            {
                ts = chaveValor[1];
            }
            else if (chaveValor[0] == "v1")
            {
                v1 = chaveValor[1];
            }
        }

        if (ts is null || v1 is null)
        {
            return false;
        }

        // Partes ausentes saem do manifesto (regra da documentação); id alfanumérico vai em minúsculas.
        var manifesto = new StringBuilder();
        if (!string.IsNullOrEmpty(idDado))
        {
            manifesto.Append($"id:{idDado.ToLowerInvariant()};");
        }

        if (!string.IsNullOrEmpty(idRequisicao))
        {
            manifesto.Append($"request-id:{idRequisicao};");
        }

        manifesto.Append($"ts:{ts};");

        var esperado = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(chaveSecreta), Encoding.UTF8.GetBytes(manifesto.ToString())));

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperado), Encoding.UTF8.GetBytes(v1.ToLowerInvariant()));
    }

    private void AdicionarAutorizacao(HttpRequestMessage requisicao)
    {
        if (string.IsNullOrWhiteSpace(Opcoes.AccessToken))
        {
            throw new InvalidOperationException("Pagamentos:MercadoPago:AccessToken não configurado.");
        }

        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Opcoes.AccessToken);
    }

    private record PagamentoRequisicao(
        [property: JsonPropertyName("transaction_amount")] decimal Valor,
        [property: JsonPropertyName("description")] string Descricao,
        [property: JsonPropertyName("payment_method_id")] string Metodo,
        [property: JsonPropertyName("external_reference")] string ReferenciaExterna,
        [property: JsonPropertyName("notification_url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? UrlNotificacao,
        [property: JsonPropertyName("date_of_expiration")] string Validade,
        [property: JsonPropertyName("payer")] PagadorRequisicao Pagador);

    private record PagadorRequisicao(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("first_name")] string Nome,
        [property: JsonPropertyName("last_name")] string Sobrenome,
        [property: JsonPropertyName("identification")] IdentificacaoRequisicao Identificacao,
        [property: JsonPropertyName("address"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EnderecoRequisicao? Endereco);

    private record IdentificacaoRequisicao(
        [property: JsonPropertyName("type")] string Tipo,
        [property: JsonPropertyName("number")] string Numero);

    private record EnderecoRequisicao(
        [property: JsonPropertyName("zip_code")] string Cep,
        [property: JsonPropertyName("street_name")] string Logradouro,
        [property: JsonPropertyName("street_number")] string Numero,
        [property: JsonPropertyName("neighborhood")] string Bairro,
        [property: JsonPropertyName("city")] string Cidade,
        [property: JsonPropertyName("federal_unit")] string Uf);

    private record PagamentoCriadoResposta(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("status_detail")] string? StatusDetail,
        [property: JsonPropertyName("date_of_expiration")] DateTimeOffset? DateOfExpiration,
        [property: JsonPropertyName("point_of_interaction")] PontoDeInteracaoResposta? PointOfInteraction,
        [property: JsonPropertyName("transaction_details")] DetalhesDaTransacaoResposta? TransactionDetails);

    private record PontoDeInteracaoResposta(
        [property: JsonPropertyName("transaction_data")] DadosDaTransacaoResposta? TransactionData);

    private record DadosDaTransacaoResposta(
        [property: JsonPropertyName("qr_code")] string? QrCode,
        [property: JsonPropertyName("ticket_url")] string? TicketUrl);

    private record DetalhesDaTransacaoResposta(
        [property: JsonPropertyName("external_resource_url")] string? ExternalResourceUrl,
        [property: JsonPropertyName("digitable_line")] string? DigitableLine);

    private record PagamentoResposta(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("external_reference")] string? ExternalReference);
}

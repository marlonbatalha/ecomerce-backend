# ecomerce-backend

API backend de um e-commerce, em **.NET 10 / C#**, seguindo **Clean Architecture**. Consumida por um frontend em Next.js (repositório irmão `ecomerce-frontend`).

> 🚧 Em desenvolvimento — funcionalidades de negócio completas; falta validar contra o Oracle real e fazer o deploy.

## Tecnologias

- **.NET 10** + **ASP.NET Core Web API** (Controllers)
- **Clean Architecture** — camadas `Domain`, `Application`, `Infrastructure`, `Api`
- **Entity Framework Core** + provider **Oracle** (`Oracle.EntityFrameworkCore`)
- **ASP.NET Core Identity** + **JWT** (access/refresh token) para autenticação
- **FluentValidation** para validação de entrada
- **Swagger / OpenAPI** (Swashbuckle) para o contrato da API
- **Serilog** para logs estruturados
- **xUnit** + **FluentAssertions** para testes unitários; **WebApplicationFactory** + EF Core InMemory para testes de integração
- **Mercado Pago** (Checkout Pro) para pagamentos — Pix, cartão e boleto
- **AWSSDK.S3** para imagens em Object Storage S3-compatível (OCI Object Storage, S3, R2, MinIO)
- **MailKit** para e-mails transacionais via SMTP
- **Docker** para ambiente local e deploy
- **GitHub Actions** para CI/CD

## Arquitetura

```
src/
├─ Ecommerce.Api/              # Controllers, Program.cs, middlewares, DI, Swagger
├─ Ecommerce.Application/      # Casos de uso, DTOs, validators, interfaces
├─ Ecommerce.Domain/           # Entidades e regras de negócio puras (sem dependências externas)
└─ Ecommerce.Infrastructure/   # EF Core, integrações externas

tests/
├─ Ecommerce.UnitTests/
└─ Ecommerce.IntegrationTests/
```

Regra de dependência (Clean Architecture): sempre de fora pra dentro. `Domain` não depende de nada; `Application` depende só de `Domain`; `Infrastructure` e `Api` é que dependem das camadas internas — nunca o contrário.

## Funcionalidades

- Catálogo (categorias, produtos, variações, imagens)
- Carrinho de compras (autenticado e anônimo)
- Checkout, pedidos e integração com gateway de pagamento
- Cálculo de frete
- Conta do cliente (cadastro/login, endereços, histórico de pedidos)
- Painel administrativo (produto, estoque, pedidos)
- Cupons de desconto
- E-mails transacionais

## Rodando localmente

```bash
docker compose -f docker/docker-compose.yml up -d   # API + Oracle local
dotnet test                                          # suíte de testes
dotnet run --project src/Ecommerce.Api
```

Configurações sensíveis (connection strings, chaves de API, segredos) **não** ficam neste repositório — são lidas de variáveis de ambiente / `.env` local, fora do controle de versão.

## Status

- [x] Estrutura de camadas (Clean Architecture) + solution .NET 10
- [x] CI básico e Docker para ambiente local
- [x] Primeira entidade de domínio (`Categoria`), com testes
- [x] EF Core + Oracle com `Categoria`, Identity e refresh tokens mapeados
- [x] Autenticação (Identity + JWT — registro, login, refresh, logout)
- [x] Catálogo: categorias (árvore) e produtos/variantes (CRUD admin, busca pública paginada/filtrada)
- [x] Carrinho (autenticado ou anônimo) e Pedido (checkout, máquina de estados, webhook de pagamento)
- [x] Conta (endereços, perfil, troca de senha) e Admin (clientes, pedidos, auditoria de estoque)
- [x] Imagens de produto: upload, remoção e reordenação — disco local (dev) ou Object Storage S3-compatível
- [x] Cupons de desconto (percentual/valor fixo, validade, limite de usos, pedido mínimo)
- [x] Pagamento real com Mercado Pago (Checkout Pro + webhook assinado)
- [x] E-mails transacionais (boas-vindas, pedido recebido, pagamento confirmado, enviado, cancelado)
- [ ] Validação das migrations contra Oracle real + deploy

## Configuração dos provedores externos

Cada integração externa tem um provedor "de mentira" pra dev/testes, escolhido por configuração. Em
staging/produção os valores vêm de variáveis de ambiente (`Secao__Chave`), nunca do repositório.

| Seção | Dev (padrão) | Produção | Variáveis principais |
|---|---|---|---|
| `Pagamentos` | `Provedor=Fake` (confirma via `POST /api/v1/pagamentos/webhook` assinado com `Webhook__ChaveSecreta`) | `Provedor=MercadoPago` | `Pagamentos__MercadoPago__AccessToken`, `Pagamentos__MercadoPago__ChaveSecretaWebhook` |
| `Storage` | `Provedor=Local` (`wwwroot/uploads`) | `Provedor=S3` | `Storage__UrlPublica`, `Storage__S3__Endpoint`, `Storage__S3__Regiao`, `Storage__S3__Bucket`, `Storage__S3__ChaveDeAcesso`, `Storage__S3__ChaveSecreta` |
| `Email` | `Provedor=Log` (só registra no log) | `Provedor=Smtp` | `Email__Remetente`, `Email__Smtp__Host`, `Email__Smtp__Porta`, `Email__Smtp__Usuario`, `Email__Smtp__Senha` |
| `Aplicacao` | `UrlFrontend=http://localhost:3000` | URLs públicas | `Aplicacao__UrlFrontend`, `Aplicacao__UrlPublicaApi` (usada como `notification_url` do Mercado Pago) |

**Mercado Pago:** em *Suas integrações → Webhooks*, cadastre `{UrlPublicaApi}/api/v1/pagamentos/webhook/mercadopago`
com o evento *Pagamentos* e copie a "assinatura secreta" pra `ChaveSecretaWebhook`. Sem ela, todo webhook é recusado.

**OCI Object Storage:** endpoint S3 é `https://{namespace}.compat.objectstorage.{regiao}.oraclecloud.com`;
as chaves são uma *Customer Secret Key* do usuário. O bucket precisa de leitura pública (ou CDN na frente) — a
`UrlPublica` é algo como `https://objectstorage.{regiao}.oraclecloud.com/n/{namespace}/b/{bucket}/o`.

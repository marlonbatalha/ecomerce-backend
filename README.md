# ecomerce-backend

API backend de um e-commerce, em **.NET 10 / C#**, seguindo **Clean Architecture**. Consumida por um frontend em Next.js (repositório irmão `ecomerce-frontend`).

> 🚧 Em desenvolvimento — fase de fundação (estrutura de camadas e primeiras entidades de domínio).

## Tecnologias

- **.NET 10** + **ASP.NET Core Web API** (Controllers)
- **Clean Architecture** — camadas `Domain`, `Application`, `Infrastructure`, `Api`
- **Entity Framework Core** + provider **Oracle** (`Oracle.EntityFrameworkCore`)
- **ASP.NET Core Identity** + **JWT** (access/refresh token) para autenticação
- **FluentValidation** para validação de entrada
- **Swagger / OpenAPI** (Swashbuckle) para o contrato da API
- **Serilog** para logs estruturados
- **xUnit** + **FluentAssertions** para testes unitários; **Testcontainers** para testes de integração
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

## Funcionalidades previstas

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

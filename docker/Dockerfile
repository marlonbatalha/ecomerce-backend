# syntax=docker/dockerfile:1

# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia só os arquivos de projeto primeiro, para cache de `dotnet restore` entre builds.
COPY Ecommerce.slnx .
COPY src/Ecommerce.Api/Ecommerce.Api.csproj src/Ecommerce.Api/
COPY src/Ecommerce.Application/Ecommerce.Application.csproj src/Ecommerce.Application/
COPY src/Ecommerce.Domain/Ecommerce.Domain.csproj src/Ecommerce.Domain/
COPY src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj src/Ecommerce.Infrastructure/
RUN dotnet restore src/Ecommerce.Api/Ecommerce.Api.csproj

COPY src/ src/
RUN dotnet publish src/Ecommerce.Api/Ecommerce.Api.csproj -c Release -o /app --no-restore

# --- runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Usuário não-root (a imagem base já traz o usuário "app").
USER app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Ecommerce.Api.dll"]

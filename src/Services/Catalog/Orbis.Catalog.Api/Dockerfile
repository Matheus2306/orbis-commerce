# ETAPA 1: Compilacao
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copia primeiro o projeto para aproveitar o cache
COPY Orbis.Catalog.Api.csproj ./

# Restaura as dependencias NuGet
RUN dotnet restore Orbis.Catalog.Api.csproj

# Copia os demais arquivos
COPY . ./

# Publica a aplicacao
RUN dotnet publish Orbis.Catalog.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ETAPA 2: Execucao
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# Copia somente os arquivos publicados
COPY --from=build /app/publish ./

# Inicia a API
ENTRYPOINT ["dotnet", "Orbis.Catalog.Api.dll"]
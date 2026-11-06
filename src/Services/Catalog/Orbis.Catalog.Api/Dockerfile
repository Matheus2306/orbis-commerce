
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY BuildingBlocks/Orbis.Contracts/Orbis.Contracts.csproj \
    BuildingBlocks/Orbis.Contracts/

COPY Services/Catalog/Orbis.Catalog.Api/Orbis.Catalog.Api.csproj \
    Services/Catalog/Orbis.Catalog.Api/

RUN dotnet restore \
    Services/Catalog/Orbis.Catalog.Api/Orbis.Catalog.Api.csproj

COPY . .

RUN dotnet publish \
    Services/Catalog/Orbis.Catalog.Api/Orbis.Catalog.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Orbis.Catalog.Api.dll"]

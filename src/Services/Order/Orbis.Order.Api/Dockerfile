
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY BuildingBlocks/Orbis.Contracts/Orbis.Contracts.csproj \
    BuildingBlocks/Orbis.Contracts/

COPY Services/Order/Orbis.Order.Api/Orbis.Order.Api.csproj \
    Services/Order/Orbis.Order.Api/

RUN dotnet restore \
    Services/Order/Orbis.Order.Api/Orbis.Order.Api.csproj

COPY . .

RUN dotnet publish \
    Services/Order/Orbis.Order.Api/Orbis.Order.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Orbis.Order.Api.dll"]

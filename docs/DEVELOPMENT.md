# Ambiente de desenvolvimento

[Voltar ao README](../README.md)

Os comandos abaixo usam PowerShell a partir da raiz do repositório. O procedimento documenta a configuração existente; não pressupõe que os problemas listados no [roadmap](ROADMAP.md) estejam corrigidos.

## Ferramentas

Instale SDK .NET 10, Git e Docker Desktop com engine Linux/integração WSL 2 e Compose v2. Abra um novo terminal após a instalação para atualizar o `PATH`. Um editor como Visual Studio ou VS Code é opcional.

```powershell
dotnet --list-sdks
git --version
docker --version
docker compose version
docker info
dotnet tool install --global dotnet-ef --version 10.0.15
dotnet ef --version
```

Se `dotnet-ef` já estiver instalado, use `dotnet tool update --global dotnet-ef --version 10.0.15`. Essa versão corresponde à anotação EF do snapshot de Order; Catalog/Inventory têm snapshots 10.0.4. Não há `global.json` ou manifesto de ferramentas no repositório. Order usa referências `10.*`, portanto um restore posterior pode selecionar outros patches; revise a compatibilidade e os avisos, sem regenerar migrations apenas para ocultá-los.

## Configuração do Compose

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Edite `.env` e substitua os exemplos por valores locais próprios. Preserve um arquivo já existente. As duas variáveis obrigatórias são `POSTGRES_PASSWORD` e `RABBITMQ_PASSWORD`. O usuário configurado para PostgreSQL e RabbitMQ no Compose é `orbis`.

O [.env.example](../.env.example) contém somente as variáveis interpoladas pelo Compose. Não copie segredos para documentos, commits ou `appsettings.json`. Não publique a saída de `docker compose config` sem remover valores sensíveis; para validação sem exibir a configuração, use:

```powershell
docker compose config --quiet
docker compose up -d --wait postgres redis rabbitmq
docker compose ps
```

Portas de infraestrutura vinculadas a `127.0.0.1`: PostgreSQL 5432, Redis 6379, RabbitMQ AMQP 5672 e gerenciamento 15672. Os volumes `postgres_data`, `redis_data` e `rabbitmq_data` persistem os dados. Redis usa AOF (`--appendonly yes`).

## Variáveis para execução no host

ASP.NET Core aceita `__` como separador de configuração. Os `appsettings` versionados contêm logging e `AllowedHosts`, sem conexões. Os projetos têm suporte a User Secrets, mas seu conteúdo não faz parte desta documentação e não é necessário consultá-lo para seguir estes exemplos.

| Variável | Serviço | Exemplo sem segredo |
| --- | --- | --- |
| `ConnectionStrings__CatalogDb` | Catalog | `Host=localhost;Port=5432;Database=orbis_catalog;Username=orbis;Password=EXEMPLO` |
| `ConnectionStrings__InventoryDb` | Inventory | Mesmo formato, banco `orbis_inventory` |
| `ConnectionStrings__OrderDb` | Order | Mesmo formato, banco `orbis_orders` |
| `ConnectionStrings__Redis` | Catalog | `localhost:6379` |
| `RabbitMq__Host` | Todas | `localhost` no host; `rabbitmq` nos containers |
| `RabbitMq__Username` | Todas | `orbis` |
| `RabbitMq__Password` | Todas | Senha local do broker, sem valor real neste documento |
| `ASPNETCORE_ENVIRONMENT` | Todas | `Development` habilita OpenAPI/Scalar |
| `ASPNETCORE_HTTP_PORTS` | Containers | `8080`, já definido nos Dockerfiles/Compose |

Configure em cada terminal que executará `dotnet run` ou migrations. As entradas abaixo não exibem as senhas; use os mesmos valores configurados no `.env`. A conexão é montada por um builder para tratar caracteres especiais:

```powershell
$pgCredential = Get-Credential -UserName 'orbis' -Message 'Senha PostgreSQL do ambiente local'
$mqCredential = Get-Credential -UserName 'orbis' -Message 'Senha RabbitMQ do ambiente local'
$dbConnection = [System.Data.Common.DbConnectionStringBuilder]::new()
$dbConnection['Host'] = 'localhost'
$dbConnection['Port'] = 5432
$dbConnection['Username'] = 'orbis'
$dbConnection['Password'] = $pgCredential.GetNetworkCredential().Password
$dbConnection['Database'] = 'orbis_catalog'
$env:ConnectionStrings__CatalogDb = $dbConnection.ConnectionString
$dbConnection['Database'] = 'orbis_inventory'
$env:ConnectionStrings__InventoryDb = $dbConnection.ConnectionString
$dbConnection['Database'] = 'orbis_orders'
$env:ConnectionStrings__OrderDb = $dbConnection.ConnectionString
$env:ConnectionStrings__Redis = 'localhost:6379'
$env:RabbitMq__Host = 'localhost'
$env:RabbitMq__Username = 'orbis'
$env:RabbitMq__Password = $mqCredential.GetNetworkCredential().Password
$env:ASPNETCORE_ENVIRONMENT = 'Development'
```

Não imprima essas variáveis. Elas ficam disponíveis aos processos filhos do terminal. O `.env` do Compose não é importado por `dotnet run`. Os nomes de host `postgres`, `redis` e `rabbitmq` resolvem na rede dos containers, não no host Windows.

## Restore e build

```powershell
dotnet restore OrbisCommerce.slnx
dotnet build OrbisCommerce.slnx --no-restore
```

O build da solução inclui os testes de Inventory, que referenciam membros de enum inexistentes no estado inspecionado. Para verificar as APIs separadamente:

```powershell
dotnet build src/Services/Catalog/Orbis.Catalog.Api/Orbis.Catalog.Api.csproj
dotnet build src/Services/Inventory/Orbis.Inventory.Api/Orbis.Inventory.Api.csproj
dotnet build src/Services/Order/Orbis.Order.Api/Orbis.Order.Api.csproj
```

Esses comandos não equivalem à aprovação da solução completa. A documentação não modifica o código para contornar a divergência.

## Bancos e migrations

Nenhum `Program.cs` chama `Migrate`/`MigrateAsync`. Subir as APIs não prepara as tabelas. O container PostgreSQL inicializa apenas `orbis_catalog` em um volume novo. Após configurar as variáveis do host e iniciar a infraestrutura, aplique:

```powershell
dotnet ef database update --project src/Services/Catalog/Orbis.Catalog.Api --startup-project src/Services/Catalog/Orbis.Catalog.Api
dotnet ef database update --project src/Services/Inventory/Orbis.Inventory.Api --startup-project src/Services/Inventory/Orbis.Inventory.Api
dotnet ef database update --project src/Services/Order/Orbis.Order.Api --startup-project src/Services/Order/Orbis.Order.Api
```

O provider pode criar o banco inexistente se o usuário tiver permissão; o usuário inicial do container PostgreSQL possui esse privilégio. Se usar outro servidor/usuário sem permissão de criação, providencie previamente os três bancos com um administrador. Para o ambiente Compose, também é possível criar explicitamente os dois bancos ausentes (execute apenas se ainda não existirem):

```powershell
docker compose exec postgres createdb -U orbis orbis_inventory
docker compose exec postgres createdb -U orbis orbis_orders
```

| Serviço | Migrations versionadas, em ordem |
| --- | --- |
| Catalog | `20261104184326_InitialCatalog`; `20261109125355_AddTransactionalOutbox` |
| Inventory | `20261106175931_InitialInventory`; `20261109181353_AddInventoryConsumerOutbox`; `20261109185927_AddStockReservations`; `20261111165253_AddStockReservationEntity` |
| Order | `20261113115234_InitialOrders` |

`AddStockReservations` adiciona o saldo reservado e sua restrição; a tabela de reservas aparece somente em `AddStockReservationEntity`. Não há seed de produtos ou pedidos. A criação automática de estoque depende do evento de produto.

Para inspecionar migrations sem aplicar alterações:

```powershell
dotnet ef migrations list --project src/Services/Catalog/Orbis.Catalog.Api
dotnet ef migrations list --project src/Services/Inventory/Orbis.Inventory.Api
dotnet ef migrations list --project src/Services/Order/Orbis.Order.Api
```

## Execução das APIs

Abra três terminais na raiz, configure as variáveis em cada um e execute um comando por terminal:

```powershell
dotnet run --project src/Services/Catalog/Orbis.Catalog.Api --launch-profile http
dotnet run --project src/Services/Inventory/Orbis.Inventory.Api --launch-profile http
dotnet run --project src/Services/Order/Orbis.Order.Api --launch-profile http
```

Os perfis usam respectivamente 5141, 5227 e 5038. Inicie Inventory e aguarde a conexão do consumidor antes de publicar produtos pela primeira vez.

Para containers, após migrations:

```powershell
docker compose up -d --build
docker compose logs --tail 100 catalog-api inventory-api order-api
```

Os Dockerfiles usam `./src` como contexto, copiam o projeto de contratos, restauram e publicam em Release. Inventory tem arquivo `dockerfile` minúsculo, mas o Compose referencia `Dockerfile`. Se ocorrer erro de arquivo ausente, um caminho alternativo sem editar o Compose é:

```powershell
docker build -f src/Services/Inventory/Orbis.Inventory.Api/dockerfile -t orbis-inventory-api:1.0 ./src
docker compose build catalog-api order-api
docker compose up -d --no-build
```

Isso usa a tag já declarada no Compose. A correção definitiva de capitalização permanece no roadmap. Catalog depende apenas da saúde de PostgreSQL no Compose, embora também use Redis e RabbitMQ; iniciar a infraestrutura com `--wait` reduz essa corrida de inicialização.

Verificação HTTP por Compose:

```powershell
Invoke-RestMethod http://localhost:5080/health/live
Invoke-RestMethod http://localhost:5081/health/live
Invoke-RestMethod http://localhost:5082/health/live
```

Essas chamadas não validam dependências. Use os exemplos de [API.md](API.md) para verificar dados e fluxo de eventos. Para parar os containers preservando os volumes:

```powershell
docker compose down
```

Não acrescente `-v` se quiser preservar os dados. Alterar senhas no `.env` não atualiza automaticamente usuários já inicializados em volumes persistentes.

## Testes

```powershell
dotnet test tests/Orbis.Catalog.UnitTests/Orbis.Catalog.UnitTests.csproj
dotnet test tests/Orbis.Catalog.IntegrationTests/Orbis.Catalog.IntegrationTests.csproj
dotnet test tests/Orbis.Inventory.IntegrationTests/Orbis.Inventory.IntegrationTests.csproj
```

Veja [TESTING.md](TESTING.md) antes de interpretar os resultados, especialmente o bloqueio atual de Inventory.

## Troubleshooting

| Sintoma | Verificação fundamentada no projeto |
| --- | --- |
| Connection string não configurada | Defina a variável correspondente no mesmo terminal; os appsettings não fornecem conexões |
| Host `rabbitmq`/`postgres` não encontrado | Fora dos containers, use `localhost`; dentro, use os nomes dos serviços |
| Relação/tabela inexistente | Aplique migrations de cada API; não há migração automática no startup |
| Banco Inventory/Order inexistente | Prepare os bancos ou use EF com usuário que possa criá-los |
| Build de Inventory no Docker não encontra Dockerfile | Use o nome minúsculo real no comando de build explícito acima |
| Erro CS0117 nos testes Inventory | Corrigir futuramente as referências `invalidQuantity`/`insufficientStock`; filtrar testes não evita compilação |
| Pedido permanece pendente | O consumidor de `StockReservationRequested` não foi implementado |
| 409 ao criar estoque após cadastrar produto | O consumidor pode já ter criado saldo zero; consulte e use `/add` |
| Consulta de existência retorna vazio ou exige query | Limitações dos handlers documentadas em [API.md](API.md) |
| `/weatherforecast` retorna 404 | Arquivos `.http` são templates desatualizados; use as rotas documentadas |
| Scalar/OpenAPI não disponíveis | Mapeados apenas em `Development` |
| Liveness responde, mas operações falham | Liveness não verifica PostgreSQL, RabbitMQ ou Redis |
| Credenciais falham após editar `.env` | Confira credenciais persistidas no volume; não apague dados como primeira solução |

Na revisão documental de 13/11/2026, `dotnet --version` retornou `10.0.403`; `docker` não foi encontrado no terminal. Build, migrations, testes e containers não foram executados nesta tarefa. Os comandos foram conferidos contra os caminhos e configurações versionados.

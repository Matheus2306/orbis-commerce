# Testes

[Voltar ao README](../README.md)

## Projetos e ferramentas

Todos os projetos de teste usam `net10.0`, xUnit 2.9.3, runner Visual Studio 3.1.4 e Microsoft.NET.Test.Sdk 17.14.1.

| Projeto | Tipo e dependências | Escopo observado |
| --- | --- | --- |
| [Orbis.Catalog.UnitTests](../tests/Orbis.Catalog.UnitTests) | Unidade; Moq 4.20.72, fake em memória, Coverlet 6.0.4 | `ProductService` e `CachedProductReader` |
| [Orbis.Catalog.IntegrationTests](../tests/Orbis.Catalog.IntegrationTests) | Integração; Testcontainers.PostgreSql 4.16.0, Coverlet 6.0.4 | Repository e migrations em PostgreSQL 17 Alpine |
| [Orbis.Inventory.IntegrationTests](../tests/Orbis.Inventory.IntegrationTests) | Integração; SQLite EF 10.0.4, SQLitePCLRaw 3.0.5 e Testcontainers.PostgreSql 4.16.0 | Sobrecarga antiga de reserva e disputa concorrente em PostgreSQL |

Não existe projeto de testes de Order. A presença de Coverlet não implica relatório ou percentual de cobertura conhecido.

## Escopo observado

Catalog unitário: criação válida, dados inválidos, trim de nome/SKU, preço zero, duplicidade de SKU, consulta, contagem e mapeamento de campos. Mocks verificam a sequência de adicionar, publicar `ProductCreated` e salvar, além da tradução específica de violação de SKU e propagação de outras falhas.

`FakeProductRepository` mantém uma lista de produtos e um contador de chamadas a `SaveChangesAsync`. Não reproduz transações nem todas as regras do PostgreSQL. Moq substitui `IPublishEndpoint`, `IProductRepository`, `IProductService` e `IDistributedCache`; os testes de publicação não demonstram entrega real no RabbitMQ ou atomicidade real do outbox.

Os testes de cache verificam hit, miss, validade de cinco minutos, ausência de cache negativo, fallback em exceções selecionadas, cancelamento e invalidação de uma chave. Não iniciam Redis real.

Catalog integração: aplica migrations, verifica tabela consultável, persistência lida por outro contexto, índice único de SKU, existência e contagem. O fixture `IAsyncLifetime` cria e descarta PostgreSQL via Testcontainers; os testes têm categoria `Docker`.

Inventory SQLite: abre conexão em memória durante cada teste, usa `EnsureCreatedAsync` e exercita `ExecuteUpdateAsync`, saldo, produto ausente, quantidade inválida, reservas sucessivas, cancelamento e isolamento entre produtos. Isso não verifica migrations PostgreSQL.

Inventory concorrência: o cenário escrito dispara 20 solicitações de uma unidade sobre saldo 10, com contextos separados, esperando 10 reservas aceitas e 10 recusadas. Essa é a expectativa do teste, não um resultado de execução ou benchmark. O teste usa PostgreSQL via Testcontainers e `MigrateAsync`.

## Correção dos testes Inventory

As cinco referências inválidas ao enum foram corrigidas. Os testes usam `InsufficientStock`, inclusive para quantidade não positiva, conforme o comportamento existente da sobrecarga `ReserveAsync(productId, quantity)`. A lógica da aplicação não foi alterada.

Os endpoints usam a sobrecarga com `OrderId`, que retorna `ReservationResult`, persiste `StockReservation` e tem regras de idempotência. Os testes existentes não exercitam esse fluxo nem `ReleaseAsync`.

## Comandos

Execute da raiz com SDK .NET 10. A primeira execução pode restaurar pacotes NuGet.

```powershell
# Catalog unitário: não exige infraestrutura externa.
dotnet test tests/Orbis.Catalog.UnitTests/Orbis.Catalog.UnitTests.csproj

# Catalog integração: exige Docker Linux disponível.
dotnet test tests/Orbis.Catalog.IntegrationTests/Orbis.Catalog.IntegrationTests.csproj

# Inventory: SQLite sem Docker e PostgreSQL com Docker.
dotnet test tests/Orbis.Inventory.IntegrationTests/Orbis.Inventory.IntegrationTests.csproj --filter "Category!=Docker"
dotnet test tests/Orbis.Inventory.IntegrationTests/Orbis.Inventory.IntegrationTests.csproj --filter "Category=Docker"

# Solução: use Docker para executar também os testes PostgreSQL.
dotnet test OrbisCommerce.slnx --filter "Category!=Docker"
dotnet test OrbisCommerce.slnx --filter "Category=Docker"
dotnet test OrbisCommerce.slnx
```

O filtro seleciona casos a executar; não elimina a compilação dos projetos da solução. Testcontainers gerencia bancos descartáveis próprios e não depende dos bancos iniciados pelo Compose. É necessário acesso às imagens de container e ao daemon Docker.

Coleta opcional nos projetos de Catalog, onde `coverlet.collector` está declarado:

```powershell
dotnet test tests/Orbis.Catalog.UnitTests/Orbis.Catalog.UnitTests.csproj --collect:"XPlat Code Coverage"
dotnet test tests/Orbis.Catalog.IntegrationTests/Orbis.Catalog.IntegrationTests.csproj --collect:"XPlat Code Coverage"
```

Analise os arquivos gerados antes de publicar qualquer percentual. Não há threshold de cobertura configurado no repositório.

## Lacunas

- Testes HTTP dos handlers, incluindo binding das consultas de existência, códigos, cabeçalhos e serialização.
- Reserva por pedido, repetição, conflito de quantidade, corrida do mesmo par e liberação concorrente.
- Serviço Order, persistência de pedidos, eventos e transições de estado.
- Consumidor `ProductCreated`, retries, deduplicação, recuperação de falhas e entrega do outbox com RabbitMQ real.
- Integração com Redis real e comportamento com conteúdo de cache inválido.
- Fluxo completo entre serviços, incluindo o consumidor de reserva ainda ausente.
- Segurança, autorização, carga e desempenho; não há evidência de testes desses tipos.

## Evidência desta revisão

Em 16/11/2026, após corrigir os testes Inventory, o build da solução passou com zero erros e zero avisos. Os testes sem Docker passaram: 33 de Catalog e 13 de Inventory (46 no total). Docker não está disponível neste ambiente; testes PostgreSQL/Testcontainers e validação do Compose em execução continuam pendentes.

| Comando executado | Resultado |
| --- | --- |
| `dotnet build OrbisCommerce.slnx --no-restore --nologo -v minimal` | Êxito; zero avisos e zero erros |
| `dotnet test OrbisCommerce.slnx --no-build --no-restore --filter "Category!=Docker" --nologo -v minimal` | 33 testes Catalog e 13 Inventory aprovados; zero falhas |

SDK 10.0.403; pacotes previamente restaurados. Não foi validado restore limpo, cobertura ou desempenho. O projeto Catalog.IntegrationTests não possui casos que correspondam ao filtro sem Docker. Os testes PostgreSQL ficaram fora desta execução, não foram aprovados nem marcados como ignorados pelo runner. O workflow de CI existente executa a suíte completa, mas seu resultado remoto não foi verificado nesta revisão.

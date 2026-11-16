# Testes dos serviços

Consulte o [guia de testes](../docs/TESTING.md) para projetos, ferramentas, comandos e lacunas de cobertura.

Em 16/11/2026, após corrigir os testes Inventory, o build da solução passou com zero erros e zero avisos. Os testes sem Docker passaram: 33 de Catalog e 13 de Inventory (46 no total). Docker não está disponível neste ambiente; testes PostgreSQL/Testcontainers e validação do Compose em execução continuam pendentes. Consulte o guia de testes para comandos e limites da validação.

Para executar Catalog unitário separadamente, a partir da raiz:

```powershell
dotnet test tests/Orbis.Catalog.UnitTests/Orbis.Catalog.UnitTests.csproj
```

Os testes PostgreSQL usam Testcontainers e exigem Docker. Consulte também o [ambiente de desenvolvimento](../docs/DEVELOPMENT.md).

# Testes dos serviços

Consulte o [guia de testes](../docs/TESTING.md) para projetos, ferramentas, comandos e lacunas de cobertura.

O estado atual contém referências de enum desatualizadas nos testes de Inventory, que impedem compilar esse projeto. Os filtros por categoria não evitam a compilação. A documentação não declara testes aprovados.

Para executar Catalog unitário separadamente, a partir da raiz:

```powershell
dotnet test tests/Orbis.Catalog.UnitTests/Orbis.Catalog.UnitTests.csproj
```

Os testes PostgreSQL usam Testcontainers e exigem Docker. Consulte também o [ambiente de desenvolvimento](../docs/DEVELOPMENT.md).

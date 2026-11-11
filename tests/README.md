# Testes dos serviços

Requisito: SDK .NET 10.

Executar testes locais (Catalog com mocks e Inventory com SQLite em memória):

```powershell
dotnet test OrbisCommerce.slnx --filter "Category!=Docker"
```

Executar toda a suíte, incluindo PostgreSQL (Docker em execução):

```powershell
dotnet test OrbisCommerce.slnx
```

Executar somente os testes com PostgreSQL:

```powershell
dotnet test OrbisCommerce.slnx --filter "Category=Docker"
```

- Catalog: validação, normalização, consultas, publicação de ProductCreated antes do commit,
  conflito de SKU, propagação de falhas e leitura/invalidação de cache.
- Inventory: validação, produto ausente, saldo disponível, reservas sucessivas,
  persistência, cancelamento e isolamento entre produtos.
- Concorrência: 20 reservas disputam 10 unidades em PostgreSQL, usando contextos independentes;
  exatamente 10 devem ser aceitas, sem ultrapassar o saldo.

Os testes SQLite exercitam ExecuteUpdateAsync em um banco relacional descartável.
Eles não substituem a validação de migrações e concorrência no PostgreSQL.
Os testes com a categoria Docker criam e descartam seus próprios containers PostgreSQL.

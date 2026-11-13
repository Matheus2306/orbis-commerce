# Referência da API

[Voltar ao README](../README.md)

Inventário derivado de [ProductEndpoints](../src/Services/Catalog/Orbis.Catalog.Api/Endpoints/ProductEndpoints.cs), [StockEndpoints](../src/Services/Inventory/Orbis.Inventory.Api/Endpoints/StockEndpoints.cs), [OrderEndpoints](../src/Services/Order/Orbis.Order.Api/Endpoints/OrderEndpoints.cs) e dos respectivos `Program.cs`. Os exemplos são ilustrativos, com identificadores fictícios; não são respostas coletadas em execução.

## Convenções

| Serviço | Base Compose | Base local com perfil `http` |
| --- | --- | --- |
| Catalog | `http://localhost:5080` | `http://localhost:5141` |
| Inventory | `http://localhost:5081` | `http://localhost:5227` |
| Order | `http://localhost:5082` | `http://localhost:5038` |

Envie JSON com `Content-Type: application/json`. Não há autenticação ou autorização configurada. Os GUIDs das rotas com `:guid` precisam ser válidos; ausência de correspondência de rota resulta em 404. Erros de binding podem gerar 400 antes do handler. Os códigos abaixo descrevem os retornos explícitos; falhas não tratadas de infraestrutura podem produzir 500, sem formato de erro de negócio garantido.

## Catalog

| Método | Rota | Entrada | Retorno implementado |
| --- | --- | --- | --- |
| POST | `/api/products/` | `name: string`, `sku: string`, `price: decimal` | 201 produto e `Location`; 400 validação; 409 SKU duplicado |
| GET | `/api/products/` | Sem corpo | 200 array de produtos, ordenado por nome, sem paginação |
| GET | `/api/products/count` | Sem corpo | 200 `{"total": 0}` |
| GET | `/api/products/{id:guid}` | ID na rota | 200 produto; 404 sem corpo; usa cache |
| GET | `/api/products/sku/{sku}/exists` | Segmento na rota **e** `?sku=...` | 200 `{"exists": true}`; query ausente pode gerar 400 |
| GET | `/api/products/id/{id}/exists` | Segmento na rota **e** `?id=...` | Consulta executada, mas handler não retorna o resultado; resposta sem corpo (200 padrão); binding inválido/ausente pode gerar 400 |

As duas rotas de existência estão registradas, mas usam `[FromQuery]` para o valor que também aparece na rota. O valor consultado vem da query. A rota por ID está incompleta: calcula `exists` e não o devolve. Não deve ser usada como contrato funcional de verificação até ser corrigida.

O domínio remove espaços nas extremidades de nome e SKU e rejeita valores vazios e preço negativo. Preço zero é aceito. Limites de nome (150) e SKU (60) estão no mapeamento do banco, sem tradução específica para 400 quando ultrapassados. Não há normalização explícita de maiúsculas/minúsculas do SKU.

```http
POST http://localhost:5080/api/products/
Content-Type: application/json

{"name":"Teclado mecânico","sku":"ORB-TEC-001","price":249.90}
```

Exemplo de corpo para `201 Created` e `GET` por ID:

```json
{
  "id": "11111111-1111-4111-8111-111111111111",
  "name": "Teclado mecânico",
  "sku": "ORB-TEC-001",
  "price": 249.90,
  "createdAt": "2026-11-13T12:00:00Z"
}
```

O cabeçalho `Location` é `/api/products/{id}`. A listagem devolve um array desse formato, ou `[]`. Exemplos de consulta:

```http
GET http://localhost:5080/api/products/11111111-1111-4111-8111-111111111111

###
GET http://localhost:5080/api/products/count

###
GET http://localhost:5080/api/products/sku/ORB-TEC-001/exists?sku=ORB-TEC-001
```

Exemplos de erros: `400 {"error":"O preço não pode ser negativo."}` e `409 {"error":"SKU já cadastrado."}`.

## Inventory

| Método | Rota | Corpo JSON | Retorno implementado |
| --- | --- | --- | --- |
| POST | `/api/stock/` | `productId: guid`, `quantity: int`, `reservedQuantity: int` | 201 estoque e `Location`; 400 ID vazio/quantidade negativa; 409 produto com estoque existente |
| GET | `/api/stock/` | Sem corpo | 200 array de estoques, sem paginação nem ordenação explícita |
| GET | `/api/stock/{productId:guid}` | Sem corpo | 200 estoque; 404 sem corpo |
| POST | `/api/stock/{productId:guid}/add` | `quantity: int` | 200 confirmação; 400 quantidade não positiva; 404 estoque ausente; 409 limite de inteiro excedido |
| POST | `/api/stock/{productId:guid}/reserve` | `orderId: guid`, `quantity: int` | 200 criada ou existente; 400 identificadores vazios/quantidade não positiva; 404 estoque ausente; 409 saldo insuficiente ou reserva incompatível |
| POST | `/api/stock/{productId:guid}/release` | `orderId: guid` | 200 liberada ou já liberada; 400 identificadores vazios; 404 reserva ausente |

No cadastro manual, `reservedQuantity` negativo ou superior a `quantity` viola a restrição do banco, mas não é validado pelo construtor nem traduzido para 400 pelo endpoint. Isso pode resultar em erro não tratado. A criação manual não verifica Catalog e não cria registros em `stock_reservations` para um saldo inicialmente reservado.

O consumidor de `ProductCreated` já cria estoque zero. Nesse fluxo, aguarde o estoque existir e use `/add`; tentar cadastrá-lo novamente retorna 409.

```http
POST http://localhost:5081/api/stock/
Content-Type: application/json

{"productId":"11111111-1111-4111-8111-111111111111","quantity":10,"reservedQuantity":0}
```

Exemplo de corpo para criação (`201`) ou consulta (`200`):

```json
{
  "id": "22222222-2222-4222-8222-222222222222",
  "productId": "11111111-1111-4111-8111-111111111111",
  "quantity": 10,
  "reservedQuantity": 0,
  "avaliableQuantity": 10,
  "updatedAt": "2026-11-13T12:00:00Z"
}
```

O nome JSON `avaliableQuantity` reproduz a grafia atual da propriedade C#. `Location` usa o **ProductId**, não o ID interno do estoque. A listagem devolve um array desse formato.

```http
POST http://localhost:5081/api/stock/11111111-1111-4111-8111-111111111111/add
Content-Type: application/json

{"quantity":10}
```

Resposta `200`:

```json
{"message":"Entrada de estoque registrada.","quantityAdded":10}
```

Reserva manual, sem atualização automática do pedido:

```http
POST http://localhost:5081/api/stock/11111111-1111-4111-8111-111111111111/reserve
Content-Type: application/json

{"orderId":"33333333-3333-4333-8333-333333333333","quantity":2}
```

Resposta `200`:

```json
{
  "message": "Reserva criada.",
  "reservationId": "44444444-4444-4444-8444-444444444444",
  "orderId": "33333333-3333-4333-8333-333333333333",
  "productId": "11111111-1111-4111-8111-111111111111"
}
```

Repetir o mesmo par e quantidade com reserva ativa devolve `200` e `message: "Reserva já existente."`. Uma quantidade diferente ou a tentativa de reservar novamente o par já liberado devolve `409 {"error":"Reserva existente com dados ou estado incompatíveis."}`. Saldo insuficiente devolve `409 {"error":"Estoque insuficiente."}`.

```http
POST http://localhost:5081/api/stock/11111111-1111-4111-8111-111111111111/release
Content-Type: application/json

{"orderId":"33333333-3333-4333-8333-333333333333"}
```

Resposta `200 {"message":"Reserva liberada."}`; repetição retorna `200 {"message":"Reserva já estava liberada."}`. A liberação usa a quantidade persistida na reserva. Uma inconsistência de saldo gera exceção não tratada, sem commit da transação.

## Order

| Método | Rota | Entrada | Retorno implementado |
| --- | --- | --- | --- |
| POST | `/api/orders/` | `productId: guid`, `quantity: int` | 202 pedido e `Location`; 400 produto vazio ou quantidade não positiva |
| GET | `/api/orders/{id:guid}` | ID na rota | 200 pedido; 404 sem corpo |

```http
POST http://localhost:5082/api/orders/
Content-Type: application/json

{"productId":"11111111-1111-4111-8111-111111111111","quantity":2}
```

Exemplo de resposta `202 Accepted`, também usado na consulta `200`:

```json
{
  "id": "33333333-3333-4333-8333-333333333333",
  "productId": "11111111-1111-4111-8111-111111111111",
  "quantity": 2,
  "status": "PendingReservation",
  "createdAt": "2026-11-13T12:00:00Z",
  "updatedAt": "2026-11-13T12:00:00Z"
}
```

`Location` é `/api/orders/{id}`. O `202` significa que pedido e solicitação foram persistidos pelo fluxo de outbox, não que houve reserva. Não há consumidor de `StockReservationRequested`; consultar repetidamente não conclui o pedido.

```http
GET http://localhost:5082/api/orders/33333333-3333-4333-8333-333333333333
```

Erro de validação: `400 {"error":"Produto ou quantidade inválida."}`. Não há verificação de produto existente, quantidade disponível, preço ou idempotency key na criação.

## Infraestrutura HTTP e documentação

| Disponibilidade | Método e rota | Resposta/finalidade |
| --- | --- | --- |
| Todas as APIs | `GET /health/live` | 200 `{"status":"alive"}`; verifica apenas que o handler responde |
| Apenas Development | `GET /openapi/v1.json` | Documento OpenAPI gerado por `MapOpenApi()` |
| Apenas Development | `GET /scalar/v1` | Referência interativa pelo mapeamento padrão do Scalar |

O Compose define `Development` nas três APIs. Não há Swagger UI, readiness de dependências ou endpoint de métricas mapeado.

## Funcionalidades ausentes ou planejadas

Não existem rotas implementadas de edição/exclusão de produto, listagem/cancelamento de pedidos, autenticação, pagamento, carrinho ou frontend. Confirmação/rejeição de pedido existe somente como métodos de domínio, sem endpoint ou consumidor. As rotas futuras ainda não foram definidas; veja o [roadmap](ROADMAP.md).

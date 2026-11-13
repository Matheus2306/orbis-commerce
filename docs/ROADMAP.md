# Roadmap e prioridades

[Voltar ao README](../README.md)

Este documento separa código existente, integrações incompletas e propostas futuras. Não estabelece prazos nem promete entregas.

## Implementado e verificado por leitura do código

- [x] Três APIs .NET 10: Catalog, Inventory e Order, com contratos de eventos compartilhados.
- [x] Cadastro, consulta, listagem e contagem de produtos; unicidade de SKU no banco.
- [x] Cache-aside por ID com TTL de cinco minutos e fallback para falhas selecionadas.
- [x] Bus Outbox no Catalog e Order; Consumer Outbox configurado em Inventory.
- [x] Consumo de `ProductCreated` para criar estoque inicial zero.
- [x] Operações HTTP de estoque, reserva por pedido/produto e liberação transacional.
- [x] Criação/consulta de pedidos em `PendingReservation` e publicação de `StockReservationRequested`.
- [x] Migrations dos três serviços, Dockerfiles e Compose com PostgreSQL, Redis e RabbitMQ.
- [x] Projetos de testes Catalog e Inventory, com mocks, fake, SQLite e Testcontainers.
- [x] Documentação técnica e exemplo de variáveis sem segredos.

Itens marcados significam presença de implementação, não homologação ou aprovação da suíte.

## Em desenvolvimento ou parcialmente implementado

| Área | Presente | Falta concluir |
| --- | --- | --- |
| Pedido e reserva | Evento de solicitação, entidade e estados do pedido | Consumidor em Inventory, eventos de resultado e consumidores de retorno em Order |
| Transição de pedido | Métodos `ConfirmReservation`/`RejectReservation` | Integração desses métodos em fluxos de aplicação |
| Existência de produto | Rotas registradas por SKU e ID | Corrigir origem dos parâmetros; retornar resultado na rota por ID |
| Testes Inventory | Cenários SQLite/PostgreSQL | Atualizar enum, comportamento esperado e cobertura da reserva por pedido/liberação |
| Execução em containers | Imagens, portas e serviços definidos | Alinhar capitalização do Dockerfile Inventory e validar execução limpa |

## Prioridade 1 — publicação reproduzível

- [ ] Corrigir referências de enum nos testes Inventory e decidir a regra da sobrecarga antiga para quantidade inválida.
- [ ] Corrigir as consultas de existência e adicionar verificação dos contratos HTTP.
- [ ] Alinhar `Dockerfile`/`dockerfile` entre arquivo e Compose para checkouts sensíveis a maiúsculas.
- [ ] Atualizar os arquivos `.http`, que ainda chamam `/weatherforecast` inexistente.
- [ ] Executar restore/build e testes em ambiente com Docker; registrar apenas resultados observados.
- [ ] Validar migrations dos três serviços em bancos vazios e avaliar alinhamento de EF/Npgsql/MassTransit, incluindo referências `10.*` de Order.
- [ ] Validar os fluxos Catalog → Inventory e recuperação de entrega de eventos com infraestrutura real.
- [ ] Revisar arquivos e histórico antes de publicar para detectar segredos ou dados locais; definir uma licença somente por decisão do titular.

## Prioridade 2 — concluir o fluxo de negócio

- [ ] Implementar consumo de `StockReservationRequested` e contratos de resultado compatíveis.
- [ ] Processar confirmação/rejeição no Order e definir compensação/cancelamento.
- [ ] Testar idempotência e concorrência da reserva por pedido e liberação.
- [ ] Definir validação de produto/pedido entre serviços e tratamento de referências desconhecidas.
- [ ] Validar saldo reservado no cadastro manual e comprimentos de produto antes da persistência, com erros HTTP consistentes.
- [ ] Definir idempotência da criação de pedidos e política para repetição de entrada de estoque.
- [ ] Definir paginação e evolução dos contratos antes de ampliar o volume de dados.

## Melhorias futuras propostas

| Tema | Proposta, ainda não implementada |
| --- | --- |
| Autenticação/autorização | Definir identidade, perfis e permissões por operação |
| CI/CD | Pipeline de restore, build, testes, validação de migrations e imagens; publicação condicionada a evidências |
| Observabilidade | Readiness, logs correlacionados, métricas, tracing e acompanhamento de filas/outbox |
| Segurança | Segredos externos, menor privilégio, transporte protegido, revisão de dependências e limitação de requisições |
| Deploy | Definir plataforma, provisionamento, migrations controladas, backup e recuperação |
| Frontend | Cliente para demonstrar catálogo, estoque e estado dos pedidos |
| Robustez | Reconciliação de estoque ausente, tratamento de mensagens com falha e testes de indisponibilidade |

Não há frontend, deploy publicado, pipeline, autenticação ou métricas de desempenho comprovadas no estado documentado.

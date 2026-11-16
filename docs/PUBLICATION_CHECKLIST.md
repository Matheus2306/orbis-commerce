# Checklist de publicação

[Voltar ao README](../README.md)

Revisão local em **16/11/2026**, sobre o commit `557a751` e as alterações documentais/de exclusão desta revisão. Nenhuma publicação, alteração de credenciais, commit, push ou reescrita de histórico foi realizada. O Git estava limpo antes das alterações.

## Evidências disponíveis

| Verificação | Resultado e limite |
| --- | --- |
| Código e documentação | Revisados três serviços, contratos, endpoints, consumidores, configurações, migrations, testes e Dockerfiles; documentação correta preservada |
| Build | `dotnet build OrbisCommerce.slnx --no-restore --nologo -v minimal`: cinco erros CS0117 nos testes Inventory; contratos, três APIs e projetos de teste Catalog compilaram |
| Testes unitários Catalog | `dotnet test tests/Orbis.Catalog.UnitTests/Orbis.Catalog.UnitTests.csproj --no-build --no-restore --nologo -v minimal`: 33 aprovados, zero falhas, zero ignorados |
| Dependências | Pacotes já restaurados foram utilizados; restore limpo não validado |
| Docker/Compose | CLI ausente no PATH e no caminho padrão conferido; validação oficial de sintaxe e execução indisponíveis |
| Segredos nos arquivos atuais | Nenhum candidato detectado pela busca heurística nos 96 arquivos preparados (95 rastreados e o novo checklist); exemplos mantêm valores fictícios |
| Histórico local | 36 commits alcançáveis pelas referências locais; 190 blobs únicos examinados, sem candidatos detectados pelos padrões usados |
| Arquivos sensíveis rastreados | Nenhum `.env` real, arquivo de User Secrets, certificado/chave privada ou artefato de IDE/build/testes encontrado no inventário rastreado |
| Compose por inspeção | Senhas interpoladas de variáveis obrigatórias; portas vinculadas a `127.0.0.1`; bancos lógicos separados na mesma instância/usuário |
| Links e exemplos | 65 links relativos, 24 blocos PowerShell e cinco blocos JSON validados; caminhos de projetos conferidos; quatro diagramas Mermaid revisados estruturalmente, sem renderização |
| Exclusões | 19 caminhos representativos verificados com `git check-ignore --no-index`, incluindo preservação de `.env.example` e código-fonte; nenhum arquivo rastreado coincide com as regras de exclusão |
| Diff | `git diff --check` sem erros; apenas documentos e arquivos de exclusão alterados, sem mudanças no código, migrations, Compose ou credenciais |

A busca de segredos examinou padrões de chaves privadas, tokens de provedores, senhas literais em configuração/connection strings/chamadas C#, segredos literais e URIs com credenciais. Não exibiu valores nem acessou o conteúdo de User Secrets ou do `.env` local. É uma revisão heurística, não uma garantia de ausência de segredos: não cobre referências remotas não disponíveis, reflogs/objetos inalcançáveis, conteúdo codificado arbitrariamente ou segredos sem padrão reconhecível. Nenhuma credencial foi testada em serviço externo.

## Proteção dos arquivos

- [x] Ampliar `.gitignore` para variantes de `.env`, segredos locais, IDEs, logs, resultados de testes, temporários e chaves/certificados.
- [x] Preservar `.env.example` com somente valores fictícios e nomes exigidos pelo Compose.
- [x] Ampliar `src/.dockerignore`, efetivo para o contexto `./src` usado no Compose.
- [x] Alinhar os `.dockerignore` de Catalog e Inventory, sem alterar Dockerfiles ou código.
- [ ] Repetir a revisão de segredos após qualquer nova alteração e antes de tornar o repositório público.
- [ ] Revisar manualmente arquivos incluídos à força: ignore não protege arquivos já rastreados.
- [ ] Se surgir um segredo histórico, pausar a publicação, identificar caminho/commits sem divulgar o valor e revogar/rotacionar a credencial com o responsável. Planejar eventual saneamento do histórico separadamente, sem executá-lo automaticamente.

## Documentação e contratos

- [x] Separar fluxo implementado de reservas planejadas no README e na arquitetura.
- [x] Documentar sequência `ProductCreated` e criação de pedidos até a publicação de `StockReservationRequested`.
- [x] Distinguir bancos logicamente independentes de uma instância PostgreSQL compartilhada.
- [x] Documentar cache-aside, outbox, consistência eventual e limites de idempotência com base no código.
- [x] Manter inventário de rotas e exemplos com dados fictícios em [API.md](API.md).
- [x] Informar resultado real dos testes, sem badges de CI/cobertura, deploy ou licença inventados.
- [ ] Conferir a renderização Mermaid no preview GitHub antes da publicação; a revisão local foi estrutural, sem renderizador Mermaid.
- [ ] Definir licença por decisão do titular. Até lá, não apresentar o projeto como licenciado para uso open source.
- [ ] Configurar um canal de reporte privado, se desejado, e só então documentar sua disponibilidade.

## Bloqueios e limitações a resolver

| Prioridade | Pendência | Evidência/caminho |
| --- | --- | --- |
| Alta | Corrigir os cinco erros CS0117 antes de declarar build da solução aprovado | `tests/Orbis.Inventory.IntegrationTests/Application/StockReservationServiceTests.cs` e `StockReservationConcurrencyTests.cs` |
| Alta | Validar restore limpo, migrations e testes PostgreSQL em ambiente com Docker | [Guia de testes](TESTING.md) e [desenvolvimento](DEVELOPMENT.md) |
| Alta | Corrigir diferença entre `Dockerfile` no Compose e `dockerfile` versionado em Inventory para portabilidade | `compose.yaml`; `src/Services/Inventory/Orbis.Inventory.Api/dockerfile` |
| Média | Corrigir binding e retorno das rotas de existência | `src/Services/Catalog/Orbis.Catalog.Api/Endpoints/ProductEndpoints.cs` |
| Média | Atualizar os arquivos `.http` com `/weatherforecast` inexistente | Arquivos `.http` das três APIs |
| Evolução funcional | Implementar consumidor de reserva e retorno ao Order | `StockReservationRequested` existe, mas não há consumidor registrado |
| Antes de exposição pública das APIs | Autenticação, autorização, transporte protegido, menor privilégio e observabilidade | [Segurança](../SECURITY.md) e [roadmap](ROADMAP.md) |

O Compose atual atende a uma demonstração local: não há senha fixa identificada, mas existem `Development`, gerenciamento RabbitMQ e Redis sem autenticação. Catalog espera somente PostgreSQL no `depends_on`; o guia recomenda iniciar toda a infraestrutura com `--wait`. Nenhum desses pontos autoriza expor as APIs na internet sem trabalho adicional.

## Conferência final pelo responsável

Execute da raiz, depois de configurar as ferramentas e variáveis locais:

```powershell
git status --short
git diff --check
git diff --cached --check
git ls-files -ci --exclude-standard
docker compose config --quiet
```

O último comando valida a configuração sem imprimir valores interpolados. A saída de `git ls-files -ci --exclude-standard` deve ser analisada: ela identifica arquivos já rastreados que agora coincidem com regras de exclusão. Não os remova automaticamente.

- [ ] Executar os comandos de build/testes descritos em [TESTING.md](TESTING.md) após resolver bloqueios.
- [ ] Rever links relativos, caminhos, exemplos e diff final.
- [ ] Confirmar que não há novas credenciais, logs ou certificados no conjunto que será publicado.
- [ ] Escolher descrição e tópicos do GitHub sem prometer funcionalidades planejadas.
- [ ] Decidir explicitamente sobre publicação; esta tarefa não realizou nenhuma operação remota.

## Tópicos sugeridos para o GitHub

`dotnet`, `csharp`, `aspnet-core`, `minimal-api`, `microservices`, `postgresql`, `entity-framework-core`, `redis`, `rabbitmq`, `masstransit`, `transactional-outbox`, `docker-compose`, `xunit`, `testcontainers`, `ecommerce`, `backend`, `portfolio`.

Esses tópicos descrevem tecnologias e objetivos presentes no repositório; não indicam certificação, prontidão de produção ou integração completa de pedidos.

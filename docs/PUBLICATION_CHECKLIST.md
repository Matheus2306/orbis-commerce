# Checklist de publicação

[Voltar ao README](../README.md)

Revisão inicial em **16/11/2026** sobre o commit `557a751`; correção posterior dos testes e do caminho Docker sobre `87568b0`, preservando o trabalho existente. O Git estava limpo antes de cada etapa. A correção posterior foi autorizada para commit e push; não altera credenciais nem reescreve histórico.

## Evidências disponíveis

| Verificação | Resultado e limite |
| --- | --- |
| Código e documentação | Revisados três serviços, contratos, endpoints, consumidores, configurações, migrations, testes e Dockerfiles; documentação correta preservada |
| Build | `dotnet build OrbisCommerce.slnx --no-restore --nologo -v minimal`: êxito, zero erros e zero avisos após a correção |
| Testes sem Docker | `dotnet test OrbisCommerce.slnx --no-build --no-restore --filter "Category!=Docker" --nologo -v minimal`: 33 Catalog e 13 Inventory aprovados, zero falhas |
| Dependências | Pacotes já restaurados foram utilizados; restore limpo não validado |
| Docker/Compose | CLI ausente no PATH e no caminho padrão conferido; validação oficial de sintaxe e execução indisponíveis |
| Segredos nos arquivos atuais | Nenhum candidato detectado pela busca heurística nos 96 arquivos preparados (95 rastreados e o novo checklist); exemplos mantêm valores fictícios |
| Histórico local | 36 commits alcançáveis pelas referências locais; 190 blobs únicos examinados, sem candidatos detectados pelos padrões usados |
| Arquivos sensíveis rastreados | Nenhum `.env` real, arquivo de User Secrets, certificado/chave privada ou artefato de IDE/build/testes encontrado no inventário rastreado |
| Compose por inspeção | Senhas interpoladas de variáveis obrigatórias; portas vinculadas a `127.0.0.1`; bancos lógicos separados na mesma instância/usuário |
| Links e exemplos | 65 links relativos, 24 blocos PowerShell e cinco blocos JSON validados; caminhos de projetos conferidos; quatro diagramas Mermaid revisados estruturalmente, sem renderização |
| Exclusões | 19 caminhos representativos verificados com `git check-ignore --no-index`, incluindo preservação de `.env.example` e código-fonte; nenhum arquivo rastreado coincide com as regras de exclusão |
| Diff | Correção posterior limitada a dois arquivos de testes, caminho do Dockerfile no Compose e documentação; lógica da aplicação e migrations preservadas |

A busca de segredos da revisão inicial examinou padrões de chaves privadas, tokens de provedores, senhas literais em configuração/connection strings/chamadas C#, segredos literais e URIs com credenciais. Não exibiu valores nem acessou o conteúdo de User Secrets ou do `.env` local. É uma revisão heurística, não uma garantia de ausência de segredos: não cobre referências remotas não disponíveis, reflogs/objetos inalcançáveis, conteúdo codificado arbitrariamente ou segredos sem padrão reconhecível. Nenhuma credencial foi testada em serviço externo.

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
| Alta | Validar restore limpo, migrations e testes PostgreSQL em ambiente com Docker | [Guia de testes](TESTING.md) e [desenvolvimento](DEVELOPMENT.md) |
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

- [x] Executar build e testes sem Docker após corrigir as referências de enum.
- [ ] Executar testes PostgreSQL/Testcontainers e validar Compose em ambiente com Docker.
- [ ] Rever links relativos, caminhos, exemplos e diff final.
- [ ] Confirmar que não há novas credenciais, logs ou certificados no conjunto que será publicado.
- [ ] Escolher descrição e tópicos do GitHub sem prometer funcionalidades planejadas.
- [ ] Decidir sobre visibilidade pública e licença; o envio das correções ao remoto não valida os requisitos de produção.

## Tópicos sugeridos para o GitHub

`dotnet`, `csharp`, `aspnet-core`, `minimal-api`, `microservices`, `postgresql`, `entity-framework-core`, `redis`, `rabbitmq`, `masstransit`, `transactional-outbox`, `docker-compose`, `xunit`, `testcontainers`, `ecommerce`, `backend`, `portfolio`.

Esses tópicos descrevem tecnologias e objetivos presentes no repositório; não indicam certificação, prontidão de produção ou integração completa de pedidos.

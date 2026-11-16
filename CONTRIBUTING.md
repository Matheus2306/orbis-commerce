# Contribuição

## Preparar o ambiente

Leia o [README](README.md), a [arquitetura](docs/ARCHITECTURE.md) e o [guia de desenvolvimento](docs/DEVELOPMENT.md). Use SDK .NET 10 e Docker Linux para testes PostgreSQL. Configure as variáveis locais com base em [.env.example](.env.example), prepare os três bancos e aplique migrations quando a contribuição exigir execução das APIs.

Consulte as [limitações conhecidas](docs/ROADMAP.md) antes de atribuir uma falha à sua alteração. Não inclua segredos, dados reais, conteúdo de User Secrets ou resultados de testes não executados.

## Branches e commits

Como orientação para novas contribuições, use branches descritivas, por exemplo `feature/reserva-pedido`, `fix/consulta-produto` ou `docs/arquitetura`. Não há validação automatizada dessas convenções no repositório.

Prefira commits pequenos e focados com mensagens que expliquem a mudança. Prefixos como `feat:`, `fix:`, `test:` e `docs:` podem facilitar a leitura; esta é uma convenção sugerida, não uma regra já aplicada por ferramenta. Não misture refatorações amplas com correções pontuais sem justificar a necessidade.

## Requisitos para pull requests

- Descreva o problema, a mudança de comportamento e como verificá-la.
- Mantenha o escopo focado e atualize a documentação se mudar rotas, configuração, persistência ou eventos.
- Informe comandos executados e resultados reais; declare testes não executados e o motivo.
- Em alterações de banco, inclua migrations e snapshot coerentes e descreva efeitos sobre dados existentes.
- Em alterações de eventos, explique compatibilidade entre publicadores e consumidores.
- Inclua testes relevantes para novas regras ou correções; para documentação, valide exemplos, links e consistência com o código.
- Revise o diff para excluir arquivos locais, credenciais e mudanças não relacionadas.

Há um workflow de CI em `.github/workflows/ci.yml` para build e testes. Não foi verificado se existem regras remotas de proteção de branch ou checks obrigatórios. Não há template de PR versionado.

## Verificações antes de contribuir

Da raiz:

```powershell
dotnet restore OrbisCommerce.slnx
dotnet build OrbisCommerce.slnx --no-restore
dotnet test tests/Orbis.Catalog.UnitTests/Orbis.Catalog.UnitTests.csproj
dotnet test OrbisCommerce.slnx --filter "Category!=Docker"
dotnet test OrbisCommerce.slnx --filter "Category=Docker"
git diff --check
git status --short
```

O build e os 46 testes sem Docker passaram na revisão de 16/11/2026. Execute também os testes com Docker antes de declarar a suíte completa aprovada. Veja [TESTING.md](docs/TESTING.md).

Para mudanças exclusivamente documentais, confira caminhos, blocos de código, diagramas Mermaid e exemplos sem segredos. Não é necessário alterar o código-fonte para fazer o texto parecer executável; documente os limites existentes. Antes de publicar, percorra o [checklist de publicação](docs/PUBLICATION_CHECKLIST.md); não inclua arquivos ignorados à força sem revisar seu conteúdo.

Para possíveis vulnerabilidades, siga [SECURITY.md](SECURITY.md) e evite detalhes sensíveis em discussões públicas.

# Segurança

Orbis Commerce é um projeto educacional e de portfólio em desenvolvimento. As APIs atuais não implementam autenticação ou autorização. O Compose é destinado ao ambiente local e não constitui uma configuração de produção.

## Relato responsável de vulnerabilidades

Antes de divulgar detalhes exploráveis, verifique se o repositório no GitHub oferece o recurso privado de reporte de vulnerabilidades na aba Security. Se estiver habilitado, utilize-o. Esta documentação não afirma que esse canal esteja configurado.

Se não houver canal privado disponível, use apenas um contato efetivamente publicado pelo responsável. Não publique credenciais, dados pessoais, conteúdo de bancos ou instruções de exploração em uma issue pública. Se precisar solicitar um canal, limite a mensagem pública ao pedido de contato privado, sem detalhes sensíveis.

Um relato útil inclui versão ou commit analisado, componente afetado, pré-condições, impacto e passos mínimos de reprodução em ambiente próprio com dados fictícios. Diferencie evidência observada de hipótese e remova segredos de logs e capturas. Não teste instalações de terceiros sem autorização.

Não há política de versões suportadas, prazo de resposta, programa de recompensa ou compromisso de suporte definido neste repositório.

## Segredos e desenvolvimento

- Use [.env.example](.env.example) apenas como modelo; substitua os valores fictícios no `.env` local, ignorado pelo Git.
- Não versione senhas, tokens, connection strings reais, conteúdo de User Secrets, dumps ou arquivos de configuração particulares.
- Não publique saídas de comandos que resolvam a configuração completa, variáveis de processo ou logs com dados sensíveis.
- Se um segredo for exposto, revogue ou rotacione a credencial e avalie sua presença no histórico; apagar somente o arquivo atual não elimina a exposição anterior.
- Use dados fictícios. As portas do Compose estão vinculadas ao loopback, mas alterações de rede ou publicação externa exigem revisão explícita.

O `.gitignore` cobre variantes de `.env` (preservando `.env.example`), `secrets.json`, diretórios de User Secrets, arquivos de IDE, binários, logs, resultados de testes, temporários e formatos de chaves/certificados privados. User Secrets permanece fora do repositório por padrão; não copie seu conteúdo para a árvore de trabalho.

O contexto de build usado pelo Compose é `src/`, portanto o arquivo efetivo de exclusão é [src/.dockerignore](src/.dockerignore). Os arquivos `.dockerignore` nas pastas de Catalog e Inventory foram alinhados como proteção adicional para outros contextos; eles não se acumulam automaticamente ao arquivo da raiz do contexto.

Regras de exclusão não removem arquivos já rastreados nem dados do histórico e podem ser contornadas por inclusão forçada. Confira o índice, os diffs e o histórico antes de publicar. O escopo e os limites da varredura desta revisão estão no [checklist de publicação](docs/PUBLICATION_CHECKLIST.md).

## Limites do ambiente atual

O Compose compartilha um usuário de PostgreSQL entre os bancos, habilita `Development` nas APIs e disponibiliza gerenciamento RabbitMQ local. Redis não tem autenticação configurada no Compose. Os serviços não têm configuração de TLS, autorização, rate limiting ou auditoria de acesso implementada no código inspecionado.

As verificações de liveness não atestam a segurança ou a disponibilidade das dependências. Antes de qualquer implantação exposta, defina controles de acesso, segredos, privilégios de banco, proteção de transporte, backup, observabilidade e atualização de dependências. Essas medidas são trabalho futuro, descrito no [roadmap](docs/ROADMAP.md).

Consulte também o [guia de desenvolvimento](docs/DEVELOPMENT.md) e as [orientações de contribuição](CONTRIBUTING.md).

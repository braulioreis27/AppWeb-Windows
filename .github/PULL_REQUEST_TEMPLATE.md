
## Resumo

<!-- O que muda e por quê? 2–5 linhas. -->

## Tipo de mudança

- [ ] `feat` — nova funcionalidade
- [ ] `fix` — correção
- [ ] `fix(security)` — correção de segurança
- [ ] `refactor` — refatoração sem mudança funcional
- [ ] `perf` — performance
- [ ] `test` — testes
- [ ] `build` / `ci` — build, dependências ou automação
- [ ] `docs` — documentação
- [ ] `chore` — manutenção

## Issue relacionada

Closes #

## O que mudou

<!-- Liste as alterações principais. -->

## Como testar

1.
2.
3.

## Evidências

<!-- Screenshots, GIF, logs redigidos ou saída de testes. -->

## Checklist de segurança

- [ ] Validei entradas de URL, esquema, host e porta; requisições de rede possuem proteção SSRF.
- [ ] Validei caminhos de arquivo contra traversal, symlink/junction escape e reserved names do Windows.
- [ ] Operações de exclusão/escrita estão restritas a raízes conhecidas e possuem testes negativos.
- [ ] Processos externos usam lista de argumentos, sem interpolação de shell.
- [ ] Alterações em WebView2 revisam permissões, popups, downloads, host objects e `file:`.
- [ ] Não introduzi secrets, tokens, certificados, dados pessoais ou logs sensíveis.
- [ ] Revisei novas dependências e não há vulnerabilidades conhecidas relevantes.
- [ ] Atualizei o threat model/documentação de segurança quando aplicável.
- [ ] Adicionei testes de regressão para o comportamento de segurança alterado.

## Checklist de qualidade

- [ ] `dotnet build AppWeb.sln -c Release` passa sem warnings novos.
- [ ] `dotnet test tests\AppWeb.Tests\AppWeb.Tests.csproj -c Release` passa.
- [ ] Testei manualmente em Windows 11 x64.
- [ ] Testei upgrade/instalação/uninstall quando o PR altera instalador ou paths.
- [ ] Não há mudanças não relacionadas no diff.
- [ ] Documentação e notas de release foram atualizadas.

## Breaking changes

<!-- Descreva impacto, migração e rollback. Se não houver, escreva "Nenhum". -->

## Notas de release

<!-- Texto curto para o changelog, se necessário. -->

## Segurança — impacto declarado

- [ ] Nenhum impacto de segurança identificado; justificativa:
 [ ] Impacto baixo/médio/alto; revisão de Code Owner de segurança solicitada.

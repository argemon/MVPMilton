# 02 — Definition of Done

## Uso

Aplicar estes itens a cada bloco de implementação, mais a lista específica do módulo. Item não aplicável deve trazer justificativa concreta. Documento escrito não comprova código executado.

## Escopo e rastreio

- [ ] RF/RN/RNF e CA do bloco identificados, com resultados observados.
- [ ] Somente escopo liberado implementado; exclusões respeitadas.
- [ ] Hipóteses novas registradas e decisões propagadas aos documentos afetados.
- [ ] Nenhum bloqueio P0/P1 do bloco permanece sem solução.

## Arquitetura e integridade

- [ ] Referências de camada e separação física verificadas.
- [ ] DTOs de negócio e conteúdo ao navegador não expõem entidade, senha, token ou campos sem autorização. Credencial de login/sessão é transporte exclusivo servidor-servidor no contrato de autenticação, nunca Shared Contracts renderizável, log ou estado Blazor serializado.
- [ ] Dinheiro/quantidade/tempo usam tipos e limites normativos.
- [ ] Migrations criam do zero e atualizam base anterior em PostgreSQL.
- [ ] Constraints, índices, versão, transação, idempotência e histórico testados quando aplicáveis.
- [ ] Saldo/razão e baixa/estorno conciliam após comandos do bloco.

## Segurança e interface

- [ ] API testa anônimo, perfil permitido, negado e propriedade quando existir.
- [ ] Sessões e autorização não dependem exclusivamente do estado do circuito.
- [ ] Sem credencial no Git, logs, URL, HTML ou armazenamento do navegador.
- [ ] Formulários pt-BR com validação, carregamento, vazio, erro, conflito e sessão expirada.
- [ ] Operável por teclado e nos dois tamanhos de tela definidos.

## Validação e entrega

- [ ] Restore/build Release/testes realmente executados e resultados registrados.
- [ ] Integração usa PostgreSQL real; testes não ignorados silenciosamente.
- [ ] OpenAPI, cliente HTTP e documentação refletem contrato final.
- [ ] Arquivos alterados e evidências constam no estado por bloco.
- [ ] Próximo bloco e pré-requisitos explícitos; funcionalidades parciais não declaradas prontas.

## Registro mínimo de evidência

Para cada gate: `ID do cenário | ambiente/versões | comando ou roteiro | esperado | observado | data | referência da evidência`. Para concorrência, informar conexões independentes e mecanismo de sincronização; para restore, registrar backup escolhido e tempo medido. Não registrar segredos nos anexos.

## DoD desta entrega documental

Conferir os 35 caminhos obrigatórios e índice adicional, links relativos, IDs rastreáveis, consistência entre banco/API/segurança/módulos, hipóteses identificadas e ausência de código de produção. Registrar apenas essas verificações no estado inicial.


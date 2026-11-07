# 02 — Definition of Done

> Atualização vigente00 — ADR-27–29: implementação autorizada com Atlas já configurado em .env, sem sobrescrever segredos. Development reutiliza MONGODB_URI; Production/Staging exigem configuração externa específica. Integração pode executar transações reais em bancos efêmeros exclusivos no mesmo cluster ou perfil Testcontainers explícito; não criar outro MongoDB quando Atlas atende. rs0 é nome apenas do perfil local, não do Atlas. As exigências universais de Docker/rs0, somente Markdown e adaptação ainda não autorizada nas seções históricas anteriores ficam superadas nesses pontos.


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
- [ ] Inicializador de coleções/índices/validadores cria banco vazio, reexecuta idempotentemente e evolui versão anterior MongoDB com dados preservados.
- [ ] Validadores e filtros condicionais, índices, versão, transação, idempotência e histórico testados quando aplicáveis.
- [ ] Saldo/razão e baixa/estorno conciliam após comandos do bloco.

## Segurança e interface

- [ ] API testa anônimo, perfil permitido, negado e propriedade quando existir.
- [ ] Sessões e autorização não dependem exclusivamente do estado do circuito.
- [ ] Sem credencial no Git, logs, URL, HTML ou armazenamento do navegador.
- [ ] Formulários pt-BR com validação, carregamento, vazio, erro, conflito e sessão expirada.
- [ ] Operável por teclado e nos dois tamanhos de tela definidos.

## Validação e entrega

- [ ] Restore/build Release/testes realmente executados e resultados registrados.
- [ ] Integração usa MongoDB real isolado transacional via Atlas ou perfil explícito Testcontainers (ADR-28); testes não ignorados silenciosamente.
- [ ] OpenAPI, cliente HTTP e documentação refletem contrato final.
- [ ] Arquivos alterados e evidências constam no estado por bloco.
- [ ] Próximo bloco e pré-requisitos explícitos; funcionalidades parciais não declaradas prontas.

## Registro mínimo de evidência

Para cada gate: `ID do cenário | ambiente/versões | comando ou roteiro | esperado | observado | data | referência da evidência`. Para concorrência, informar sessões independentes e mecanismo de sincronização; para restore, registrar backup escolhido e tempo medido. Não registrar segredos nos anexos.

## DoD desta entrega documental

Inventariar todos os Markdown atuais, sem reutilizar contagem fixa da entrega inicial; conferir links relativos, IDs rastreáveis, consistência entre banco/API/segurança/módulos, hipóteses e ausência de alteração de código/infraestrutura nesta revisão. Registrar verificações realmente executadas no estado e no relatório de auditoria. Aprovação documental não comprova execução da nova stack; incompatibilidade do artefato Compose/hosts é registrada separadamente e pode bloquear a aprovação global solicitada.

## Complemento MongoDB (ADR-20–25)

- [ ] Confirmar primary/replica set, readiness e volumes; standalone não é evidência.
- [ ] Verificar BSON/driver restritos à infraestrutura, IMongoClient singleton e sessão curta/sequencial por operação.
- [ ] Testar centavos Int64, conversão HTTP/CSV em reais, escala, limites, overflow e média final.
- [ ] Provar filtros condicionais/resultado, referências contra write skew, idempotência e resolução de commit incerto.
- [ ] Provar RBAC de banco/coleção, sem alteração de histórico/DDL/validação ou leitura cruzada por runtime.
- [ ] Invalidar evidências incompatíveis da stack anterior e registrar diferenças sem apagar histórico.

# 01 — Dependências

> Atualização vigente00 — ADR-27–29: implementação autorizada com Atlas já configurado em .env, sem sobrescrever segredos. Development reutiliza MONGODB_URI; Production/Staging exigem configuração externa específica. Integração pode executar transações reais em bancos efêmeros exclusivos no mesmo cluster ou perfil Testcontainers explícito; não criar outro MongoDB quando Atlas atende. rs0 é nome apenas do perfil local, não do Atlas. As exigências universais de Docker/rs0, somente Markdown e adaptação ainda não autorizada nas seções históricas anteriores ficam superadas nesses pontos.


## Grafo e ordem

```mermaid
flowchart LR
    B00[00 Fundação] --> B01[01 Autenticação]
    B01 --> B02[02 Funcionários]
    B02 --> B03[03 Clientes]
    B03 --> B04[04 Produtos]
    B04 --> B05[05 Estoque]
    B05 --> B06[06 Vendas]
    B03 --> B06
    B06 --> B07[07 Dashboard]
    B07 --> B08[08 Relatórios]
    B08 --> B09[09 Homologação]
    B09 --> B10[10 Publicação]
    B10 -. novo escopo .-> B11[11 Catálogo e pedidos]
```

A cadeia impõe a ordem administrativa mesmo quando a dependência técnica direta não existe. Módulo não usa recurso futuro para passar seu gate.

## Dependências materiais

| Bloco | Reutiliza | Acrescenta | Condição externa |
| --- | --- | --- | --- |
| 00 | Documentação | Solução, adaptadores de persistência, baseline, Compose | SDK .NET10, Docker, MongoDB.Driver compatível e replica set primary |
| 01 | API/Web/DB e contratos | Identity, sessões, policies, bootstrap | Segredo local seguro e certificados |
| 02 | Identity/contas | Funcionário e referência UUID com índice unique parcial de usuário | Nenhuma |
| 03 | Policies e auditoria | Cliente e histórico vazio | Q-01 pode revisar dados mínimos |
| 04 | Policies | Produto/categoria/marca, imagem, saldo zero | Volume de imagens; hipótese UN |
| 05 | Produto/saldo | Movimento manual e idempotência | MongoDB real em replica set/Testcontainers para disputa |
| 06 | Cliente/produto/saldo/movimento | Venda/item e baixa/estorno | Hipóteses comerciais confirmadas na validação |
| 07 | Venda/estoque | Projeções de indicadores | Fuso configurado |
| 08 | Fórmulas e filtros | Projeções/CSV | Nenhuma |
| 09 | Todos os fluxos | Relatório de evidências e correções | Recursos de teste/carga |
| 10 | Homologação aprovada | Ambiente publicado e recuperação | Q-02, Q-03 para dados reais |
| 11 | MVP publicado | Escopo a definir | Q-04 e autorização de início |

Revisão documental: módulo11 agora contém recorte mínimo detalhado como proposta ADR-18, com RF-21–24/RN-21–24. A proposta ainda precisa aceite/ajuste em Q-04 e não modifica a sequência MVP. Consulte [a matriz](06-MATRIZ-MODULOS.md) para requests, páginas e testes por bloco e a análise de ausência de dependências futuras.

## Gestão de impedimentos de execução

Falta de Docker impede somente o perfil Testcontainers; Atlas real isolado comprova transações conforme ADR-28. Sem ambos os caminhos, a comprovação transacional fica bloqueada. Registrar limitação sem marcar integração como aprovada. Falta de domínio/host não bloqueia implementação local, mas bloqueia publicação. Q-03 não bloqueia demonstração com dados sintéticos. Não contornar restrição com token no navegador, substitutos em memória ou senha versionada.

## Mudanças transversais

Alteração de quantidade inteira para fracionada exige revisar Domain, campos, DTOs, formulários, CSV e cenários de concorrência. Alteração de autenticação exige ADR e revisão de BFF/API/deploy/testes. Modificar indicador exige atualizar RF-17/19, dashboard, relatórios e aceites juntos.

Condição transversal da baseline MongoDB: nó standalone não atende transações. IMongoClient reutilizado não implica sessão compartilhada; cada operação/teste concorrente tem sessão própria. Código anterior da fundação precisa adaptação autorizada antes de reutilizar seus gates. Não adicionar dependência de bloco futuro para criar índices, coleções ou referências.

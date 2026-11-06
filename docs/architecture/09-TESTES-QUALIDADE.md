# 09 — Testes e qualidade

## Estratégia

| Nível | Ferramenta / alvo | Evidência mínima |
| --- | --- | --- |
| Unidade | xUnit, Domain/Application, relógio controlado e portas falsas | Regras de dinheiro, status, permissão contextual e validação |
| Integração | xUnit, WebApplicationFactory e PostgreSQL real isolado em container | HTTP, Identity, migrations, índices, locks e transações |
| Web/BFF | xUnit para serviço/handler e integração host Web | CSRF, cookie, isolamento e falhas da API |
| Jornada | Roteiro de navegador em homologação; automação se disponível | Login → entrada → venda → cancelamento → relatório |

PostgreSQL por Testcontainers ou serviço Compose dedicado é dependência de teste. Nunca apontar para produção, nunca usar EF InMemory/SQLite para provar locks ou constraints PostgreSQL. Usar bancos/schemas isolados por execução e limpeza apenas do ambiente identificado como teste. Não desligar paralelismo para esconder erro de concorrência.

## Casos que não podem faltar

1. Saldo inicial 1, duas conexões e transações independentes confirmando vendas de 1: barreira sincroniza disputa; exatamente uma 200 e uma 409, saldo 0 e uma baixa.
2. Venda com dois itens, um insuficiente: zero baixas, saldo de ambos intacto e estado Rascunho.
3. Duas chamadas com a mesma chave: mesmo resultado e um efeito; mesma chave/corpo diferente: 409. Testar rollback antes de commit e resposta perdida após commit.
4. Confirmação repetida com nova chave: 409 sem efeito. Cancelamento simultâneo/repetido: um estorno por item, saldo restaurado uma vez.
5. Atualização concorrente de preço/inativação versus confirmação: resultado deve respeitar serialização pelos locks, nunca confirmar estado lido antes do bloqueio.
6. Duas edições com mesma versão: uma vence, outra 409. Soma dos movimentos igual ao saldo após todas as operações.
7. Vendedor tentando cancelar ou ler venda alheia; Estoquista tentando confirmar; chamadas diretas à API devem falhar.
8. Dois administradores tentando remover o último acesso simultaneamente: pelo menos um continua ativo.
9. Circuito antigo após inativação/troca de perfil, expiração por relógio controlado, duas sessões BFF sem mistura de token, CSRF e logout com API indisponível.
10. Snapshot histórico preservado após renomear produto/cliente; filtros por período na virada de dia/fuso; CSV com texto que inicia fórmula.

## Pipeline planejado

No bloco 00, preparar restore bloqueado por versões, build Release, testes unitários e integração, análise estática, verificação de dependências vulneráveis e ausência de segredos. A etapa de integração deve falhar explicitamente se PostgreSQL não estiver disponível, sem marcar testes como aprovados por skip.

Comandos previstos após existir solução: `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build`. A forma exata de subir o banco deve ser registrada pelo executor. Não executar esses comandos nesta entrega documental.

## Critérios mensuráveis

- Zero testes críticos falhando e zero avisos novos no build.
- Todo RF entregue mapeado a CA e evidência; segurança e integridade exigem teste automatizado, não apenas captura de tela.
- Cobertura serve para identificar lacunas; não aprovar regra crítica somente por percentual.
- RNF-05: medição com 20 usuários concorrentes, base sintética de 10 mil produtos/50 mil vendas, aquecimento e janela de 5 min. p95 de listas <= 2 s, confirmação <= 3 s, excluindo inicialização do container. Registrar CPU/RAM, volume e scripts. Meta sujeita à revisão documentada após baseline, sem mascarar falha.
- Verificação visual em 360 px e 1280 px, navegação por teclado, labels e foco em erros.

## Gate

Bloco só termina com [DoD](../execution/02-DEFINITION-OF-DONE.md) e critérios específicos satisfeitos. Falha de integridade ou autorização bloqueia avanço. Evidências ficam no estado por bloco; resultados desta etapa se limitam à consistência documental.


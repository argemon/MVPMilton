# 00 — Plano oficial de execução do TCC

Atualizado em 07/11/2026. Este é um **projeto acadêmico para demonstração local**. Um único ambiente, um único banco MongoDB Atlas e interface Blazor acessada no navegador. Não há publicação, catálogo público nem pedidos online.

## Blocos oficiais

| Bloco | Entrega | Estado |
| --- | --- | --- |
| 00 | Fundação .NET/Blazor/API/MongoDB | Concluído |
| 01 | Autenticação e autorização | Concluído |
| 02 | Funcionários | Concluído |
| 03 | Clientes | Concluído |
| 04 | Produtos | Concluído |
| 05 | Estoque | Concluído |
| 06 | Vendas | Concluído |
| 07 | Dashboard | Concluído |
| 08 | Relatórios e CSV | Concluído |
| 09 | Carga e restauração de dados fictícios | Concluído |
| 10 | Homologação, correções e acabamento visual | Pendente, depende de 09 |

## Sequência

1. Implementar **09**, populando o banco Atlas já configurado; registrar dados da carga sem apagar registros manuais; testar repetição e restauração.
2. Após aprovação comprovada de 09, executar **10**: testar todos os fluxos reais, corrigir falhas e revisar visualmente todas as telas num navegador.
3. Preparar a demonstração na máquina do apresentador (conexão à internet necessária para Atlas). Nenhum deploy ou ambiente adicional.

## Regras para os executores

- Ler `AGENTS.md`, este plano, `03-ESTADO-PROJETO.md`, requisitos e o documento do módulo.
- Manter Clean Architecture, SOLID e KISS; evitar novas funcionalidades ou infraestrutura desnecessária.
- Reaproveitar modelos, serviços e autenticação existentes. Não alterar `.env` nem versionar segredos.
- Testar primeiro o fluxo afetado e depois regressões; registrar comandos e resultados realmente observados.
- Usar o banco existente para a funcionalidade de carga, **sem excluir coleções inteiras**; testes automatizados existentes podem continuar usando infraestrutura isolada de teste, sem constituir outro ambiente de apresentação.
- Ao concluir cada bloco, atualizar estado e documentação, sem marcar o próximo como concluído.

Documentos normativos: [dependências](01-DEPENDENCIAS.md), [Definition of Done](02-DEFINITION-OF-DONE.md), [carga](../modules/09-CARGA-DEMONSTRACAO.md) e [homologação](../modules/10-HOMOLOGACAO.md).

## Escopo09 vigente — ADR-37

Carga acadêmica usa exclusivamente banco Atlas e conexão existentes no .env, sem banco Demo/instância/URI/ambiente adicional. Gate09: [carga](../modules/09-CARGA-DEMONSTRACAO.md), com quantidades, conciliação, reexecução, restauração confirmada, dependências manuais protegidas, autorização e resultados reais de restore/build/test/CI. Não exige nova infraestrutura nem libera10. Homologação/backup e metas comerciais antigas não são o escopo desta carga. Testes técnicos existentes mantêm ADR-28.

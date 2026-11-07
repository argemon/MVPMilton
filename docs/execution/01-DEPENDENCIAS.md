# 01 — Dependências oficiais

O sistema é executado localmente com **um único banco MongoDB Atlas** para uso e demonstração. Não existe bloco de deploy ou catálogo online.

```mermaid
flowchart TD
    B00[00 Fundação] --> B01[01 Autenticação]
    B01 --> B02[02 Funcionários]
    B01 --> B03[03 Clientes]
    B01 --> B04[04 Produtos]
    B04 --> B05[05 Estoque]
    B03 --> B06[06 Vendas]
    B05 --> B06
    B06 --> B07[07 Dashboard]
    B07 --> B08[08 Relatórios]
    B08 --> B09[09 Carga de demonstração]
    B09 --> B10[10 Homologação e UX]
```

| Bloco | Pré-requisitos | Resultado |
| --- | --- | --- |
| 00–08 | Conforme especificações já implementadas | Base funcional do TCC |
| 09 | 00–08 | Gerar dados de usuários, funcionários, clientes, produtos, estoque, vendas e histórico; restaurar somente dados da carga no banco existente |
| 10 | 00–09 aprovados | Auditar funcionamento, corrigir bugs, revisar layout responsivo e preparar apresentação presencial |

**Dependência crítica:** o Bloco 10 só pode ser aprovado depois que carga e restauração do Bloco 09 forem verificadas. O processo de homologação não adiciona uma segunda instância ou banco.

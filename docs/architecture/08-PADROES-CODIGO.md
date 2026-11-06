# 08 — Padrões de código

## Convenções

- Código de domínio em português sem acentos (`ConfirmarVenda`, `SaldoEstoque`); nomes de framework preservados. Tipos PascalCase, locais camelCase, privados `_camelCase`.
- C# do SDK .NET 10, nullable habilitado, análise estática e avisos tratados em CI. Evitar `!` para ocultar nulabilidade real.
- Dinheiro `decimal`, literais decimais; quantidades `int`; validar overflow e limites antes de multiplicar. Não converter por `double` nem usar cultura da máquina para contratos.
- Instantes `DateTimeOffset` UTC, relógio injetável. Data civil recebida como data e convertida pelo fuso configurado, sem assumir UTC-4 fixo.
- Métodos assíncronos com sufixo Async quando apropriado e CancellationToken. Não usar `.Result`, `.Wait()` ou `async void` fora de eventos exigidos.
- DI explícita; composição na Api/Web. Sem service locator ou estado global de usuário.

## Responsabilidades e validação

Validar formato/limite no transporte; invariantes no Domain; existência, concorrência, autorização de propriedade e transação em Application/Infrastructure. Repetir validação na UI melhora feedback, mas nunca substitui a API.

Controllers/endpoints traduzem transporte e chamam caso de uso, sem lógica de totalização ou manipulação direta de saldo. Entidades não são retornadas por JSON. DTO de criação difere de edição quando há campos distintos; impedir mass assignment.

Use resultado de negócio explícito para conflitos esperados. Exceções inesperadas são tratadas centralmente com ProblemDetails seguro. Não capturar exceção e retornar sucesso vazio. Código de erro é estável; mensagens podem mudar sem quebrar o cliente.

## SOLID, KISS e DRY na prática

Um serviço de venda coordena confirmação; estoque não é atualizado separadamente pelo controller. Reusar validação monetária e paginação quando houver duplicação real. Criar interface para dependência externa ou substituição de teste necessária, não para cada classe. Sem framework genérico de CRUD.

Query de lista projeta DTO interno sem tracking, pagina no banco e aceita somente sort/filter permitidos. Comando que precisa de consistência usa transação e locks documentados; evitar N+1. Frontend recebe contratos pelo HTTP mesmo quando está na mesma máquina da API.

## UI e revisão

Títulos, labels e erros pt-BR; número decimal de formulário aceita cultura pt-BR e serializa contrato numérico. Estados obrigatórios: carregando, vazio, erro recuperável, confirmação de ação sensível e sessão expirada. Botão desabilitado evita clique acidental; idempotência backend garante integridade.

Revisar autorização na rota e no caso de uso, ausência de segredos, referências de camada, acessibilidade básica, limites e tratamento de falhas. Mudança de contrato atualiza documento 06, OpenAPI, cliente e teste de contrato no mesmo bloco.

## Decisões

Não adotar MediatR/CQRS, AutoMapper obrigatório ou repositório genérico. Mapeamento manual é o padrão inicial. Biblioteca adicional exige justificativa concreta no registro ADR e licença compatível.


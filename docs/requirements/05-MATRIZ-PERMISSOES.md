# 05 — Matriz de permissões

## Policies normativas

Legenda: Sim = todo recurso permitido; Próprias = apenas vendas cujo criador é o usuário atual; — = negar. Todos pressupõem conta/sessão/funcionário ativos e troca de senha concluída.

| Policy / ação | Administrador | Gerente | Vendedor | Estoquista |
| --- | --- | --- | --- | --- |
| Conta.Me / Logout / AlterarSenha | Sim | Sim | Sim | Sim |
| Usuarios.Gerenciar | Sim | — | — | — |
| Funcionarios.Ler / Gerenciar | Sim | Sim | — | — |
| Clientes.Ler / Gerenciar | Sim | Sim | Sim | — |
| Clientes.Historico | Sim | Sim | Próprias | — |
| Produtos.Ler (inclui categorias/marcas/imagem) | Sim | Sim | Sim | Sim |
| Produtos.Gerenciar (inclui categorias/marcas/imagem) | Sim | Sim | — | Sim |
| Estoque.Ler | Sim | Sim | Sim | Sim |
| Estoque.Historico | Sim | Sim | — | Sim |
| Estoque.Movimentar (entrada/saída manual) | Sim | Sim | — | Sim |
| Estoque.Ajustar | Sim | Sim | — | — |
| Vendas.Ler | Sim | Sim | Próprias | — |
| Vendas.Criar | Sim | Sim | Sim | — |
| Vendas.Editar / Confirmar | Sim | Sim | Próprias | — |
| Vendas.Cancelar | Sim | Sim | — | — |
| Dashboard.Ler | Sim | Sim | — | — |
| Relatorios.Vendas | Sim | Sim | — | — |
| Relatorios.Estoque / Movimentos | Sim | Sim | — | Sim |
| Auditoria.Ler (sem endpoint geral no MVP) | Sim | — | — | — |

## Aplicação

Policy de rota protege operação e caso de uso aplica propriedade de venda. Listagens são filtradas no banco antes de paginação/contagem; vendedor nunca recebe totais de vendas de outros. Histórico do cliente também filtra por criador. Não confiar em usuário/perfil/ator enviados pelo BFF no corpo.

Mudanças no perfil fixo revogam sessões. Não há editor de permissões arbitrárias, múltiplos perfis por conta ou herança configurável. Administrador tem acesso completo às operações existentes, mas não ignora estados, constraints, saldo, versões nem proteção do último administrador.

Administrador é a função administrativa da instalação, não um papel de vendedor; não há restrição de propriedade para ele ou para Gerente. Funcionário sem conta não possui qualquer permissão. Leituras e escritas em andamento antes de revogação podem concluir; toda requisição iniciada após o commit da revogação deve ser negada. Se a regra mudar para interromper transações em andamento, registrar nova decisão e protocolo de locks de segurança.

Leitura de imagens passa pelo BFF com sessão, sem publicar caminho do volume. Desativação de funcionário por gerente é permitida para funcionários comuns; se funcionário possui conta Administrador, exigir adicionalmente Usuarios.Gerenciar e proteger último administrador. Gerente não pode usar cadastro para revogar conta administrativa.

## Extensão P2 proposta — não liberada no MVP

| Policy / ação | Administrador | Gerente | Vendedor | Estoquista | Visitante |
| --- | --- | --- | --- | --- | --- |
| Catalogo.Gerenciar | Sim | Sim | — | — | — |
| Pedidos.Gerenciar | Sim | Sim | — | — | — |
| Catálogo/imagem publicados e envio de solicitação pública | Sim | Sim | Sim | Sim | Sim, sem sessão e com limites |

Somente no 11 após aceite da ADR-18. Superfície pública usa DTOs próprios, sem acesso à API interna/saldo/contato. Receber pedido não concede identidade ou policy interna. Não há consulta pública de pedido.

## Testes de permissão

Para cada endpoint sensível, testar anônimo (401), perfil negado (403), perfil permitido e escopo de propriedade (404). Testar URL direta, não apenas visibilidade de botão. Ações ainda não implementadas continuam negadas por padrão.

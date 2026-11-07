# 05 — Matriz de permissões

Perfis vigentes por solicitação expressa no bloco01 (ADR-30): Administrador, Vendedor e Estoquista. As permissões limitadas não foram ampliadas; ações do perfil removido exigem Administrador.

## Policies normativas

Legenda: Sim = todo recurso permitido; Próprias = apenas vendas cujo criador é o usuário atual; — = negar. Conta e sessão devem estar ativas; funcionário ativo é exigido quando há vínculo, com exceção do bootstrap que não possui funcionário. Troca de senha deve estar concluída para operações comerciais/administrativas; Conta.Me, Logout e AlterarSenha permanecem permitidas enquanto a troca obrigatória está pendente, evitando impedir a própria regularização. O vínculo humano obrigatório começa no02;01 não consulta employees inexistente.

| Policy / ação | Administrador | Vendedor | Estoquista |
| --- | --- | --- | --- |
| Conta.Me / Logout / AlterarSenha | Sim | Sim | Sim |
| Usuarios.Gerenciar | Sim | — | — |
| Funcionarios.Ler / Gerenciar | Sim | — | — |
| Clientes.Ler / Gerenciar | Sim | Sim | — |
| Clientes.Historico | Sim | Próprias | — |
| Produtos.Ler (inclui categorias/marcas/imagem) | Sim | Sim | Sim |
| Produtos.Gerenciar (inclui categorias/marcas/imagem) | Sim | — | Sim |
| Estoque.Ler | Sim | Sim | Sim |
| Estoque.Historico | Sim | — | Sim |
| Estoque.Movimentar (entrada/saída manual) | Sim | — | Sim |
| Estoque.Ajustar | Sim | — | — |
| Vendas.Ler | Sim | Próprias | — |
| Vendas.Criar | Sim | Sim | — |
| Vendas.Editar / Confirmar | Sim | Próprias | — |
| Vendas.Cancelar | Sim | — | — |
| Dashboard.Ler | Sim | — | — |
| Relatorios.Vendas | Sim | — | — |
| Relatorios.Estoque / Movimentos | Sim | — | Sim |
| Auditoria.Ler (sem endpoint geral no MVP) | Sim | — | — |

## Aplicação

Policy de rota protege operação e caso de uso aplica propriedade de venda. Listagens são filtradas no banco antes de paginação/contagem; vendedor nunca recebe totais de vendas de outros. Histórico do cliente também filtra por criador. Não confiar em usuário/perfil/ator enviados pelo BFF no corpo.

Mudanças no perfil fixo revogam sessões. Não há editor de permissões arbitrárias, múltiplos perfis por conta ou herança configurável. Administrador tem acesso completo às operações existentes, mas não ignora estados, validadores e filtros condicionais, saldo, versões nem proteção do último administrador.

Administrador é a função administrativa da instalação, não um papel de vendedor; não há restrição de propriedade para ele. Funcionário sem conta não possui qualquer permissão. Leituras e escritas em andamento antes de revogação podem concluir; toda requisição iniciada após o commit da revogação deve ser negada. Se a regra mudar para interromper transações em andamento, registrar nova decisão e protocolo de escritas de guarda de segurança.

Leitura de imagens passa pelo BFF com sessão, sem publicar caminho do volume. A manutenção de funcionários será exclusiva do Administrador (ADR-30), com Usuarios.Gerenciar adicional para contas e proteção transacional do último administrador.

## Extensão P2 proposta — não liberada no MVP

| Policy / ação | Administrador | Vendedor | Estoquista | Visitante |
| --- | --- | --- | --- | --- |
| Catalogo.Gerenciar | Sim | — | — | — |
| Pedidos.Gerenciar | Sim | — | — | — |
| Catálogo/imagem publicados e envio de solicitação pública | Sim | Sim | Sim | Sim, sem sessão e com limites |

Somente no 11 após aceite da ADR-18. Superfície pública usa DTOs próprios, sem acesso à API interna/saldo/contato. Receber pedido não concede identidade ou policy interna. Não há consulta pública de pedido.

## Testes de permissão

Para cada endpoint sensível, testar anônimo (401), perfil negado (403), perfil permitido e escopo de propriedade (404). Testar URL direta, não apenas visibilidade de botão. Ações ainda não implementadas continuam negadas por padrão.

## Aplicação com MongoDB

A matriz e seus resultados 401/403/404 permanecem iguais. Sessão e conta/stamp atuais são conferidos no backend/primary, sem cache ou perfil do navegador como autoridade. Referências de funcionário/usuário e proteção do último Admin usam a mesma transação e controle administrativo efetivamente atualizado; login/revogação seguem arquitetura07. Credencial MongoDB do Web só acessa armazenamento técnico, sem acesso às coleções comerciais. Nenhuma role de banco equivale automaticamente a perfil de aplicação.

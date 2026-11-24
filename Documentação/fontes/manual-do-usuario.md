# Manual do usuário do Gestão Comercial

Guia da versão acadêmica analisada em 24 de novembro de 2026, commit 1940b83460ec93d8efa2011db9dc9177570b0586. Destina-se aos perfis Administrador, Vendedor e Estoquista. Os procedimentos descrevem as telas implementadas; não foram executados no banco durante a elaboração deste manual.

<!-- toc -->

# 1 Antes de começar

O Gestão Comercial reúne clientes, produtos, estoque, vendas e consultas em um sistema usado pelo navegador. A apresentação executa no computador do responsável e utiliza o banco Atlas já configurado. É necessária conexão com a internet. Uma pessoa com conta autorizada entra com seu email e senha; não existe cadastro público nem senha padrão publicada.

Os dados da demonstração são fictícios. Isso não significa que qualquer exclusão seja reversível: a página administrativa possui ações com efeitos distintos, explicadas no capítulo 11. O uso comercial normal conserva vendas e movimentos. Para corrigir saldo, registra-se novo ajuste autorizado; para desfazer venda confirmada, utiliza-se cancelamento.

## 1.1 O que cada perfil pode fazer

| Perfil | Atividades disponíveis |
| --- | --- |
| Administrador | Contas, funcionários, clientes, produtos, estoque, vendas, cancelamento, dashboard, relatórios e demonstração |
| Vendedor | Clientes, consulta de produtos e saldo; criação, edição, confirmação, descarte e consulta das próprias vendas |
| Estoquista | Produtos, categorias, marcas, imagens, entradas/saídas manuais, histórico e relatórios de estoque/movimentos |

Somente Administrador realiza ajuste de estoque ou cancelamento de venda. O Vendedor não consulta vendas de outro criador. O Estoquista não acessa vendas, clientes ou relatórios comerciais. Uma opção ausente pode decorrer do perfil; abrir seu endereço diretamente não amplia a permissão.

# 2 Preparar e iniciar a aplicação

## 2.1 Pré-requisitos da máquina

O responsável deve ter o projeto baixado, SDK .NET 10.0.401, configuração privada existente do Atlas, acesso de rede autorizado no cluster e certificado HTTPS de desenvolvimento confiável. A conta de acesso e o banco devem estar preparados. Baixar o código público não fornece essas informações privadas.

Use a pasta que contém GestaoComercial.slnx como raiz de execução. Preserve o arquivo .env e a configuração privada fornecida pelo responsável. Não copie credenciais para este manual, capturas ou mensagens. Instalação ainda não preparada requer o procedimento explícito do README do projeto, executado pelo responsável; iniciar o sistema normalmente não aplica evoluções de persistência.

## 2.2 Dois terminais

Abra o primeiro terminal na raiz do projeto e execute:

```powershell
dotnet run --project backend/GestaoComercial.Api --launch-profile https
```

Aguarde o backend iniciar. Abra outro terminal na mesma raiz e execute:

```powershell
dotnet run --project frontend/GestaoComercial.Web --launch-profile https
```

Esses comandos restauram dependências e compilam automaticamente. Mantenha os dois terminais abertos. No navegador, acesse https://localhost:7243 e entre com a conta fornecida. Para encerrar os programas, pressione Ctrl+C em cada terminal após concluir o trabalho em andamento.

## 2.3 Imagens e continuidade da instalação

Preserve o caminho Images__RootPath configurado na instalação. Sem configuração específica, o backend utiliza backend/GestaoComercial.Api/.local/images. Se imagens foram cadastradas em outro diretório, o responsável deve manter esse caminho. Não criar outra configuração apenas para substituir a já existente.

As chaves que protegem sessões também possuem diretório persistente. Apagar arquivos locais de imagens ou chaves pode comprometer continuidade, mesmo que o banco permaneça disponível. A página de demonstração não substitui procedimento de backup completo.

# 3 Acesso e navegação

## 3.1 Entrar e trocar senha

1. Abra o site e informe email de login e senha.
2. Se houver exigência de troca, informe senha atual e nova senha.
3. A nova senha deve ter de 12 a 128 caracteres, com letra maiúscula, minúscula, número e símbolo.
4. Após a troca, entre novamente quando solicitado.

Credenciais incorretas exibem mensagem genérica. Após repetidas falhas, a conta pode ficar bloqueada por quinze minutos; há também limitação de tentativas. Não continue repetindo rapidamente. Solicite ao Administrador conferir a conta se necessário.

## 3.2 Reconhecer a área de trabalho

O cabeçalho mostra identificação do usuário e perfil, além de Alterar senha e Sair. O menu lateral reúne atalhos permitidos. Em telas menores, o grupo Menu de navegação pode ser expandido ou recolhido. O início oferece atalhos conforme o acesso. Os endereços mais usados são:

| Área | Endereço após o host local |
| --- | --- |
| Início | / |
| Clientes | /clientes |
| Produtos | /produtos |
| Estoque | /estoque |
| Nova venda | /vendas/nova |
| Histórico de vendas | /vendas |
| Dashboard | /dashboard |
| Funcionários e usuários | /funcionarios e /usuarios |
| Demonstração | /administracao/dados-demonstracao |

Durante consultas, aguarde a indicação de carregamento. Tabelas podem ter rolagem própria quando largas. Use Anterior/Próxima para navegar. Em buscas por nome, o sistema utiliza início do nome; não se deve esperar busca por qualquer trecho. Documento e SKU são utilizados por igualdade normalizada conforme a área.

## 3.3 Sair e expiração

Acione Sair para encerrar o acesso. Fechar apenas a aba não é o mesmo procedimento. Sessões expiram e podem ser revogadas por alteração de senha, perfil ou situação. Ao perder o acesso, entre novamente; não repita comandos antigos com dados desatualizados sem conferir o estado.

Se o servidor remoto estiver indisponível no logout, o sistema encerra o acesso local e registra a indisponibilidade da revogação. Não se deve concluir que essa situação comprovou revogação imediata no backend.

# 4 Funcionários e contas

## 4.1 Funcionários

Administrador: abra Funcionários, consulte a lista e utilize a ação de novo cadastro. Informe nome e, quando necessários, email de contato e telefone. Salve e confira o retorno. Para editar, abra a ficha, altere os campos e salve. Uma mensagem de conflito significa que a ficha deve ser relida antes de tentar novamente.

Inativar funcionário mantém o registro e as autorias anteriores, mas revoga a conta e as sessões vinculadas. Reativar o cadastro não deve ser interpretado como restauração de sessões antigas. O último Administrador possui proteção: uma ação que deixaria a instalação sem administração é recusada.

## 4.2 Criar e manter usuário

Cadastrar funcionário não cria automaticamente acesso. Na criação de usuário, selecione funcionário ativo, informe email de login, perfil e senha temporária válida. Só pode existir uma conta por funcionário. O email de login é independente do email de contato; não existe troca livre do vínculo existente no MVP.

Ao redefinir senha, informe nova senha temporária e comunique-a por meio privado adequado. O sistema exige troca no acesso seguinte. Alterar perfil ou desativar conta revoga acessos anteriores conforme a operação. O administrador inicial pode não ter funcionário vinculado por ser a conta de preparação da instalação.

# 5 Clientes

Administrador e Vendedor podem cadastrar e editar clientes. Abra Clientes e pesquise por início do nome ou documento. Use os filtros de situação e tipo quando necessário. Para novo cliente, informe nome e tipo PF/PJ; documento e contatos são opcionais, mas devem ser válidos quando preenchidos.

CPF ou CNPJ informado passa por validação dos dígitos e unicidade. Um documento de cliente inativo continua reservado. Se surgir duplicidade, procure o registro existente em Todos ou Inativos antes de cadastrar outra pessoa. Não existe consulta automática à situação cadastral de órgãos externos.

Abra a ficha para editar ou inativar. O histórico de vendas é preservado. Administrador consulta o conjunto permitido; Vendedor vê apenas vendas que criou. Uma lista vazia pode indicar ausência de vendas no período ou no seu escopo, e não perda de histórico.

# 6 Produtos categorias marcas e imagens

## 6.1 Referências

Administrador e Estoquista mantêm categorias e marcas pela área de Produtos e seus links de manutenção. Cadastre nomes de 2 a 80 caracteres. Nomes normalizados duplicados são recusados, inclusive quando o registro anterior está inativo. Categoria é obrigatória no produto; marca é opcional.

Categoria ou marca ligada a produto ativo não pode ser inativada. Examine os produtos associados e resolva a situação de negócio antes de tentar novamente. Não exclua registros históricos para contornar uma mensagem de conflito.

## 6.2 Cadastrar e editar produto

Informe SKU, nome, descrição opcional, categoria, marca opcional, preço e estoque mínimo. SKU tem de 1 a 40 caracteres e aceita letras A–Z, números, ponto, hífen e sublinhado; é normalizado em maiúsculas. O preço deve ser positivo, com até duas casas decimais. O mínimo é quantidade inteira não negativa.

O saldo inicial é zero e não é digitado no cadastro. Após criar produto, utilize entrada de estoque para disponibilizar unidades. A edição de preço, nome ou referência não altera o saldo. Inativar impede novas utilizações comerciais, preservando os dados históricos.

## 6.3 Imagem

Na ficha, selecione JPEG ou PNG válido, com até 2 MiB e dimensões de até 4096 por 4096 pixels. Envie pela ação de imagem. O sistema valida o conteúdo e armazena PNG privado. Trocar extensão de arquivo não transforma seu conteúdo em imagem válida; SVG não é aceito.

A remoção de imagem remove o arquivo substituível/metadado, sem excluir o produto. Vendedor pode visualizar imagem autenticada, mas não a alterar. Se uma imagem antiga não carregar após mover a instalação, o responsável deve conferir a configuração do diretório.

# 7 Estoque

## 7.1 Consultar saldo e movimentos

Em Estoque, pesquise pelo início do nome ou SKU exato. Filtre Ativos, Inativos ou Todos e, se necessário, Saldo no mínimo ou abaixo. O saldo é expresso em unidades inteiras. Mínimo zero e saldo zero também atendem ao filtro de estoque baixo quando o produto está ativo.

Administrador e Estoquista acessam Histórico para conferir produto, tipo, data, delta, saldos, motivo e operador. Entrada é positiva; saída manual e venda são negativas; estorno de venda é positivo. Uma conciliação considera a soma dos movimentos, sem apagar os anteriores.

## 7.2 Entrada e saída manual

1. Abra Registrar entrada ou Registrar saída.
2. Selecione produto ativo e quantidade positiva.
3. Informe motivo entre 10 e 500 caracteres.
4. Confirme e aguarde o resultado.
5. Confira saldo e movimento.

A saída é recusada se exceder o saldo. Não registre saída manual para representar uma venda que será confirmada no sistema: a confirmação realiza sua própria baixa. Em caso de resposta incerta, utilize a repetição oferecida pela tela para conservar a intenção, em vez de abrir outra operação.

## 7.3 Ajuste pelo Administrador

O ajuste informa diferença, não novo saldo absoluto. Para corrigir saldo de dez para oito, o delta é -2. Informe motivo suficiente para explicar a divergência e confira o resultado. O ajuste não permite saldo negativo e não edita movimentações antigas.

# 8 Vendas e frente de caixa

## 8.1 Montar o cupom

1. Abra Nova venda.
2. Selecione cliente ativo; use a busca de cliente se necessário.
3. Localize produtos por início do nome ou SKU.
4. Informe a quantidade e adicione os produtos ao cupom.
5. Confira quantidade, preço unitário, subtotal e total.

O cupom possui controles para aumentar, diminuir ou remover item. O sistema trabalha com até cem produtos distintos e não aceita quantidade fracionária. O valor exibido durante edição é revisto pelo servidor ao salvar. Não há desconto, parcelamento, pagamento integrado ou emissão fiscal.

## 8.2 Revisar salvar e concluir

Use Salvar rascunho para depois quando desejar guardar sem baixar estoque. A ação Revisar e finalizar salva/revisa preços; após revisar os valores, use Concluir venda quando disponível. Só a confirmação concluída baixa as quantidades. Um rascunho não reserva produtos: outro operador pode vender o saldo antes de sua confirmação.

Se o preço do cadastro mudou, o sistema pede salvar o rascunho para atualizar e revisar explicitamente. Se faltar saldo em algum item, a venda permanece rascunho e não há baixa parcial dos demais. Não tente compensar com movimento manual: corrija o cupom ou a disponibilidade conforme o procedimento autorizado.

Após conclusão, a tela apresenta venda encerrada e opção Atender próximo cliente. Os valores registrados ficam preservados. O cupom é registro interno do sistema e não deve ser apresentado como documento fiscal.

## 8.3 Resposta pendente e atualização

Quando aparecer Resultado ainda não confirmado, use Reconsultar / repetir mesma operação. Essa ação preserva a identificação do comando e evita duplicação. Não altere o cupom nem abra outra venda para substituir uma confirmação cujo resultado ainda é desconhecido.

Use Atualizar venda para consultar o estado corrente. Um retorno antigo de confirmação não significa que a venda continua confirmada se ela já foi cancelada depois. Se houver conflito de versão, releia antes de repetir uma decisão.

## 8.4 Consultar ou descartar

No Histórico de vendas, use cliente, situação e período conforme a tela e abra a ficha desejada. O período da lista é de criação; os relatórios comerciais usam confirmação. Vendedor consulta apenas próprias vendas. Dados copiados na operação preservam o nome e o preço da época.

Rascunho pode ser descartado com motivo, sem movimento de estoque. Descartar não apaga a venda e não se aplica a venda confirmada. Uma confirmada não aceita edição retroativa de itens.

## 8.5 Cancelar venda

Somente Administrador: abra a venda Confirmada e expanda Cancelar venda. Leia a identificação, informe motivo de 10 a 500 caracteres, marque a confirmação exigida e acione o botão final. Abrir a área, sozinho, não cancela.

O cancelamento devolve integralmente as quantidades e preserva a baixa original e o motivo. Produto posteriormente inativo pode receber estorno. Não há devolução parcial. A operação pode ser recusada se o estorno exceder a capacidade de saldo; nesse caso nenhum item é estornado parcialmente.

# 9 Dashboard

Administrador: selecione o período para consultar quantidades, receita líquida, ticket médio, evolução, ranking, vendas recentes e estoque baixo. Ausência de período utiliza mês corrente conforme configuração. Datas são interpretadas no fuso da instalação; o fim do intervalo é exclusivo.

A receita considera vendas atualmente Confirmadas cuja confirmação pertence ao período. Canceladas continuam no conjunto, mas contribuem zero. Por exemplo, vendas de R$ 25,00 e R$ 15,00, com a segunda cancelada, deixam receita de R$ 25,00 e uma venda confirmada. O indicador não é fluxo de caixa nem fechamento contábil congelado.

# 10 Relatórios e exportação

Administrador acessa vendas por período, vendas por funcionário/operador e produtos mais vendidos, além dos relatórios de estoque e movimentos. Estoquista acessa somente os dois últimos. O Vendedor não acessa relatórios. No relatório por operador, a identificação é a do criador da venda, não um vínculo atual inferido.

Escolha o relatório e aplique os filtros da tela. Para relatórios por período, o limite é366 dias e o fim é exclusivo. Para incluir operações do dia7, por exemplo, utilize o início desejado e fim no dia8, conforme a indicação da interface. Totais representam o filtro completo, não apenas a página atual.

O relatório de estoque mostra posição atual; não reconstrói saldo de uma data anterior. Movimentações usam data em que ocorreram. Para CSV, clique na ação de exportar e aguarde o download. O arquivo usa UTF-8, ponto e vírgula e ponto decimal. Ao importar na planilha, selecione essas opções se a detecção automática não corresponder.

Acima de dez mil linhas, a exportação é recusada sem truncar. Restrinja o período ou outros filtros. CSV contém texto protegido contra fórmulas; não remova essa proteção para executar conteúdo vindo dos cadastros. Um erro de exportação não deve ser interpretado como arquivo vazio válido.

# 11 Administração da demonstração

Esta página é exclusiva de Administrador. A explicação abaixo é orientação de uso, não autorização para executar exclusão durante leitura do manual. Nenhuma ação da página foi acionada para produzir esta documentação.

| Ação | Finalidade | Efeito relevante |
| --- | --- | --- |
| Gerar ou retomar | Preparar cenário fictício | Retoma etapas sem duplicar; não apaga dados manuais |
| Restaurar carga | Remover conjunto demonstrativo identificado quando seguro | Verifica dependências e protege alterações/dados manuais |
| Limpar dados | Limpeza explícita da instalação acadêmica | Exclui também dados manuais e histórico, preservando executor/vínculo/sessão e estrutura técnica |

## 11.1 Gerar e restaurar

A página informa banco e última conclusão. Gerar utiliza o cenário definido de12 funcionários/usuários,40 clientes,100 produtos e250 vendas fictícias; isso não é a contagem atual do banco. A geração pode levar alguns minutos. Aguarde o resultado antes de iniciar outra operação.

Restaurar exige a confirmação oferecida. Se houver registros manuais relacionados à carga ou alterações que impeçam remoção segura, a operação é recusada. Não tente apagar dependências para forçar sucesso sem analisar a situação. Restaurar carga não recupera backup nem apaga indiscriminadamente todas as coleções.

<!-- page -->

## 11.2 Limpeza explícita

A área destrutiva exige digitar exatamente APAGAR DADOS e confirmar a operação. O efeito inclui histórico comercial, dados manuais e demais contas, com exceção do Administrador executor, funcionário vinculado e sessão atual. Imagens armazenadas em arquivos fora do banco não são apagadas por essa ação.

Após a limpeza, não há nova carga automática: gerar é outra ação. O sistema registra a operação para que uma repetição da mesma intenção não apague dados criados depois. Esse mecanismo não oferece botão de desfazer. Para demonstração à banca, prefira explicar a área sem executar limpeza desnecessária.

# 12 Mensagens e solução de problemas

| Mensagem ou situação | O que fazer |
| --- | --- |
| Não foi possível entrar | Conferir credenciais e situação da conta; aguardar em caso de bloqueio |
| Serviço indisponível | Conferir os dois processos e conexão; responsável verifica readiness/Atlas |
| Formulário inválido | Recarregar o formulário para renovar proteção; não desativar antiforgery |
| Acesso negado | Conferir perfil e troca obrigatória; solicitar ao Administrador |
| Registro alterado ou versão em conflito | Atualizar a ficha e revisar antes de salvar |
| Documento, SKU ou nome já cadastrado | Pesquisar também inativos; reutilizar/manter cadastro correto |
| Preço alterado | Salvar rascunho, revisar os valores e então concluir |
| Estoque insuficiente | Rever quantidade ou reposição autorizada; nenhuma baixa parcial deve ser presumida |
| Saldo inconsistente | Solicitar investigação; não criar saldo manualmente no banco |
| Resultado ainda não confirmado | Usar repetição da mesma operação e consultar estado |
| Exportação excede limite | Reduzir filtros; não esperar arquivo truncado |
| Imagem indisponível | Conferir formato, tamanho e diretório persistente com o responsável |

Se precisar de suporte, informe tela, horário, ação e referência de erro quando houver. Não envie senha, token, arquivo .env ou capturas com dados privados. Descreva se o resultado ficou pendente, pois a operação pode ter sido concluída mesmo quando a resposta não chegou.

# 13 Roteiro sugerido para apresentação

Com o cenário já preparado, apresentar acesso e os perfis; consultar clientes e produtos; explicar saldo e movimentos; mostrar uma venda e seu histórico; apresentar dashboard e filtros/CSV; e explicar a arquitetura e os mecanismos de consistência. Qualquer nova venda, entrada ou cancelamento é operação real no banco de demonstração e deve ser combinado com o responsável.

Não incluir como resultado uma jornada que não foi executada. A documentação técnica identifica evidências históricas e limitações. Este manual não contém screenshots funcionais porque as capturas locais disponíveis são montagens estáticas de layout. Diagramas e instruções em texto descrevem as funções sem fabricar prova de execução.

# 14 Glossário e fontes internas

| Termo | Significado neste sistema |
| --- | --- |
| Rascunho | Venda editável, sem reserva nem baixa |
| Confirmada | Venda concluída com baixa transacional |
| Cancelada | Venda anteriormente confirmada, estornada integralmente |
| Descartada | Rascunho encerrado sem movimentação |
| Delta | Diferença somada ao saldo, positiva ou negativa |
| Snapshot | Cópia de dados da época da operação |
| Versão | Revisão usada para detectar edição concorrente |
| Intenção pendente | Comando cujo resultado ainda precisa ser confirmado |
| CSV | Arquivo tabular para consulta/exportação |

Fontes internas: README da raiz, componentes e clientes HTTP do Web, contratos compartilhados, endpoints API, matriz de permissões e ADR-37 a39. Versão e inventário constam no índice da entrega. Este manual acompanha a Documentação técnica e não substitui treinamento, backup ou autorização operacional.

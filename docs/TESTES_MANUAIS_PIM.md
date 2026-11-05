# Testes manuais recomendados antes da apresentação do PIM

## Modo demonstração (não publicar online)

1. Inicie `dotnet run`. Confirme que a home abre e as quatro categorias exibem produtos fictícios.
2. Faça login como Cliente recém-cadastrado. Tente acessar `/Admin`, `/Admin/Financeiro` e executar POST para `/Admin/Movimentar`. Deve exibir **Acesso negado** (ou 403 se a requisição não seguir redirecionamento).
3. Entre como `funcionario@demo.local` / `Demo@12345` e acesse `/Admin`. Confirme que vê estoque, faturamento e PDV, mas **não consegue** `/Admin/Equipe`.
4. Entre como `dono@demo.local` / `Demo@12345`. Confirme que pode criar uma conta Funcionário e que essa conta consegue fazer login.
5. Em `/Admin/Movimentar`, registre entrada de 5 unidades com validade futura. Confirme o aumento no saldo e a criação do lote.
6. Em `/Admin/Pdv`, venda 2 unidades de um produto disponível e selecione DINHEIRO. Confirme a venda em `/Admin/Vendas`, o novo saldo em `/Admin/Estoque` e a entrada em `/Admin/Financeiro`.
7. Tente vender uma quantidade superior ao estoque: a operação deve ser bloqueada, sem alterar estoque nem caixa.
8. Peça um produto via `/Pedidos/Novo/{id}`. Confira que o pedido aparece como PENDENTE, sem baixa de estoque nem entrada de caixa. Depois confirme em `/Admin/Vendas` e verifique ambos os lançamentos.
9. Confira o percentual por produto em `/Admin/Financeiro`: a soma deve se aproximar de 100% (considerando os arredondamentos).
10. Confira alertas de baixo estoque e vencimento próximo na página principal do painel.
11. Tente solicitar um produto da categoria Cigarros: pedido online deve ser indisponível.
12. Reinicie o servidor: os dados em demonstração devem ser resetados.

## Com MySQL

1. Importe `database/01_ADEGA2L_BANCO_COMPLETO.sql`.
2. Configure `ConnectionStrings__MySql` e visite `/diagnostico/banco` em ambiente Development: deve retornar MySql com tabela Produtos OK.
3. Crie Dono pelo comando `dotnet run -- --criar-dono`; não exponha a senha em prints ou no GitHub.
4. Repita testes 1–11 em MySQL. Reinicie o site: venda, movimento, estoque e usuários devem permanecer.
5. No phpMyAdmin, verifique `ADG2L_Vendas`, `ADG2L_ItensVenda`, `ADG2L_CaixaMovimentos`, `ADG2L_MovimentacoesEstoque`, `ADG2L_Produtos`, `ADG2L_Lotes` e relacione registros.
6. Verifique que o botão de confirmação do pedido não permite duplicar o caixa da mesma venda.
7. Simule indisponibilidade do banco (desligue MySQL): a aplicação **não deve** trocar automaticamente para memória e perder referências.

O código não foi executado com o SDK .NET neste ambiente; faça `dotnet build` antes da avaliação. Antes de produção, é necessária auditoria adicional de privacidade, segurança, regras de venda por idade e meios de pagamento.

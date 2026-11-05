# Adega2L — PIM | Site + gestão interna + MySQL

Projeto acadêmico refeito a partir dos arquivos `.php` enviados: **ASP.NET Core MVC em C# (.NET 10), HTML/Razor (`.cshtml`), CSS e JavaScript auxiliar**. Foi preservada a estrutura conceitual de home, catálogo por categorias, detalhes de produtos, login, cadastro, institucional e carrinho futuro. Como os arquivos de CSS e as imagens originais não foram enviados, foi criado um novo CSS responsivo e foram incluídas ilustrações SVG locais para os produtos de exemplo.

> **O projeto não precisa de banco para abrir em desenvolvimento.** Em `Dados:Origem=Auto`, uma conexão MySQL vazia ativa o modo demonstração. Quando `ConnectionStrings:MySql` for preenchida, o site passa a utilizar o banco **sem alterar os controllers nem as views**. A importação do arquivo SQL e a criação da primeira conta Dono são passos manuais necessários. Se o banco estiver configurado mas indisponível, o programa mostrará erro (não apagará dados caindo silenciosamente para a memória).

## 1. Pré-requisitos

- Windows com **SDK .NET 10** (não somente runtime). Instalar pelo site oficial: https://dotnet.microsoft.com/download/dotnet/10.0
- VS Code com extensão **C# Dev Kit** ou Visual Studio.
- Para o banco, **XAMPP com MySQL/MariaDB** ou MySQL local. PHP e Apache NÃO executam o C#: somente o MySQL do XAMPP é utilizado. O site é servido pelo `dotnet run` (Kestrel).
- Internet no primeiro `dotnet restore` para baixar **MySqlConnector 2.6.2** (pacote do NuGet).

## 2. Iniciar SEM banco (modo de demonstração)

1. Extraia o arquivo ZIP e abra a pasta `Adega2L-PIM-Completo` no VS Code.
2. Abra **Terminal > Novo Terminal** dentro dessa pasta.
3. Execute:

```powershell
dotnet restore
dotnet run
```

4. Abra **http://localhost:5050** (configurado em `Properties/launchSettings.json`). No Windows, pode também dar duplo clique em `INICIAR-SITE.bat`.

**Credenciais apenas para testar localmente:**

| Perfil | E-mail | Senha |
|---|---|---|
| Dono | `dono@demo.local` | `Demo@12345` |
| Funcionário | `funcionario@demo.local` | `Demo@12345` |

Contas de cliente criadas no formulário recebem sempre o papel **Cliente** e não abrem `/Admin`. Todas as informações inseridas no modo demonstração são perdidas quando o programa é reiniciado. Nunca use o modo Demo em um servidor público ou senha real nesse modo.

## 3. Criar o banco MySQL

1. Abra o XAMPP e inicie **MySQL**.
2. Entre no `http://localhost/phpmyadmin` e escolha **Importar**.
3. Importe **`database/01_ADEGA2L_BANCO_COMPLETO.sql`**. Ele cria o banco `adega2l`, 8 tabelas, as 4 categorias e 28 produtos fictícios. As tabelas de carrinho foram intencionalmente deixadas de fora.
4. Configure a **string de conexão**, usando uma variável de ambiente, por exemplo no PowerShell *apenas para desenvolvimento local*:

```powershell
$env:ConnectionStrings__MySql = "Server=127.0.0.1;Port=3306;Database=adega2l;User ID=root;Password=;SslMode=None"
dotnet run
```

> Ajuste usuário, senha, host e porta conforme o seu MySQL. É recomendável criar um usuário **exclusivo do site** com permissões SELECT, INSERT, UPDATE e DELETE nesse banco, em vez de usar `root`. **Nunca envie senhas de banco para o GitHub ou escreva credenciais reais em `appsettings.json`.** O arquivo PHP antigo continha credenciais embutidas: se forem reais, altere-as no servidor antigo.

Com a string configurada, acesse `http://localhost:5050/diagnostico/banco` **somente em Development**, para conferir a conexão.

## 4. Criar a primeira conta Dono no MySQL

O banco nasce **sem usuário administrativo e sem senha padrão**, por segurança. No terminal PowerShell da pasta do projeto, com a string de conexão do passo 3 configurada:

```powershell
$env:ADEGA_OWNER_NAME = "Proprietário da Adega"
$env:ADEGA_OWNER_EMAIL = "dono@exemplo.com"
$env:ADEGA_OWNER_PASSWORD = "CrieUmaSenhaForte@2026"
dotnet run -- --criar-dono
Remove-Item Env:ADEGA_OWNER_PASSWORD
Remove-Item Env:ADEGA_OWNER_EMAIL
Remove-Item Env:ADEGA_OWNER_NAME
dotnet run
```

Use um e-mail seu e **substitua a senha do exemplo** por outra longa e exclusiva. O comando só funciona em modo MySQL e cria o primeiro Dono se ainda não existir um Dono. Depois acesse **/Conta/Login** e entre com os dados escolhidos. A conta Dono pode criar novos funcionários em **/Admin/Equipe**. Funcionários não podem criar outros funcionários. Clientes não têm permissão de gestão.

## 5. Endereços do site

| Tela | URL |
|---|---|
| Início / destaques | `/` |
| Categorias | `/categoria/bebidas`, `/categoria/destilados`, `/categoria/snacks`, `/categoria/cigarros` |
| Pesquisa | `/Produtos/Buscar?q=agua` |
| Detalhes do produto | `/Produtos/Detalhes/1` |
| Pedido direto (sem carrinho) | `/Pedidos/Novo/1` |
| Cadastro do cliente | `/Conta/Cadastro` |
| Login da equipe e clientes | `/Conta/Login` |
| Institucional / história | `/Home/Institucional` |
| Carrinho futuro | `/Home/Carrinho` |
| Visão geral interna | `/Admin` |
| Estoque e lotes | `/Admin/Estoque` |
| Entrada e saída | `/Admin/Movimentar` |
| Cadastrar produto | `/Admin/NovoProduto` |
| PDV | `/Admin/Pdv` |
| Vendas, pedidos e confirmação | `/Admin/Vendas` |
| Fluxo de caixa e % de vendas | `/Admin/Financeiro` |
| Criar funcionário (**só Dono**) | `/Admin/Equipe` |

## 6. Requisitos do PIM: alcance e limites

| Requisito | Implementação nesta entrega |
|---|---|
| RF01 Caixa | Registro de entradas e saídas; saldo e faturamento no painel. |
| RF02 Estoque | Cadastro de produtos, entradas e saídas, histórico, lotes e quantidades. |
| RF03 % vendida | Percentual de **unidades vendidas** por produto, calculado sobre vendas concluídas. |
| RF04 PDV | Venda com até 4 linhas, checagem de estoque, baixa de lotes e lançamento automático no caixa **em transação** no MySQL. |
| RF05 Estoque baixo | Alerta no painel quando estoque <= mínimo configurado. **Não envia notificações push/e-mail.** |
| RF06 Validade | Alerta de lote vencido/vencendo em até 30 dias. PDV não consome lotes vencidos; descarte deve ser feito por saída manual. |
| RF07 Catálogo e história | Home, categorias, busca, detalhes e página institucional. |
| RF08 Pedido online | **Pedido direto de um produto, sem carrinho**, com telefone/endereço e status PENDENTE. A equipe confirma para dar baixa em estoque/caixa. **Sem checkout concluído ou entrega automática.** Pedidos online de Cigarros estão desativados. |
| RF09 Pagamento | Cadastro da preferência **dinheiro, débito ou crédito** no PDV e pedido. **Sem gateway, captura de cartão ou cobrança eletrônica.** |
| RNF01 Facilidade | UI com navegação simples, formulários e mensagens de validação. |
| RNF02 Desempenho | SQL parametrizado, índices e transações; **não foram realizados testes de carga ou medição de latência**. |
| RNF03 Segurança | Hash PBKDF2 de senhas, autenticação via cookie, anti-CSRF nos POSTs, checagem de papel no servidor e SQL parametrizado. Antes de produção, precisam ser adicionados HTTPS obrigatório, auditoria mais robusta, backup, rate limit, política de senhas, segurança de dados e revisão jurídica/LGPD. |
| RNF04 Responsividade | CSS com grids e adaptações para desktop, tablet e celular. |
| RNF05 Integridade | Saldo e lotes atualizados em transação no MySQL; vendas e caixa são atômicos. **Não alterar os saldos diretamente por SQL.** |

### Permissões do painel

- **Cliente:** apenas catálogo, pedido direto, cadastro/login/perfil; bloqueado no servidor para qualquer ação `/Admin` (incluindo chamadas manuais à URL).
- **Funcionário:** dashboard, estoque, produtos, PDV, vendas, pedidos, financeiro, movimentação de caixa.
- **Dono:** tudo acima e criação de novas contas de funcionários.
- Uma simples ocultação do menu não garante segurança; o controlador usa `[Authorize(Roles = "Funcionario,Dono")]` e a sessão é validada contra o cadastro do banco.

### Limitações e recomendações antes de uso real

Este é um **protótipo acadêmico**. Produtos, preços e estoques exemplificativos não representam oferta comercial. Pagamentos por cartão são apenas **formas escolhidas**, sem autenticação com adquirente ou gateway. A tela de pedido recebe uma *solicitação*, não confirma pagamento nem reserva estoque. Não há gestão de estorno, cancelamento, fechamento diário, múltiplos caixas/filiais, fornecedores, controle de acesso refinado a dados financeiros ou relatórios fiscais/NF-e. A verificação de maioridade é apenas uma declaração no protótipo e **não substitui a verificação legal de identidade e conformidade para venda de bebidas alcoólicas e tabaco**.

Os lotes incluem validade opcional; para uma operação real, exija o vencimento onde aplicável e defina critérios de bloqueio/recall. Os dados pessoais de pedidos exigem política de retenção, privacidade e controles de acesso antes de uso real. Não utilize dados pessoais verdadeiros no modo Demo.

## 7. Organização das pastas

```text
Adega2L-PIM-Completo/
├── Program.cs                         # DI, cookies, autorização e bootstrap do Dono
├── Controllers/                       # Home, Produtos, Conta, Pedidos, Admin
├── Models/                            # Produto, Usuario, Venda, Lote, Caixa ...
├── Repositories/
│   ├── Demo*.cs                       # Persistência temporária em memória
│   ├── MySql*.cs                      # Acesso a dados SQL parametrizado e transações
│   └── I*.cs                          # Interfaces reutilizadas pelo site
├── Views/                              # HTML + Razor (loja e administrativo)
├── ViewModels/                         # Campos/validação de formulários
├── Services/ServicoSenha.cs            # Hash PBKDF2
├── Data/                               # Seletor de origem e fábrica MySQL
├── wwwroot/css/site.css                # Estilos responsivos
├── wwwroot/js/site.js                  # Interações de layout
├── wwwroot/images/                    # SVGs ilustrativos
├── database/01_ADEGA2L_BANCO_COMPLETO.sql
├── Properties/launchSettings.json
└── INICIAR-SITE.bat
```

**Importante:** é necessário ter o SDK do .NET instalado. Este projeto foi montado e revisado estaticamente nesta sessão, mas **não foi compilado/testado com `dotnet build` neste ambiente**, porque o comando `dotnet` não está instalado aqui. Faça `dotnet restore` e `dotnet build` no seu PC antes da entrega acadêmica.

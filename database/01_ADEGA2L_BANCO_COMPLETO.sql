-- ADEGA2L PIM · Banco MySQL 8 / MariaDB 10.4+ (XAMPP)
-- INSTRUÇÕES: phpMyAdmin -> Importar -> este arquivo; rodar uma vez.
-- Todas as tabelas abaixo são utilizadas pelo site em modo MySQL.
-- Nenhuma senha/conta de dono vem embutida. Criar dono pelo comando local --criar-dono.
-- Carrinho não existe (atualização futura).
CREATE DATABASE IF NOT EXISTS `adega2l` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `adega2l`;

CREATE TABLE IF NOT EXISTS `ADG2L_Categorias` (
  `idCategoria` INT NOT NULL AUTO_INCREMENT,
  `nomeCategoria` VARCHAR(60) NOT NULL,
  `ativo` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`idCategoria`), UNIQUE KEY `uq_categoria` (`nomeCategoria`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `ADG2L_Produtos` (
  `idProduto` INT NOT NULL AUTO_INCREMENT,
  `idCategoria` INT NOT NULL,
  `nomeProduto` VARCHAR(180) NOT NULL,
  `descricaoBebidas` TEXT NULL,
  `precoProduto` DECIMAL(12,2) NOT NULL DEFAULT 0,
  `precoCusto` DECIMAL(12,2) NOT NULL DEFAULT 0,
  `quantidadeEstoqueProduto` INT NOT NULL DEFAULT 0,
  `estoqueMinimo` INT NOT NULL DEFAULT 5,
  `imagemProduto` VARCHAR(255) NULL,
  `tipoUnidade` VARCHAR(40) NULL,
  `tamanhoUnidade` VARCHAR(40) NULL,
  `ativo` TINYINT(1) NOT NULL DEFAULT 1,
  `destaque` TINYINT(1) NOT NULL DEFAULT 0,
  `criadoEm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `atualizadoEm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`idProduto`),
  KEY `ix_produto_categoria` (`idCategoria`, `ativo`),
  KEY `ix_produto_nome` (`nomeProduto`),
  CONSTRAINT `fk_produto_categoria` FOREIGN KEY (`idCategoria`) REFERENCES `ADG2L_Categorias` (`idCategoria`) ON UPDATE CASCADE,
  CONSTRAINT `ck_produto_valores` CHECK (`precoProduto`>=0 AND `precoCusto`>=0 AND `quantidadeEstoqueProduto`>=0 AND `estoqueMinimo`>=0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Cadastro público atribui exclusivamente Cliente. Funcionário só é cadastrado pelo Dono.
CREATE TABLE IF NOT EXISTS `ADG2L_Usuarios` (
  `idUsuario` INT NOT NULL AUTO_INCREMENT,
  `nomeUsuario` VARCHAR(150) NOT NULL,
  `emailUsuario` VARCHAR(200) NOT NULL,
  `senhaUsuario` VARCHAR(255) NOT NULL,
  `papel` ENUM('Cliente','Funcionario','Dono') NOT NULL DEFAULT 'Cliente',
  `cpfUsuario` CHAR(11) NULL,
  `tipoLogradouro` VARCHAR(30) NOT NULL DEFAULT '',
  `nomeLogradouro` VARCHAR(150) NOT NULL DEFAULT '',
  `numeroLogradouro` VARCHAR(16) NOT NULL DEFAULT '',
  `complementoLogradouro` VARCHAR(100) NOT NULL DEFAULT '',
  `bairro` VARCHAR(100) NOT NULL DEFAULT '',
  `cidade` VARCHAR(100) NOT NULL DEFAULT '',
  `cep` CHAR(8) NULL,
  `DDI` VARCHAR(3) NOT NULL DEFAULT '55',
  `DDD` CHAR(2) NOT NULL DEFAULT '',
  `numeroTelefone` VARCHAR(9) NOT NULL DEFAULT '',
  `dataNasc` DATE NULL,
  `ativo` TINYINT(1) NOT NULL DEFAULT 1,
  `criadoEm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`idUsuario`), UNIQUE KEY `uq_usuario_email` (`emailUsuario`), UNIQUE KEY `uq_usuario_cpf` (`cpfUsuario`),
  KEY `ix_usuario_papel` (`papel`, `ativo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Validade por lote e saldo detalhado. O saldo de ADG2L_Produtos corresponde a soma dos lotes.
CREATE TABLE IF NOT EXISTS `ADG2L_Lotes` (
  `idLote` INT NOT NULL AUTO_INCREMENT,
  `idProduto` INT NOT NULL,
  `codigoLote` VARCHAR(40) NOT NULL,
  `quantidadeAtual` INT NOT NULL,
  `dataValidade` DATE NULL,
  `criadoEm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`idLote`), UNIQUE KEY `uq_lote_codigo` (`codigoLote`),
  KEY `ix_lote_validade` (`dataValidade`,`quantidadeAtual`),
  KEY `ix_lote_produto` (`idProduto`,`quantidadeAtual`),
  CONSTRAINT `fk_lote_produto` FOREIGN KEY (`idProduto`) REFERENCES `ADG2L_Produtos` (`idProduto`) ON UPDATE CASCADE,
  CONSTRAINT `ck_lote_quantidade` CHECK (`quantidadeAtual`>=0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Online = pedido PENDENTE; PDV = CONCLUIDA. Não há processamento real de cartão.
CREATE TABLE IF NOT EXISTS `ADG2L_Vendas` (
  `idVenda` INT NOT NULL AUTO_INCREMENT,
  `origem` ENUM('PDV','ONLINE') NOT NULL,
  `status` ENUM('PENDENTE','CONCLUIDA','CANCELADA') NOT NULL,
  `metodoPagamento` ENUM('DINHEIRO','DEBITO','CREDITO') NOT NULL,
  `clienteNome` VARCHAR(120) NULL,
  `clienteTelefone` VARCHAR(20) NULL,
  `clienteEndereco` VARCHAR(240) NULL,
  `idUsuario` INT NULL,
  `total` DECIMAL(12,2) NOT NULL,
  `dataVenda` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`idVenda`),
  KEY `ix_vendas_data_status` (`dataVenda`,`status`),
  KEY `ix_vendas_origem_status` (`origem`,`status`),
  CONSTRAINT `fk_venda_usuario` FOREIGN KEY (`idUsuario`) REFERENCES `ADG2L_Usuarios` (`idUsuario`) ON DELETE SET NULL,
  CONSTRAINT `ck_venda_total` CHECK (`total`>=0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `ADG2L_ItensVenda` (
  `idItemVenda` INT NOT NULL AUTO_INCREMENT,
  `idVenda` INT NOT NULL,
  `idProduto` INT NOT NULL,
  `nomeProduto` VARCHAR(180) NOT NULL,
  `quantidade` INT NOT NULL,
  `precoUnitario` DECIMAL(12,2) NOT NULL,
  PRIMARY KEY (`idItemVenda`), KEY `ix_item_venda` (`idVenda`), KEY `ix_item_produto` (`idProduto`),
  CONSTRAINT `fk_item_venda` FOREIGN KEY (`idVenda`) REFERENCES `ADG2L_Vendas` (`idVenda`),
  CONSTRAINT `fk_item_produto` FOREIGN KEY (`idProduto`) REFERENCES `ADG2L_Produtos` (`idProduto`),
  CONSTRAINT `ck_item_valores` CHECK (`quantidade`>0 AND `precoUnitario`>=0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `ADG2L_MovimentacoesEstoque` (
  `idMovimento` INT NOT NULL AUTO_INCREMENT,
  `idProduto` INT NOT NULL,
  `idUsuario` INT NULL,
  `idVenda` INT NULL,
  `tipo` ENUM('ENTRADA','SAIDA') NOT NULL,
  `quantidade` INT NOT NULL,
  `observacao` VARCHAR(250) NOT NULL DEFAULT '',
  `dataMovimento` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`idMovimento`), KEY `ix_mov_estoque_data` (`dataMovimento`),
  CONSTRAINT `fk_mov_produto` FOREIGN KEY (`idProduto`) REFERENCES `ADG2L_Produtos` (`idProduto`),
  CONSTRAINT `fk_mov_usuario` FOREIGN KEY (`idUsuario`) REFERENCES `ADG2L_Usuarios` (`idUsuario`) ON DELETE SET NULL,
  CONSTRAINT `fk_mov_venda` FOREIGN KEY (`idVenda`) REFERENCES `ADG2L_Vendas` (`idVenda`),
  CONSTRAINT `ck_mov_quantidade` CHECK (`quantidade`>0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `ADG2L_CaixaMovimentos` (
  `idMovimento` INT NOT NULL AUTO_INCREMENT,
  `idUsuario` INT NULL,
  `idVenda` INT NULL,
  `tipo` ENUM('ENTRADA','SAIDA') NOT NULL,
  `origem` ENUM('VENDA','MANUAL') NOT NULL,
  `valor` DECIMAL(12,2) NOT NULL,
  `descricao` VARCHAR(220) NOT NULL,
  `dataMovimento` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`idMovimento`), UNIQUE KEY `uq_caixa_venda` (`idVenda`), KEY `ix_caixa_data` (`dataMovimento`),
  CONSTRAINT `fk_caixa_usuario` FOREIGN KEY (`idUsuario`) REFERENCES `ADG2L_Usuarios` (`idUsuario`) ON DELETE SET NULL,
  CONSTRAINT `fk_caixa_venda` FOREIGN KEY (`idVenda`) REFERENCES `ADG2L_Vendas` (`idVenda`),
  CONSTRAINT `ck_caixa_valor` CHECK (`valor`>0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO `ADG2L_Categorias` (`idCategoria`,`nomeCategoria`) VALUES
 (1,'Bebidas'),(2,'Destilados'),(3,'Snacks'),(4,'Cigarros');

-- Dados fictícios do catálogo de demonstração (podem ser substituídos pela adega).
-- Para configurar banco vazio: remova os INSERTs de produtos e lotes no fim do arquivo.

-- ADEGA2L | Produtos EXEMPLIFICATIVOS apenas para teste academico.
-- Estes dados ja estao incluidos neste arquivo unico de importacao.
-- Os precos e descricoes abaixo nao representam ofertas verdadeiras.
USE `adega2l`;

INSERT IGNORE INTO `ADG2L_Produtos`
    (`idProduto`, `idCategoria`, `nomeProduto`, `descricaoBebidas`, `precoProduto`,
     `quantidadeEstoqueProduto`, `imagemProduto`, `tipoUnidade`,
     `tamanhoUnidade`, `ativo`, `destaque`)
VALUES
    (1, 1, 'Cerveja Premium 350 ml', 'Cerveja gelada para ocasiões especiais.', 7.90, 10, 'bebidas.svg', 'unidade', NULL, 1, 1),
    (2, 1, 'Refrigerante Cola 2 L', 'Refrigerante de 2 litros para compartilhar.', 11.99, 10, 'bebidas.svg', 'unidade', NULL, 1, 1),
    (3, 1, 'Energético 473 ml', 'Bebida energética em lata.', 9.50, 10, 'bebidas.svg', 'unidade', NULL, 1, 1),
    (4, 1, 'Água Mineral 500 ml', 'Água mineral sem gás.', 3.00, 10, 'bebidas.svg', 'unidade', NULL, 1, 1),
    (5, 1, 'Suco de Uva 1 L', 'Bebida de uva para toda a família.', 8.90, 10, 'bebidas.svg', 'unidade', NULL, 1, 1),
    (6, 1, 'Cerveja Lager 600 ml', 'Cerveja estilo lager em garrafa.', 12.90, 10, 'bebidas.svg', 'unidade', NULL, 1, 0),
    (7, 1, 'Refrigerante Guaraná 2 L', 'Refrigerante sabor guaraná.', 10.50, 10, 'bebidas.svg', 'unidade', NULL, 1, 0),
    (8, 1, 'Água com Gás 500 ml', 'Água mineral com gás.', 3.90, 10, 'bebidas.svg', 'unidade', NULL, 1, 0),
    (9, 2, 'Gin London Dry 750 ml', 'Gin de estilo clássico, para maiores de 18 anos.', 89.90, 10, 'destilados.svg', 'unidade', NULL, 1, 1),
    (10, 2, 'Vodka Tradicional 1 L', 'Vodka tradicional, para maiores de 18 anos.', 49.90, 10, 'destilados.svg', 'unidade', NULL, 1, 1),
    (11, 2, 'Whisky Blended 1 L', 'Whisky blended, para maiores de 18 anos.', 139.90, 10, 'destilados.svg', 'unidade', NULL, 1, 1),
    (12, 2, 'Rum Branco 980 ml', 'Rum branco, para maiores de 18 anos.', 44.90, 10, 'destilados.svg', 'unidade', NULL, 1, 1),
    (13, 2, 'Licor de Chocolate 720 ml', 'Licor, para maiores de 18 anos.', 72.90, 10, 'destilados.svg', 'unidade', NULL, 1, 1),
    (14, 2, 'Cachaça Artesanal 700 ml', 'Cachaça, para maiores de 18 anos.', 39.90, 10, 'destilados.svg', 'unidade', NULL, 1, 0),
    (15, 2, 'Gin de Frutas 750 ml', 'Gin aromatizado, para maiores de 18 anos.', 95.90, 10, 'destilados.svg', 'unidade', NULL, 1, 0),
    (16, 3, 'Batata Chips Original', 'Batata crocante e levemente salgada.', 8.99, 10, 'snacks.svg', 'unidade', NULL, 1, 1),
    (17, 3, 'Amendoim Torrado 150 g', 'Amendoim torrado para petiscar.', 6.50, 10, 'snacks.svg', 'unidade', NULL, 1, 1),
    (18, 3, 'Salgadinho de Queijo', 'Salgadinho crocante sabor queijo.', 6.99, 10, 'snacks.svg', 'unidade', NULL, 1, 1),
    (19, 3, 'Chocolate ao Leite 90 g', 'Chocolate ao leite em barra.', 7.50, 10, 'snacks.svg', 'unidade', NULL, 1, 1),
    (20, 3, 'Biscoito Recheado', 'Biscoito doce recheado.', 4.20, 10, 'snacks.svg', 'unidade', NULL, 1, 1),
    (21, 3, 'Pipoca de Micro-ondas', 'Pipoca salgada para preparar em casa.', 5.80, 10, 'snacks.svg', 'unidade', NULL, 1, 0),
    (22, 3, 'Mix de Castanhas 100 g', 'Mix de castanhas selecionadas.', 13.90, 10, 'snacks.svg', 'unidade', NULL, 1, 0),
    (23, 4, 'Cigarros Clássicos - Maço', 'Produto destinado exclusivamente a maiores de 18 anos.', 12.00, 10, 'cigarros.svg', 'unidade', NULL, 1, 1),
    (24, 4, 'Cigarros Slim - Maço', 'Produto destinado exclusivamente a maiores de 18 anos.', 13.50, 10, 'cigarros.svg', 'unidade', NULL, 1, 1),
    (25, 4, 'Fumo para Enrolar 30 g', 'Produto destinado exclusivamente a maiores de 18 anos.', 19.90, 10, 'cigarros.svg', 'unidade', NULL, 1, 1),
    (26, 4, 'Papel para Enrolar', 'Acessório para fumantes adultos.', 4.90, 10, 'cigarros.svg', 'unidade', NULL, 1, 1),
    (27, 4, 'Isqueiro Simples', 'Isqueiro básico.', 4.50, 10, 'cigarros.svg', 'unidade', NULL, 1, 1),
    (28, 4, 'Filtro para Enrolar', 'Acessório para fumantes adultos.', 6.90, 10, 'cigarros.svg', 'unidade', NULL, 1, 0);


-- Um lote inicial por produto de exemplo. Saldo de lotes e saldo de produto permanecem iguais.
INSERT INTO `ADG2L_Lotes` (`idProduto`,`codigoLote`,`quantidadeAtual`,`dataValidade`)
SELECT p.idProduto, CONCAT('INICIAL-',p.idProduto),p.quantidadeEstoqueProduto,
       CASE WHEN MOD(p.idProduto,6)=0 THEN DATE_ADD(CURDATE(),INTERVAL 20 DAY)
            ELSE DATE_ADD(CURDATE(),INTERVAL 1 YEAR) END
FROM `ADG2L_Produtos` p
WHERE p.quantidadeEstoqueProduto>0
 AND NOT EXISTS (SELECT 1 FROM `ADG2L_Lotes` l WHERE l.idProduto=p.idProduto);

-- As contas Cliente/Funcionario/Dono NÃO são criadas por SQL com senhas simples.
-- Inicie primeiro dono com variáveis de ambiente e: dotnet run -- --criar-dono

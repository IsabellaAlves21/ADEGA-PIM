using System.Data;
using Adega2L.Data;
using Adega2L.Models;
using MySqlConnector;

namespace Adega2L.Repositories;

// Todas as operacoes críticas usam transacao e bloqueio FOR UPDATE.
// Não existe fallback silencioso de MySQL para demo: isso evita perda de dados real.
public sealed class MySqlGestaoRepository(FabricaConexaoMySql fabrica, IProdutoRepository catalogo) : IGestaoRepository
{
    private static MySqlCommand Comando(MySqlConnection c, string sql, MySqlTransaction? t = null) => new(sql, c, t);
    private static void Param(MySqlCommand c, string n, object? v) => c.Parameters.AddWithValue(n, v ?? DBNull.Value);
    private static bool PagamentoOk(string p) => p is "DINHEIRO" or "DEBITO" or "CREDITO";

    public async Task<DashboardResumo> ResumoAsync(CancellationToken ct = default)
    {
        await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var cmd = Comando(c, """
            SELECT (SELECT COALESCE(SUM(total),0) FROM ADG2L_Vendas WHERE status='CONCLUIDA') fat,
                   (SELECT COALESCE(SUM(valor),0) FROM ADG2L_CaixaMovimentos WHERE tipo='SAIDA') saidas,
                   (SELECT COALESCE(SUM(CASE WHEN tipo='ENTRADA' THEN valor ELSE -valor END),0) FROM ADG2L_CaixaMovimentos) saldo,
                   (SELECT COUNT(*) FROM ADG2L_Vendas WHERE status='CONCLUIDA') vendas,
                   (SELECT COUNT(*) FROM ADG2L_Vendas WHERE status='PENDENTE') pedidos,
                   (SELECT COUNT(*) FROM ADG2L_Produtos WHERE ativo=1 AND quantidadeEstoqueProduto <= estoqueMinimo) baixos,
                   (SELECT COUNT(*) FROM ADG2L_Lotes WHERE quantidadeAtual > 0 AND dataValidade IS NOT NULL AND dataValidade <= DATE_ADD(CURDATE(), INTERVAL 30 DAY)) vencendo
            """);
        await using var r = await cmd.ExecuteReaderAsync(ct); await r.ReadAsync(ct);
        return new DashboardResumo { Faturamento = r.GetDecimal("fat"), SaidasCaixa = r.GetDecimal("saidas"), SaldoCaixa = r.GetDecimal("saldo"),
            VendasConcluidas = r.GetInt32("vendas"), PedidosPendentes = r.GetInt32("pedidos"), AlertasEstoque = r.GetInt32("baixos"), AlertasVencimento = r.GetInt32("vencendo") };
    }
    public Task<IReadOnlyList<Produto>> ProdutosAsync(CancellationToken ct = default) => catalogo.ListarAsync(ct);
    public async Task<IReadOnlyList<Lote>> LotesAsync(CancellationToken ct = default)
    {
        var result = new List<Lote>(); await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var cmd = Comando(c, "SELECT l.idLote,l.idProduto,p.nomeProduto,l.codigoLote,l.quantidadeAtual,l.dataValidade FROM ADG2L_Lotes l JOIN ADG2L_Produtos p ON p.idProduto=l.idProduto WHERE l.quantidadeAtual > 0 ORDER BY l.dataValidade IS NULL,l.dataValidade");
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) result.Add(new Lote { Id = r.GetInt32("idLote"), ProdutoId = r.GetInt32("idProduto"), Produto = r.GetString("nomeProduto"),
            Codigo = r.GetString("codigoLote"), Quantidade = r.GetInt32("quantidadeAtual"), Vencimento = r.IsDBNull(r.GetOrdinal("dataValidade")) ? null : r.GetDateTime("dataValidade") });
        return result;
    }
    public async Task<IReadOnlyList<MovimentoEstoque>> EstoqueHistoricoAsync(CancellationToken ct = default)
    {
        var result = new List<MovimentoEstoque>(); await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var cmd = Comando(c, "SELECT m.idMovimento,m.idProduto,p.nomeProduto,m.tipo,m.quantidade,m.observacao,m.dataMovimento FROM ADG2L_MovimentacoesEstoque m JOIN ADG2L_Produtos p ON p.idProduto=m.idProduto ORDER BY m.dataMovimento DESC,m.idMovimento DESC LIMIT 100");
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) result.Add(new MovimentoEstoque { Id = r.GetInt32("idMovimento"), ProdutoId = r.GetInt32("idProduto"), Produto = r.GetString("nomeProduto"),
            Tipo = r.GetString("tipo"), Quantidade = r.GetInt32("quantidade"), Observacao = r.IsDBNull(r.GetOrdinal("observacao")) ? "" : r.GetString("observacao"), Data = r.GetDateTime("dataMovimento") });
        return result;
    }
    public async Task<IReadOnlyList<Venda>> VendasAsync(CancellationToken ct = default)
    {
        var vendas = new List<Venda>(); await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using (var cmd = Comando(c, "SELECT idVenda,origem,status,metodoPagamento,clienteNome,clienteTelefone,clienteEndereco,dataVenda FROM ADG2L_Vendas ORDER BY idVenda DESC LIMIT 150"))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            while (await r.ReadAsync(ct)) vendas.Add(new Venda { Id = r.GetInt32("idVenda"), Origem = r.GetString("origem"), Status = r.GetString("status"),
                Pagamento = r.GetString("metodoPagamento"), Cliente = r.IsDBNull(r.GetOrdinal("clienteNome")) ? "" : r.GetString("clienteNome"),
                Telefone = r.IsDBNull(r.GetOrdinal("clienteTelefone")) ? "" : r.GetString("clienteTelefone"),
                Endereco = r.IsDBNull(r.GetOrdinal("clienteEndereco")) ? "" : r.GetString("clienteEndereco"), Data = r.GetDateTime("dataVenda") });
        }
        if (vendas.Count == 0) return vendas;
        await using var ic = Comando(c, "SELECT i.idVenda,i.idProduto,i.nomeProduto,i.quantidade,i.precoUnitario FROM ADG2L_ItensVenda i JOIN (SELECT idVenda FROM ADG2L_Vendas ORDER BY idVenda DESC LIMIT 150) v ON v.idVenda=i.idVenda");
        await using var ir = await ic.ExecuteReaderAsync(ct);
        var dict = vendas.ToDictionary(x => x.Id);
        while (await ir.ReadAsync(ct)) dict[ir.GetInt32("idVenda")].Itens.Add(new ItemVenda { ProdutoId = ir.GetInt32("idProduto"), NomeProduto = ir.GetString("nomeProduto"), Quantidade = ir.GetInt32("quantidade"), PrecoUnitario = ir.GetDecimal("precoUnitario") });
        return vendas;
    }
    public async Task<IReadOnlyList<MovimentoCaixa>> CaixaAsync(CancellationToken ct = default)
    {
        var result = new List<MovimentoCaixa>(); await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var cmd = Comando(c, "SELECT idMovimento,tipo,origem,valor,descricao,dataMovimento FROM ADG2L_CaixaMovimentos ORDER BY idMovimento DESC LIMIT 150");
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) result.Add(new MovimentoCaixa { Id = r.GetInt32("idMovimento"), Tipo = r.GetString("tipo"), Origem = r.GetString("origem"), Valor = r.GetDecimal("valor"), Descricao = r.GetString("descricao"), Data = r.GetDateTime("dataMovimento") });
        return result;
    }
    public async Task<IReadOnlyList<ParticipacaoVenda>> ParticipacaoAsync(CancellationToken ct = default)
    {
        var result = new List<ParticipacaoVenda>(); await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var cmd = Comando(c, """
            SELECT i.nomeProduto, SUM(i.quantidade) unidades,
                   ROUND(SUM(i.quantidade) * 100.0 / NULLIF((SELECT SUM(i2.quantidade) FROM ADG2L_ItensVenda i2 JOIN ADG2L_Vendas v2 ON v2.idVenda=i2.idVenda WHERE v2.status='CONCLUIDA'),0),2) percentual
            FROM ADG2L_ItensVenda i JOIN ADG2L_Vendas v ON v.idVenda=i.idVenda
            WHERE v.status='CONCLUIDA' GROUP BY i.idProduto,i.nomeProduto ORDER BY unidades DESC
            """);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) result.Add(new ParticipacaoVenda { Produto = r.GetString("nomeProduto"), Unidades = Convert.ToInt32(r["unidades"]), Percentual = r.IsDBNull(r.GetOrdinal("percentual")) ? 0 : r.GetDecimal("percentual") });
        return result;
    }
    public async Task<int> CriarProdutoAsync(Produto p, int usuarioId, CancellationToken ct = default)
    {
        if (p.Preco < 0 || p.Custo < 0 || p.EstoqueMinimo < 0) throw new InvalidOperationException("Preço, custo e estoque mínimo não podem ser negativos.");
        await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var cmd = Comando(c, """
            INSERT INTO ADG2L_Produtos(idCategoria,nomeProduto,descricaoBebidas,precoProduto,precoCusto,quantidadeEstoqueProduto,estoqueMinimo,imagemProduto,tipoUnidade,ativo,destaque)
            SELECT idCategoria,@nome,@descricao,@preco,@custo,0,@minimo,@imagem,'unidade',1,0 FROM ADG2L_Categorias WHERE nomeCategoria=@categoria
            """);
        Param(cmd,"@nome",p.Nome); Param(cmd,"@descricao",p.Descricao); Param(cmd,"@preco",p.Preco); Param(cmd,"@custo",p.Custo); Param(cmd,"@minimo",p.EstoqueMinimo);
        Param(cmd,"@imagem",p.Categoria.ToLowerInvariant() + ".svg"); Param(cmd,"@categoria",p.Categoria);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new InvalidOperationException("Categoria não encontrada.");
        return Convert.ToInt32(cmd.LastInsertedId);
    }
    public async Task MovimentarEstoqueAsync(int produtoId, int quantidade, string tipo, DateTime? vencimento, string observacao, int usuarioId, CancellationToken ct = default)
    {
        if (quantidade <= 0 || quantidade > 100000 || (tipo != "ENTRADA" && tipo != "SAIDA")) throw new InvalidOperationException("Movimentação inválida.");
        await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var p = await BloquearProduto(c,t,produtoId,ct);
        if (tipo == "ENTRADA")
        {
            await using var lote = Comando(c, "INSERT INTO ADG2L_Lotes(idProduto,codigoLote,quantidadeAtual,dataValidade) VALUES(@id,@codigo,@qtd,@validade)", t);
            Param(lote,"@id",produtoId); Param(lote,"@codigo",$"ENT-{Guid.NewGuid():N}"); Param(lote,"@qtd",quantidade); Param(lote,"@validade",vencimento);
            await lote.ExecuteNonQueryAsync(ct);
            await AlterarSaldo(c,t,produtoId,quantidade,ct);
        }
        else await ConsumirEstoque(c,t,produtoId,quantidade,p.Estoque,true,ct);
        await InserirMovimento(c,t,produtoId,tipo,quantidade,observacao,usuarioId,null,ct);
        await t.CommitAsync(ct);
    }
    private static async Task<(string Nome,decimal Preco,int Estoque)> BloquearProduto(MySqlConnection c, MySqlTransaction t, int id, CancellationToken ct)
    {
        await using var cmd = Comando(c,"SELECT nomeProduto,precoProduto,quantidadeEstoqueProduto FROM ADG2L_Produtos WHERE idProduto=@id AND ativo=1 FOR UPDATE",t);
        Param(cmd,"@id",id);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) throw new InvalidOperationException("Produto inválido ou inativo.");
        return (r.GetString("nomeProduto"),r.GetDecimal("precoProduto"),r.GetInt32("quantidadeEstoqueProduto"));
    }
    private static async Task AlterarSaldo(MySqlConnection c, MySqlTransaction t, int id, int delta, CancellationToken ct)
    {
        await using var cmd = Comando(c,"UPDATE ADG2L_Produtos SET quantidadeEstoqueProduto=quantidadeEstoqueProduto+@delta WHERE idProduto=@id",t);
        Param(cmd,"@id",id); Param(cmd,"@delta",delta);
        if (await cmd.ExecuteNonQueryAsync(ct)!=1) throw new InvalidOperationException("Falha ao atualizar estoque.");
    }
    private static async Task ConsumirEstoque(MySqlConnection c, MySqlTransaction t, int id, int qtd, int saldo, bool permitirVencidos, CancellationToken ct)
    {
        if (saldo < qtd) throw new InvalidOperationException("Estoque insuficiente.");
        var lotes = new List<(int Id,int Qtd)>();
        await using (var cmd = Comando(c,"SELECT idLote,quantidadeAtual FROM ADG2L_Lotes WHERE idProduto=@id AND quantidadeAtual>0 AND (@permitir=1 OR dataValidade IS NULL OR dataValidade >= CURDATE()) ORDER BY dataValidade IS NULL,dataValidade,idLote FOR UPDATE",t))
        {
            Param(cmd,"@id",id); Param(cmd,"@permitir",permitirVencidos ? 1 : 0);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) lotes.Add((r.GetInt32("idLote"),r.GetInt32("quantidadeAtual")));
        }
        if (lotes.Sum(l=>l.Qtd)<qtd) throw new InvalidOperationException("Saldo dos lotes não corresponde ao estoque. Corrija antes de vender.");
        var rest = qtd;
        foreach(var l in lotes)
        {
            if (rest == 0) break;
            int retirar = Math.Min(l.Qtd,rest);
            await using var cmd = Comando(c,"UPDATE ADG2L_Lotes SET quantidadeAtual=quantidadeAtual-@qtd WHERE idLote=@id",t);
            Param(cmd,"@qtd",retirar); Param(cmd,"@id",l.Id); await cmd.ExecuteNonQueryAsync(ct); rest -= retirar;
        }
        await AlterarSaldo(c,t,id,-qtd,ct);
    }
    
private static async Task InserirMovimento(MySqlConnection c, MySqlTransaction t, int id, string tipo, int qtd, string obs, int usuarioId, int? vendaId, CancellationToken ct)
{
    obs = string.IsNullOrWhiteSpace(obs)
        ? "Sem observação"
        : obs;

    await using var cmd = Comando(c,"INSERT INTO ADG2L_MovimentacoesEstoque(idProduto,tipo,quantidade,observacao,idUsuario,idVenda) VALUES(@id,@tipo,@qtd,@obs,@usuario,@venda)",t);

    Param(cmd,"@id",id);
    Param(cmd,"@tipo",tipo);
    Param(cmd,"@qtd",qtd);
    Param(cmd,"@obs",obs);
    Param(cmd,"@usuario",usuarioId==0?DBNull.Value:usuarioId);
    Param(cmd,"@venda",vendaId);

    await cmd.ExecuteNonQueryAsync(ct);
}

    private static async Task<int> InserirVenda(MySqlConnection c, MySqlTransaction t, string origem, string status, string pagamento, string cliente, string tel, string endereco, int? usuario, decimal total, CancellationToken ct)
    {
        await using var cmd = Comando(c,"INSERT INTO ADG2L_Vendas(origem,status,metodoPagamento,clienteNome,clienteTelefone,clienteEndereco,idUsuario,total) VALUES(@origem,@status,@pagamento,@cliente,@tel,@endereco,@usuario,@total)",t);
        Param(cmd,"@origem",origem); Param(cmd,"@status",status); Param(cmd,"@pagamento",pagamento); Param(cmd,"@cliente",string.IsNullOrWhiteSpace(cliente)?DBNull.Value:cliente);
        Param(cmd,"@tel",string.IsNullOrWhiteSpace(tel)?DBNull.Value:tel); Param(cmd,"@endereco",string.IsNullOrWhiteSpace(endereco)?DBNull.Value:endereco);
        Param(cmd,"@usuario",usuario); Param(cmd,"@total",total);
        await cmd.ExecuteNonQueryAsync(ct); return Convert.ToInt32(cmd.LastInsertedId);
    }
    private static async Task InserirItem(MySqlConnection c, MySqlTransaction t, int vendaId, ItemVenda i, CancellationToken ct)
    {
        await using var cmd = Comando(c,"INSERT INTO ADG2L_ItensVenda(idVenda,idProduto,nomeProduto,quantidade,precoUnitario) VALUES(@venda,@produto,@nome,@quantidade,@preco)",t);
        Param(cmd,"@venda",vendaId); Param(cmd,"@produto",i.ProdutoId); Param(cmd,"@nome",i.NomeProduto); Param(cmd,"@quantidade",i.Quantidade); Param(cmd,"@preco",i.PrecoUnitario); await cmd.ExecuteNonQueryAsync(ct);
    }
    private static async Task InserirCaixa(MySqlConnection c, MySqlTransaction t, string tipo, decimal valor, string origem, string descricao, int? user, int? venda, CancellationToken ct)
    {
        await using var cmd = Comando(c,"INSERT INTO ADG2L_CaixaMovimentos(tipo,valor,origem,descricao,idUsuario,idVenda) VALUES(@tipo,@valor,@origem,@desc,@user,@venda)",t);
        Param(cmd,"@tipo",tipo);Param(cmd,"@valor",valor);Param(cmd,"@origem",origem);Param(cmd,"@desc",descricao);Param(cmd,"@user",user);Param(cmd,"@venda",venda);
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<int> VenderPdvAsync(IReadOnlyList<ItemSolicitado> itens, string pagamento, int usuarioId, CancellationToken ct = default)
    {
        if (!PagamentoOk(pagamento) || itens.Count==0 || itens.Any(x=>x.ProdutoId<=0 || x.Quantidade<=0 || x.Quantidade>10000)) throw new InvalidOperationException("Itens ou pagamento inválidos.");
        var agrupados = itens.GroupBy(x=>x.ProdutoId).Select(g=>new ItemSolicitado { ProdutoId=g.Key,Quantidade=g.Sum(x=>x.Quantidade) }).OrderBy(x=>x.ProdutoId).ToList();
        await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
        var estoque = new Dictionary<int,int>(); var detalhes = new List<ItemVenda>();
        foreach(var i in agrupados) { var p=await BloquearProduto(c,t,i.ProdutoId,ct); if(p.Estoque<i.Quantidade) throw new InvalidOperationException($"Estoque insuficiente: {p.Nome}.");
            detalhes.Add(new ItemVenda { ProdutoId=i.ProdutoId,NomeProduto=p.Nome,Quantidade=i.Quantidade,PrecoUnitario=p.Preco }); estoque[i.ProdutoId]=p.Estoque; }
        var total = detalhes.Sum(x=>x.Subtotal);
        var idVenda=await InserirVenda(c,t,"PDV","CONCLUIDA",pagamento,"","","",usuarioId,total,ct);
        foreach(var item in detalhes) { await ConsumirEstoque(c,t,item.ProdutoId,item.Quantidade,estoque[item.ProdutoId],false,ct);
            await InserirItem(c,t,idVenda,item,ct); await InserirMovimento(c,t,item.ProdutoId,"SAIDA",item.Quantidade,$"PDV #{idVenda}",usuarioId,idVenda,ct); }
        await InserirCaixa(c,t,"ENTRADA",total,"VENDA",$"PDV #{idVenda} - {pagamento}",usuarioId,idVenda,ct);
        await t.CommitAsync(ct); return idVenda;
    }
    public async Task<int> CriarPedidoOnlineAsync(PedidoSolicitado pedido, CancellationToken ct = default)
    {
        if (!PagamentoOk(pedido.Pagamento) || pedido.Quantidade<=0 || pedido.Quantidade>100 || pedido.ProdutoId<=0) throw new InvalidOperationException("Pedido inválido.");
        await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
        var p = await BloquearProduto(c,t,pedido.ProdutoId,ct);
        if(p.Estoque<pedido.Quantidade) throw new InvalidOperationException("Produto sem estoque suficiente.");
        var item=new ItemVenda { ProdutoId=pedido.ProdutoId,NomeProduto=p.Nome,PrecoUnitario=p.Preco,Quantidade=pedido.Quantidade };
        var id=await InserirVenda(c,t,"ONLINE","PENDENTE",pedido.Pagamento,pedido.Cliente,pedido.Telefone,pedido.Endereco,null,item.Subtotal,ct);
        await InserirItem(c,t,id,item,ct); await t.CommitAsync(ct); return id;
    }
    public async Task<bool> ConfirmarPedidoAsync(int vendaId, int usuarioId, CancellationToken ct = default)
    {
        await using var c = fabrica.Criar(); await c.OpenAsync(ct);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
        decimal total; string pagamento;
        await using (var cmd = Comando(c,"SELECT status,origem,total,metodoPagamento FROM ADG2L_Vendas WHERE idVenda=@id FOR UPDATE",t))
        {
            Param(cmd,"@id",vendaId);
            await using var r=await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct) || r.GetString("status")!="PENDENTE" || r.GetString("origem")!="ONLINE") return false;
            total=r.GetDecimal("total"); pagamento=r.GetString("metodoPagamento");
        }
        var itens = new List<ItemSolicitado>();
        await using (var cmd=Comando(c,"SELECT idProduto,quantidade FROM ADG2L_ItensVenda WHERE idVenda=@id",t))
        {
            Param(cmd,"@id",vendaId); await using var r=await cmd.ExecuteReaderAsync(ct);
            while(await r.ReadAsync(ct)) itens.Add(new ItemSolicitado { ProdutoId=r.GetInt32("idProduto"),Quantidade=r.GetInt32("quantidade") });
        }
        var saldos = new Dictionary<int,int>();
        foreach(var i in itens.OrderBy(i=>i.ProdutoId)) { var p=await BloquearProduto(c,t,i.ProdutoId,ct); if(p.Estoque<i.Quantidade) throw new InvalidOperationException("Estoque insuficiente para confirmar o pedido."); saldos[i.ProdutoId]=p.Estoque; }
        foreach(var i in itens.OrderBy(i=>i.ProdutoId)) { await ConsumirEstoque(c,t,i.ProdutoId,i.Quantidade,saldos[i.ProdutoId],false,ct);
            await InserirMovimento(c,t,i.ProdutoId,"SAIDA",i.Quantidade,$"Pedido #{vendaId}",usuarioId,vendaId,ct); }
        await using (var cmd=Comando(c,"UPDATE ADG2L_Vendas SET status='CONCLUIDA',idUsuario=@usuario WHERE idVenda=@id",t)) { Param(cmd,"@id",vendaId); Param(cmd,"@usuario",usuarioId); await cmd.ExecuteNonQueryAsync(ct); }
        await InserirCaixa(c,t,"ENTRADA",total,"VENDA",$"Pedido #{vendaId} - {pagamento}",usuarioId,vendaId,ct);
        await t.CommitAsync(ct); return true;
    }
    public async Task RegistrarCaixaAsync(string tipo, decimal valor, string descricao, int usuarioId, CancellationToken ct = default)
    {
        if (valor<=0 || (tipo!="ENTRADA" && tipo!="SAIDA")) throw new InvalidOperationException("Lançamento financeiro inválido.");
        await using var c=fabrica.Criar(); await c.OpenAsync(ct);
        await using var t=await c.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
        await InserirCaixa(c,t,tipo,valor,"MANUAL",descricao,usuarioId,null,ct);
        await t.CommitAsync(ct);
    }
}

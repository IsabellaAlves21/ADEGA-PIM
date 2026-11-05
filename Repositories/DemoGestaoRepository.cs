using Adega2L.Models;

namespace Adega2L.Repositories;

// Todas as operações em demo sao em memoria, sincronizadas. Reiniciar app zera registros.
public sealed class DemoGestaoRepository(DemoDadosStore s) : IGestaoRepository
{
    private static bool PagamentoOk(string p) => p is "DINHEIRO" or "DEBITO" or "CREDITO";
    public Task<DashboardResumo> ResumoAsync(CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var entradas = s.Caixa.Where(x => x.Tipo == "ENTRADA").Sum(x => x.Valor);
            var saidas = s.Caixa.Where(x => x.Tipo == "SAIDA").Sum(x => x.Valor);
            return Task.FromResult(new DashboardResumo {
                Faturamento = s.Vendas.Where(x => x.Status == "CONCLUIDA").Sum(x => x.Total),
                SaidasCaixa = saidas, SaldoCaixa = entradas - saidas,
                VendasConcluidas = s.Vendas.Count(x => x.Status == "CONCLUIDA"),
                PedidosPendentes = s.Vendas.Count(x => x.Status == "PENDENTE"),
                AlertasEstoque = s.Produtos.Count(x => x.Ativo && x.Estoque <= x.EstoqueMinimo),
                AlertasVencimento = s.Lotes.Count(x => x.Quantidade > 0 && x.Vencimento.HasValue && x.Vencimento.Value.Date <= DateTime.Today.AddDays(30))
            });
        }
    }
    public Task<IReadOnlyList<Produto>> ProdutosAsync(CancellationToken ct = default) { lock(s.Sync) return Task.FromResult((IReadOnlyList<Produto>)s.Produtos.ToList()); }
    public Task<IReadOnlyList<Lote>> LotesAsync(CancellationToken ct = default) { lock(s.Sync) return Task.FromResult((IReadOnlyList<Lote>)s.Lotes.Where(x => x.Quantidade > 0).OrderBy(x => x.Vencimento ?? DateTime.MaxValue).ToList()); }
    public Task<IReadOnlyList<MovimentoEstoque>> EstoqueHistoricoAsync(CancellationToken ct = default) { lock(s.Sync) return Task.FromResult((IReadOnlyList<MovimentoEstoque>)s.Movimentos.OrderByDescending(x => x.Data).Take(100).ToList()); }
    public Task<IReadOnlyList<Venda>> VendasAsync(CancellationToken ct = default) { lock(s.Sync) return Task.FromResult((IReadOnlyList<Venda>)s.Vendas.OrderByDescending(x => x.Data).ToList()); }
    public Task<IReadOnlyList<MovimentoCaixa>> CaixaAsync(CancellationToken ct = default) { lock(s.Sync) return Task.FromResult((IReadOnlyList<MovimentoCaixa>)s.Caixa.OrderByDescending(x => x.Data).Take(150).ToList()); }
    public Task<IReadOnlyList<ParticipacaoVenda>> ParticipacaoAsync(CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var itens = s.Vendas.Where(v => v.Status == "CONCLUIDA").SelectMany(v => v.Itens).ToList();
            var total = itens.Sum(x => x.Quantidade);
            return Task.FromResult((IReadOnlyList<ParticipacaoVenda>)itens.GroupBy(x => x.NomeProduto)
                .Select(g => new ParticipacaoVenda { Produto = g.Key, Unidades = g.Sum(x => x.Quantidade), Percentual = total == 0 ? 0 : 100m * g.Sum(x => x.Quantidade) / total })
                .OrderByDescending(x => x.Unidades).ToList());
        }
    }
    public Task<int> CriarProdutoAsync(Produto p, int usuarioId, CancellationToken ct = default)
    {
        lock (s.Sync) { p.Id = ++s.ProximoProduto; p.Estoque = 0; s.Produtos.Add(p); return Task.FromResult(p.Id); }
    }
    public Task MovimentarEstoqueAsync(int id, int qtd, string tipo, DateTime? vencimento, string observacao, int usuarioId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var p = s.Produtos.SingleOrDefault(x => x.Id == id && x.Ativo) ?? throw new InvalidOperationException("Produto não encontrado.");
            if (qtd <= 0 || (tipo != "ENTRADA" && tipo != "SAIDA")) throw new InvalidOperationException("Movimentação inválida.");
            if (tipo == "ENTRADA")
            {
                p.Estoque = checked(p.Estoque + qtd);
                s.Lotes.Add(new Lote { Id = ++s.ProximoLote, ProdutoId = id, Produto = p.Nome, Quantidade = qtd, Vencimento = vencimento, Codigo = $"ENT-{s.ProximoLote}" });
            }
            else Consumir(id, qtd, permitirVencidos: true);
            s.Movimentos.Add(new MovimentoEstoque { Id = ++s.ProximoEstoque, ProdutoId = id, Produto = p.Nome, Tipo = tipo, Quantidade = qtd, Observacao = observacao });
        }
        return Task.CompletedTask;
    }
    private void Consumir(int produtoId, int qtd, bool permitirVencidos = false)
    {
        var p = s.Produtos.Single(x => x.Id == produtoId);
        if (p.Estoque < qtd) throw new InvalidOperationException($"Estoque insuficiente: {p.Nome}.");
        if (s.Lotes.Where(x => x.ProdutoId == produtoId && x.Quantidade > 0 && (permitirVencidos || !x.Vencimento.HasValue || x.Vencimento.Value.Date >= DateTime.Today)).Sum(x => x.Quantidade) < qtd)
            throw new InvalidOperationException("Estoque disponível em lotes é insuficiente.");
        var restante = qtd;
        foreach (var lote in s.Lotes.Where(x => x.ProdutoId == produtoId && x.Quantidade > 0 && (permitirVencidos || !x.Vencimento.HasValue || x.Vencimento.Value.Date >= DateTime.Today)).OrderBy(x => x.Vencimento ?? DateTime.MaxValue))
        {
            var retirar = Math.Min(lote.Quantidade, restante);
            lote.Quantidade -= retirar; restante -= retirar;
            if (restante == 0) break;
        }
        if (restante > 0) throw new InvalidOperationException("Inconsistência entre lotes e saldo de estoque.");
        p.Estoque -= qtd;
    }
    public Task<int> VenderPdvAsync(IReadOnlyList<ItemSolicitado> itens, string pagamento, int usuarioId, CancellationToken ct = default)
    {
        lock(s.Sync)
        {
            if (!PagamentoOk(pagamento)) throw new InvalidOperationException("Forma de pagamento inválida.");
            if (itens.Count == 0 || itens.Any(i => i.Quantidade <= 0 || i.ProdutoId <= 0)) throw new InvalidOperationException("Adicione produtos válidos.");
            var agrupados = itens.GroupBy(x => x.ProdutoId).Select(g => new ItemSolicitado { ProdutoId = g.Key, Quantidade = g.Sum(x => x.Quantidade) }).ToList();
            var detalhes = agrupados.Select(i => {
                var p = s.Produtos.SingleOrDefault(x => x.Id == i.ProdutoId && x.Ativo) ?? throw new InvalidOperationException("Produto não encontrado.");
                if (p.Estoque < i.Quantidade) throw new InvalidOperationException($"Estoque insuficiente: {p.Nome}.");
                return new ItemVenda { ProdutoId = p.Id, NomeProduto = p.Nome, Quantidade = i.Quantidade, PrecoUnitario = p.Preco };
            }).ToList();
            // Validar todos os lotes antes de modificar o estoque.
            foreach (var i in detalhes) if (s.Lotes.Where(l => l.ProdutoId == i.ProdutoId && (!l.Vencimento.HasValue || l.Vencimento.Value.Date >= DateTime.Today)).Sum(l => l.Quantidade) < i.Quantidade) throw new InvalidOperationException("Lotes inconsistentes.");
            var venda = new Venda { Id = ++s.ProximaVenda, Origem = "PDV", Status = "CONCLUIDA", Pagamento = pagamento, Itens = detalhes };
            foreach (var i in detalhes) { Consumir(i.ProdutoId, i.Quantidade); s.Movimentos.Add(new MovimentoEstoque { Id = ++s.ProximoEstoque, ProdutoId = i.ProdutoId, Produto = i.NomeProduto, Tipo = "SAIDA", Quantidade = i.Quantidade, Observacao = $"Venda PDV #{venda.Id}" }); }
            s.Vendas.Add(venda);
            s.Caixa.Add(new MovimentoCaixa { Id = ++s.ProximoCaixa, Tipo = "ENTRADA", Origem = "VENDA", Valor = venda.Total, Descricao = $"PDV #{venda.Id} - {pagamento}" });
            return Task.FromResult(venda.Id);
        }
    }
    public Task<int> CriarPedidoOnlineAsync(PedidoSolicitado pedido, CancellationToken ct = default)
    {
        lock(s.Sync)
        {
            if (!PagamentoOk(pedido.Pagamento) || pedido.Quantidade <= 0) throw new InvalidOperationException("Pedido inválido.");
            var p = s.Produtos.SingleOrDefault(x => x.Id == pedido.ProdutoId && x.Ativo) ?? throw new InvalidOperationException("Produto não encontrado.");
            if (p.Estoque < pedido.Quantidade || s.Lotes.Where(l => l.ProdutoId==p.Id && (!l.Vencimento.HasValue || l.Vencimento.Value.Date>=DateTime.Today)).Sum(l=>l.Quantidade) < pedido.Quantidade) throw new InvalidOperationException("Produto indisponível no momento.");
            var v = new Venda { Id = ++s.ProximaVenda, Origem = "ONLINE", Status = "PENDENTE", Pagamento = pedido.Pagamento, Cliente = pedido.Cliente, Telefone = pedido.Telefone, Endereco = pedido.Endereco,
                Itens = [new ItemVenda { ProdutoId = p.Id, NomeProduto = p.Nome, PrecoUnitario = p.Preco, Quantidade = pedido.Quantidade }] };
            s.Vendas.Add(v); // Nao baixa estoque nem registra entrada de caixa até confirmação.
            return Task.FromResult(v.Id);
        }
    }
    public Task<bool> ConfirmarPedidoAsync(int vendaId, int usuarioId, CancellationToken ct = default)
    {
        lock(s.Sync)
        {
            var v = s.Vendas.FirstOrDefault(x => x.Id == vendaId && x.Origem == "ONLINE" && x.Status == "PENDENTE");
            if (v is null) return Task.FromResult(false);
            foreach (var i in v.Itens) if (s.Produtos.Single(x => x.Id == i.ProdutoId).Estoque < i.Quantidade || s.Lotes.Where(l => l.ProdutoId == i.ProdutoId && (!l.Vencimento.HasValue || l.Vencimento.Value.Date >= DateTime.Today)).Sum(l => l.Quantidade) < i.Quantidade) throw new InvalidOperationException("Estoque insuficiente para confirmar pedido.");
            foreach (var i in v.Itens) { Consumir(i.ProdutoId, i.Quantidade); s.Movimentos.Add(new MovimentoEstoque { Id = ++s.ProximoEstoque, ProdutoId = i.ProdutoId, Produto = i.NomeProduto, Tipo = "SAIDA", Quantidade = i.Quantidade, Observacao = $"Pedido #{v.Id}" }); }
            v.Status = "CONCLUIDA";
            s.Caixa.Add(new MovimentoCaixa { Id = ++s.ProximoCaixa, Tipo = "ENTRADA", Origem = "VENDA", Valor = v.Total, Descricao = $"Pedido #{v.Id} - {v.Pagamento}" });
            return Task.FromResult(true);
        }
    }
    public Task RegistrarCaixaAsync(string tipo, decimal valor, string descricao, int usuarioId, CancellationToken ct = default)
    {
        lock(s.Sync)
        {
            if (valor <= 0 || (tipo != "ENTRADA" && tipo != "SAIDA")) throw new InvalidOperationException("Lançamento inválido.");
            s.Caixa.Add(new MovimentoCaixa { Id = ++s.ProximoCaixa, Tipo = tipo, Valor = valor, Origem = "MANUAL", Descricao = descricao });
        }
        return Task.CompletedTask;
    }
}

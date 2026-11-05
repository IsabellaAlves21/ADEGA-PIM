using Adega2L.Models;

namespace Adega2L.Repositories;

public sealed class DemoDadosStore
{
    public object Sync { get; } = new();
    public List<Produto> Produtos { get; } = DemoProdutoRepository.CriarCatalogo().ToList();
    public List<Lote> Lotes { get; } = [];
    public List<MovimentoEstoque> Movimentos { get; } = [];
    public List<Venda> Vendas { get; } = [];
    public List<MovimentoCaixa> Caixa { get; } = [];
    public int ProximoProduto = 28;
    public int ProximoLote;
    public int ProximoEstoque;
    public int ProximaVenda;
    public int ProximoCaixa;

    public DemoDadosStore()
    {
        foreach (var produto in Produtos)
            Lotes.Add(new Lote { Id = ++ProximoLote, ProdutoId = produto.Id, Produto = produto.Nome,
                Codigo = $"DEMO-{produto.Id:000}", Quantidade = produto.Estoque,
                Vencimento = produto.Id % 6 == 0 ? DateTime.Today.AddDays(12) : DateTime.Today.AddMonths(10) });
    }
}

using Adega2L.Models;

namespace Adega2L.Repositories;

// As mesmas operações existem em memoria e em MySQL. Controllers nao dependem do banco.
public interface IGestaoRepository
{
    Task<DashboardResumo> ResumoAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Produto>> ProdutosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Lote>> LotesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MovimentoEstoque>> EstoqueHistoricoAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Venda>> VendasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MovimentoCaixa>> CaixaAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ParticipacaoVenda>> ParticipacaoAsync(CancellationToken ct = default);
    Task<int> CriarProdutoAsync(Produto produto, int usuarioId, CancellationToken ct = default);
    Task MovimentarEstoqueAsync(int produtoId, int quantidade, string tipo, DateTime? vencimento, string observacao, int usuarioId, CancellationToken ct = default);
    Task<int> VenderPdvAsync(IReadOnlyList<ItemSolicitado> itens, string pagamento, int usuarioId, CancellationToken ct = default);
    Task<int> CriarPedidoOnlineAsync(PedidoSolicitado pedido, CancellationToken ct = default);
    Task<bool> ConfirmarPedidoAsync(int vendaId, int usuarioId, CancellationToken ct = default);
    Task RegistrarCaixaAsync(string tipo, decimal valor, string descricao, int usuarioId, CancellationToken ct = default);
}

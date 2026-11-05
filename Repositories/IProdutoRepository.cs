using Adega2L.Models;

namespace Adega2L.Repositories;

// Trocar a implementação deste contrato NÃO exige mexer nas Views / Controllers.
public interface IProdutoRepository
{
    Task<IReadOnlyList<Produto>> ListarAsync(CancellationToken ct = default);
    Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default);
}

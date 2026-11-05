using Adega2L.Models;

namespace Adega2L.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExisteEmailOuCpfAsync(string email, string cpf, CancellationToken ct = default);
    Task<bool> ExisteDonoAsync(CancellationToken ct = default);
    Task CadastrarAsync(Usuario usuario, CancellationToken ct = default); // sempre cadastra CLIENTE
    Task CadastrarInternoAsync(Usuario usuario, CancellationToken ct = default); // apenas owner autorizado ou comando local
}

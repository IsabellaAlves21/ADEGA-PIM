using System.Collections.Concurrent;
using Adega2L.Models;
using Adega2L.Services;

namespace Adega2L.Repositories;

// Conta de exemplo: funciona apenas em Dados:Origem=Demo; nunca publicar essa configuracao.
public sealed class MemoriaUsuarioRepository : IUsuarioRepository
{
    private readonly ConcurrentDictionary<string, Usuario> _usuarios = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private int _sequencia = 2;

    public MemoriaUsuarioRepository(ServicoSenha senhas)
    {
        _usuarios["dono@demo.local"] = new Usuario { Id = 1, Nome = "Dono (demonstração)", Email = "dono@demo.local", Papel = Papeis.Dono, SenhaHash = senhas.GerarHash("Demo@12345") };
        _usuarios["funcionario@demo.local"] = new Usuario { Id = 2, Nome = "Funcionário (demonstração)", Email = "funcionario@demo.local", Papel = Papeis.Funcionario, SenhaHash = senhas.GerarHash("Demo@12345") };
    }
    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
    {
        _usuarios.TryGetValue(email.Trim(), out var u);
        return Task.FromResult(u is { Ativo: true } ? u : null);
    }
    public Task<bool> ExisteEmailOuCpfAsync(string email, string cpf, CancellationToken ct = default) =>
        Task.FromResult(_usuarios.ContainsKey(email.Trim()) || (!string.IsNullOrWhiteSpace(cpf) && _usuarios.Values.Any(u => u.Cpf == cpf)));
    public Task<bool> ExisteDonoAsync(CancellationToken ct = default) => Task.FromResult(_usuarios.Values.Any(u => u.Papel == Papeis.Dono));
    public Task CadastrarAsync(Usuario usuario, CancellationToken ct = default)
    {
        usuario.Papel = Papeis.Cliente; // formulario público nunca promove clientes
        return CadastrarInternoAsync(usuario, ct);
    }
    public Task CadastrarInternoAsync(Usuario usuario, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_usuarios.ContainsKey(usuario.Email) || (!string.IsNullOrEmpty(usuario.Cpf) && _usuarios.Values.Any(u => u.Cpf == usuario.Cpf)))
                throw new InvalidOperationException("E-mail ou CPF já cadastrado.");
            usuario.Id = ++_sequencia;
            _usuarios[usuario.Email] = usuario;
        }
        return Task.CompletedTask;
    }
}

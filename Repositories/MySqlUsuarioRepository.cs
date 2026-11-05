using Adega2L.Data;
using Adega2L.Models;
using MySqlConnector;

namespace Adega2L.Repositories;

public sealed class MySqlUsuarioRepository(FabricaConexaoMySql fabrica) : IUsuarioRepository
{
    public async Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = fabrica.Criar(); await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT idUsuario,nomeUsuario,emailUsuario,senhaUsuario,cpfUsuario,cidade,papel,ativo FROM ADG2L_Usuarios WHERE emailUsuario=@email AND ativo=1 LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new Usuario {
            Id = r.GetInt32("idUsuario"), Nome = r.GetString("nomeUsuario"), Email = r.GetString("emailUsuario"),
            SenhaHash = r.GetString("senhaUsuario"), Cpf = r.IsDBNull(r.GetOrdinal("cpfUsuario")) ? "" : r.GetString("cpfUsuario"),
            Cidade = r.IsDBNull(r.GetOrdinal("cidade")) ? "" : r.GetString("cidade"),
            Papel = r.GetString("papel"), Ativo = r.GetBoolean("ativo")
        };
    }
    public async Task<bool> ExisteEmailOuCpfAsync(string email, string cpf, CancellationToken ct = default)
    {
        await using var conn = fabrica.Criar(); await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM ADG2L_Usuarios WHERE emailUsuario=@email OR (@cpf IS NOT NULL AND cpfUsuario=@cpf)", conn);
        cmd.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant()); cmd.Parameters.AddWithValue("@cpf", string.IsNullOrWhiteSpace(cpf) ? DBNull.Value : cpf);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
    }
    public async Task<bool> ExisteDonoAsync(CancellationToken ct = default)
    {
        await using var conn = fabrica.Criar(); await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM ADG2L_Usuarios WHERE papel='Dono'", conn);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
    }
    public Task CadastrarAsync(Usuario u, CancellationToken ct = default) { u.Papel = Papeis.Cliente; return CadastrarInternoAsync(u, ct); }
    public async Task CadastrarInternoAsync(Usuario u, CancellationToken ct = default)
    {
        if (u.Papel != Papeis.Cliente && u.Papel != Papeis.Funcionario && u.Papel != Papeis.Dono) throw new InvalidOperationException("Papel inválido.");
        await using var conn = fabrica.Criar(); await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("""
            INSERT INTO ADG2L_Usuarios
            (nomeUsuario,emailUsuario,senhaUsuario,cpfUsuario,tipoLogradouro,nomeLogradouro,numeroLogradouro,
             complementoLogradouro,bairro,cidade,cep,DDI,DDD,numeroTelefone,dataNasc,papel,ativo)
            VALUES (@nome,@email,@senha,@cpf,@tipo,@rua,@numero,@complemento,@bairro,@cidade,@cep,@ddi,@ddd,@telefone,@nascimento,@papel,1)
            """, conn);
        cmd.Parameters.AddWithValue("@nome", u.Nome);
        cmd.Parameters.AddWithValue("@email", u.Email.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@senha", u.SenhaHash);
        cmd.Parameters.AddWithValue("@cpf", string.IsNullOrWhiteSpace(u.Cpf) ? DBNull.Value : u.Cpf);
        cmd.Parameters.AddWithValue("@tipo", u.TipoLogradouro);
        cmd.Parameters.AddWithValue("@rua", u.NomeLogradouro);
        cmd.Parameters.AddWithValue("@numero", u.NumeroLogradouro);
        cmd.Parameters.AddWithValue("@complemento", u.ComplementoLogradouro);
        cmd.Parameters.AddWithValue("@bairro", u.Bairro);
        cmd.Parameters.AddWithValue("@cidade", u.Cidade);
        cmd.Parameters.AddWithValue("@cep", string.IsNullOrWhiteSpace(u.Cep) ? DBNull.Value : u.Cep);
        cmd.Parameters.AddWithValue("@ddi", u.Ddi);
        cmd.Parameters.AddWithValue("@ddd", u.Ddd);
        cmd.Parameters.AddWithValue("@telefone", u.NumeroTelefone);
        cmd.Parameters.AddWithValue("@nascimento", u.DataNascimento == default ? DBNull.Value : u.DataNascimento);
        cmd.Parameters.AddWithValue("@papel", u.Papel);
        try { await cmd.ExecuteNonQueryAsync(ct); }
        catch (MySqlException ex) when (ex.Number == 1062) { throw new InvalidOperationException("E-mail ou CPF já cadastrado.", ex); }
    }
}

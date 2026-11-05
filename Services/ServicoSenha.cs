using System.Security.Cryptography;

namespace Adega2L.Services;

// Hash seguro para contas NOVAS. Não é compatível automaticamente com hashes PHP bcrypt antigos.
public sealed class ServicoSenha
{
    private const int Iteracoes = 210_000;
    private const int TamanhoHash = 32;

    public string GerarHash(string senha)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return $"pbkdf2-sha256${Iteracoes}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public bool Conferir(string senha, string hashArmazenado)
    {
        var partes = hashArmazenado.Split('$');
        if (partes.Length != 4 || partes[0] != "pbkdf2-sha256" ||
            !int.TryParse(partes[1], out var iteracoes) || iteracoes < 100_000 || iteracoes > 2_000_000)
            return false;
        try
        {
            var sal = Convert.FromBase64String(partes[2]);
            var hashReal = Convert.FromBase64String(partes[3]);
            if (sal.Length != 16 || hashReal.Length != TamanhoHash) return false;
            var comparacao = Rfc2898DeriveBytes.Pbkdf2(senha, sal, iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
            return CryptographicOperations.FixedTimeEquals(hashReal, comparacao);
        }
        catch (FormatException) { return false; }
    }
}

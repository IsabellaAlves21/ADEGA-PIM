namespace Adega2L.Data;

/// <summary>
/// Auto: usa dados demonstrativos quando a conexão está vazia;
/// ao configurar ConnectionStrings:MySql, passa a usar o banco.
/// Demo: força os dados de teste. MySql: exige uma conexão válida.
/// </summary>
public static class OrigemDados
{
    public static bool UsarMySql(IConfiguration configuracao)
    {
        var origem = configuracao["Dados:Origem"] ?? "Auto";
        if (origem.Equals("Demo", StringComparison.OrdinalIgnoreCase)) return false;
        if (origem.Equals("MySql", StringComparison.OrdinalIgnoreCase)) return true;
        if (origem.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            return !string.IsNullOrWhiteSpace(configuracao.GetConnectionString("MySql"));
        throw new InvalidOperationException("Dados:Origem deve ser Auto, Demo ou MySql.");
    }
}

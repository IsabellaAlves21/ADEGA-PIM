using MySqlConnector;
namespace Adega2L.Data;
public sealed class FabricaConexaoMySql(IConfiguration configuracao)
{
    public MySqlConnection Criar()
    {
        var cs = configuracao.GetConnectionString("MySql");
        if (string.IsNullOrWhiteSpace(cs)) throw new InvalidOperationException("Conexão MySQL não configurada.");
        return new MySqlConnection(cs);
    }
}

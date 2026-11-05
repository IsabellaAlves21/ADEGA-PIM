using System.Data;
using System.Data.Common;
using Adega2L.Data;
using Adega2L.Models;

namespace Adega2L.Repositories;

// Preparado a partir das tabelas citadas nos PHPs recebidos.
// Conferir os nomes reais das colunas quando o banco for criado.
public sealed class MySqlProdutoRepository(FabricaConexaoMySql fabrica) : IProdutoRepository
{
    private const string Campos = """
        SELECT p.idProduto, p.nomeProduto, p.precoProduto, p.imagemProduto,
               p.descricaoBebidas, p.tipoUnidade, p.tamanhoUnidade, p.destaque, p.ativo,
               p.quantidadeEstoqueProduto, p.estoqueMinimo, p.precoCusto, c.nomeCategoria
        FROM ADG2L_Produtos AS p
        INNER JOIN ADG2L_Categorias AS c ON p.idCategoria = c.idCategoria
        WHERE p.ativo = 1
        """;

    public async Task<IReadOnlyList<Produto>> ListarAsync(CancellationToken ct = default)
    {
        await using var conexao = fabrica.Criar();
        await conexao.OpenAsync(ct);
        await using var comando = conexao.CreateCommand();
        comando.CommandText = Campos + " ORDER BY c.nomeCategoria, p.nomeProduto";
        await using var leitor = await comando.ExecuteReaderAsync(ct);
        var produtos = new List<Produto>();
        while (await leitor.ReadAsync(ct)) produtos.Add(LerProduto(leitor));
        return produtos;
    }

    public async Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var conexao = fabrica.Criar();
        await conexao.OpenAsync(ct);
        await using var comando = conexao.CreateCommand();
        comando.CommandText = Campos + " AND p.idProduto = @id LIMIT 1";
        var parametro = comando.CreateParameter();
        parametro.ParameterName = "@id";
        parametro.DbType = DbType.Int32;
        parametro.Value = id;
        comando.Parameters.Add(parametro);
        await using var leitor = await comando.ExecuteReaderAsync(ct);
        return await leitor.ReadAsync(ct) ? LerProduto(leitor) : null;
    }

    private static Produto LerProduto(DbDataReader leitor)
    {
        static string Texto(object valor) => valor == DBNull.Value ? "" : Convert.ToString(valor) ?? "";
        var nomeImagem = Path.GetFileName(Texto(leitor["imagemProduto"]));
        return new Produto
        {
            Id = Convert.ToInt32(leitor["idProduto"]),
            Nome = Texto(leitor["nomeProduto"]),
            Preco = Convert.ToDecimal(leitor["precoProduto"]),
            Destaque = Convert.ToBoolean(leitor["destaque"]),
            Estoque = Convert.ToInt32(leitor["quantidadeEstoqueProduto"]),
            EstoqueMinimo = Convert.ToInt32(leitor["estoqueMinimo"]),
            Custo = Convert.ToDecimal(leitor["precoCusto"]),
            Categoria = Texto(leitor["nomeCategoria"]),
            Descricao = Texto(leitor["descricaoBebidas"]),
            TipoUnidade = Texto(leitor["tipoUnidade"]),
            TamanhoUnidade = Texto(leitor["tamanhoUnidade"]),
            // Copiar as imagens do PHP antigo para wwwroot/images/produtos/.
            Imagem = string.IsNullOrEmpty(nomeImagem) ? "/images/placeholder.svg" : "/images/produtos/" + Uri.EscapeDataString(nomeImagem)
        };
    }
}

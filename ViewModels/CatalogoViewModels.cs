using Adega2L.Models;

namespace Adega2L.ViewModels;

public sealed class GrupoProdutos
{
    public string Categoria { get; set; } = "";
    public IReadOnlyList<Produto> Produtos { get; set; } = [];
}

public sealed class InicioViewModel
{
    public IReadOnlyList<GrupoProdutos> Grupos { get; set; } = [];
}

public sealed class CategoriaViewModel
{
    public string Categoria { get; set; } = "";
    public IReadOnlyList<Produto> Destaques { get; set; } = [];
    public IReadOnlyList<Produto> Produtos { get; set; } = [];
    public int Pagina { get; set; }
    public int TotalPaginas { get; set; }
}

public sealed class BuscaViewModel
{
    public string Termo { get; set; } = "";
    public IReadOnlyList<Produto> Resultados { get; set; } = [];
}

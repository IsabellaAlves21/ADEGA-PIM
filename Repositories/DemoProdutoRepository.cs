using Adega2L.Models;

namespace Adega2L.Repositories;

// Itens e preços são FICTÍCIOS, usados exclusivamente para testar a interface.
public sealed class DemoProdutoRepository : IProdutoRepository
{
    private readonly DemoDadosStore _store;
    public DemoProdutoRepository(DemoDadosStore store) => _store = store;

    public Task<IReadOnlyList<Produto>> ListarAsync(CancellationToken ct = default)
    {
        lock(_store.Sync) return Task.FromResult((IReadOnlyList<Produto>)_store.Produtos.Where(p => p.Ativo).ToList());
    }

    public Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        lock(_store.Sync) return Task.FromResult(_store.Produtos.FirstOrDefault(p => p.Id == id && p.Ativo));
    }

    internal static IReadOnlyList<Produto> CriarCatalogo()
    {
        var produtos = new List<Produto>();
        var id = 1;
        void Adicionar(string categoria, string imagem, params (string Nome, decimal Preco, string Descricao)[] dados)
        {
            var indice = 0;
            foreach (var item in dados)
                produtos.Add(new Produto
                {
                    Id = id++, Nome = item.Nome, Preco = item.Preco,
                    Descricao = item.Descricao, Categoria = categoria, Imagem = $"/images/{imagem}",
                    TipoUnidade = "unidade", Destaque = indice++ < 5, Estoque = id % 7 == 0 ? 3 : 18, EstoqueMinimo = 5, Custo = item.Preco * .62m
                });
        }

        Adicionar("Bebidas", "bebidas.svg",
            ("Cerveja Premium 350 ml", 7.90m, "Cerveja gelada para ocasiões especiais."),
            ("Refrigerante Cola 2 L", 11.99m, "Refrigerante de 2 litros para compartilhar."),
            ("Energético 473 ml", 9.50m, "Bebida energética em lata."),
            ("Água Mineral 500 ml", 3.00m, "Água mineral sem gás."),
            ("Suco de Uva 1 L", 8.90m, "Bebida de uva para toda a família."),
            ("Cerveja Lager 600 ml", 12.90m, "Cerveja estilo lager em garrafa."),
            ("Refrigerante Guaraná 2 L", 10.50m, "Refrigerante sabor guaraná."),
            ("Água com Gás 500 ml", 3.90m, "Água mineral com gás."));

        Adicionar("Destilados", "destilados.svg",
            ("Gin London Dry 750 ml", 89.90m, "Gin de estilo clássico, para maiores de 18 anos."),
            ("Vodka Tradicional 1 L", 49.90m, "Vodka tradicional, para maiores de 18 anos."),
            ("Whisky Blended 1 L", 139.90m, "Whisky blended, para maiores de 18 anos."),
            ("Rum Branco 980 ml", 44.90m, "Rum branco, para maiores de 18 anos."),
            ("Licor de Chocolate 720 ml", 72.90m, "Licor, para maiores de 18 anos."),
            ("Cachaça Artesanal 700 ml", 39.90m, "Cachaça, para maiores de 18 anos."),
            ("Gin de Frutas 750 ml", 95.90m, "Gin aromatizado, para maiores de 18 anos."));

        Adicionar("Snacks", "snacks.svg",
            ("Batata Chips Original", 8.99m, "Batata crocante e levemente salgada."),
            ("Amendoim Torrado 150 g", 6.50m, "Amendoim torrado para petiscar."),
            ("Salgadinho de Queijo", 6.99m, "Salgadinho crocante sabor queijo."),
            ("Chocolate ao Leite 90 g", 7.50m, "Chocolate ao leite em barra."),
            ("Biscoito Recheado", 4.20m, "Biscoito doce recheado."),
            ("Pipoca de Micro-ondas", 5.80m, "Pipoca salgada para preparar em casa."),
            ("Mix de Castanhas 100 g", 13.90m, "Mix de castanhas selecionadas."));

        Adicionar("Cigarros", "cigarros.svg",
            ("Cigarros Clássicos - Maço", 12.00m, "Produto destinado exclusivamente a maiores de 18 anos."),
            ("Cigarros Slim - Maço", 13.50m, "Produto destinado exclusivamente a maiores de 18 anos."),
            ("Fumo para Enrolar 30 g", 19.90m, "Produto destinado exclusivamente a maiores de 18 anos."),
            ("Papel para Enrolar", 4.90m, "Acessório para fumantes adultos."),
            ("Isqueiro Simples", 4.50m, "Isqueiro básico."),
            ("Filtro para Enrolar", 6.90m, "Acessório para fumantes adultos."));

        return produtos;
    }
}

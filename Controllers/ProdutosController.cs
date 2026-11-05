using Adega2L.Repositories;
using Adega2L.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Adega2L.Controllers;

public sealed class ProdutosController(IProdutoRepository repositorio) : Controller
{
    private const int PorPagina = 15;
    private static readonly Dictionary<string, string> Categorias = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bebidas"] = "Bebidas", ["destilados"] = "Destilados", ["snacks"] = "Snacks", ["cigarros"] = "Cigarros"
    };

    public async Task<IActionResult> Categoria(string categoria, int pagina = 1, CancellationToken ct = default)
    {
        if (!Categorias.TryGetValue(categoria ?? "", out var nomeCategoria)) return NotFound();
        var todos = (await repositorio.ListarAsync(ct))
            .Where(p => p.Categoria.Equals(nomeCategoria, StringComparison.OrdinalIgnoreCase)).ToList();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(todos.Count / (double)PorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        return View(new CategoriaViewModel
        {
            Categoria = nomeCategoria,
            Destaques = todos.OrderByDescending(p => p.Destaque).Take(5).ToList(),
            Produtos = todos.Skip((pagina - 1) * PorPagina).Take(PorPagina).ToList(),
            Pagina = pagina, TotalPaginas = totalPaginas
        });
    }

    public async Task<IActionResult> Detalhes(int id, CancellationToken ct)
    {
        var produto = await repositorio.ObterPorIdAsync(id, ct);
        return produto is null ? NotFound() : View(produto);
    }

    public async Task<IActionResult> Buscar(string? q, CancellationToken ct)
    {
        var termo = (q ?? "").Trim();
        if (termo.Length > 100) termo = termo[..100];
        var todos = await repositorio.ListarAsync(ct);
        List<Adega2L.Models.Produto> encontrados = string.IsNullOrWhiteSpace(termo) ? new() : todos.Where(p =>
            p.Nome.Contains(termo, StringComparison.CurrentCultureIgnoreCase) ||
            p.Categoria.Contains(termo, StringComparison.CurrentCultureIgnoreCase) ||
            p.Descricao.Contains(termo, StringComparison.CurrentCultureIgnoreCase)).ToList();
        return View(new BuscaViewModel { Termo = termo, Resultados = encontrados });
    }
}

using Adega2L.Repositories;
using Adega2L.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Adega2L.Controllers;

public sealed class HomeController(IProdutoRepository repositorio) : Controller
{
    private static readonly string[] Categorias = ["Bebidas", "Destilados", "Snacks", "Cigarros"];

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var todos = await repositorio.ListarAsync(ct);
        var grupos = Categorias.Select(c => new GrupoProdutos
        {
            Categoria = c,
            Produtos = todos.Where(p => p.Categoria.Equals(c, StringComparison.OrdinalIgnoreCase)).OrderByDescending(p => p.Destaque).Take(5).ToList()
        }).ToList();
        return View(new InicioViewModel { Grupos = grupos });
    }

    public IActionResult Institucional() => View();
    public IActionResult Carrinho() => View();
    public IActionResult Administrativo() => RedirectToAction("Index", "Admin");
    public IActionResult Erro() => View();
}

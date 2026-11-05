using Adega2L.Models;
using Adega2L.Repositories;
using Adega2L.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Adega2L.Controllers;

public sealed class PedidosController(IProdutoRepository produtos, IGestaoRepository gestao) : Controller
{
    // Sem carrinho: pedido direto por produto. Nenhum pagamento eletrônico é processado.
    [HttpGet]
    public async Task<IActionResult> Novo(int id, CancellationToken ct)
    {
        var p = await produtos.ObterPorIdAsync(id,ct);
        if(p is null || p.Categoria == "Cigarros") return NotFound();
        return View(new PedidoOnlineViewModel { ProdutoId=p.Id, Produto=p });
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Novo(PedidoOnlineViewModel model,CancellationToken ct)
    {
        var p = await produtos.ObterPorIdAsync(model.ProdutoId,ct);
        if(p is null || p.Categoria == "Cigarros") return NotFound();
        model.Produto = p;
        if(!model.MaiorDe18) ModelState.AddModelError(nameof(model.MaiorDe18),"Confirme a maioridade. A comprovação de identidade é obrigatória no atendimento real.");
        if(model.Pagamento is not ("DINHEIRO" or "DEBITO" or "CREDITO")) ModelState.AddModelError(nameof(model.Pagamento),"Forma de pagamento inválida.");
        if(!ModelState.IsValid) return View(model);
        try
        {
            var id=await gestao.CriarPedidoOnlineAsync(new PedidoSolicitado { ProdutoId=p.Id,Quantidade=model.Quantidade,Cliente=model.Nome.Trim(),Telefone=model.Telefone,Endereco=model.Endereco.Trim(),Pagamento=model.Pagamento },ct);
            return RedirectToAction(nameof(Recebido),new { id });
        }
        catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); return View(model); }
    }
    [HttpGet] public IActionResult Recebido(int id) => id<=0 ? NotFound() : View(id);
}

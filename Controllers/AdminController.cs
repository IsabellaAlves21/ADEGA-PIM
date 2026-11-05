using System.Security.Claims;
using Adega2L.Models;
using Adega2L.Repositories;
using Adega2L.Services;
using Adega2L.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adega2L.Controllers;

[Authorize(Roles = Papeis.Equipe)]
public sealed class AdminController(IGestaoRepository gestao, IUsuarioRepository usuarios, ServicoSenha senhas) : Controller
{
    private int UsuarioId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id) ? id : 0;
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var dados = new DashboardViewModel { Resumo = await gestao.ResumoAsync(ct), Produtos = await gestao.ProdutosAsync(ct), Lotes = await gestao.LotesAsync(ct), Participacoes = await gestao.ParticipacaoAsync(ct) };
        return View(dados);
    }
    [HttpGet]
    public async Task<IActionResult> Estoque(CancellationToken ct) => View(new EstoqueViewModel { Produtos = await gestao.ProdutosAsync(ct), Lotes = await gestao.LotesAsync(ct), Historico = await gestao.EstoqueHistoricoAsync(ct) });
    [HttpGet] public IActionResult Movimentar() => View(new EntradaSaidaViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Movimentar(EntradaSaidaViewModel model, CancellationToken ct)
    {
        if (model.Tipo is not ("ENTRADA" or "SAIDA")) ModelState.AddModelError(nameof(model.Tipo), "Tipo inválido.");
        if (!ModelState.IsValid) return View(model);
        try { await gestao.MovimentarEstoqueAsync(model.ProdutoId,model.Quantidade,model.Tipo,model.DataValidade,model.Observacao,UsuarioId,ct); TempData["Mensagem"] = "Movimentação registrada no estoque."; return RedirectToAction(nameof(Estoque)); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); return View(model); }
    }
    [HttpGet] public IActionResult NovoProduto() => View(new NovoProdutoViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NovoProduto(NovoProdutoViewModel model, CancellationToken ct)
    {
        if (model.Categoria is not ("Bebidas" or "Destilados" or "Snacks" or "Cigarros")) ModelState.AddModelError(nameof(model.Categoria),"Categoria inválida.");
        if(!ModelState.IsValid) return View(model);
        try { await gestao.CriarProdutoAsync(new Produto { Nome=model.Nome.Trim(),Categoria=model.Categoria,Preco=model.Preco,Custo=model.Custo,EstoqueMinimo=model.EstoqueMinimo,Descricao=model.Descricao },UsuarioId,ct);
            TempData["Mensagem"] = "Produto cadastrado; registre a entrada de mercadoria para disponibilizar o estoque."; return RedirectToAction(nameof(Estoque)); }
        catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); return View(model); }
    }
    [HttpGet] public IActionResult Pdv() => View(new PdvViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pdv(PdvViewModel model, CancellationToken ct)
    {
        if(model.Pagamento is not ("DINHEIRO" or "DEBITO" or "CREDITO")) ModelState.AddModelError(nameof(model.Pagamento),"Pagamento inválido.");
        var itens = model.Itens.Where(x=>x.ProdutoId>0 && x.Quantidade>0).Select(x=>new ItemSolicitado { ProdutoId=x.ProdutoId,Quantidade=x.Quantidade }).ToList();
        if(itens.Count==0) ModelState.AddModelError("","Selecione ao menos um produto e a quantidade.");
        if(model.Itens.Any(x=>(x.ProdutoId>0) != (x.Quantidade>0))) ModelState.AddModelError("","Cada linha deve ter produto e quantidade, ou ficar em branco.");
        if(!ModelState.IsValid) return View(model);
        try { var id = await gestao.VenderPdvAsync(itens,model.Pagamento,UsuarioId,ct); TempData["Mensagem"] = $"Venda #{id} concluída: estoque e caixa atualizados."; return RedirectToAction(nameof(Vendas)); }
        catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); return View(model); }
    }
    [HttpGet] public async Task<IActionResult> Vendas(CancellationToken ct) => View(await gestao.VendasAsync(ct));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarPedido(int id, CancellationToken ct)
    {
        try { TempData["Mensagem"] = await gestao.ConfirmarPedidoAsync(id,UsuarioId,ct) ? $"Pedido #{id} concluído e lançado no estoque e caixa." : "Pedido não está pendente ou não foi encontrado."; }
        catch(InvalidOperationException ex) { TempData["Erro"] = ex.Message; }
        return RedirectToAction(nameof(Vendas));
    }
    [HttpGet] public async Task<IActionResult> Financeiro(CancellationToken ct) => View(new FinanceiroViewModel { Resumo=await gestao.ResumoAsync(ct), Movimentos=await gestao.CaixaAsync(ct), Participacoes=await gestao.ParticipacaoAsync(ct) });
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> LancarCaixa([Bind(Prefix="NovoLancamento")] CaixaViewModel model, CancellationToken ct)
    {
        if(model.Tipo is not ("ENTRADA" or "SAIDA")) ModelState.AddModelError("","Tipo inválido.");
        if(!ModelState.IsValid) { TempData["Erro"]="Preencha valor, tipo e descrição válidos."; return RedirectToAction(nameof(Financeiro)); }
        try { await gestao.RegistrarCaixaAsync(model.Tipo,model.Valor,model.Descricao,UsuarioId,ct); TempData["Mensagem"]="Lançamento financeiro registrado."; }
        catch(InvalidOperationException ex) { TempData["Erro"] = ex.Message; }
        return RedirectToAction(nameof(Financeiro));
    }
    [Authorize(Roles = Papeis.Dono)]
    [HttpGet] public IActionResult Equipe() => View(new EquipeViewModel());
    [Authorize(Roles = Papeis.Dono)]
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Equipe(EquipeViewModel model,CancellationToken ct)
    {
        if(!ModelState.IsValid) return View(model);
        if(await usuarios.ExisteEmailOuCpfAsync(model.Email,"",ct)) { ModelState.AddModelError("","E-mail já cadastrado."); return View(model); }
        try { await usuarios.CadastrarInternoAsync(new Usuario { Nome=model.Nome.Trim(),Email=model.Email.Trim().ToLowerInvariant(),Papel=Papeis.Funcionario,SenhaHash=senhas.GerarHash(model.Senha) },ct);
            TempData["Mensagem"]="Funcionário cadastrado com acesso ao painel."; return RedirectToAction(nameof(Index)); }
        catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); return View(model); }
    }
}

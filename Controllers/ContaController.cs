using System.Security.Claims;
using Adega2L.Models;
using Adega2L.Repositories;
using Adega2L.Services;
using Adega2L.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adega2L.Controllers;

public sealed class ContaController(IUsuarioRepository repositorio, ServicoSenha senhas) : Controller
{
    [HttpGet] public IActionResult Login() => View(new LoginViewModel());
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo,CancellationToken ct)
    {
        if(!ModelState.IsValid) return View(modelo);
        var u=await repositorio.ObterPorEmailAsync(modelo.Email,ct);
        if(u is null || !u.Ativo || !senhas.Conferir(modelo.Senha,u.SenhaHash)) { ModelState.AddModelError("","E-mail ou senha incorretos."); return View(modelo); }
        var identidade=new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier,u.Id.ToString()),
            new Claim(ClaimTypes.Name,u.Nome),
            new Claim(ClaimTypes.Email,u.Email),
            new Claim(ClaimTypes.Role,u.Papel)
        ],CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identidade));
        return u.Papel is Papeis.Funcionario or Papeis.Dono ? RedirectToAction("Index","Admin") : RedirectToAction("Index","Home");
    }
    [HttpGet] public IActionResult Cadastro() => View(new CadastroViewModel());
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Cadastro(CadastroViewModel modelo,CancellationToken ct)
    {
        if(modelo.DataNascimento is not null)
        {
            var nascimento=modelo.DataNascimento.Value.Date;
            if(nascimento>DateTime.Today || nascimento<DateTime.Today.AddYears(-120)) ModelState.AddModelError(nameof(modelo.DataNascimento),"Informe uma data válida.");
            else if(nascimento>DateTime.Today.AddYears(-18)) ModelState.AddModelError(nameof(modelo.DataNascimento),"Cadastro disponível para maiores de 18 anos.");
        }
        if(!ModelState.IsValid) return View(modelo);
        if(await repositorio.ExisteEmailOuCpfAsync(modelo.Email,modelo.Cpf,ct)) { ModelState.AddModelError("","E-mail ou CPF já cadastrado."); return View(modelo); }
        try { var u=modelo.ParaUsuario(senhas.GerarHash(modelo.Senha)); u.Papel=Papeis.Cliente; await repositorio.CadastrarAsync(u,ct); }
        catch(InvalidOperationException) { ModelState.AddModelError("","E-mail ou CPF já cadastrado."); return View(modelo); }
        TempData["Mensagem"]="Cadastro realizado. Agora faça seu login."; return RedirectToAction(nameof(Login));
    }
    [Authorize] public async Task<IActionResult> Perfil(CancellationToken ct)
    {
        var email=User.FindFirstValue(ClaimTypes.Email)??""; var u=await repositorio.ObterPorEmailAsync(email,ct);
        if(u is null) { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
        return View(u);
    }
    [HttpGet] public IActionResult AcessoNegado() => View();
    [Authorize,HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Sair() { await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return RedirectToAction("Index","Home"); }
}

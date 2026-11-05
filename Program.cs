using Microsoft.AspNetCore.Authentication;
using Adega2L.Data;
using Adega2L.Models;
using Adega2L.Repositories;
using Adega2L.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using System.Security.Claims;

// Execucao local opcional para criar o primeiro DONO sem endpoint publico de instalacao.
var criarDono = args.Contains("--criar-dono", StringComparer.OrdinalIgnoreCase);
var argumentosWeb = args.Where(x => !x.Equals("--criar-dono",StringComparison.OrdinalIgnoreCase)).ToArray();
var builder = WebApplication.CreateBuilder(argumentosWeb);
var ptBR = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBR;
CultureInfo.DefaultThreadCurrentUICulture = ptBR;

builder.Services.AddControllersWithViews(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes=true);
builder.Services.AddSingleton<ServicoSenha>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath="/Conta/Login";
        options.AccessDeniedPath="/Conta/AcessoNegado";
        options.Cookie.Name="Adega2L.Auth";
        options.Cookie.HttpOnly=true;
        options.Cookie.SameSite=SameSiteMode.Lax;
        options.Cookie.SecurePolicy=builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.SlidingExpiration=true;
        options.ExpireTimeSpan=TimeSpan.FromHours(4);
        options.Events.OnValidatePrincipal=async ctx => {
            var id=ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email=ctx.Principal?.FindFirstValue(ClaimTypes.Email);
            var role=ctx.Principal?.FindFirstValue(ClaimTypes.Role);
            var repo=ctx.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
            var user=string.IsNullOrWhiteSpace(email) ? null : await repo.ObterPorEmailAsync(email,ctx.HttpContext.RequestAborted);
            if(user is null || !user.Ativo || user.Id.ToString()!=id || user.Papel!=role)
            { ctx.RejectPrincipal(); await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); }
        };
    });
builder.Services.AddAuthorization();

if(OrigemDados.UsarMySql(builder.Configuration)) {
    builder.Services.AddScoped<FabricaConexaoMySql>();
    builder.Services.AddScoped<IProdutoRepository,MySqlProdutoRepository>();
    builder.Services.AddScoped<IUsuarioRepository,MySqlUsuarioRepository>();
    builder.Services.AddScoped<IGestaoRepository,MySqlGestaoRepository>();
} else {
    builder.Services.AddSingleton<DemoDadosStore>();
    builder.Services.AddSingleton<IProdutoRepository,DemoProdutoRepository>();
    builder.Services.AddSingleton<IUsuarioRepository,MemoriaUsuarioRepository>();
    builder.Services.AddSingleton<IGestaoRepository,DemoGestaoRepository>();
}

var app = builder.Build();
if(!app.Environment.IsDevelopment() && !OrigemDados.UsarMySql(app.Configuration))
    throw new InvalidOperationException("Modo Demo não é permitido fora de Development. Configure o MySQL para produção.");
if(criarDono)
{
    if(!OrigemDados.UsarMySql(app.Configuration)) throw new InvalidOperationException("Configure o MySQL antes de criar o dono.");
    var email=Environment.GetEnvironmentVariable("ADEGA_OWNER_EMAIL")?.Trim().ToLowerInvariant();
    var senha=Environment.GetEnvironmentVariable("ADEGA_OWNER_PASSWORD");
    var nome=Environment.GetEnvironmentVariable("ADEGA_OWNER_NAME")?.Trim() ?? "Responsável";
    if(string.IsNullOrWhiteSpace(email) || !email.Contains('@') || string.IsNullOrWhiteSpace(senha) || senha.Length<12)
        throw new InvalidOperationException("Defina ADEGA_OWNER_EMAIL e ADEGA_OWNER_PASSWORD (mínimo 12 caracteres) no terminal. Não use credenciais padrão.");
    using var scope=app.Services.CreateScope();
    var repo=scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
    if(await repo.ExisteDonoAsync()) { Console.WriteLine("Já existe um Dono. Nenhuma conta foi criada."); return; }
    var hash=scope.ServiceProvider.GetRequiredService<ServicoSenha>().GerarHash(senha);
    await repo.CadastrarInternoAsync(new Usuario { Nome=nome, Email=email, SenhaHash=hash, Papel=Papeis.Dono },CancellationToken.None);
    Console.WriteLine("Primeira conta de Dono criada. Remova as variáveis de ambiente sensíveis após o uso.");
    return;
}
if(!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Erro"); app.UseHsts(); }
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllerRoute("categoria","categoria/{categoria}", new {controller="Produtos",action="Categoria"});
app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");
app.Run();

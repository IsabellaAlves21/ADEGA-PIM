using Adega2L.Data;
using Microsoft.AspNetCore.Mvc;

namespace Adega2L.Controllers;

// Rota de desenvolvimento para conferir se o SQL foi importado corretamente.
// Desativada fora do ambiente Development; nunca devolve a senha da conexao.
[Route("diagnostico")]
public sealed class DiagnosticoController(IConfiguration configuracao, IWebHostEnvironment ambiente) : Controller
{
    [HttpGet("banco")]
    public async Task<IActionResult> Banco(CancellationToken ct)
    {
        if (!ambiente.IsDevelopment()) return NotFound();
        if (!OrigemDados.UsarMySql(configuracao))
            return Json(new { modo = "Demo", mensagem = "Sem banco configurado. A vitrine de teste esta ativa." });
        try
        {
            await using var conexao = new FabricaConexaoMySql(configuracao).Criar();
            await conexao.OpenAsync(ct);
            await using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT COUNT(*) FROM ADG2L_Produtos";
            var quantidade = Convert.ToInt32(await comando.ExecuteScalarAsync(ct));
            return Json(new { modo = "MySql", conexao = "OK", tabelaProdutos = "OK", totalProdutos = quantidade });
        }
        catch (Exception)
        {
            return StatusCode(503, new { modo = "MySql", conexao = "Falha", mensagem = "Confira a instancia, os dados de acesso e a importacao do SQL. Consulte os logs locais." });
        }
    }
}

using Adega2L.Models;
using System.ComponentModel.DataAnnotations;

namespace Adega2L.ViewModels;

public sealed class PedidoOnlineViewModel
{
    [Range(1,int.MaxValue)] public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }
    [Range(1,100,ErrorMessage="Quantidade entre 1 e 100.")] public int Quantidade { get; set; } = 1;
    [Required,MaxLength(120)] public string Nome { get; set; } = "";
    [Required,RegularExpression(@"^\d{10,11}$",ErrorMessage="Digite DDD e telefone, só números.")] public string Telefone { get; set; } = "";
    [Required,MaxLength(240)] public string Endereco { get; set; } = "";
    [Required] public string Pagamento { get; set; } = "DINHEIRO";
    public bool MaiorDe18 { get; set; }
}

using System.ComponentModel.DataAnnotations;
using Adega2L.Models;

namespace Adega2L.ViewModels;

public sealed class EntradaSaidaViewModel
{
    [Range(1,int.MaxValue,ErrorMessage="Selecione um produto.")] public int ProdutoId { get; set; }
    [Required] public string Tipo { get; set; } = "ENTRADA";
    [Range(1,100000)] public int Quantidade { get; set; } = 1;
    [DataType(DataType.Date)] public DateTime? DataValidade { get; set; }
    [MaxLength(250)] public string Observacao { get; set; } = "";
}
public sealed class NovoProdutoViewModel
{
    [Required, MaxLength(180)] public string Nome { get; set; } = "";
    [Required] public string Categoria { get; set; } = "Bebidas";
    [Range(typeof(decimal),"0","9999999",ParseLimitsInInvariantCulture = true)] public decimal Preco { get; set; }
    [Range(typeof(decimal),"0","9999999",ParseLimitsInInvariantCulture = true)] public decimal Custo { get; set; }
    [Range(0,100000)] public int EstoqueMinimo { get; set; } = 5;
    [MaxLength(800)] public string Descricao { get; set; } = "";
}
public sealed class LinhaPdvViewModel
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
}
public sealed class PdvViewModel
{
    public string Pagamento { get; set; } = "DINHEIRO";
    public List<LinhaPdvViewModel> Itens { get; set; } = Enumerable.Range(0,4).Select(_=>new LinhaPdvViewModel()).ToList();
}
public sealed class CaixaViewModel
{
   
public string Tipo { get; set; } = "SAIDA";

[Range(typeof(decimal), "0.01", "9999999", ParseLimitsInInvariantCulture = true)]
public decimal Valor { get; set; }

[Required, MaxLength(220)]
public string Descricao { get; set; } = "";

}
public sealed class EquipeViewModel
{
    [Required,MaxLength(150)] public string Nome { get; set; } = "";
    [Required,EmailAddress,MaxLength(200)] public string Email { get; set; } = "";
    [Required,MinLength(12),DataType(DataType.Password)] public string Senha { get; set; } = "";
    [Required,Compare(nameof(Senha)),DataType(DataType.Password)] public string ConfirmarSenha { get; set; } = "";
}
public sealed class DashboardViewModel
{
    public DashboardResumo Resumo { get; set; } = new();
    public IReadOnlyList<Produto> Produtos { get; set; } = [];
    public IReadOnlyList<Lote> Lotes { get; set; } = [];
    public IReadOnlyList<ParticipacaoVenda> Participacoes { get; set; } = [];
}
public sealed class EstoqueViewModel
{
    public IReadOnlyList<Produto> Produtos { get; set; } = [];
    public IReadOnlyList<Lote> Lotes { get; set; } = [];
    public IReadOnlyList<MovimentoEstoque> Historico { get; set; } = [];
}
public sealed class FinanceiroViewModel
{
    public DashboardResumo Resumo { get; set; } = new();
    public IReadOnlyList<MovimentoCaixa> Movimentos { get; set; } = [];
    public IReadOnlyList<ParticipacaoVenda> Participacoes { get; set; } = [];
    public CaixaViewModel NovoLancamento { get; set; } = new();
}

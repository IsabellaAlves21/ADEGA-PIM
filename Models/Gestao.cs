namespace Adega2L.Models;

public static class Papeis
{
    public const string Cliente = "Cliente";
    public const string Funcionario = "Funcionario";
    public const string Dono = "Dono";
    public const string Equipe = "Funcionario,Dono";
}

public sealed class Lote
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Produto { get; set; } = "";
    public string Codigo { get; set; } = "";
    public int Quantidade { get; set; }
    public DateTime? Vencimento { get; set; }
}

public sealed class MovimentoEstoque
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Produto { get; set; } = "";
    public string Tipo { get; set; } = "";
    public int Quantidade { get; set; }
    public string Observacao { get; set; } = "";
    public DateTime Data { get; set; } = DateTime.Now;
}

public sealed class ItemVenda
{
    public int ProdutoId { get; set; }
    public string NomeProduto { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal => Quantidade * PrecoUnitario;
}

public sealed class Venda
{
    public int Id { get; set; }
    public string Origem { get; set; } = "PDV";
    public string Status { get; set; } = "PENDENTE";
    public string Pagamento { get; set; } = "DINHEIRO";
    public string Cliente { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = "";
    public DateTime Data { get; set; } = DateTime.Now;
    public List<ItemVenda> Itens { get; set; } = [];
    public decimal Total => Itens.Sum(x => x.Subtotal);
}

public sealed class MovimentoCaixa
{
    public int Id { get; set; }
    public string Tipo { get; set; } = "ENTRADA";
    public string Origem { get; set; } = "MANUAL";
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = "";
    public DateTime Data { get; set; } = DateTime.Now;
}

public sealed class ParticipacaoVenda
{
    public string Produto { get; set; } = "";
    public int Unidades { get; set; }
    public decimal Percentual { get; set; }
}

public sealed class DashboardResumo
{
    public decimal Faturamento { get; set; }
    public decimal SaidasCaixa { get; set; }
    public decimal SaldoCaixa { get; set; }
    public int VendasConcluidas { get; set; }
    public int PedidosPendentes { get; set; }
    public int AlertasEstoque { get; set; }
    public int AlertasVencimento { get; set; }
}

public sealed class ItemSolicitado
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
}

public sealed class PedidoSolicitado
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public string Cliente { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Pagamento { get; set; } = "";
}

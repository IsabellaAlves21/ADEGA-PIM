namespace Adega2L.Models;

public sealed class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Categoria { get; set; } = "";
    public decimal Preco { get; set; }
    public bool Destaque { get; set; }
    public int Estoque { get; set; }
    public int EstoqueMinimo { get; set; } = 5;
    public decimal Custo { get; set; }
    public bool Ativo { get; set; } = true;
    public string Descricao { get; set; } = "";
    public string Imagem { get; set; } = "/images/placeholder.svg";
    public string? TipoUnidade { get; set; }
    public string? TamanhoUnidade { get; set; }
}

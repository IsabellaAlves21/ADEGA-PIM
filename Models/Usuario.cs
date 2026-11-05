namespace Adega2L.Models;

public sealed class Usuario
{
    public int Id { get; set; }
    public bool Ativo { get; set; } = true;
    public string Papel { get; set; } = Papeis.Cliente;
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public string Cpf { get; set; } = "";
    public string TipoLogradouro { get; set; } = "";
    public string NomeLogradouro { get; set; } = "";
    public string NumeroLogradouro { get; set; } = "";
    public string ComplementoLogradouro { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Cep { get; set; } = "";
    public string Ddi { get; set; } = "55";
    public string Ddd { get; set; } = "";
    public string NumeroTelefone { get; set; } = "";
    public DateTime DataNascimento { get; set; }
}

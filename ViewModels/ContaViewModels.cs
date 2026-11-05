using System.ComponentModel.DataAnnotations;
using Adega2L.Models;

namespace Adega2L.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Informe sua senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = "";
}

public sealed class CadastroViewModel
{
    [Required(ErrorMessage = "Informe seu nome completo.")]
    [MaxLength(150)]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = "";

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(200)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Informe sua senha.")]
    [MinLength(8, ErrorMessage = "Use pelo menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = "";

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não coincidem.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    public string ConfirmarSenha { get; set; } = "";

    [Required(ErrorMessage = "Informe seu CPF.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Digite os 11 números do CPF, sem pontuação.")]
    [Display(Name = "CPF (somente números)")]
    public string Cpf { get; set; } = "";

    [Display(Name = "Tipo de logradouro")]
    public string TipoLogradouro { get; set; } = "";
    [Display(Name = "Rua / avenida")]
    public string NomeLogradouro { get; set; } = "";
    [Display(Name = "Número")]
    public string NumeroLogradouro { get; set; } = "";
    [Display(Name = "Complemento")]
    public string ComplementoLogradouro { get; set; } = "";
    [Display(Name = "Bairro")]
    public string Bairro { get; set; } = "";
    [Display(Name = "Cidade")]
    public string Cidade { get; set; } = "";

    [Required(ErrorMessage = "Informe o CEP.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "Digite os 8 números do CEP.")]
    [Display(Name = "CEP (somente números)")]
    public string Cep { get; set; } = "";

    [Required]
    [RegularExpression(@"^\d{1,3}$", ErrorMessage = "DDI inválido.")]
    [Display(Name = "DDI")]
    public string Ddi { get; set; } = "55";

    [Required(ErrorMessage = "Informe o DDD.")]
    [RegularExpression(@"^\d{2}$", ErrorMessage = "Digite 2 números.")]
    [Display(Name = "DDD")]
    public string Ddd { get; set; } = "";

    [Required(ErrorMessage = "Informe seu telefone.")]
    [RegularExpression(@"^\d{8,9}$", ErrorMessage = "Digite 8 ou 9 números.")]
    [Display(Name = "Telefone")]
    public string NumeroTelefone { get; set; } = "";

    [Required(ErrorMessage = "Informe sua data de nascimento.")]
    [DataType(DataType.Date)]
    [Display(Name = "Data de nascimento")]
    public DateTime? DataNascimento { get; set; }

    public Usuario ParaUsuario(string hash) => new()
    {
        Nome = Nome.Trim(), Email = Email.Trim().ToLowerInvariant(),
        SenhaHash = hash, Cpf = Cpf, TipoLogradouro = TipoLogradouro,
        NomeLogradouro = NomeLogradouro, NumeroLogradouro = NumeroLogradouro,
        ComplementoLogradouro = ComplementoLogradouro, Bairro = Bairro,
        Cidade = Cidade, Cep = Cep, Ddi = Ddi, Ddd = Ddd,
        NumeroTelefone = NumeroTelefone, DataNascimento = DataNascimento!.Value
    };
}

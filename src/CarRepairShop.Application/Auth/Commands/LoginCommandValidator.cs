using CarRepairShop.Application.Auth.Commands;
using CarRepairShop.Application.Common.Validators;
using FluentValidation;

namespace CarRepairShop.Application.Auth.Commands;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF is required.")
            .Must(value => value is not null && IsValidCpf(value))
            .WithMessage("CPF is invalid.");
    }

    private static bool IsValidCpf(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 11 && BrazilianDocumentValidator.IsValidCpfOrCnpj(digits);
    }
}

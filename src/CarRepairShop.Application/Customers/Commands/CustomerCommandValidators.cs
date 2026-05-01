using CarRepairShop.Application.Common.Validators;
using FluentValidation;

namespace CarRepairShop.Application.Customers.Commands;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            .Must(name => name.Trim().Contains(' ')).WithMessage("Name must include both a first name and a last name.");

        RuleFor(x => x.PersonalId)
            .NotEmpty().WithMessage("PersonalId is required.")
            .Must(doc =>
            {
                var digits = new string(doc.Where(char.IsDigit).ToArray());
                return digits.Length is 11 or 14;
            }).WithMessage("PersonalId must be a valid CPF (11 digits) or CNPJ (14 digits).")
            .Must(BrazilianDocumentValidator.IsValidCpfOrCnpj).WithMessage("PersonalId is not a valid CPF or CNPJ.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.Telephone)
            .NotEmpty().WithMessage("Telephone is required.")
            .Must(tel =>
            {
                var digits = new string(tel.Where(char.IsDigit).ToArray());
                return digits.Length is 10 or 11;
            }).WithMessage("Telephone must have 10 or 11 digits (Brazilian standard).");
    }
}

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            .Must(name => name.Trim().Contains(' ')).WithMessage("Name must include both a first name and a last name.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.Telephone)
            .NotEmpty().WithMessage("Telephone is required.")
            .Must(tel =>
            {
                var digits = new string(tel.Where(char.IsDigit).ToArray());
                return digits.Length is 10 or 11;
            }).WithMessage("Telephone must have 10 or 11 digits (Brazilian standard).");
    }
}

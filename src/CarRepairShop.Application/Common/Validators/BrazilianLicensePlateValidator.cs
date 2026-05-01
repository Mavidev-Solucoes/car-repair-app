using System.Text.RegularExpressions;

namespace CarRepairShop.Application.Common.Validators;

public static class BrazilianLicensePlateValidator
{
    private static readonly Regex MercosulPattern =
        new(@"^[A-Za-z]{3}[0-9]{1}[A-Za-z]{1}[0-9]{2}$", RegexOptions.Compiled);

    /// <summary>
    /// Validates a Brazilian Mercosul license plate (LLLNLNN).
    /// Dashes are stripped before validation.
    /// </summary>
    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var stripped = new string(value.Where(c => c != '-').ToArray());
        return MercosulPattern.IsMatch(stripped);
    }
}

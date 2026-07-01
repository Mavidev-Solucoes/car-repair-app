namespace CarRepairShop.Domain.ValueObjects;

/// <summary>
/// Value object representing a normalised phone number (digits only).
/// Encapsulates the stripping rule so that no entity duplicates this knowledge.
/// </summary>
public sealed record PhoneNumber
{
    public string Value { get; }

    public PhoneNumber(string raw)
    {
        Value = new string(raw.Where(char.IsDigit).ToArray());
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;
    public override string ToString() => Value;
}

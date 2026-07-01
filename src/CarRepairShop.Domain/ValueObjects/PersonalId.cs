namespace CarRepairShop.Domain.ValueObjects;

/// <summary>
/// Value object representing a normalised personal document number (digits only).
/// Encapsulates the stripping rule so that no entity duplicates this knowledge.
/// </summary>
public sealed record PersonalId
{
    public string Value { get; }

    public PersonalId(string raw)
    {
        Value = new string(raw.Where(char.IsDigit).ToArray());
    }

    public static implicit operator string(PersonalId id) => id.Value;
    public override string ToString() => Value;
}

namespace Novolis.Civics.Core;

/// <summary>Stable nation identity (Guid — bridges cleanly to Economy <c>LegalEntityId</c>).</summary>
public readonly record struct NationId(Guid Value) : IEquatable<NationId>
{
    public static NationId New() => new(Guid.NewGuid());

    public static NationId From(Guid value) => new(value);

    public override string ToString() => Value.ToString("N");
}

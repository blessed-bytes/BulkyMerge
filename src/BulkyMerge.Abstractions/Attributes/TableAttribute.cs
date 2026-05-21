namespace BulkyMerge;

/// <summary>Maps an entity to a database table.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TableAttribute(string name) : Attribute
{
    public string Name { get; } = name;

    public string? Schema { get; init; }
}

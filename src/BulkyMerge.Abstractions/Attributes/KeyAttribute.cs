namespace BulkyMerge;

/// <summary>Marks a property as part of the primary key.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class KeyAttribute : Attribute
{
    public int Order { get; init; }
}

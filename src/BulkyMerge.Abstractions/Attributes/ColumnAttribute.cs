namespace BulkyMerge;

/// <summary>Maps a property to a column name when it differs from the property name.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class ColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

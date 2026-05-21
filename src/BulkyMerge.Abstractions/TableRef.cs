namespace BulkyMerge;

/// <summary>Identifies a destination table (and optional schema).</summary>
public readonly record struct TableRef(string Name, string? Schema = null)
{
    public static TableRef From<T>() where T : class => new(typeof(T).Name);

    public static TableRef Parse(string name, string? schema = null) => new(name, schema);
}

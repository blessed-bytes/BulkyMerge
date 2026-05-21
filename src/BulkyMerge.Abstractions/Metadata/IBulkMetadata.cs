namespace BulkyMerge.Metadata;

/// <summary>Describes how entity properties map to table columns.</summary>
public interface IBulkMetadata
{
    Type EntityType { get; }

    IReadOnlyList<ColumnDescriptor> Columns { get; }

    string TableName { get; }

    string? Schema { get; }

    IReadOnlyList<string> KeyColumns { get; }
}

/// <summary>Typed bulk metadata with row accessors.</summary>
public interface IBulkMetadata<T> : IBulkMetadata where T : class
{
    object? GetValue(T entity, int columnIndex);

    void SetValue(T entity, int columnIndex, object? value);
}

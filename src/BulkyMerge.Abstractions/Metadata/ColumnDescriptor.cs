namespace BulkyMerge.Metadata;

public readonly record struct ColumnDescriptor(
    string Name,
    string PropertyName,
    Type PropertyType,
    bool IsKey,
    int KeyOrder);

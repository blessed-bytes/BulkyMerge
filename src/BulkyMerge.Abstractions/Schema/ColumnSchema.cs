namespace BulkyMerge.Schema;

public readonly record struct ColumnSchema(
    string Name,
    string DataType,
    bool IsIdentity,
    bool IsPrimaryKey);

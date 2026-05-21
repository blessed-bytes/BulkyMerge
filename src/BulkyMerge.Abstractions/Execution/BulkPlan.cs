using BulkyMerge.Metadata;
using BulkyMerge.Schema;

namespace BulkyMerge.Execution;

public sealed class BulkPlan<T> where T : class
{
    public BulkOperationKind Kind { get; init; }

    public required IBulkMetadata<T> Metadata { get; init; }

    public required string TableName { get; init; }

    public required string? Schema { get; init; }

    public required string StagingTable { get; init; }

    public required IReadOnlyList<ColumnDescriptor> WriteColumns { get; init; }

    public required IReadOnlyDictionary<string, ColumnSchema> SchemaByColumn { get; init; }

    public ColumnSchema? Identity { get; init; }

    public IReadOnlyList<string> KeyColumns { get; init; } = [];
}

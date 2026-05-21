using System.Data.Common;
using BulkyMerge.Metadata;
using BulkyMerge.Schema;

namespace BulkyMerge.Execution;

public sealed class BulkWriteContext<T> where T : class
{
    public required DbConnection Connection { get; init; }

    public DbTransaction? Transaction { get; init; }

    public required IEnumerable<T> Rows { get; init; }

    public required IBulkMetadata<T> Metadata { get; init; }

    public required IReadOnlyList<ColumnDescriptor> WriteColumns { get; init; }

    public required IReadOnlyDictionary<string, ColumnSchema> SchemaByColumn { get; init; }

    public required string StagingTable { get; init; }

    public TimeSpan CommandTimeout { get; init; }
}

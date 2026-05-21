namespace BulkyMerge;

/// <summary>Options for bulk insert-or-update (merge by primary key).</summary>
public sealed class UpsertManyOptions : BulkOperationOptions
{
    /// <summary>Writes database-generated key values back into inserted rows.</summary>
    public bool MapGeneratedKeys { get; init; }
}

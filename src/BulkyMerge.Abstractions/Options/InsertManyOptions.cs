namespace BulkyMerge;

/// <summary>Options for <see cref="IBulkOperations.InsertManyAsync{T}"/>.</summary>
public sealed class InsertManyOptions : BulkOperationOptions
{
    /// <summary>Writes database-generated key values back into the source entities (identity / serial).</summary>
    public bool MapGeneratedKeys { get; init; }
}

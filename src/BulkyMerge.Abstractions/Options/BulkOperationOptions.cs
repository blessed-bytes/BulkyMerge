namespace BulkyMerge;

/// <summary>Common settings for bulk operations.</summary>
public abstract class BulkOperationOptions
{
    public TableRef? Table { get; init; }

    public int BatchSize { get; init; } = 5_000;

    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public IReadOnlyList<string>? Exclude { get; init; }

    public IReadOnlyList<string>? Keys { get; init; }

    /// <summary>When true and no external transaction is supplied, the operation runs inside its own transaction.</summary>
    public bool UseTransaction { get; init; } = true;
}

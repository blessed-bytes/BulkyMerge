namespace BulkyMerge;

public class MergeOptions
{
    public string Schema { get; set; }
    public string TableName { get; set; }
    public int BatchSize { get; set; } = BulkExtensions.DefaultBatchSize;
    public IEnumerable<string> ExcludeProperties { get; set; }
    public IEnumerable<string> PrimaryKeys { get; set; }
    public int Timeout { get; set; } = int.MaxValue;
    public string IdentityColumnName { get; set; }
    public bool MapOutputIdentity { get; set; }
}

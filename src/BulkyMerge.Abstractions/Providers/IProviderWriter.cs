using BulkyMerge.Execution;

namespace BulkyMerge.Providers;

public interface IProviderWriter
{
    Task WriteStagingAsync<T>(
        BulkWriteContext<T> context,
        CancellationToken cancellationToken = default)
        where T : class;
}

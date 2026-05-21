using System.Data.Common;
using BulkyMerge.Execution;
using BulkyMerge.Metadata;
using BulkyMerge.Providers;

namespace BulkyMerge;

public sealed class BulkOperations(IDialect dialect, IProviderWriter writer) : IBulkOperations
{
    private readonly StagingBulkExecutor _executor = new(dialect, writer);

    public Task InsertManyAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        InsertManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => _executor.InsertAsync(
            connection,
            rows,
            BulkMetadataResolver.Resolve<T>(),
            options,
            transaction,
            cancellationToken);

    public Task UpdateManyAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        UpdateManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => _executor.UpdateAsync(
            connection,
            rows,
            BulkMetadataResolver.Resolve<T>(),
            options,
            transaction,
            cancellationToken);

    public Task UpsertManyAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        UpsertManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => _executor.UpsertAsync(
            connection,
            rows,
            BulkMetadataResolver.Resolve<T>(),
            options,
            transaction,
            cancellationToken);

    public Task DeleteManyAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        DeleteManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => _executor.DeleteAsync(
            connection,
            rows,
            BulkMetadataResolver.Resolve<T>(),
            options,
            transaction,
            cancellationToken);
}

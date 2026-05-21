using System.Data.Common;

namespace BulkyMerge;

public interface IBulkOperations
{
    Task InsertManyAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        InsertManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task UpdateManyAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        UpdateManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task UpsertManyAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        UpsertManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task DeleteManyAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        DeleteManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class;
}

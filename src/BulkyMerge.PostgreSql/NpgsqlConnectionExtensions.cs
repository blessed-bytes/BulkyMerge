using System.Data.Common;
using Npgsql;

namespace BulkyMerge.PostgreSql;

/// <summary>PostgreSQL bulk operation extensions.</summary>
public static class NpgsqlConnectionExtensions
{
    public static Task InsertManyAsync<T>(
        this NpgsqlConnection connection,
        IEnumerable<T> rows,
        InsertManyOptions? options = null,
        NpgsqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => PostgreSqlBulkOperations.Instance.InsertManyAsync(
            connection, rows, options, transaction, cancellationToken);

    public static Task UpdateManyAsync<T>(
        this NpgsqlConnection connection,
        IEnumerable<T> rows,
        UpdateManyOptions? options = null,
        NpgsqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => PostgreSqlBulkOperations.Instance.UpdateManyAsync(
            connection, rows, options, transaction, cancellationToken);

    public static Task UpsertManyAsync<T>(
        this NpgsqlConnection connection,
        IEnumerable<T> rows,
        UpsertManyOptions? options = null,
        NpgsqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => PostgreSqlBulkOperations.Instance.UpsertManyAsync(
            connection, rows, options, transaction, cancellationToken);

    public static Task DeleteManyAsync<T>(
        this NpgsqlConnection connection,
        IEnumerable<T> rows,
        DeleteManyOptions? options = null,
        NpgsqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => PostgreSqlBulkOperations.Instance.DeleteManyAsync(
            connection, rows, options, transaction, cancellationToken);

    public static Task InsertManyAsync<T>(
        this DbConnection connection,
        IEnumerable<T> rows,
        InsertManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => RequireNpgsql(connection).InsertManyAsync(rows, options, transaction as NpgsqlTransaction, cancellationToken);

    public static Task UpdateManyAsync<T>(
        this DbConnection connection,
        IEnumerable<T> rows,
        UpdateManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => RequireNpgsql(connection).UpdateManyAsync(rows, options, transaction as NpgsqlTransaction, cancellationToken);

    public static Task UpsertManyAsync<T>(
        this DbConnection connection,
        IEnumerable<T> rows,
        UpsertManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => RequireNpgsql(connection).UpsertManyAsync(rows, options, transaction as NpgsqlTransaction, cancellationToken);

    public static Task DeleteManyAsync<T>(
        this DbConnection connection,
        IEnumerable<T> rows,
        DeleteManyOptions? options = null,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : class
        => RequireNpgsql(connection).DeleteManyAsync(rows, options, transaction as NpgsqlTransaction, cancellationToken);

    private static NpgsqlConnection RequireNpgsql(DbConnection connection)
        => connection as NpgsqlConnection
           ?? throw new BulkyMergeException("Connection must be NpgsqlConnection.");
}

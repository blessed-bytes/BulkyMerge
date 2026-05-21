using System.Collections.Concurrent;
using System.Data.Common;
using BulkyMerge.Providers;
using BulkyMerge.Schema;

namespace BulkyMerge.Schema;

internal static class SchemaCache
{
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, ColumnSchema>> Cache = new();

    public static async Task<IReadOnlyDictionary<string, ColumnSchema>> GetOrLoadAsync(
        DbConnection connection,
        IDialect dialect,
        string databaseName,
        string tableName,
        string? schema,
        DbTransaction? transaction,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var key = $"{connection.GetType().Name}:{databaseName}:{schema}:{tableName}";
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        var loaded = await LoadAsync(connection, dialect, databaseName, tableName, schema, transaction, timeout, cancellationToken)
            .ConfigureAwait(false);
        Cache[key] = loaded;
        return loaded;
    }

    private static async Task<IReadOnlyDictionary<string, ColumnSchema>> LoadAsync(
        DbConnection connection,
        IDialect dialect,
        string databaseName,
        string tableName,
        string? schema,
        DbTransaction? transaction,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = dialect.GetColumnsQuery(databaseName, tableName, schema);
        command.Transaction = transaction;
        command.CommandTimeout = (int)Math.Clamp(timeout.TotalSeconds, 1, int.MaxValue);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var columns = new Dictionary<string, ColumnSchema>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var name = reader.GetString(0);
            var dataType = reader.GetString(1);
            var isIdentity = reader.GetInt32(2) == 1;
            var isPrimaryKey = reader.GetInt32(3) == 1;
            columns[name] = new ColumnSchema(name, dataType, isIdentity, isPrimaryKey);
        }

        return columns;
    }
}

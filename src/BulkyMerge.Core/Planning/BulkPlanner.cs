using BulkyMerge.Execution;
using BulkyMerge.Metadata;
using BulkyMerge.Providers;
using BulkyMerge.Schema;
using BulkyMerge.Sql;

namespace BulkyMerge.Planning;

internal static class BulkPlanner
{
    public static Task<BulkPlan<T>> PlanInsertAsync<T>(
        IBulkMetadata<T> metadata,
        IDialect dialect,
        System.Data.Common.DbConnection connection,
        InsertManyOptions? options,
        System.Data.Common.DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
        => PlanAsync(
            BulkOperationKind.Insert,
            metadata,
            dialect,
            connection,
            options ?? new InsertManyOptions(),
            transaction,
            cancellationToken);

    public static Task<BulkPlan<T>> PlanUpdateAsync<T>(
        IBulkMetadata<T> metadata,
        IDialect dialect,
        System.Data.Common.DbConnection connection,
        UpdateManyOptions? options,
        System.Data.Common.DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
        => PlanAsync(
            BulkOperationKind.Update,
            metadata,
            dialect,
            connection,
            options ?? new UpdateManyOptions(),
            transaction,
            cancellationToken);

    public static Task<BulkPlan<T>> PlanUpsertAsync<T>(
        IBulkMetadata<T> metadata,
        IDialect dialect,
        System.Data.Common.DbConnection connection,
        UpsertManyOptions? options,
        System.Data.Common.DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
        => PlanAsync(
            BulkOperationKind.Upsert,
            metadata,
            dialect,
            connection,
            options ?? new UpsertManyOptions(),
            transaction,
            cancellationToken);

    public static Task<BulkPlan<T>> PlanDeleteAsync<T>(
        IBulkMetadata<T> metadata,
        IDialect dialect,
        System.Data.Common.DbConnection connection,
        DeleteManyOptions? options,
        System.Data.Common.DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
        => PlanAsync(
            BulkOperationKind.Delete,
            metadata,
            dialect,
            connection,
            options ?? new DeleteManyOptions(),
            transaction,
            cancellationToken);

    private static async Task<BulkPlan<T>> PlanAsync<T>(
        BulkOperationKind kind,
        IBulkMetadata<T> metadata,
        IDialect dialect,
        System.Data.Common.DbConnection connection,
        BulkOperationOptions options,
        System.Data.Common.DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
    {
        var table = options.Table ?? new TableRef(metadata.TableName, metadata.Schema);
        var tableName = SqlIdentifier.RequireValid(table.Name, nameof(options.Table));
        var schema = table.Schema ?? metadata.Schema ?? dialect.DefaultSchema;

        var exclude = options.Exclude is { Count: > 0 }
            ? new HashSet<string>(options.Exclude, StringComparer.Ordinal)
            : null;

        var databaseName = connection.Database;
        var schemaByColumn = await SchemaCache.GetOrLoadAsync(
            connection,
            dialect,
            databaseName,
            tableName,
            schema,
            transaction,
            options.CommandTimeout,
            cancellationToken).ConfigureAwait(false);

        var keyColumns = ResolveKeys(options.Keys, metadata, schemaByColumn);
        if (kind is not BulkOperationKind.Insert && keyColumns.Count == 0)
            throw new BulkyMergeException(
                $"Bulk {kind} on '{tableName}' requires primary keys. Add [Key] attributes or specify {nameof(BulkOperationOptions.Keys)}.");

        var writeColumns = ResolveWriteColumns(kind, metadata, exclude, keyColumns);
        if (writeColumns.Length == 0)
            throw new BulkyMergeException($"No columns to write for '{typeof(T).Name}'.");

        var identityCandidate = schemaByColumn.Values.FirstOrDefault(c => c.IsIdentity);
        ColumnSchema? identity = string.IsNullOrEmpty(identityCandidate.Name) ? null : identityCandidate;

        return new BulkPlan<T>
        {
            Kind = kind,
            Metadata = metadata,
            TableName = tableName,
            Schema = schema,
            StagingTable = dialect.GetTempTableName(tableName),
            WriteColumns = writeColumns,
            SchemaByColumn = schemaByColumn,
            Identity = identity,
            KeyColumns = keyColumns,
        };
    }

    private static ColumnDescriptor[] ResolveWriteColumns(
        BulkOperationKind kind,
        IBulkMetadata metadata,
        HashSet<string>? exclude,
        IReadOnlyList<string> keyColumns)
    {
        IEnumerable<ColumnDescriptor> columns = metadata.Columns;

        if (exclude is not null)
            columns = columns.Where(c => !exclude.Contains(c.PropertyName));

        if (kind == BulkOperationKind.Delete)
        {
            var keySet = new HashSet<string>(keyColumns, StringComparer.OrdinalIgnoreCase);
            columns = columns.Where(c => keySet.Contains(c.Name));
        }

        return columns.ToArray();
    }

    private static IReadOnlyList<string> ResolveKeys(
        IReadOnlyList<string>? optionKeys,
        IBulkMetadata metadata,
        IReadOnlyDictionary<string, ColumnSchema> schema)
    {
        if (optionKeys is { Count: > 0 })
            return optionKeys.Select(k => SqlIdentifier.RequireValid(k, nameof(BulkOperationOptions.Keys))).ToArray();

        if (metadata.KeyColumns.Count > 0)
            return metadata.KeyColumns;

        return schema.Values
            .Where(c => c.IsPrimaryKey)
            .Select(c => c.Name)
            .ToArray();
    }
}

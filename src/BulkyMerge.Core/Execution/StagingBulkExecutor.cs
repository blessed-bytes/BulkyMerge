using System.Data.Common;
using BulkyMerge.Execution;
using BulkyMerge.Metadata;
using BulkyMerge.Planning;
using BulkyMerge.Providers;

namespace BulkyMerge.Execution;

internal sealed class StagingBulkExecutor(IDialect dialect, IProviderWriter writer)
{
    public Task InsertAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        IBulkMetadata<T> metadata,
        InsertManyOptions? options,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
        => ExecuteAsync(
            connection,
            rows,
            metadata,
            BulkOperationKind.Insert,
            options ?? new InsertManyOptions(),
            transaction,
            mapGeneratedKeys: options?.MapGeneratedKeys ?? false,
            cancellationToken);

    public Task UpdateAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        IBulkMetadata<T> metadata,
        UpdateManyOptions? options,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
        => ExecuteAsync(
            connection,
            rows,
            metadata,
            BulkOperationKind.Update,
            options ?? new UpdateManyOptions(),
            transaction,
            mapGeneratedKeys: false,
            cancellationToken);

    public Task UpsertAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        IBulkMetadata<T> metadata,
        UpsertManyOptions? options,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
        => ExecuteAsync(
            connection,
            rows,
            metadata,
            BulkOperationKind.Upsert,
            options ?? new UpsertManyOptions(),
            transaction,
            mapGeneratedKeys: options?.MapGeneratedKeys ?? false,
            cancellationToken);

    public Task DeleteAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        IBulkMetadata<T> metadata,
        DeleteManyOptions? options,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
        => ExecuteAsync(
            connection,
            rows,
            metadata,
            BulkOperationKind.Delete,
            options ?? new DeleteManyOptions(),
            transaction,
            mapGeneratedKeys: false,
            cancellationToken);

    private async Task ExecuteAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        IBulkMetadata<T> metadata,
        BulkOperationKind kind,
        BulkOperationOptions options,
        DbTransaction? transaction,
        bool mapGeneratedKeys,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(rows);

        var ownsConnection = false;
        var ownsTransaction = false;
        DbTransaction? tx = transaction;

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                ownsConnection = true;
            }

            if (tx is null && options.UseTransaction)
            {
                tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                ownsTransaction = true;
            }

            var plan = await CreatePlanAsync(
                dialect,
                kind,
                metadata,
                connection,
                options,
                tx,
                cancellationToken).ConfigureAwait(false);

            await CreateAndFillStagingAsync(
                connection,
                rows,
                metadata,
                plan,
                options,
                tx,
                cancellationToken).ConfigureAwait(false);

            var mergeSql = BuildMergeSql(dialect, plan, mapGeneratedKeys);
            var needsKeyMapping = mapGeneratedKeys && plan.Identity is not null
                && kind is BulkOperationKind.Insert or BulkOperationKind.Upsert;

            if (needsKeyMapping)
            {
                await MapGeneratedKeysAsync(
                    connection,
                    rows,
                    metadata,
                    plan,
                    mergeSql,
                    tx,
                    options.CommandTimeout,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await DbCommandExecutor.ExecuteAsync(
                    connection,
                    mergeSql,
                    tx,
                    options.CommandTimeout,
                    cancellationToken).ConfigureAwait(false);
            }

            if (ownsTransaction && tx is not null)
                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (ownsTransaction && tx is not null)
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (ownsTransaction)
                await tx!.DisposeAsync().ConfigureAwait(false);

            if (ownsConnection)
                await connection.CloseAsync().ConfigureAwait(false);
        }
    }

    private static Task<BulkPlan<T>> CreatePlanAsync<T>(
        IDialect dialect,
        BulkOperationKind kind,
        IBulkMetadata<T> metadata,
        DbConnection connection,
        BulkOperationOptions options,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
        => kind switch
        {
            BulkOperationKind.Insert => BulkPlanner.PlanInsertAsync(
                metadata, dialect, connection, (InsertManyOptions)options, transaction, cancellationToken),
            BulkOperationKind.Update => BulkPlanner.PlanUpdateAsync(
                metadata, dialect, connection, (UpdateManyOptions)options, transaction, cancellationToken),
            BulkOperationKind.Upsert => BulkPlanner.PlanUpsertAsync(
                metadata, dialect, connection, (UpsertManyOptions)options, transaction, cancellationToken),
            BulkOperationKind.Delete => BulkPlanner.PlanDeleteAsync(
                metadata, dialect, connection, (DeleteManyOptions)options, transaction, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private async Task CreateAndFillStagingAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        IBulkMetadata<T> metadata,
        BulkPlan<T> plan,
        BulkOperationOptions options,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
        where T : class
    {
        var columnNames = plan.WriteColumns.Select(c => c.Name).ToArray();
        var createStaging = dialect.BuildCreateStagingTableSql(
            plan.StagingTable,
            plan.TableName,
            columnNames,
            plan.Schema);

        await DbCommandExecutor.ExecuteAsync(
            connection,
            createStaging,
            transaction,
            options.CommandTimeout,
            cancellationToken).ConfigureAwait(false);

        if (plan.Identity is { } identity
            && plan.Kind is BulkOperationKind.Insert or BulkOperationKind.Upsert)
        {
            var alter = dialect.BuildAlterIdentityColumnSql(plan.StagingTable, identity);
            await DbCommandExecutor.ExecuteAsync(
                connection,
                alter,
                transaction,
                options.CommandTimeout,
                cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteStagingAsync(
            new BulkWriteContext<T>
            {
                Connection = connection,
                Transaction = transaction,
                Rows = rows,
                Metadata = metadata,
                WriteColumns = plan.WriteColumns,
                SchemaByColumn = plan.SchemaByColumn,
                StagingTable = plan.StagingTable,
                CommandTimeout = options.CommandTimeout,
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static string BuildMergeSql<T>(IDialect dialect, BulkPlan<T> plan, bool mapGeneratedKeys)
        where T : class
    {
        var columnNames = plan.WriteColumns.Select(c => c.Name).ToArray();

        return plan.Kind switch
        {
            BulkOperationKind.Insert => dialect.BuildInsertFromStagingSql(
                columnNames,
                plan.TableName,
                plan.StagingTable,
                plan.Schema,
                plan.Identity,
                mapGeneratedKeys),

            BulkOperationKind.Update => dialect.BuildUpdateFromStagingSql(
                columnNames,
                plan.KeyColumns,
                plan.TableName,
                plan.StagingTable,
                plan.Schema),

            BulkOperationKind.Upsert => dialect.BuildUpsertFromStagingSql(
                columnNames,
                plan.KeyColumns,
                plan.TableName,
                plan.StagingTable,
                plan.Schema,
                plan.Identity,
                mapGeneratedKeys),

            BulkOperationKind.Delete => dialect.BuildDeleteFromStagingSql(
                plan.KeyColumns,
                plan.TableName,
                plan.StagingTable,
                plan.Schema),

            _ => throw new ArgumentOutOfRangeException(nameof(plan.Kind)),
        };
    }

    private static async Task MapGeneratedKeysAsync<T>(
        DbConnection connection,
        IEnumerable<T> rows,
        IBulkMetadata<T> metadata,
        BulkPlan<T> plan,
        string sql,
        DbTransaction? transaction,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        where T : class
    {
        if (plan.Identity is not { } identity)
            throw new BulkyMergeException("Identity column was expected but not found in schema.");

        var identityColumn = identity.Name;
        var identityPropertyIndex = metadata.Columns
            .Select((c, i) => (c, i))
            .First(c => c.c.Name.Equals(identityColumn, StringComparison.OrdinalIgnoreCase))
            .i;

        var propertyType = metadata.Columns[identityPropertyIndex].PropertyType;
        var defaultValue = propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;

        await using var reader = await DbCommandExecutor.ExecuteReaderAsync(
            connection,
            sql,
            transaction,
            timeout,
            cancellationToken).ConfigureAwait(false);

        foreach (var row in rows)
        {
            var current = metadata.GetValue(row, identityPropertyIndex);
            if (!Equals(current, defaultValue))
                continue;

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                break;

            var generated = reader.GetValue(0);
            if (generated is not null && generated != DBNull.Value)
                metadata.SetValue(row, identityPropertyIndex, Convert.ChangeType(generated, propertyType));
        }
    }
}

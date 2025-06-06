using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;

namespace BulkyMerge;

public static class BulkExtensions
{
    public const int DefaultBatchSize = 1000;
    private readonly static ConcurrentDictionary<string, List<ColumnInfo>> ColumnsCache = new();
    private static async Task<MergeContext<T>> BuildContextAsync<T>(ISqlDialect sqlDialect,
            DbConnection connection,
            IEnumerable<T> items,
            DbTransaction transaction,
            MergeOptions options)
    {
        var type = typeof(T);
        var propertyAccessor = PropertyAccessor<T>.Create();
        var tableAttribute = type.GetCustomAttribute<TableAttribute>(true);
        var tableName = options.TableName ?? tableAttribute?.Name ?? type.Name;
        var primaryKeys = options.PrimaryKeys ?? propertyAccessor.Properties.Where(x => x.GetCustomAttribute<KeyAttribute>(true) is not null).Select(x =>
            x.GetCustomAttribute<ColumnAttribute>(true)?.Name ?? x.Name).ToList();

        List<ColumnInfo> columns = null;
        if (sqlDialect != null)
        {
            var key = $"{connection.Database}.{tableName}";
            if (!ColumnsCache.TryGetValue(key, out var cache))
            {
                cache = await FindColumnsAsync(sqlDialect, connection, transaction, tableName);
                ColumnsCache[key] = cache;
            }
            columns = cache;
        }
        if (!primaryKeys.Any())
        {
            primaryKeys = columns?.Where(x => x.IsPrimaryKey).Select(x => x.Name);
        }

        var columnsToProperties = propertyAccessor.Properties.Where(x => x.GetCustomAttribute<NotMappedAttribute>(true) is null).ToDictionary(x =>
            x.GetCustomAttribute<ColumnAttribute>(true)?.Name ?? x.Name);
        var tempTableName = sqlDialect?.GetTempTableName(tableName);
        return new MergeContext<T>(connection,
            transaction,
            items,
            propertyAccessor,
            tableName,
            options.Schema ?? tableAttribute?.Schema ?? sqlDialect.DefaultScheme,
            tempTableName,
            columnsToProperties.Where(x => options.ExcludeProperties?.Any(c => c == x.Key) != true).ToDictionary(),
            columns?.ToDictionary(x => x.Name?.ToLower()),
            columns?.FirstOrDefault(x => x.IsIdentity),
            primaryKeys.ToList(),
            options.BatchSize,
            options.Timeout);
    }

    public static async Task BulkCopyAsync<T>(IBulkWriter bulkWriter, DbConnection connection,
        DbTransaction transaction,
        IEnumerable<T> items,
        string tableName = default,
        IEnumerable<string> excludeColumns = default,
        int timeout = 1000,
        int batchSize = DefaultBatchSize)
    {
        var shouldCloseConnection =  await OpenConnectionAsync(connection);

        var context = await BuildContextAsync(null, connection, items, transaction,
            new MergeOptions
            {
                TableName = tableName,
                BatchSize = batchSize,
                Timeout = timeout,
                MapOutputIdentity = false
            } );
        await bulkWriter.WriteAsync(context.TableName, context);
        if (shouldCloseConnection) await connection.CloseAsync();
    }

     public static Task BulkInsertOrUpdateAsync<T>(IBulkWriter bulkWriter, ISqlDialect dialect, DbConnection connection,
            IEnumerable<T> items,
            string tableName = default,
            DbTransaction transaction = default,
            int batchSize = DefaultBatchSize,
            IEnumerable<string> excludeProperties = default,
            IEnumerable<string> primaryKeys = default,
            int timeout = 1000,
            bool mapIdentity = true)
    => ExecuteInternalAsync(
            (dialect, context) => dialect.GetInsertOrUpdateMergeStatement(context.ColumnsToProperty.Keys, context.TableName, context.TempTableName, context.PrimaryKeys, context.Identity),
            bulkWriter, dialect, connection, items, tableName, transaction, batchSize, excludeProperties, primaryKeys, timeout,  mapIdentity);

    public static async Task ExecuteInternalAsync<T>(
            Func<ISqlDialect, MergeContext<T>, string> dialectCall,
            IBulkWriter bulkWriter, 
            ISqlDialect dialect, 
            DbConnection connection,
            IEnumerable<T> items,
            string tableName = default,
            DbTransaction transaction = default,
            int batchSize = DefaultBatchSize,
            IEnumerable<string> excludeProperties = default,
            IEnumerable<string> primaryKeys = default,
            int timeout = 1000,
            bool mapOutputIdentity = false)
    {
        var shouldCloseConnection = await OpenConnectionAsync(connection);

        var context = await BuildContextAsync(dialect, connection, items, transaction,
            new MergeOptions
            {
                TableName = tableName,
                ExcludeProperties = excludeProperties,
                PrimaryKeys = primaryKeys,
                Timeout = timeout,
                MapOutputIdentity = mapOutputIdentity,
                BatchSize = batchSize
            });

        await WriteToTempAsync(bulkWriter,
            dialect,
            context);

        var merge = dialectCall(dialect, context);

        if (!mapOutputIdentity || context.Identity is null)
        {
            await ExecuteAsync(connection, merge, transaction, context.Timeout);
            if (shouldCloseConnection) await connection.CloseAsync();
            return;
        }

        await using (var reader = await ExecuteReaderAsync(connection, merge, transaction))
        {
            await MapIdentityAsync(reader, context);
        }

        if (shouldCloseConnection) await connection.CloseAsync();
    }
     
     private static async Task WriteToTempAsync<T>(IBulkWriter bulkWriter, 
         ISqlDialect dialect,
         MergeContext<T> context,
         bool excludePrimaryKeys = false)
     {
         await CreateTemporaryTableAsync(dialect, context.Connection, context.Transaction, context.Identity, context.TableName, context.TempTableName, context.ColumnsToProperty.Keys);
        await bulkWriter.WriteAsync(context.TempTableName, context);
    }
    public static Task BulkInsertAsync<T>(IBulkWriter bulkWriter, ISqlDialect dialect, 
         DbConnection connection,
         IEnumerable<T> items,
         string tableName = default,
         DbTransaction transaction = default,
         int batchSize = DefaultBatchSize,
         string[] excludeProperties = default,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000,
         bool mapOutputIdentity = false)
     => ExecuteInternalAsync(
            (dialect, context) => dialect.GetInsertQuery(context.ColumnsToProperty.Keys, context.TableName, context.TempTableName, context.PrimaryKeys, context.Identity),
            bulkWriter, dialect, connection, items, tableName, transaction, batchSize, excludeProperties, primaryKeys, timeout, mapOutputIdentity);

    public static Task BulkUpdateAsync<T>(IBulkWriter bulkWriter, ISqlDialect dialect, DbConnection connection,
         IEnumerable<T> items,
         string tableName = default,
         DbTransaction transaction = default,
         int batchSize = DefaultBatchSize,
         string[] excludeProperties = default,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000)
     => ExecuteInternalAsync(
            (dialect, context) => dialect.GetUpdateQuery(context.ColumnsToProperty.Keys, context.TableName, context.TempTableName, context.PrimaryKeys, context.Identity),
            bulkWriter, dialect, connection, items, tableName, transaction, batchSize, excludeProperties, primaryKeys, timeout, false);

    public static Task BulkDeleteAsync<T>(IBulkWriter bulkWriter, 
         ISqlDialect dialect, 
         DbConnection connection,
         IEnumerable<T> items,
         string tableName = default,
         DbTransaction transaction = default,
         int batchSize = DefaultBatchSize,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000)
     => ExecuteInternalAsync(
            (dialect, context) => dialect.GetDeleteQuery(context.TableName, context.TempTableName, context.PrimaryKeys, context.Identity),
            bulkWriter, dialect, connection, items, tableName, transaction, batchSize, null, primaryKeys, timeout, false);

    private static Task CreateTemporaryTableAsync(ISqlDialect dialect, DbConnection connection, 
        DbTransaction transaction, 
        ColumnInfo identity, 
        string tableName, 
        string tempTableName, 
        IEnumerable<string> columnNames = default,
        int timeout = 1000)
    {
        var queryString = new StringBuilder(dialect.GetCreateTempTableQuery(tempTableName, tableName, columnNames));
        if (identity is not null)
        {
            queryString.AppendLine(dialect.GetAlterIdentityColumnQuery(tempTableName, identity));
        }

        return ExecuteAsync(connection, queryString.ToString(), transaction, timeout);
    }

    private static async Task<List<ColumnInfo>> FindColumnsAsync(ISqlDialect dialect, DbConnection connection, DbTransaction transaction, string tableName, int timeout = 1000)
    {
        var result = new List<ColumnInfo>();
        await using var reader = await ExecuteReaderAsync(connection, dialect.GetColumnsQuery(connection.Database, tableName), transaction, timeout);
        while (await reader.ReadAsync())
        {
            result.Add(new ColumnInfo(reader.GetString(0), reader.GetString(1), reader.GetInt32(2) == 1, reader.GetInt32(3) == 1));
        }
        return result;
    }
    
    private static async Task<bool> OpenConnectionAsync(DbConnection connection)
    {
        if (connection.State is ConnectionState.Open) return false;
        await connection.OpenAsync();
        return true;
    }
    
    private static async Task ExecuteAsync(DbConnection connection, string sql, DbTransaction transaction, int timeout = 1000)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = timeout;
        await command.ExecuteNonQueryAsync();
    }
    
    private static async Task<DbDataReader> ExecuteReaderAsync(DbConnection connection, string sql, DbTransaction transaction, int timeout = 1000)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = timeout;
        return await command.ExecuteReaderAsync();
    }

    private static async Task MapIdentityAsync<T>(DbDataReader reader, MergeContext<T> context)
    {
        if (context.Identity is null) return;
        var identityTypeCacheItem = context.ColumnsToProperty.Where(x =>
            x.Key.Equals(context.Identity.Name, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).First();
        var identityType = identityTypeCacheItem.PropertyType;
        var defaultIdentityValue = !identityType.IsGenericType ? Activator.CreateInstance(identityType) : null;
        var identityName = identityTypeCacheItem.Name;
        foreach (var item in context.Items)
        {
            var value = context.TypeAccessor.GetValue(item, identityName);
            if (!object.Equals(defaultIdentityValue, value))
                continue;
            if (await reader.ReadAsync())
            {
                context.TypeAccessor.SetValue(item, identityName, System.Convert.ChangeType(reader[0], identityType));
            }
        }
    }
    public static void BulkCopy<T>(IBulkWriter bulkWriter, DbConnection connection,
        DbTransaction transaction,
        IEnumerable<T> items,
        string tableName = default,
        IEnumerable<string> excludeColumns = default,
        int timeout = 1000,
        int batchSize = DefaultBatchSize)
    => BulkCopyAsync(bulkWriter, connection, transaction, items, tableName, excludeColumns, timeout, batchSize).GetAwaiter().GetResult();

    public static void BulkInsertOrUpdate<T>(IBulkWriter bulkWriter, ISqlDialect dialect, DbConnection connection,
            IEnumerable<T> items,
            string tableName = default,
            DbTransaction transaction = default,
            int batchSize = DefaultBatchSize,
            IEnumerable<string> excludeProperties = default,
            IEnumerable<string> primaryKeys = default,
            int timeout = 1000,
            bool mapOutputIdentity = false)
    => BulkInsertOrUpdateAsync(bulkWriter, dialect, connection, items, tableName, transaction, batchSize, excludeProperties, primaryKeys, timeout, mapOutputIdentity).GetAwaiter().GetResult();

    public static void BulkInsert<T>(IBulkWriter bulkWriter, ISqlDialect dialect,
         DbConnection connection,
         IList<T> items,
         string tableName = default,
         DbTransaction transaction = default,
         int batchSize = DefaultBatchSize,
         string[] excludeProperties = default,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000, 
         bool mapOutputIdentity = false)
    => BulkInsertAsync(bulkWriter, dialect, connection, items, tableName, transaction, batchSize, excludeProperties, primaryKeys, timeout, mapOutputIdentity).GetAwaiter().GetResult();

    public static void BulkUpdate<T>(IBulkWriter bulkWriter, ISqlDialect dialect, DbConnection connection,
         IList<T> items,
         string tableName = default,
         DbTransaction transaction = default,
         int batchSize = DefaultBatchSize,
         string[] excludeProperties = default,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000)
     => BulkUpdateAsync(bulkWriter, dialect, connection, items, tableName, transaction, batchSize, excludeProperties, primaryKeys, timeout).GetAwaiter().GetResult();

    public static void BulkDelete<T>(IBulkWriter bulkWriter, ISqlDialect dialect, DbConnection connection,
         IList<T> items,
         string tableName = default,
         DbTransaction transaction = default,
         int batchSize = DefaultBatchSize,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000)
     => BulkDeleteAsync(bulkWriter, dialect, connection, items, tableName, transaction, batchSize, primaryKeys, timeout).GetAwaiter().GetResult();

}
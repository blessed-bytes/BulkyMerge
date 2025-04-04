using System.Collections.Generic;
using System.Threading.Tasks;
using BulkyMerge;
using Npgsql;

namespace BulkyMerge.PostgreSql;

public static partial class NpgsqlBulkExtensions
{
    private static readonly NpgsqlDialect Dialect = new();
    private static readonly NpgsqlBulkWriter BulkWriter = new();
    public static Task BulkCopyAsync<T>(this NpgsqlConnection connection,
        IEnumerable<T> items,
        string tableName = default,
        NpgsqlTransaction transaction = default,
        IEnumerable<string> excludeColumns = default,
        int timeout = int.MaxValue,
        int batchSize = BulkExtensions.DefaultBatchSize)
    => BulkExtensions.BulkCopyAsync(BulkWriter, connection, transaction, items, tableName, excludeColumns, timeout, batchSize);

    public static Task BulkInsertOrUpdateAsync<T>(this NpgsqlConnection connection,
        IList<T> items,
        string tableName = default,
        NpgsqlTransaction transaction = default,
        int batchSize = BulkExtensions.DefaultBatchSize,
        IEnumerable<string> excludeProperties = default,
        IEnumerable<string> primaryKeys = default,
        int timeout = int.MaxValue,
        bool mapIdentity = true)
        => BulkExtensions.BulkInsertOrUpdateAsync(BulkWriter, 
            Dialect, 
            connection, 
            items, 
            tableName, 
            transaction,
            batchSize, 
            excludeProperties, 
            primaryKeys, 
            timeout,
            mapIdentity);
     
     public static Task BulkInsertAsync<T>(this NpgsqlConnection connection,
         IList<T> items,
         string tableName = default,
         NpgsqlTransaction transaction = default,
         int batchSize =  BulkExtensions.DefaultBatchSize,
         string[] excludeProperties = default,
         IEnumerable<string> primaryKeys = default,
         int timeout = int.MaxValue,
         bool mapIdentity = true)
     => BulkExtensions.BulkInsertAsync(BulkWriter, 
         Dialect, 
         connection, 
         items, 
         tableName, 
         transaction,
         batchSize, 
         excludeProperties, 
         primaryKeys, 
         timeout,
         mapIdentity);
     
     public static Task BulkUpdateAsync<T>(this NpgsqlConnection connection,
         IEnumerable<T> items,
         string tableName = default,
         NpgsqlTransaction transaction = default,
         int batchSize = BulkExtensions.DefaultBatchSize,
         string[] excludeProperties = default,
         IEnumerable<string> primaryKeys = default,
         int timeout = int.MaxValue)
     => BulkExtensions.BulkUpdateAsync(BulkWriter, 
         Dialect, 
         connection, 
         items, 
         tableName, 
         transaction,
         batchSize, 
         excludeProperties, 
         primaryKeys, 
         timeout);
     
     public static Task BulkDeleteAsync<T>(this NpgsqlConnection connection,
         IEnumerable<T> items,
         string tableName = default,
         NpgsqlTransaction transaction = default,
         int batchSize = BulkExtensions.DefaultBatchSize,
         int bulkCopyTimeout = int.MaxValue,
         IEnumerable<string> primaryKeys = default,
         int timeout = int.MaxValue)
         => BulkExtensions.BulkDeleteAsync(BulkWriter, 
             Dialect, 
             connection, 
             items, 
             tableName, 
             transaction,
             batchSize, 
             primaryKeys, 
             timeout);

    public static void BulkCopy<T>(this NpgsqlConnection connection,
        IEnumerable<T> items,
        string tableName = default,
        NpgsqlTransaction transaction = default,
        IEnumerable<string> excludeColumns = default,
        int timeout = int.MaxValue,
        int batchSize = BulkExtensions.DefaultBatchSize)
    => BulkExtensions.BulkCopy(BulkWriter, connection, transaction, items, tableName, excludeColumns, timeout, batchSize);

    public static void BulkInsertOrUpdate<T>(this NpgsqlConnection connection,
           IEnumerable<T> items,
           string tableName = default,
           NpgsqlTransaction transaction = default,
           int batchSize = BulkExtensions.DefaultBatchSize,
           IEnumerable<string> excludeProperties = default,
           IEnumerable<string> primaryKeys = default,
           int timeout = int.MaxValue)
    => BulkExtensions.BulkInsertOrUpdate(BulkWriter,
        Dialect,
        connection,
        items,
        tableName,
        transaction,
        batchSize,
        excludeProperties,
        primaryKeys,
        timeout);

    public static void BulkInsert<T>(this NpgsqlConnection connection,
        IList<T> items,
        string tableName = default,
        NpgsqlTransaction transaction = default,
        int batchSize = BulkExtensions.DefaultBatchSize,
        int bulkCopyTimeout = int.MaxValue,
        string[] excludeProperties = default,
        IEnumerable<string> primaryKeys = default,
        int timeout = int.MaxValue,
        bool mapOutputIdentity = true)
    => BulkExtensions.BulkInsert(BulkWriter,
        Dialect,
        connection,
        items,
        tableName,
        transaction,
        batchSize,
        excludeProperties,
        primaryKeys,
        timeout,
        mapOutputIdentity);

    public static void BulkUpdate<T>(this NpgsqlConnection connection,
        IList<T> items,
        string tableName = default,
        NpgsqlTransaction transaction = default,
        int batchSize = BulkExtensions.DefaultBatchSize,
        string[] excludeProperties = default,
        IEnumerable<string> primaryKeys = default,
        int timeout = int.MaxValue)
    => BulkExtensions.BulkUpdate(BulkWriter,
        Dialect,
        connection,
        items,
        tableName,
        transaction,
        batchSize,
        excludeProperties,
        primaryKeys,
        timeout);

    public static void BulkDelete<T>(this NpgsqlConnection connection,
        IList<T> items,
        string tableName = default,
        NpgsqlTransaction transaction = default,
        int batchSize = BulkExtensions.DefaultBatchSize,
        IEnumerable<string> primaryKeys = default,
        int timeout = int.MaxValue)
    => BulkExtensions.BulkDelete(BulkWriter,
        Dialect,
        connection,
        items,
        tableName,
        transaction,
        batchSize,
        primaryKeys,
        timeout);
}
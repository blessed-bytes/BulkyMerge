using System.Collections.Generic;
using System.Threading.Tasks;
using BulkyMerge;
using Npgsql;

namespace BulkyMerge.PostgreSql;

public static class NpgsqlBulkExtensions
{
    public static NpgsqlDialect Dialect = new();
    public static NpgsqlBulkWriter BulkWriter = new();
    public static Task BulkCopyAsync<T>(this NpgsqlConnection connection,
        IEnumerable<T> items,
        string tableName = default,
        NpgsqlTransaction transaction = default,
        IEnumerable<string> excludeColumns = default,
        int timeout = 1000,
        int batchSize = BulkExtensions.DefaultBatchSize)
    => BulkExtensions.BulkCopyAsync(BulkWriter, connection, transaction, items, tableName, excludeColumns, timeout, batchSize);

    public static Task BulkInsertOrUpdateAsync<T>(this NpgsqlConnection connection,
        IList<T> items,
        string tableName = default,
        NpgsqlTransaction transaction = default,
        int batchSize = BulkExtensions.DefaultBatchSize,
        IEnumerable<string> excludeProperties = default,
        IEnumerable<string> primaryKeys = default,
        int timeout = 1000,
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
         int timeout = 1000,
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
         int timeout = 1000)
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
         int bulkCopyTimeout = 1000,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000)
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
        int timeout = 1000,
        int batchSize = BulkExtensions.DefaultBatchSize)
    => BulkExtensions.BulkCopy(BulkWriter, connection, transaction, items, tableName, excludeColumns, timeout, batchSize);

    public static void BulkInsertOrUpdate<T>(this NpgsqlConnection connection,
           IEnumerable<T> items,
           string tableName = default,
           NpgsqlTransaction transaction = default,
           int batchSize = BulkExtensions.DefaultBatchSize,
           IEnumerable<string> excludeProperties = default,
           IEnumerable<string> primaryKeys = default,
           int timeout = 1000)
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
        int bulkCopyTimeout = 1000,
        string[] excludeProperties = default,
        IEnumerable<string> primaryKeys = default,
        int timeout = 1000,
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
        int timeout = 1000)
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
        int timeout = 1000)
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
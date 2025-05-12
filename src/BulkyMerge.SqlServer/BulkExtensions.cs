using Microsoft.Data.SqlClient;

namespace BulkyMerge.SqlServer;

public static class SqlServerBulkExtensions
{
    public static SqlServerDialect Dialect = new();
    public static SqlServerBulkWriter BulkWriter = new(Dialect);
    public static Task BulkCopyAsync<T>(this SqlConnection connection,
        IEnumerable<T> items,
        string tableName = default,
        SqlTransaction transaction = default,
        IEnumerable<string> excludeColumns = default,
        int timeout = int.MaxValue,
        int batchSize = BulkExtensions.DefaultBatchSize)
    => BulkExtensions.BulkCopyAsync(BulkWriter, connection, transaction, items, tableName, excludeColumns, timeout, batchSize);

    public static Task BulkInsertOrUpdateAsync<T>(this SqlConnection connection,
        IList<T> items,
        string tableName = default,
        SqlTransaction transaction = default,
        int batchSize = BulkExtensions.DefaultBatchSize,
        IEnumerable<string> excludeProperties = default,
        IEnumerable<string> primaryKeys = default,
        int timeout = int.MaxValue,
        bool mapOutputIdentity = true)
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
            mapOutputIdentity);
     
     public static Task BulkInsertAsync<T>(this SqlConnection connection,
         IList<T> items,
         string tableName = default,
         SqlTransaction transaction = default,
         int batchSize =  BulkExtensions.DefaultBatchSize,
         string[] excludeProperties = default,
         IEnumerable<string> primaryKeys = default,
         int timeout = int.MaxValue,
         bool mapOutputIdentity = false)
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
         mapOutputIdentity);
     
     public static Task BulkUpdateAsync<T>(this SqlConnection connection,
         IList<T> items,
         string tableName = default,
         SqlTransaction transaction = default,
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
     
     public static Task BulkDeleteAsync<T>(this SqlConnection connection,
         IList<T> items,
         string tableName = default,
         SqlTransaction transaction = default,
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

    public static void BulkCopy<T>(this SqlConnection connection,
        IEnumerable<T> items,
        string tableName = default,
        SqlTransaction transaction = default,
        IEnumerable<string> excludeColumns = default,
        int timeout = int.MaxValue,
        int batchSize = BulkExtensions.DefaultBatchSize)
    => BulkExtensions.BulkCopy(BulkWriter, connection, transaction, items, tableName, excludeColumns, timeout, batchSize);

    public static void BulkInsertOrUpdate<T>(this SqlConnection connection,
           IList<T> items,
           string tableName = default,
           SqlTransaction transaction = default,
           int batchSize = BulkExtensions.DefaultBatchSize,
           IEnumerable<string> excludeProperties = default,
           IEnumerable<string> primaryKeys = default,
           int timeout = int.MaxValue,
           bool mapOutputIdentity = true)
     => BulkExtensions.BulkInsertOrUpdate(BulkWriter,
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

    public static void BulkInsert<T>(this SqlConnection connection,
        IList<T> items,
        string tableName = default,
        SqlTransaction transaction = default,
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

    public static void BulkUpdate<T>(this SqlConnection connection,
        IList<T> items,
        string tableName = default,
        SqlTransaction transaction = default,
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

    public static void BulkDelete<T>(this SqlConnection connection,
        IList<T> items,
        string tableName = default,
        SqlTransaction transaction = default,
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
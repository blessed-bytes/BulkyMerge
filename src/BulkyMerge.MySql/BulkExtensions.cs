using MySqlConnector;

namespace BulkyMerge.MySql;

public static class MySqlBulkExtensions
{
    private static MySqlDialect Dialect = new();
    private static MySqlBulkWriter BulkWriter = new();

    public static Task BulkCopyAsync<T>(this MySqlConnection connection,
        IEnumerable<T> items,
        string tableName = default,
        MySqlTransaction transaction = default,
        IEnumerable<string> excludeColumns = default,
        int timeout = 1000,
        int batchSize = BulkExtensions.DefaultBatchSize)
    => BulkExtensions.BulkCopyAsync(BulkWriter, connection, transaction, items, tableName, excludeColumns, timeout, batchSize);

    public static Task BulkInsertOrUpdateAsync<T>(this MySqlConnection connection,
        IEnumerable<T> items,
        string tableName = default,
        MySqlTransaction transaction = default,
        int batchSize = BulkExtensions.DefaultBatchSize,
        IEnumerable<string> excludeProperties = default,
        IEnumerable<string> primaryKeys = default,
        int timeout = 1000,
        bool mapOutputIdentity = false)
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
     
     public static Task BulkInsertAsync<T>(this MySqlConnection connection,
         IEnumerable<T> items,
         string tableName = default,
         MySqlTransaction transaction = default,
         int batchSize =  BulkExtensions.DefaultBatchSize,
         string[] excludeProperties = default,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000,
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
     
     public static Task BulkUpdateAsync<T>(this MySqlConnection connection,
         IEnumerable<T> items,
         string tableName = default,
         MySqlTransaction transaction = default,
         int batchSize = BulkExtensions.DefaultBatchSize,
         string[] excludeProperties = default,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000)
     => BulkExtensions.BulkUpdateAsync(BulkWriter, 
         Dialect, 
         connection, 
         items.ToList(), 
         tableName, 
         transaction,
         batchSize, 
         excludeProperties, 
         primaryKeys, 
         timeout);
     
     public static Task BulkDeleteAsync<T>(this MySqlConnection connection,
         IEnumerable<T> items,
         string tableName = default,
         MySqlTransaction transaction = default,
         int batchSize = BulkExtensions.DefaultBatchSize,
         int bulkCopyTimeout = 1000,
         IEnumerable<string> primaryKeys = default,
         int timeout = 1000)
         => BulkExtensions.BulkDeleteAsync(BulkWriter, 
             Dialect, 
             connection, 
             items.ToList(), 
             tableName, 
             transaction,
             batchSize, 
             primaryKeys, 
             timeout);

    public static void BulkCopy<T>(this MySqlConnection connection,
        IEnumerable<T> items,
        string tableName = default,
        MySqlTransaction transaction = default,
        IEnumerable<string> excludeColumns = default,
        int timeout = 1000,
        int batchSize = BulkExtensions.DefaultBatchSize)
    => BulkExtensions.BulkCopy(BulkWriter, connection, transaction, items, tableName, excludeColumns, timeout, batchSize);

    public static void BulkInsertOrUpdate<T>(this MySqlConnection connection,
           IEnumerable<T> items,
           string tableName = default,
           MySqlTransaction transaction = default,
           int batchSize = BulkExtensions.DefaultBatchSize,
           IEnumerable<string> excludeProperties = default,
           IEnumerable<string> primaryKeys = default,
           int timeout = 1000,
           bool mapOutputIdentity = false)
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

    public static void BulkInsert<T>(this MySqlConnection connection,
        IList<T> items,
        string tableName = default,
        MySqlTransaction transaction = default,
        int batchSize = BulkExtensions.DefaultBatchSize,
        string[] excludeProperties = default,
        IEnumerable<string> primaryKeys = default,
        int timeout = 1000,
        bool mapOutputIdentity = false)
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

    public static void BulkUpdate<T>(this MySqlConnection connection,
        IList<T> items,
        string tableName = default,
        MySqlTransaction transaction = default,
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

    public static void BulkDelete<T>(this MySqlConnection connection,
        IList<T> items,
        string tableName = default,
        MySqlTransaction transaction = default,
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
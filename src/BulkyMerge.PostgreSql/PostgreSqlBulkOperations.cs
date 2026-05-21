using BulkyMerge.Providers;

namespace BulkyMerge.PostgreSql;

public static class PostgreSqlBulkOperations
{
    public static readonly IBulkOperations Instance = new BulkOperations(
        new PostgreSqlDialect(),
        new NpgsqlRowWriter());
}

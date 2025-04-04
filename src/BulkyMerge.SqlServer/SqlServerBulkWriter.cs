using Microsoft.Data.SqlClient;

namespace BulkyMerge.SqlServer;

public sealed class SqlServerBulkWriter : IBulkWriter
{
    private readonly ISqlDialect _dialect;

    public SqlServerBulkWriter(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public void Write<T>(string destination, MergeContext<T> context)
        => WriteAsync(destination, context).GetAwaiter().GetResult();

    public async Task WriteAsync<T>(string destination, MergeContext<T> context)
    {

        var objectReader = new DataReader<T>(context.Items, context.TypeAccessor, context.ColumnsToProperty.Values.Select(x => x.Name).ToArray());
        using var microsoftClientBulkCopy = new SqlBulkCopy(context.Connection as SqlConnection, SqlBulkCopyOptions.TableLock, context.Transaction as SqlTransaction);
        foreach (var columnMapping in context.ColumnsToProperty)
        {
            microsoftClientBulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(columnMapping.Value.Name, columnMapping.Key));
        }
        microsoftClientBulkCopy.BatchSize = context.BatchSize;
        microsoftClientBulkCopy.BulkCopyTimeout = context.Timeout;
        microsoftClientBulkCopy.DestinationTableName = destination;

        await microsoftClientBulkCopy.WriteToServerAsync(objectReader);
    }
}
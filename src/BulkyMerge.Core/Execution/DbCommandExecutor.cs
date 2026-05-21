using System.Data.Common;

namespace BulkyMerge.Execution;

internal static class DbCommandExecutor
{
    public static async Task ExecuteAsync(
        DbConnection connection,
        string sql,
        DbTransaction? transaction,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = (int)Math.Clamp(timeout.TotalSeconds, 1, int.MaxValue);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DbDataReader> ExecuteReaderAsync(
        DbConnection connection,
        string sql,
        DbTransaction? transaction,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = (int)Math.Clamp(timeout.TotalSeconds, 1, int.MaxValue);
        return await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
    }
}

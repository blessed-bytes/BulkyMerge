using BulkyMerge.Schema;

namespace BulkyMerge.Providers;

public interface IDialect
{
    string DefaultSchema { get; }

    string QuoteIdentifier(string identifier);

    string GetTempTableName(string tableName);

    string GetColumnsQuery(string databaseName, string tableName, string? schema);

    string BuildCreateStagingTableSql(string stagingTable, string targetTable, IReadOnlyList<string> columnNames, string? schema);

    string BuildAlterIdentityColumnSql(string stagingTable, ColumnSchema identity);

    string BuildInsertFromStagingSql(
        IReadOnlyList<string> columnNames,
        string targetTable,
        string stagingTable,
        string? schema,
        ColumnSchema? identity,
        bool returnGeneratedKeys);

    string BuildDropStagingTableSql(string stagingTable);

    string BuildUpdateFromStagingSql(
        IReadOnlyList<string> columnNames,
        IReadOnlyList<string> keyColumns,
        string targetTable,
        string stagingTable,
        string? schema);

    string BuildUpsertFromStagingSql(
        IReadOnlyList<string> columnNames,
        IReadOnlyList<string> keyColumns,
        string targetTable,
        string stagingTable,
        string? schema,
        ColumnSchema? identity,
        bool returnGeneratedKeys);

    string BuildDeleteFromStagingSql(
        IReadOnlyList<string> keyColumns,
        string targetTable,
        string stagingTable,
        string? schema);
}

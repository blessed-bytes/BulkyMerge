using System.Text;
using BulkyMerge.Providers;
using BulkyMerge.Schema;
using BulkyMerge.Sql;

namespace BulkyMerge.PostgreSql;

internal sealed class PostgreSqlDialect : IDialect
{
    public string DefaultSchema => "public";

    public string QuoteIdentifier(string identifier)
        => $"\"{SqlIdentifier.RequireValid(identifier)}\"";

    public string GetTempTableName(string tableName)
        => $"{SqlIdentifier.RequireValid(tableName)}_{Guid.NewGuid():N}";

    public string GetColumnsQuery(string databaseName, string tableName, string? schema)
    {
        var safeTable = tableName.Replace("'", "''", StringComparison.Ordinal);
        var safeSchema = (schema ?? DefaultSchema).Replace("'", "''", StringComparison.Ordinal);
        return $"""
            SELECT
                c.column_name,
                c.data_type,
                CASE
                    WHEN c.column_default LIKE 'nextval%' THEN 1
                    WHEN c.is_identity = 'YES' THEN 1
                    ELSE 0
                END AS is_identity,
                CASE WHEN pk.column_name IS NOT NULL THEN 1 ELSE 0 END AS is_primary_key
            FROM information_schema.columns c
            LEFT JOIN (
                SELECT kcu.table_name, kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                  ON tc.constraint_name = kcu.constraint_name
                 AND tc.table_schema = kcu.table_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
                  AND tc.table_schema = '{safeSchema}'
            ) pk ON c.table_name = pk.table_name AND c.column_name = pk.column_name
            WHERE c.table_schema = '{safeSchema}' AND c.table_name = '{safeTable}';
            """;
    }

    public string BuildCreateStagingTableSql(
        string stagingTable,
        string targetTable,
        IReadOnlyList<string> columnNames,
        string? schema)
    {
        var staging = QuoteIdentifier(stagingTable);
        var target = QuoteIdentifier(targetTable);
        return $"SELECT * INTO TEMP {staging} FROM {target} WHERE 1 = 0;";
    }

    public string BuildAlterIdentityColumnSql(string stagingTable, ColumnSchema identity)
    {
        var staging = QuoteIdentifier(stagingTable);
        var column = QuoteIdentifier(identity.Name);
        return $"""
            ALTER TABLE {staging} DROP COLUMN {column};
            ALTER TABLE {staging} ADD {column} {identity.DataType};
            """;
    }

    public string BuildInsertFromStagingSql(
        IReadOnlyList<string> columnNames,
        string targetTable,
        string stagingTable,
        string? schema,
        ColumnSchema? identity,
        bool returnGeneratedKeys)
    {
        var columns = columnNames
            .Where(n => identity is null || !n.Equals(identity.Value.Name, StringComparison.OrdinalIgnoreCase))
            .Select(QuoteIdentifier)
            .ToArray();

        var columnsList = string.Join(',', columns);
        var target = QuoteIdentifier(targetTable);
        var staging = QuoteIdentifier(stagingTable);

        var insert = $"""
            INSERT INTO {target} ({columnsList})
            SELECT {columnsList} FROM {staging}
            """;

        if (!returnGeneratedKeys || identity is null)
            return $"{insert}; DROP TABLE {staging};";

        var idColumn = QuoteIdentifier(identity.Value.Name);
        return $"""
            WITH inserted AS (
                {insert}
                RETURNING {idColumn}
            )
            SELECT {idColumn} FROM inserted;
            DROP TABLE {staging};
            """;
    }

    public string BuildDropStagingTableSql(string stagingTable)
        => $"DROP TABLE {QuoteIdentifier(stagingTable)};";

    public string BuildUpdateFromStagingSql(
        IReadOnlyList<string> columnNames,
        IReadOnlyList<string> keyColumns,
        string targetTable,
        string stagingTable,
        string? schema)
    {
        var target = QuoteIdentifier(targetTable);
        var staging = QuoteIdentifier(stagingTable);
        var keySet = new HashSet<string>(keyColumns, StringComparer.OrdinalIgnoreCase);
        var assignments = columnNames
            .Where(c => !keySet.Contains(c))
            .Select(c => $"{QuoteIdentifier(c)} = s.{QuoteIdentifier(c)}");
        var match = string.Join(" AND ", keyColumns.Select(k => $"d.{QuoteIdentifier(k)} = s.{QuoteIdentifier(k)}"));

        return $"""
            UPDATE {target} AS d
            SET {string.Join(", ", assignments)}
            FROM {staging} AS s
            WHERE {match};
            DROP TABLE {staging};
            """;
    }

    public string BuildUpsertFromStagingSql(
        IReadOnlyList<string> columnNames,
        IReadOnlyList<string> keyColumns,
        string targetTable,
        string stagingTable,
        string? schema,
        ColumnSchema? identity,
        bool returnGeneratedKeys)
    {
        var target = QuoteIdentifier(targetTable);
        var staging = QuoteIdentifier(stagingTable);
        var keySet = new HashSet<string>(keyColumns, StringComparer.OrdinalIgnoreCase);
        var identityName = identity?.Name;

        var dataColumns = columnNames
            .Where(c => identityName is null || !c.Equals(identityName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var columnsList = string.Join(',', dataColumns.Select(QuoteIdentifier));
        var match = string.Join(" AND ", keyColumns.Select(k => $"d.{QuoteIdentifier(k)} = s.{QuoteIdentifier(k)}"));
        var assignments = dataColumns
            .Where(c => !keySet.Contains(c))
            .Select(c => $"{QuoteIdentifier(c)} = s.{QuoteIdentifier(c)}");

        var sb = new StringBuilder();
        sb.AppendLine($"""
            WITH updated AS (
                UPDATE {target} AS d
                SET {string.Join(", ", assignments)}
                FROM {staging} AS s
                WHERE {match}
                RETURNING {string.Join(',', keyColumns.Select(k => $"d.{QuoteIdentifier(k)}"))}
            )
            DELETE FROM {staging} AS d
            USING updated AS s
            WHERE {match};
            """);

        var insert = $"""
            INSERT INTO {target} ({columnsList})
            SELECT {columnsList} FROM {staging}
            """;

        if (returnGeneratedKeys && identity is { } id)
        {
            var idColumn = QuoteIdentifier(id.Name);
            sb.AppendLine($"""
                WITH inserted AS (
                    {insert}
                    RETURNING {idColumn}
                )
                SELECT {idColumn} FROM inserted;
                """);
        }
        else
        {
            sb.AppendLine($"{insert};");
        }

        sb.AppendLine($"DROP TABLE {staging};");
        return sb.ToString();
    }

    public string BuildDeleteFromStagingSql(
        IReadOnlyList<string> keyColumns,
        string targetTable,
        string stagingTable,
        string? schema)
    {
        var target = QuoteIdentifier(targetTable);
        var staging = QuoteIdentifier(stagingTable);
        var match = string.Join(" AND ", keyColumns.Select(k => $"s.{QuoteIdentifier(k)} = t.{QuoteIdentifier(k)}"));

        return $"""
            DELETE FROM {target} AS s
            USING {staging} AS t
            WHERE {match};
            DROP TABLE {staging};
            """;
    }
}

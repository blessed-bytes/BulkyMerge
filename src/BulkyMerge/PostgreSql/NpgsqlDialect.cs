using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using BulkyMerge.Root;
using Npgsql;

namespace BulkyMerge.PostgreSql.PostgreSql;

public sealed class NpgsqlDialect : ISqlDialect
{
    public string DefaultScheme => "public";

    public string GetCreateTempTableQuery(string tempTableName, string destination, IEnumerable<string> columnNames = null) => $"SELECT * INTO TEMP \"{tempTableName}\" FROM \"{destination}\" WHERE 1 = 0;";

    public string GetInsertOrUpdateMergeStatement(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
    {
        var identityExist = identity is not null;
        columnNames = identityExist ? columnNames.Where(x => x != identity.Name) : columnNames;
        var merge = new StringBuilder();
        var columnsString = string.Join(',', columnNames.Select(x => $"\"{x}\""));
        var primaryKeysMatchString =
            string.Join(" AND ", primaryKeys.Select(x => $"d.\"{x}\" = s.\"{x}\""));
        merge.Append($@"WITH ""updated"" AS(UPDATE ""{tableName}"" AS d 
        SET {string.Join(',', columnNames.Except(primaryKeys).Select(x => $"\"{x}\" = s.\"{x}\""))}
        FROM ""{tempTableName}"" AS s
        WHERE {primaryKeysMatchString}
        RETURNING {string.Join(',', primaryKeys.Select(x => $"d.\"{x}\""))}
            )
DELETE 
FROM ""{tempTableName}"" d
     USING ""updated"" s WHERE {primaryKeysMatchString};");
        var insertClause = @$"INSERT INTO ""{tableName}"" ({columnsString})
        SELECT {columnsString} FROM ""{tempTableName}""";
        if (identityExist)
        {
            merge.Append(@$"WITH ""inserted"" AS ({insertClause}
        RETURNING ""{identity.Name}"")
                SELECT ""{identity.Name}"" FROM ""inserted"";
        DROP TABLE ""{tempTableName}""");
        }
        else
        {
            merge.Append(insertClause);
        }

        return merge.ToString();
    }

    public string GetAlterIdentityColumnQuery(string tempTableName, ColumnInfo identity)
        => $@"ALTER TABLE ""{tempTableName}"" DROP COLUMN ""{identity.Name}"";
ALTER TABLE ""{tempTableName}"" ADD ""{identity.Name}"" {identity.DataType}";

    public string GetInsertQuery(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
    {
        var identityExist = identity is not null;
        var merge = new StringBuilder();
        var columnsString = string.Join(',', (!identityExist ? columnNames : columnNames.Where(x => x != identity.Name)).Select(x => $"\"{x}\""));
        var insertClause = @$"INSERT INTO ""{tableName}"" ({columnsString})
        SELECT {columnsString} FROM ""{tempTableName}""";
        if (identityExist)
        {
            merge.Append(@$"WITH ""inserted"" AS ({insertClause}
        RETURNING ""{identity.Name}"")
                SELECT ""{identity.Name}"" FROM ""inserted"";
        DROP TABLE ""{tempTableName}""");
        }
        else
        {
            merge.Append($"{insertClause};DROP TABLE \"{tempTableName}\"");
        }

        return merge.ToString();
    }

    public string GetUpdateQuery(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
        => @$"UPDATE ""{tableName}"" AS d 
        SET {string.Join(',', columnNames.Except(primaryKeys).Select(x => $"\"{x}\" = s.\"{x}\""))}
FROM ""{tempTableName}"" AS s
WHERE {string.Join(" AND ", primaryKeys.Select(x => $"d.\"{x}\" = s.\"{x}\""))};
DROP TABLE ""{tempTableName}""";

    public string GetDeleteQuery(string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
        => $@"
DELETE 
FROM ""{tableName}"" s
     USING ""{tempTableName}"" t WHERE {string.Join(" AND ", primaryKeys.Select(x => @$"s.""{x}"" = t.""{x}"""))};
DROP TABLE ""{tempTableName}""";

    public string GetTempTableName(string targetTableName) => $"{targetTableName}_{Guid.NewGuid():N}";

    public IDbDataParameter CreateParameter(object value) => new NpgsqlParameter { Value = value };

    public string GetColumnsQuery(string databaseName, string tableName)
    {
        return @$"SELECT 
    c.column_name, 
    c.data_type, 
    CASE 
        WHEN c.column_default LIKE 'nextval%' THEN 1
        WHEN c.is_identity = 'YES' THEN 1
        ELSE 0
    END AS is_identity,
    CASE 
        WHEN pk.column_name IS NOT NULL THEN 1
        ELSE 0
    END AS is_primary_key
FROM information_schema.columns c
LEFT JOIN (
    SELECT kcu.table_name, kcu.column_name
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu
      ON tc.constraint_name = kcu.constraint_name
     AND tc.table_schema = kcu.table_schema
    WHERE tc.constraint_type = 'PRIMARY KEY'
      AND tc.table_schema = 'public'
) pk
  ON c.table_name = pk.table_name
  AND c.column_name = pk.column_name
WHERE c.table_name = '{tableName}';";
    }
}
using BulkyMerge;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BulkyMerge.SqlServer;

public sealed class SqlServerDialect : ISqlDialect
{
    public string DefaultScheme => "dbo";

    public string GetCreateTempTableQuery(string tempTableName, string destination, IEnumerable<string> columnNames = null)=> $"SELECT {(columnNames is null ? "*" : string.Join(',', columnNames))} INTO {tempTableName} FROM {destination} WITH(READUNCOMMITTED) WHERE 1 = 0";

    public string GetInsertOrUpdateMergeStatement(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
    {
        var identityExist = identity is not null;
        columnNames = identityExist ? columnNames.Where(x => x != identity.Name) : columnNames;
        var merge = new StringBuilder();
        var columnsString = string.Join(',', columnNames.Select(x => $"[{x}]"));

        if (identityExist)
        {
            merge.Append($"DECLARE @Id TABLE ([Action] VARCHAR(20), [Id]");
            merge.Append(identity.DataType);
            merge.Append(')');
        }
        merge.Append($@"
MERGE [{tableName}] AS T  
USING [{tempTableName}] AS S
ON ({string.Join(" AND ", primaryKeys.Select(x => $"S.[{x}] = T.[{x}]"))})
WHEN NOT MATCHED
THEN INSERT ({columnsString}) VALUES ({columnsString})
WHEN MATCHED 
THEN UPDATE SET {string.Join(',', columnNames.Except(primaryKeys).Select(x => $"T.[{x}] = S.[{x}]"))}");
        if (identityExist)
        {
            merge.AppendLine();
            merge.Append($"OUTPUT $action, inserted.");
            merge.Append(identity.Name);
            merge.AppendLine(" INTO @Id ([Action], [Id]);");
            merge.AppendLine("SELECT [Id] FROM @Id WHERE [Action] = 'INSERT' ORDER BY [Id] ASC");
        }
        else
        {
            merge.AppendLine(";");
        }
        merge.AppendLine($"DROP TABLE {tempTableName}");
        return merge.ToString();
    }

    public string GetAlterIdentityColumnQuery(string tempTableName, ColumnInfo identity)
        => $@"ALTER TABLE {tempTableName} ADD _IfIdentityJustOneColumn_{identity.Name} BIT
ALTER TABLE {tempTableName} DROP COLUMN {identity.Name}
ALTER TABLE {tempTableName} ADD {identity.Name} {identity.DataType}
ALTER TABLE {tempTableName} DROP COLUMN _IfIdentityJustOneColumn_{identity.Name}";

    public string GetInsertQuery(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
    {
        var identityExist = identity is not null;
        var columnsString = string.Join(',', (!identityExist ? columnNames : columnNames.Where(x => x != identity.Name)).Select(x => $"[{x}]"));
        var insert = new StringBuilder();
        if (identityExist)
        {
            insert.Append($"DECLARE @Id TABLE ([Id] {identity.DataType})");
        }

        insert.Append($@"INSERT INTO [{tableName}]({columnsString})");
        if (identityExist)
        {
            insert.AppendLine($"OUTPUT inserted.[{identity.Name}] INTO @Id");
        }
        insert.AppendLine($"SELECT {columnsString} FROM {tempTableName}");
        if (identityExist)
        {
            insert.AppendLine("SELECT [Id] FROM @Id ORDER BY [Id] ASC");
        }
        insert.AppendLine($"DROP TABLE {tempTableName}");
        return insert.ToString();
    }

    public string GetUpdateQuery(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity) => $@"
MERGE [{tableName}] AS T  
USING (SELECT * FROM [{tempTableName}]) AS S
ON ({string.Join(" AND ", primaryKeys.Select(x => $"S.[{x}] = T.[{x}]"))})
WHEN MATCHED 
THEN UPDATE SET {string.Join(',', columnNames.Except(primaryKeys).Select(x => $"T.[{x}] = S.[{x}]"))};
DROP TABLE {tempTableName}";

    public string GetDeleteQuery(string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity) => $@"
MERGE [{tableName}] AS T  
USING (SELECT * FROM [{tempTableName}]) AS S
ON ({string.Join(" AND ", primaryKeys.Select(x => $"S.[{x}] = T.[{x}]"))})
WHEN MATCHED
THEN DELETE;
DROP TABLE {tempTableName}";

    public string GetTempTableName(string targetTableName) => $"#{targetTableName}";

    public IDbDataParameter CreateParameter(object value) => new SqlParameter { Value = value};

    public string GetColumnsQuery(string databaseName, string tableName)
    {
        return $@"SELECT 
    c.name AS column_name,
    t.name AS data_type,
     CASE WHEN c.is_identity = 1 THEN 1 ELSE 0 END AS is_identity,
    COALESCE(i.is_primary_key, 0) AS is_primary_key
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
LEFT JOIN sys.index_columns ic 
    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
LEFT JOIN sys.indexes i 
    ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND i.is_primary_key = 1
JOIN sys.objects o ON c.object_id = o.object_id
WHERE o.type = 'U'
  AND o.name = '{tableName}'";
    }
}
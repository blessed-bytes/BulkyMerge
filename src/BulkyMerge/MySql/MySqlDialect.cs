using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using BulkyMerge.Root;
using MySqlConnector;

namespace BulkyMerge.MySql;

public sealed class MySqlDialect : ISqlDialect
{
    public string DefaultScheme => null;
    public string GetFindIdentityQuery(string databaseName, string tableName)
        => $@"SELECT COLUMN_NAME, COLUMN_TYPE
    FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{databaseName}' AND TABLE_NAME = '{tableName}'
    AND EXTRA = 'auto_increment'";

    public string GetCreateTempTableQuery(string tempTableName, string destination, IEnumerable<string> columnNames = null) => $"CREATE TEMPORARY TABLE IF NOT EXISTS `{tempTableName}` AS (SELECT * FROM `{destination}` WHERE 1=0);";

    public string GetInsertOrUpdateMergeStatement(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
    {
        var identityExist = identity is not null;
        var merge = new StringBuilder();
        var columnsString = string.Join(',', columnNames.Select(x => $"`{x}`"));
        var primaryKeysMatchString =
            string.Join(" AND ", primaryKeys.Select(x => $"d.`{x}` = s.`{x}`"));
        var insertOrUpdateClause = @$"INSERT INTO `{tableName}` ({columnsString}) SELECT {columnsString} FROM `{tempTableName}`
        ON DUPLICATE KEY UPDATE {string.Join(',', columnNames.Except(primaryKeys).Select(x => $"`{tableName}`.`{x}` = `{tempTableName}`.`{x}`"))};";
        if (identityExist)
        {
            merge.Append($"LOCK TABLES {tempTableName} READ, {tableName} WRITE;");
            merge.Append(insertOrUpdateClause);
            merge.Append($@"SET @row_count = ROW_COUNT();
SET @last_insert_id = LAST_INSERT_ID();
UNLOCK TABLES;
SELECT `{identity.Name}` FROM `{tableName}` WHERE `{identity.Name}` >= @last_insert_id AND `{identity.Name}` <= @last_insert_id + (@row_count - 1);
DROP TABLE `{tempTableName}`");
        }
        else
        {
            merge.Append($"{insertOrUpdateClause}DROP TABLE `{tempTableName}`");
        } 
       
        return merge.ToString();
    }

    public string GetAlterIdentityColumnQuery(string tempTableName, ColumnInfo identity)
        => $@"ALTER TABLE `{tempTableName}` DROP COLUMN `{identity.Name}`;
ALTER TABLE `{tempTableName}` ADD `{identity.Name}` {identity.DataType}";

    public string GetInsertQuery(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
    {
        var identityExist = identity is not null;
        var merge = new StringBuilder();
        var columnsString = string.Join(',', (!identityExist ? columnNames : columnNames.Where(x => x != identity.Name)).Select(x => $"`{x}`"));
        var insertClause = @$"INSERT INTO `{tableName}` ({columnsString})
        SELECT {columnsString} FROM `{tempTableName}`;";
        if (identityExist)
        {
            merge.Append($"LOCK TABLES {tempTableName} READ, {tableName} WRITE;");
            merge.Append(insertClause);
            merge.Append($@"SET @row_count = ROW_COUNT();
SET @last_insert_id = LAST_INSERT_ID();
UNLOCK TABLES;
SELECT `{identity.Name}` FROM `{tableName}` WHERE `{identity.Name}` >= @last_insert_id AND `{identity.Name}` <= @last_insert_id + (@row_count - 1);
DROP TABLE `{tempTableName}`");
        }
        else
        {
            merge.Append($"{insertClause}DROP TABLE `{tempTableName}`");
        }

        return merge.ToString();
    }

    public string GetUpdateQuery(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
    {
        var primaryKeysMatchString =
            string.Join(" AND ", primaryKeys.Select(x => $"`{tableName}`.`{x}` = `{tempTableName}`.`{x}`"));
        return @$"UPDATE `{tableName}` INNER JOIN `{tempTableName}`ON ({primaryKeysMatchString})
        SET {string.Join(',', columnNames.Except(primaryKeys).Select(x => $"`{tableName}`.`{x}` = `{tempTableName}`.`{x}`"))}
;DROP TABLE `{tempTableName}`";;
    }
    
    public string GetDeleteQuery(string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity)
        => $@"DELETE `{tableName}`
FROM `{tableName}`
INNER JOIN `{tempTableName}` ON ({string.Join(" AND ", primaryKeys.Select(x => @$"`{tableName}`.`{x}` = `{tempTableName}`.`{x}`"))});
DROP TABLE `{tempTableName}`";

    public string GetTempTableName(string targetTableName) 
    {
        var tempTableName = $"{targetTableName}_{Guid.NewGuid():N}";
        return tempTableName.Length >= 63 ? tempTableName[..63] : tempTableName;
   }

    public IDbDataParameter CreateParameter(object value) => new MySqlParameter { Value = value };

    public string GetColumnsQuery(string databaseName, string tableName)
    {
        return @$"SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    CASE 
        WHEN c.EXTRA LIKE '%auto_increment%' THEN 1
        ELSE 0
    END AS column_type,
    CASE 
        WHEN k.CONSTRAINT_NAME = 'PRIMARY' THEN 1
        ELSE 0
    END AS is_primary_key
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
    ON c.TABLE_SCHEMA = k.TABLE_SCHEMA 
   AND c.TABLE_NAME = k.TABLE_NAME 
   AND c.COLUMN_NAME = k.COLUMN_NAME 
   AND k.CONSTRAINT_NAME = 'PRIMARY'
WHERE c.TABLE_SCHEMA = '{databaseName}'
  AND c.TABLE_NAME = '{tableName}'
ORDER BY c.ORDINAL_POSITION;";
    }
}
using System.Collections.Generic;
using System.Data;

namespace BulkyMerge.Root;

public interface ISqlDialect
{
    string DefaultScheme { get; }
    string GetCreateTempTableQuery(string tempTableName, string destination, IEnumerable<string> columnNames = null);
    string GetInsertOrUpdateMergeStatement(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity);
    string GetAlterIdentityColumnQuery(string tempTableName, ColumnInfo identity);
    string GetInsertQuery(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity);
    string GetUpdateQuery(IEnumerable<string> columnNames, string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity);
    string GetDeleteQuery(string tableName, string tempTableName, IEnumerable<string> primaryKeys, ColumnInfo identity);
    string GetTempTableName(string targetTableName);
    string GetColumnsQuery(string databaseName, string tableName);
    IDbDataParameter CreateParameter(object value);
}
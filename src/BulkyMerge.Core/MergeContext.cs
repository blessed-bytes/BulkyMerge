using System.Data.Common;
using System.Reflection;

namespace BulkyMerge;

public record MergeContext<T>(
        DbConnection Connection,
        DbTransaction Transaction,
        IEnumerable<T> Items,
        PropertyAccessor<T> TypeAccessor,
        string TableName,
        string Schema,
        string TempTableName,
        Dictionary<string, PropertyInfo> ColumnsToProperty,
        Dictionary<string, ColumnInfo> Columns,
        ColumnInfo Identity,
        List<string> PrimaryKeys,
        int BatchSize,
        int Timeout);

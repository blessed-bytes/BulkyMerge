using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using BulkyMerge.Root;
using FastMember;
using Npgsql;
using NpgsqlTypes;

namespace BulkyMerge.PostgreSql.PostgreSql;

internal sealed class NpgsqlBulkWriter : IBulkWriter
{

    private ConcurrentDictionary<Type, Type> TypesCache = new();

    private object PrepareValue(object value, ColumnInfo column)
    {
        if (value != null)
        {
            if (column.DataType == "timestamp without time zone"
                && (value.GetType() == typeof(DateTime) || value.GetType() == typeof(DateTime?)))
            {
                try
                {
                    return DateTime.SpecifyKind((DateTime)value, DateTimeKind.Unspecified);
                }
                catch
                {
                    return value;
                }
            }
            if (TypesCache.TryGetValue(value.GetType(), out var type))
            {
                if (type.IsEnum)
                {
                    return Convert.ToInt32(value);
                }
            }
            else
            {
                var valueType = value.GetType();
                var underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;

                TypesCache[valueType] = underlyingType;
                if (underlyingType?.IsEnum == true)
                {
                    return Convert.ToInt32(value);
                }
            }
            
        }
        return value;
    }


    public void Write<T>(string destination, MergeContext<T> context)
        => WriteAsync(destination, context).GetAwaiter().GetResult();

    public async Task WriteAsync<T>(string destination, MergeContext<T> context)
    {
        var columnTypes = context.Columns;
        var columnsMapping = new List<KeyValuePair<string, Member>>(context.ColumnsToProperty);
        var columns = columnsMapping.Select(x => x.Key);
        var columnsString = string.Join(",", columns.Select(x => $"\"{x}\""));
        var accessor = TypeAccessor.Create(typeof(T));
        await using var writer = await (context.Connection as NpgsqlConnection)?.BeginBinaryImportAsync($"COPY \"{destination}\" ({columnsString}) FROM STDIN (FORMAT BINARY)");
        writer.Timeout = TimeSpan.FromDays(1);
        var row = new object[columnsMapping.Count];
        foreach (var item in context.Items)
        {
            var vals = new List<(object Value, NpgsqlDbType? Type)>();
            await writer.StartRowAsync();
            foreach (var columnMapping in columnsMapping)
            {
                var commonConverter = TypeConverters.GetConverter(columnMapping.Value.Type);
                var value = accessor[item, columnMapping.Value.Name];
                columnTypes.TryGetValue(columnMapping.Key.ToLower(), out var column);
                if (commonConverter != null)
                {
                    value = commonConverter.Convert(value);
                }
                if (column != null)
                {
                    if (value is null)
                    {
                        await writer.WriteNullAsync();
                    }
                    else
                    {
                        await writer.WriteAsync(PrepareValue(value, column), column.DataType);
                    }
                }
            }
            var log = string.Join(", ", vals.Select(x => $"({x.Value}, {x.Type})"));
            ;

        }

        await writer.CompleteAsync();
    }
}
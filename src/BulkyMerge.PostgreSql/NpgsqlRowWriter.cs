using BulkyMerge.Converters;
using BulkyMerge.Execution;
using BulkyMerge.Metadata;
using BulkyMerge.Providers;
using BulkyMerge.Schema;
using Npgsql;
namespace BulkyMerge.PostgreSql;

internal sealed class NpgsqlRowWriter : IProviderWriter
{
    public async Task WriteStagingAsync<T>(
        BulkWriteContext<T> context,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (context.Connection is not NpgsqlConnection connection)
            throw new BulkyMergeException("Expected an open NpgsqlConnection.");

        var writers = BuildColumnWriters(context);
        var columnList = string.Join(',', context.WriteColumns.Select(c => $"\"{c.Name}\""));

        await using var importer = await connection.BeginBinaryImportAsync(
            $"COPY \"{context.StagingTable}\" ({columnList}) FROM STDIN (FORMAT BINARY)",
            cancellationToken).ConfigureAwait(false);

        importer.Timeout = context.CommandTimeout;

        foreach (var row in context.Rows)
        {
            await importer.StartRowAsync(cancellationToken).ConfigureAwait(false);
            for (var i = 0; i < writers.Length; i++)
                await writers[i](importer, row, cancellationToken).ConfigureAwait(false);
        }

        await importer.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ColumnWriter<T>[] BuildColumnWriters<T>(BulkWriteContext<T> context) where T : class
    {
        var result = new ColumnWriter<T>[context.WriteColumns.Count];

        for (var i = 0; i < context.WriteColumns.Count; i++)
        {
            var column = context.WriteColumns[i];
            context.SchemaByColumn.TryGetValue(column.Name, out var schema);
            var propertyIndex = FindPropertyIndex(context.Metadata, column.PropertyName);
            var converter = ValueConverterRegistry.Get(column.PropertyType);

            result[i] = CreateWriter(column, schema, propertyIndex, converter, context.Metadata);
        }

        return result;
    }

    private static int FindPropertyIndex<T>(IBulkMetadata<T> metadata, string propertyName) where T : class
    {
        for (var i = 0; i < metadata.Columns.Count; i++)
        {
            if (metadata.Columns[i].PropertyName == propertyName)
                return i;
        }

        throw new BulkyMergeException($"Property '{propertyName}' was not found on '{typeof(T).Name}'.");
    }

    private static ColumnWriter<T> CreateWriter<T>(
        ColumnDescriptor column,
        ColumnSchema schema,
        int propertyIndex,
        IValueConverter? converter,
        IBulkMetadata<T> metadata)
        where T : class
    {
        return async (importer, row, ct) =>
        {
            var value = metadata.GetValue(row, propertyIndex);
            if (converter is not null)
                value = converter.Convert(value);

            if (value is null)
            {
                await importer.WriteNullAsync(ct).ConfigureAwait(false);
                return;
            }

            var prepared = PrepareValue(value, schema);
            if (string.IsNullOrEmpty(schema.DataType))
            {
                await importer.WriteAsync(prepared, ct).ConfigureAwait(false);
                return;
            }

            await importer.WriteAsync(prepared, schema.DataType, ct).ConfigureAwait(false);
        };
    }

    private static object PrepareValue(object value, ColumnSchema schema)
    {
        if (schema.DataType == "timestamp without time zone"
            && value is DateTime dateTime)
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);

        var type = value.GetType();
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsEnum)
            return Convert.ChangeType(value, Enum.GetUnderlyingType(underlying));

        return value;
    }

    private delegate Task ColumnWriter<T>(NpgsqlBinaryImporter importer, T row, CancellationToken cancellationToken)
        where T : class;
}

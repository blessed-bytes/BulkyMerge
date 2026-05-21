using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using BulkyMerge.Metadata;

namespace BulkyMerge.Metadata;

internal static class ReflectionBulkMetadata
{
    private static readonly ConcurrentDictionary<Type, object> Cache = new();

    public static IBulkMetadata<T> For<T>() where T : class
        => (IBulkMetadata<T>)Cache.GetOrAdd(typeof(T), static t => Build((Type)t));

    private static object Build(Type type)
    {
        var entityType = type;
        var tableAttr = type.GetCustomAttribute<TableAttribute>(inherit: false);
        var tableName = tableAttr?.Name ?? type.Name;
        var schema = tableAttr?.Schema;

        var properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetCustomAttribute<IgnoreAttribute>() is null
                        && p.GetCustomAttribute<NotMappedAttribute>() is null)
            .ToArray();

        var columns = new List<ColumnDescriptor>();
        var accessors = new List<Func<object, object?>>();
        var setters = new List<Action<object, object?>>();

        foreach (var prop in properties)
        {
            var columnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
            var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
            var isKey = keyAttr is not null
                        || prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() is not null;
            var keyOrder = keyAttr?.Order ?? 0;

            columns.Add(new ColumnDescriptor(columnName, prop.Name, prop.PropertyType, isKey, keyOrder));
            accessors.Add(CompileGetter(prop));
            setters.Add(CompileSetter(prop));
        }

        var keyColumns = columns
            .Where(c => c.IsKey)
            .OrderBy(c => c.KeyOrder)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .Select(c => c.Name)
            .ToArray();

        var metadataType = typeof(ReflectionBulkMetadata<>).MakeGenericType(entityType);
        return Activator.CreateInstance(
            metadataType,
            tableName,
            schema,
            columns,
            keyColumns,
            accessors,
            setters)!;
    }

    private static Func<object, object?> CompileGetter(PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(object), "instance");
        var cast = Expression.Convert(parameter, property.DeclaringType!);
        var access = Expression.Property(cast, property);
        var box = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<object, object?>>(box, parameter).Compile();
    }

    private static Action<object, object?> CompileSetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var castInstance = Expression.Convert(instance, property.DeclaringType!);
        var castValue = Expression.Convert(value, property.PropertyType);
        var assign = Expression.Assign(Expression.Property(castInstance, property), castValue);
        return Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
    }
}

internal sealed class ReflectionBulkMetadata<T> : IBulkMetadata<T> where T : class
{
    private readonly Func<object, object?>[] _getters;
    private readonly Action<object, object?>[] _setters;

    public ReflectionBulkMetadata(
        string tableName,
        string? schema,
        IReadOnlyList<ColumnDescriptor> columns,
        IReadOnlyList<string> keyColumns,
        List<Func<object, object?>> getters,
        List<Action<object, object?>> setters)
    {
        TableName = tableName;
        Schema = schema;
        Columns = columns;
        KeyColumns = keyColumns;
        _getters = getters.ToArray();
        _setters = setters.ToArray();
    }

    public Type EntityType => typeof(T);
    public IReadOnlyList<ColumnDescriptor> Columns { get; }
    public string TableName { get; }
    public string? Schema { get; }
    public IReadOnlyList<string> KeyColumns { get; }

    public object? GetValue(T entity, int columnIndex) => _getters[columnIndex](entity);

    public void SetValue(T entity, int columnIndex, object? value) => _setters[columnIndex](entity, value);
}

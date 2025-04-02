using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BulkyMerge.Root
{
    public interface ITypeConverter
    {
        object Convert(object value);
    }

    public class DynamicTypeConvert : ITypeConverter
    {
        private readonly Func<object, object> _callback;
        public DynamicTypeConvert(Func<object, object> callback)
        {
            _callback = callback;
        }

        public object Convert(object value)
        {
            return _callback(value);
        }
    }

    public static class TypeConverters
    {
        private static readonly ConcurrentDictionary<Type, ITypeConverter> converters = new();
        public static void RegisterTypeConverter(Type type, ITypeConverter converter)
        {
            converters[type] = converter;
        }
        public static void RegisterTypeConverter(Type type, Func<object, object> func)
        {
            converters[type] = new DynamicTypeConvert(func);
        }

        internal static ITypeConverter GetConverter(Type type) => converters.TryGetValue(type, out var converter) ? converter : null;
    }


    public class DataReader<T> : IDataReader
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly int _fieldCount;
        private readonly Func<T, object>[] _compiledGetters;
        private readonly string[] _propertyNames;

        private readonly static ConcurrentDictionary<string, Func<T, object>[]> Cache = new();

        public DataReader(IEnumerable<T> data, PropertyAccessor<T> propertyAccessor, string[] props)
        {
            _enumerator = data.GetEnumerator();
            var accessor = propertyAccessor ?? PropertyAccessor<T>.Create();

            var key = $"{typeof(T).FullName}_{string.Join("", props)}";
            var selectedProps = accessor.Properties.Where(p => props.Contains(p.Name)).ToArray();
            _fieldCount = selectedProps.Length;
            _propertyNames = selectedProps.Select(p => p.Name).ToArray();
            _compiledGetters = new Func<T, object>[_fieldCount];

            if (!Cache.TryGetValue(key, out var getters))
            {
                getters = new Func<T, object>[_fieldCount];
                for (int i = 0; i < _fieldCount; i++)
                {
                    var prop = selectedProps[i];
                    getters[i] = CompileGetterWithConverter(prop);
                }
                Cache[key] = getters;
            }
            _compiledGetters = getters;

        }
        private static Func<T, object> CompileGetterWithConverter(PropertyInfo prop)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var propertyAccess = Expression.Property(instanceParam, prop);
            var valueAsObject = Expression.Convert(propertyAccess, typeof(object));
            var converter = TypeConverters.GetConverter(prop.PropertyType);

            if (converter != null)
            {
                var converterConst = Expression.Constant(converter, typeof(ITypeConverter));
                var convertMethod = typeof(ITypeConverter).GetMethod(nameof(ITypeConverter.Convert));
                var callConvert = Expression.Call(converterConst, convertMethod, valueAsObject);
                return Expression.Lambda<Func<T, object>>(callConvert, instanceParam).Compile();
            }
            else
            {
                return Expression.Lambda<Func<T, object>>(valueAsObject, instanceParam).Compile();
            }
        }

        public bool Read() => _enumerator.MoveNext();
        public int FieldCount => _fieldCount;
        public object GetValue(int i) => _compiledGetters[i](_enumerator.Current);
        public string GetName(int i) => _propertyNames[i];
        public int GetOrdinal(string name) => Array.IndexOf(_propertyNames, name);
        public bool IsDBNull(int i) => GetValue(i) == null;
        public string GetString(int i) => GetValue(i)?.ToString();
        public int GetInt32(int i) => Convert.ToInt32(GetValue(i));
        public bool GetBoolean(int i) => Convert.ToBoolean(GetValue(i));
        public DateTime GetDateTime(int i) => Convert.ToDateTime(GetValue(i));
        public decimal GetDecimal(int i) => Convert.ToDecimal(GetValue(i));
        public long GetInt64(int i) => Convert.ToInt64(GetValue(i));
        public short GetInt16(int i) => Convert.ToInt16(GetValue(i));
        public double GetDouble(int i) => Convert.ToDouble(GetValue(i));
        public float GetFloat(int i) => Convert.ToSingle(GetValue(i));
        public Guid GetGuid(int i) => (Guid)GetValue(i);
        public Type GetFieldType(int i) => GetValue(i)?.GetType() ?? typeof(object);
        public object this[int i] => GetValue(i);
        public object this[string name] => GetValue(GetOrdinal(name));

        public void Close() { _enumerator.Dispose(); }
        public bool IsClosed => false;
        public void Dispose() { _enumerator.Dispose(); }
        public int Depth => 0;
        public int RecordsAffected => -1;
        public bool NextResult() => false;
        public DataTable GetSchemaTable() => null;
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public IDataReader GetData(int i) => throw new NotSupportedException();
        public byte GetByte(int i) => Convert.ToByte(GetValue(i));
        public char GetChar(int i) => Convert.ToChar(GetValue(i));
        public string GetDataTypeName(int i) => throw new NotSupportedException();
        public int GetValues(object[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            int count = Math.Min(values.Length, FieldCount);
            for (int i = 0; i < count; i++)
                values[i] = GetValue(i);
            return count;
        }
    }
}

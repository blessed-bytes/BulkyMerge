using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

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
        private readonly List<string> _propertyNames;
        private readonly PropertyAccessor<T> _propertyAccessor;
        private readonly int _fieldCount;
        private bool _isClosed = false;

        public DataReader(IEnumerable<T> data, PropertyAccessor<T> propertyAccessor, string[] props)
        {
            _propertyAccessor = propertyAccessor ?? PropertyAccessor<T>.Create();
            _enumerator = data.GetEnumerator();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _propertyNames = properties.Where(x => props.Contains(x.Name)).Select(p => p.Name).ToList();
            _fieldCount = _propertyNames.Count;
        }

        private static object ConvertValue(object value)
        {
            if (value == null) return null;
            var type = value.GetType();
            var converter = TypeConverters.GetConverter(type);
            if (converter != null)
            {
                return converter.Convert(value);
            }
            return value;
        }

        public bool Read() => _enumerator.MoveNext();
        public int FieldCount => _fieldCount;
        public object GetValue(int i) => ConvertValue(_propertyAccessor.GetValue(_enumerator.Current, _propertyNames[i]));
        public string GetName(int i) => _propertyNames[i];
        public int GetOrdinal(string name) => _propertyNames.IndexOf(name);
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

        public void Close() => _isClosed = true;
        public bool IsClosed => _isClosed;
        public void Dispose() { _enumerator.Dispose(); _isClosed = true; }
        public int Depth => 0;
        public int RecordsAffected => -1;
        public bool NextResult() => false;
        public DataTable GetSchemaTable() => null;
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public IDataReader GetData(int i) => throw new NotSupportedException();

        public byte GetByte(int i)
        {
            return Convert.ToByte(GetValue(i));
        }

        public char GetChar(int i)
        {
            return Convert.ToChar(GetValue(i));
        }

        public string GetDataTypeName(int i)
        {
            throw new Exception("GetDataTypeName");
        }

        public int GetValues(object[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            int count = Math.Min(values.Length, FieldCount);
            for (int i = 0; i < count; i++)
            {
                values[i] = GetValue(i);
            }
            return count;
        }
    }

}

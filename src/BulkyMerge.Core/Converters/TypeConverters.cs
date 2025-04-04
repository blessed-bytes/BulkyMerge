using System.Collections.Concurrent;

namespace BulkyMerge
{
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

        public static ITypeConverter GetConverter(Type type) => converters.TryGetValue(type, out var converter) ? converter : null;
    }
}

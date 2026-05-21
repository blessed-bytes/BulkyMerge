namespace BulkyMerge.Converters;

public static class ValueConverterRegistry
{
    private static readonly Dictionary<Type, IValueConverter> Converters = new();

    public static void Register<T>(Func<T?, object?> convert) where T : class
        => Converters[typeof(T)] = new DelegateConverter<T>(convert);

    public static void Register(Type type, IValueConverter converter)
        => Converters[type] = converter;

    public static IValueConverter? Get(Type type)
        => Converters.TryGetValue(type, out var c) ? c : null;

    private sealed class DelegateConverter<T>(Func<T?, object?> convert) : IValueConverter
    {
        public object? Convert(object? value) => convert((T?)value);
    }
}

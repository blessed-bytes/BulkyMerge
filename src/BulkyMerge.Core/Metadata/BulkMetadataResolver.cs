using System.Reflection;

namespace BulkyMerge.Metadata;

public static class BulkMetadataResolver
{
    public static IBulkMetadata<T> Resolve<T>() where T : class
    {
        var property = typeof(T).GetProperty(
            "BulkMetadata",
            BindingFlags.Public | BindingFlags.Static);

        if (property is not null
            && typeof(IBulkMetadata<T>).IsAssignableFrom(property.PropertyType)
            && property.GetValue(null) is IBulkMetadata<T> metadata)
            return metadata;

        return ReflectionBulkMetadata.For<T>();
    }
}

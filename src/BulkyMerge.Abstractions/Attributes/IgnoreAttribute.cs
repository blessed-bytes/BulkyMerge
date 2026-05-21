namespace BulkyMerge;

/// <summary>Excludes a property from bulk operations.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class IgnoreAttribute : Attribute;

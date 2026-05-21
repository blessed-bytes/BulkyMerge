namespace BulkyMerge;

/// <summary>
/// Enables compile-time metadata generation for this type (source generator).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BulkyMergeMappableAttribute : Attribute;

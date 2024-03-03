namespace Tmds.DBus.Protocol;

// This type is for writing so we don't need to add
// DynamicallyAccessedMemberTypes.PublicParameterlessConstructor.
#pragma warning disable IL2091

public static class VariantExtensions
{
    public static Variant AsVariant<TKey, TValue>(this Dictionary<TKey, TValue> value)
        where TKey : notnull
        where TValue : notnull
        => new Dict<TKey, TValue>(value).AsVariant();

    public static Variant AsVariant<T>(this T[] value)
        where T : notnull
        => new Array<T>(value).AsVariant();

    public static Variant AsVariant(this byte value)
        => new Variant(value);

    public static Variant AsVariant(this bool value)
        => new Variant(value);

    public static Variant AsVariant(this short value)
        => new Variant(value);

    public static Variant AsVariant(this ushort value)
        => new Variant(value);

    public static Variant AsVariant(this int value)
        => new Variant(value);

    public static Variant AsVariant(this uint value)
        => new Variant(value);

    public static Variant AsVariant(this long value)
        => new Variant(value);

    public static Variant AsVariant(this ulong value)
        => new Variant(value);

    public static Variant AsVariant(this double value)
        => new Variant(value);

    public static Variant AsVariant(this string value)
        => new Variant(value);

    public static Variant AsVariant(this SafeHandle value)
        => new Variant(value);
}
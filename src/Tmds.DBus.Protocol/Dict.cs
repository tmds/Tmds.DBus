using System.Collections;

namespace Tmds.DBus.Protocol;

/// <summary>
/// Strongly-typed <see cref="IDictionary{TKey, TValue}"/> that can be converted to a <see cref="VariantValueType.Dictionary"/> <see cref="VariantValue"/>.
/// </summary>
/// <remarks>
/// Supported types for keys and values: <see cref="byte"/>, <see cref="bool"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, <see cref="string"/>, <see cref="ObjectPath"/>, <see cref="Signature"/>, <see cref="SafeHandle"/>, <see cref="VariantValue"/>, <see cref="Array{T}"/>, <see cref="Dict{TKey, TValue}"/>, and <c>Struct</c> types.
/// </remarks>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public sealed class Dict<TKey, TValue> : IDBusWritable, IDictionary<TKey, TValue>, IVariantValueConvertable
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _dict;

    /// <summary>
    /// Initializes a new instance of the Dict class.
    /// </summary>
    public Dict() :
        this(new Dictionary<TKey, TValue>())
    { }

    /// <summary>
    /// Initializes a new instance of the Dict class from an existing dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy elements from.</param>
    public Dict(IDictionary<TKey, TValue> dictionary) :
        this(new Dictionary<TKey, TValue>(dictionary))
    { }

    private Dict(Dictionary<TKey, TValue> value)
    {
        TypeModel.EnsureSupportedVariantType<TKey>();
        TypeModel.EnsureSupportedVariantType<TValue>();
        _dict = value;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteDictionary<TKey, TValue>(_dict);

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => _dict.Keys;

    ICollection<TValue> IDictionary<TKey, TValue>.Values => _dict.Values;

    /// <inheritdoc/>
    public int Count => _dict.Count;

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    /// <inheritdoc/>
    public void Add(TKey key, TValue value)
        => _dict.Add(key, value);

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
        => _dict.ContainsKey(key);

    /// <inheritdoc/>
    public bool Remove(TKey key)
        => _dict.Remove(key);

    /// <inheritdoc/>
    public bool TryGetValue(TKey key,
#if NET
                            [MaybeNullWhen(false)]
#endif
                            out TValue value)
        => _dict.TryGetValue(key, out value);

    /// <inheritdoc/>
    public void Clear()
        => _dict.Clear();

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Add(item);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Contains(item);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Remove(item);

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _dict.GetEnumerator();

    /// <summary>
    /// Implicitly converts a Dict to a VariantValue.
    /// </summary>
    /// <param name="value">The dictionary to convert.</param>
    public static implicit operator VariantValue(Dict<TKey, TValue> value)
        => value.AsVariantValue();

    /// <inheritdoc/>
    public VariantValue AsVariantValue()
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        ReadOnlySpan<byte> sig = TypeModel.GetSignature<TKey>(buffer);
        DBusType keyType = (DBusType)sig[0];
        KeyValuePair<VariantValue, VariantValue>[] pairs = _dict.Select(pair => new KeyValuePair<VariantValue, VariantValue>(VariantValueConverter.ToVariantValue(pair.Key), VariantValueConverter.ToVariantValue(pair.Value))).ToArray();
        return VariantValue.Dictionary(keyType, TypeModel.GetSignature<TValue>(buffer), pairs);
    }
}

using System.Collections;

namespace Tmds.DBus.Protocol;

/// <summary>
/// Strongly-typed <see cref="IList{T}"/> that can be converted to a <see cref="VariantValueType.Array"/> <see cref="VariantValue"/>.
/// </summary>
/// <remarks>
/// <para>
/// The array becomes read-only after conversion.
/// </para>
/// <para>
/// Supported element types: <see cref="byte"/>, <see cref="bool"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, <see cref="string"/>, <see cref="ObjectPath"/>, <see cref="Signature"/>, <see cref="SafeHandle"/>, <see cref="VariantValue"/>, <see cref="Array{T}"/>, <see cref="Dict{TKey, TValue}"/>, and <c>Struct</c> types.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of elements in the array.</typeparam>
public sealed class Array<T> : IDBusWritable, IList<T>, IVariantValueConvertable
    where T : notnull
{
    private readonly List<T> _values;
    private bool _isReadOnly;

    /// <summary>
    /// Initializes a new instance of the Array class.
    /// </summary>
    public Array() :
        this(new List<T>())
    { }

    /// <summary>
    /// Initializes a new instance of the Array class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    public Array(int capacity) :
        this(new List<T>(capacity))
    { }

    /// <summary>
    /// Initializes a new instance of the Array class from a collection.
    /// </summary>
    /// <param name="collection">The collection to copy elements from.</param>
    public Array(IEnumerable<T> collection) :
        this(new List<T>(collection))
    { }

    private Array(List<T> values)
    {
        TypeModel.EnsureSupportedVariantType<T>();
        _values = values;
    }

    /// <inheritdoc/>
    public void Add(T item)
    {
        ThrowIfReadOnly();
        _values.Add(item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        ThrowIfReadOnly();
        _values.Clear();
    }

    /// <inheritdoc/>
    public int Count => _values.Count;

    bool ICollection<T>.IsReadOnly
        => _isReadOnly;

    /// <inheritdoc/>
    public T this[int index]
    {
        get => _values[index];
        set
        {
            ThrowIfReadOnly();
            _values[index] = value;
        }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _values.GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(T item)
        => _values.IndexOf(item);

    /// <inheritdoc/>
    public void Insert(int index, T item)
    {
        ThrowIfReadOnly();
        _values.Insert(index, item);
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        ThrowIfReadOnly();
        _values.RemoveAt(index);
    }

    /// <inheritdoc/>
    public bool Contains(T item)
        => _values.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
        => _values.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        ThrowIfReadOnly();
        return _values.Remove(item);
    }

    /// <summary>
    /// Implicitly converts an Array to a VariantValue.
    /// </summary>
    /// <param name="value">The array to convert.</param>
    public static implicit operator VariantValue(Array<T> value)
        => value.AsVariantValue();

    private void ThrowIfReadOnly()
    {
        if (_isReadOnly)
        {
            ThrowReadOnlyException();
        }
    }

    private void ThrowReadOnlyException()
    {
        throw new InvalidOperationException($"Can not modify {nameof(Array)} after calling {nameof(AsVariantValue)}.");
    }
    /// <inheritdoc/>
    public VariantValue AsVariantValue()
    {
        _isReadOnly = true;
        if (typeof(T) == typeof(byte))
        {
            return VariantValue.Array((List<byte>)(object)_values);
        }
        else if (typeof(T) == typeof(bool))
        {
            return VariantValue.Array((List<bool>)(object)_values);
        }
        else if (typeof(T) == typeof(short))
        {
            return VariantValue.Array((List<short>)(object)_values);
        }
        else if (typeof(T) == typeof(ushort))
        {
            return VariantValue.Array((List<ushort>)(object)_values);
        }
        else if (typeof(T) == typeof(int))
        {
            return VariantValue.Array((List<int>)(object)_values);
        }
        else if (typeof(T) == typeof(uint))
        {
            return VariantValue.Array((List<uint>)(object)_values);
        }
        else if (typeof(T) == typeof(long))
        {
            return VariantValue.Array((List<long>)(object)_values);
        }
        else if (typeof(T) == typeof(ulong))
        {
            return VariantValue.Array((List<ulong>)(object)_values);
        }
        else if (typeof(T) == typeof(double))
        {
            return VariantValue.Array((List<double>)(object)_values);
        }
        else if (typeof(T) == typeof(string))
        {
            return VariantValue.Array((List<string>)(object)_values);
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            return VariantValue.Array((List<ObjectPath>)(object)_values);
        }
        else if (typeof(T) == typeof(Signature))
        {
            return VariantValue.Array((List<Signature>)(object)_values);
        }
        else if (typeof(T) == typeof(SafeHandle))
        {
            return VariantValue.Array((List<SafeHandle>)(object)_values);
        }
        else if (typeof(T) == typeof(VariantValue))
        {
            return VariantValue.ArrayOfVariant((List<VariantValue>)(object)_values);
        }
        else if (typeof(T).IsAssignableTo(typeof(IVariantValueConvertable)))
        {
            Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
            return VariantValue.Array(TypeModel.GetSignature<T>(buffer), _values.Select(v => (v as IVariantValueConvertable)!.AsVariantValue()).ToArray());
        }
        else
        {
            throw new NotSupportedException($"Cannot convert type {typeof(T).FullName}");
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
    {
#if NET5_0_OR_GREATER
        Span<T> span = CollectionsMarshal.AsSpan(_values);
        writer.WriteArray<T>(span);
#else
        writer.WriteArray(_values);
#endif
    }
}

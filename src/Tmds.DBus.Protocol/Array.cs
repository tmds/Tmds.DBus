using System.Collections;

namespace Tmds.DBus.Protocol;

// Using obsolete generic write members
#pragma warning disable CS0618

public sealed class Array<T> : IDBusWritable, IList<T>, IVariantValueConvertable
    where T : notnull
{
    private readonly List<T> _values;

    public Array() :
        this(new List<T>())
    { }

    public Array(int capacity) :
        this(new List<T>(capacity))
    { }

    public Array(IEnumerable<T> collection) :
        this(new List<T>(collection))
    { }

    private Array(List<T> values)
    {
        TypeModel.EnsureSupportedVariantType<T>();
        _values = values;
    }

    public void Add(T item)
        => _values.Add(item);

    public void Clear()
        => _values.Clear();

    public int Count => _values.Count;

    bool ICollection<T>.IsReadOnly
        => false;

    public T this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _values.GetEnumerator();

    public int IndexOf(T item)
        => _values.IndexOf(item);

    public void Insert(int index, T item)
        => _values.Insert(index, item);

    public void RemoveAt(int index)
        => _values.RemoveAt(index);

    public bool Contains(T item)
        => _values.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => _values.CopyTo(array, arrayIndex);

    public bool Remove(T item)
        => _values.Remove(item);

    public Variant AsVariant()
        => Variant.FromArray(this);

    public static implicit operator Variant(Array<T> value)
        => value.AsVariant();

    public static implicit operator VariantValue(Array<T> value)
        => value.AsVariantValue();

    public VariantValue AsVariantValue()
    {
        if (typeof(T) == typeof(byte))
        {
            return VariantValue.Array((byte[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(bool))
        {
            return VariantValue.Array((bool[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(short))
        {
            return VariantValue.Array((short[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(ushort))
        {
            return VariantValue.Array((ushort[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(int))
        {
            return VariantValue.Array((int[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(uint))
        {
            return VariantValue.Array((uint[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(long))
        {
            return VariantValue.Array((long[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(ulong))
        {
            return VariantValue.Array((ulong[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(double))
        {
            return VariantValue.Array((double[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(string))
        {
            return VariantValue.Array((string[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            return VariantValue.Array((ObjectPath[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(Signature))
        {
            return VariantValue.Array((Signature[])(object)_values.ToArray());
        }
        else if (typeof(T) == typeof(SafeHandle))
        {
            return VariantValue.Array((SafeHandle[])(object)_values.ToArray());
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

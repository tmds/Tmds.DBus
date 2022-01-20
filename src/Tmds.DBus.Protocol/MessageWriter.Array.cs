using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteArray<T>(IEnumerable<T> value)
        where T : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            WriteStructureStart();
            Write<T>(item);
        }
        WriteArrayEnd(ref arrayStart);
    }

    public void WriteArray<T>(T[] value)
        where T : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            WriteStructureStart();
            Write<T>(item);
        }
        WriteArrayEnd(ref arrayStart);
    }

    public void WriteVariantDictionary<T>(IEnumerable<T> value)
        where T : notnull
    {
        WriteArraySignature<T>(ref this);
        WriteArray(value);
    }

    public void WriteVariantDictionary<T>(T[] value)
        where T : notnull
    {
        WriteArraySignature<T>(ref this);
        WriteArray(value);
    }

    sealed class ArrayTypeWriter<T> : ITypeWriter<IEnumerable<T>>
        where T : notnull
    {
        public void Write(ref MessageWriter writer, IEnumerable<T> value)
        {
            writer.WriteArray(value);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteArraySignature<T>(ref writer);
            writer.WriteArray((IEnumerable<T>)value);
        }
    }

    public static void AddArrayTypeWriter<T>()
        where T : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(IEnumerable<T>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ArrayTypeWriter<T>());
            }
        }
    }

    private ITypeWriter CreateArrayTypeWriter(Type elementType)
    {
        Type writerType = typeof(ArrayTypeWriter<>).MakeGenericType(new[] { elementType });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteArraySignature<T>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<T[]>());
    }
}

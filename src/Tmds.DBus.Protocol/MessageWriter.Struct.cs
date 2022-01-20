using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteStruct<T1>(T1 item1)
        where T1 : notnull
    {
        WriteStructureStart();
        Write<T1>(item1);
    }

    public void WriteVariantStruct<T1>(T1 item1)
        where T1 : notnull
    {
        WriteStructSignature<T1>(ref this);
        WriteStructureStart();
        Write<T1>(item1);
    }

    sealed class ValueTupleTypeWriter<T1> : ITypeWriter<ValueTuple<T1>>
        where T1 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1> value)
        {
            writer.WriteStruct<T1>(value.Item1);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1>(ref writer);
            Write(ref writer, (ValueTuple<T1>)value);
        }
    }

    sealed class TupleTypeWriter<T1> : ITypeWriter<Tuple<T1>>
        where T1 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1> value)
        {
            writer.WriteStruct<T1>(value.Item1);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1>(ref writer);
            Write(ref writer, (Tuple<T1>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1>()
        where T1 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1>()
        where T1 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1)
    {
        Type writerType = typeof(ValueTupleTypeWriter<>).MakeGenericType(new[] { type1 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1)
    {
        Type writerType = typeof(TupleTypeWriter<>).MakeGenericType(new[] { type1 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1>>());
    }

    public void WriteStruct<T1, T2>(T1 item1, T2 item2)
        where T1 : notnull
        where T2 : notnull
    {
        WriteStructureStart();
        Write<T1>(item1);
        Write<T2>(item2);
    }

    public void WriteVariantStruct<T1, T2>(T1 item1, T2 item2)
        where T1 : notnull
        where T2 : notnull
    {
        WriteStructSignature<T1, T2>(ref this);
        WriteStructureStart();
        Write<T1>(item1);
        Write<T2>(item2);
    }

    sealed class ValueTupleTypeWriter<T1, T2> : ITypeWriter<ValueTuple<T1, T2>>
        where T1 : notnull
        where T2 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1, T2> value)
        {
            writer.WriteStruct<T1, T2>(value.Item1, value.Item2);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2>(ref writer);
            Write(ref writer, (ValueTuple<T1, T2>)value);
        }
    }

    sealed class TupleTypeWriter<T1, T2> : ITypeWriter<Tuple<T1, T2>>
        where T1 : notnull
        where T2 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1, T2> value)
        {
            writer.WriteStruct<T1, T2>(value.Item1, value.Item2);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2>(ref writer);
            Write(ref writer, (Tuple<T1, T2>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1, T2>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1, T2>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1, T2>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1, T2>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2)
    {
        Type writerType = typeof(ValueTupleTypeWriter<,>).MakeGenericType(new[] { type1, type2 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1, Type type2)
    {
        Type writerType = typeof(TupleTypeWriter<,>).MakeGenericType(new[] { type1, type2 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1, T2>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1, T2>>());
    }

    public void WriteStruct<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        WriteStructureStart();
        Write<T1>(item1);
        Write<T2>(item2);
        Write<T3>(item3);
    }

    public void WriteVariantStruct<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        WriteStructSignature<T1, T2, T3>(ref this);
        WriteStruct(item1, item2, item3);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3> : ITypeWriter<ValueTuple<T1, T2, T3>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3> value)
        {
            writer.WriteStruct<T1, T2, T3>(value.Item1, value.Item2, value.Item3);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3>(ref writer);
            Write(ref writer, (ValueTuple<T1, T2, T3>)value);
        }
    }

    sealed class TupleTypeWriter<T1, T2, T3> : ITypeWriter<Tuple<T1, T2, T3>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1, T2, T3> value)
        {
            writer.WriteStruct<T1, T2, T3>(value.Item1, value.Item2, value.Item3);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3>(ref writer);
            Write(ref writer, (Tuple<T1, T2, T3>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1, T2, T3>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1, T2, T3>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1, T2, T3>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3)
    {
        Type writerType = typeof(ValueTupleTypeWriter<,,>).MakeGenericType(new[] { type1, type2, type3 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3)
    {
        Type writerType = typeof(TupleTypeWriter<,,>).MakeGenericType(new[] { type1, type2, type3 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1, T2, T3>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1, T2, T3>>());
    }

    public void WriteStruct<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        WriteStructureStart();
        Write<T1>(item1);
        Write<T2>(item2);
        Write<T3>(item3);
        Write<T4>(item4);
    }

    public void WriteVariantStruct<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        WriteStructSignature<T1, T2, T3, T4>(ref this);
        WriteStruct(item1, item2, item3, item4);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3, T4> : ITypeWriter<ValueTuple<T1, T2, T3, T4>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3, T4> value)
        {
            writer.WriteStruct<T1, T2, T3, T4>(value.Item1, value.Item2, value.Item3, value.Item4);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4>(ref writer);
            Write(ref writer, (ValueTuple<T1, T2, T3, T4>)value);
        }
    }

    sealed class TupleTypeWriter<T1, T2, T3, T4> : ITypeWriter<Tuple<T1, T2, T3, T4>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4> value)
        {
            writer.WriteStruct<T1, T2, T3, T4>(value.Item1, value.Item2, value.Item3, value.Item4);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4>(ref writer);
            Write(ref writer, (Tuple<T1, T2, T3, T4>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1, T2, T3, T4>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1, T2, T3, T4>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4)
    {
        Type writerType = typeof(ValueTupleTypeWriter<,,,>).MakeGenericType(new[] { type1, type2, type3, type4 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4)
    {
        Type writerType = typeof(TupleTypeWriter<,,,>).MakeGenericType(new[] { type1, type2, type3, type4 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1, T2, T3, T4>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1, T2, T3, T4>>());
    }

    public void WriteStruct<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        WriteStructureStart();
        Write<T1>(item1);
        Write<T2>(item2);
        Write<T3>(item3);
        Write<T4>(item4);
        Write<T5>(item5);
    }

    public void WriteVariantStruct<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        WriteStructSignature<T1, T2, T3, T4, T5>(ref this);
        WriteStruct(item1, item2, item3, item4, item5);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3, T4, T5> value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5>(ref writer);
            Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5>)value);
        }
    }

    sealed class TupleTypeWriter<T1, T2, T3, T4, T5> : ITypeWriter<Tuple<T1, T2, T3, T4, T5>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5> value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5>(ref writer);
            Write(ref writer, (Tuple<T1, T2, T3, T4, T5>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1, T2, T3, T4, T5>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1, T2, T3, T4, T5>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5)
    {
        Type writerType = typeof(ValueTupleTypeWriter<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5)
    {
        Type writerType = typeof(TupleTypeWriter<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1, T2, T3, T4, T5>>());
    }
}

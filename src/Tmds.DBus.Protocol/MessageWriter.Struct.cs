using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteStruct<T1>(ValueTuple<T1> value)
        where T1 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
    }

    sealed class ValueTupleTypeWriter<T1> : ITypeWriter<ValueTuple<T1>>
        where T1 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1> value)
        {
            writer.WriteStruct<T1>(new ValueTuple<T1>(value.Item1));
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
            writer.WriteStruct<T1>(new ValueTuple<T1>(value.Item1));
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

    public void WriteStruct<T1, T2>((T1, T2) value)
        where T1 : notnull
        where T2 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
    }

    sealed class ValueTupleTypeWriter<T1, T2> : ITypeWriter<ValueTuple<T1, T2>>
        where T1 : notnull
        where T2 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1, T2> value)
        {
            writer.WriteStruct<T1, T2>(value);
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
            writer.WriteStruct<T1, T2>((value.Item1, value.Item2));
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

    public void WriteStruct<T1, T2, T3>((T1, T2, T3) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3> : ITypeWriter<ValueTuple<T1, T2, T3>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3> value)
        {
            writer.WriteStruct<T1, T2, T3>(value);
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
            writer.WriteStruct<T1, T2, T3>((value.Item1, value.Item2, value.Item3));
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

    public void WriteStruct<T1, T2, T3, T4>((T1, T2, T3, T4) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3, T4> : ITypeWriter<ValueTuple<T1, T2, T3, T4>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3, T4> value)
        {
            writer.WriteStruct<T1, T2, T3, T4>(value);
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
            writer.WriteStruct<T1, T2, T3, T4>((value.Item1, value.Item2, value.Item3, value.Item4));
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

    public void WriteStruct<T1, T2, T3, T4, T5>((T1, T2, T3, T4, T5) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
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
            writer.WriteStruct<T1, T2, T3, T4, T5>(value);
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
            writer.WriteStruct<T1, T2, T3, T4, T5>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5));
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

    public void WriteStruct<T1, T2, T3, T4, T5, T6>((T1, T2, T3, T4, T5, T6) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3, T4, T5, T6> value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6>(value);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6>(ref writer);
            Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6>)value);
        }
    }

    sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6> value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6));
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6>(ref writer);
            Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1, T2, T3, T4, T5, T6>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6)
    {
        Type writerType = typeof(ValueTupleTypeWriter<,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6)
    {
        Type writerType = typeof(TupleTypeWriter<,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1, T2, T3, T4, T5, T6>>());
    }

    public void WriteStruct<T1, T2, T3, T4, T5, T6, T7>((T1, T2, T3, T4, T5, T6, T7) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3, T4, T5, T6, T7> value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7>(value);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6, T7>(ref writer);
            Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6, T7>)value);
        }
    }

    sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6, T7>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7> value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7));
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6, T7>(ref writer);
            Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6, T7>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6, T7>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7)
    {
        Type writerType = typeof(ValueTupleTypeWriter<,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7)
    {
        Type writerType = typeof(TupleTypeWriter<,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6, T7>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>());
    }

    public void WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8>((T1, T2, T3, T4, T5, T6, T7, T8) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
        Write<T8>(value.Rest.Item1);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        public void Write(ref MessageWriter writer, (T1, T2, T3, T4, T5, T6, T7, T8) value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8>(value);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8>(ref writer);
            Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>)value);
        }
    }

    sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, value.Rest.Item1));
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8>(ref writer);
            Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8)
    {
        Type writerType = typeof(ValueTupleTypeWriter<,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8)
    {
        Type writerType = typeof(TupleTypeWriter<,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>>());
    }

    public void WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>((T1, T2, T3, T4, T5, T6, T7, T8, T9) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
        Write<T8>(value.Rest.Item1);
        Write<T9>(value.Rest.Item2);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
    {
        public void Write(ref MessageWriter writer, (T1, T2, T3, T4, T5, T6, T7, T8, T9) value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>(value);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref writer);
            Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>)value);
        }
    }

    sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, value.Rest.Item1, value.Rest.Item2));
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref writer);
            Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9)
    {
        Type writerType = typeof(ValueTupleTypeWriter<,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9)
    {
        Type writerType = typeof(TupleTypeWriter<,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>>());
    }

    public void WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
        where T10 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
        Write<T8>(value.Rest.Item1);
        Write<T9>(value.Rest.Item2);
        Write<T10>(value.Rest.Item3);
    }

    sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
        where T10 : notnull
    {
        public void Write(ref MessageWriter writer, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(value);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref writer);
            Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>)value);
        }
    }

    sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
        where T10 : notnull
    {
        public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> value)
        {
            writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, value.Rest.Item1, value.Rest.Item2, value.Rest.Item3));
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref writer);
            Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>)value);
        }
    }

    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
        where T10 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>());
            }
        }
    }

    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
        where T10 : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>());
            }
        }
    }

    private ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9, Type type10)
    {
        Type writerType = typeof(ValueTupleTypeWriter<,,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type10 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9, Type type10)
    {
        Type writerType = typeof(TupleTypeWriter<,,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9, type10 });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>>());
    }
}
